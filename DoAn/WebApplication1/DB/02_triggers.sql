/* =====================================================================
   ĐỒ ÁN: HỆ THỐNG BÁN BỘT CÀ PHÊ TRỰC TUYẾN
   File 02 - TRIGGER xử lý nghiệp vụ ngay trong database
   ĐIỀU KIỆN: đã chạy 01_create_tables_constraints.sql

   Trigger là đoạn T-SQL tự chạy khi có INSERT/UPDATE/DELETE trên bảng.
   Trong trigger có 2 bảng ảo:
     inserted : các dòng SAU khi thay đổi (INSERT, UPDATE)
     deleted  : các dòng TRƯỚC khi thay đổi (UPDATE, DELETE)
   Lưu ý: 1 câu lệnh có thể tác động NHIỀU dòng -> mọi trigger dưới đây
   đều xử lý theo tập hợp (JOIN với inserted/deleted), không giả định 1 dòng.

   Khi trigger gặp vi phạm nghiệp vụ nó dùng THROW với mã lỗi 50xxx.
   Trong trigger, lỗi sẽ huỷ (ROLLBACK) toàn bộ câu lệnh gây ra nó.
   WS (C#) bắt SqlException.Number trong khoảng 50000-50099 để trả về
   400/409 dạng ProblemDetails cho client:
     50001        Sửa trực tiếp tồn kho / giá vốn sản phẩm
     50010-50013  Phiếu nhập
     50020-50021  Đơn hàng
     50030-50033  Chi tiết đơn hàng
     50040        Giỏ hàng

   !!! LƯU Ý KHI DÙNG EF CORE 7+ !!!
   EF Core mặc định sinh câu INSERT/UPDATE có mệnh đề OUTPUT, mà SQL Server
   không cho dùng OUTPUT (không INTO) trên bảng có trigger. Phải khai báo:
       modelBuilder.Entity<Product>()
           .ToTable(tb => tb.HasTrigger("trg_Products_GuardStock"));
   cho MỌI bảng có trigger: Products, ImportReceipts, ImportReceiptDetails,
   Orders, OrderDetails, CartItems.
   ===================================================================== */

USE CoffeeShopDb;
GO

/* =====================================================================
   NHÓM 1 - SẢN PHẨM
   ===================================================================== */

/* ---------------------------------------------------------------------
   T1. Ghi sổ kho "số lượng ban đầu" khi thêm sản phẩm có tồn > 0
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Products_InitialStock
ON dbo.Products
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.InventoryTransactions (ProductId, TransactionType, QuantityChange, Note)
    SELECT i.ProductId, 'INITIAL', i.StockQuantity, N'Số lượng ban đầu khi tạo sản phẩm'
    FROM inserted AS i
    WHERE i.StockQuantity > 0;
END
GO

/* ---------------------------------------------------------------------
   T2. Chặn sửa TRỰC TIẾP tồn kho và giá vốn
   Tồn kho chỉ được thay đổi qua: hoàn thành phiếu nhập, đặt đơn, huỷ đơn.
   Giá vốn chỉ được thay đổi qua: hoàn thành phiếu nhập (bình quân).
   Nếu admin sửa tay, sổ kho sẽ lệch -> báo cáo tồn tại thời điểm sai.

   TRIGGER_NESTLEVEL() = 1  : câu UPDATE do ứng dụng gửi trực tiếp -> chặn
   TRIGGER_NESTLEVEL() > 1  : UPDATE do 1 trigger khác thực hiện   -> cho qua
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Products_GuardStock
ON dbo.Products
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted  AS d ON d.ProductId = i.ProductId
        WHERE i.StockQuantity <> d.StockQuantity
           OR i.CostPrice     <> d.CostPrice
    )
    BEGIN
        THROW 50001, N'Không được sửa trực tiếp tồn kho hoặc giá nhập. Hãy dùng phiếu nhập hàng.', 1;
    END
END
GO

/* ---------------------------------------------------------------------
   T3. Xoá sản phẩm theo quy tắc của đề
   - Chưa từng nhập hàng và chưa từng bán -> XOÁ HẲN
   - Ngược lại                             -> ĐÁNH DẤU ẨN (IsVisible = 0)
   INSTEAD OF DELETE: câu DELETE của ứng dụng KHÔNG chạy, trigger chạy thay.
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Products_InsteadOfDelete
ON dbo.Products
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    -- Sản phẩm được phép xoá hẳn
    DECLARE @HardDelete TABLE (ProductId INT PRIMARY KEY);

    INSERT INTO @HardDelete (ProductId)
    SELECT d.ProductId
    FROM deleted AS d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ImportReceiptDetails AS x WHERE x.ProductId = d.ProductId)
      AND NOT EXISTS (SELECT 1 FROM dbo.OrderDetails         AS o WHERE o.ProductId = d.ProductId);

    -- 1) Xoá hẳn: xoá dòng sổ kho INITIAL trước (vì FK), CartItems tự xoá theo CASCADE
    DELETE it
    FROM dbo.InventoryTransactions AS it
    JOIN @HardDelete AS h ON h.ProductId = it.ProductId;

    DELETE p
    FROM dbo.Products AS p
    JOIN @HardDelete AS h ON h.ProductId = p.ProductId;

    -- 2) Còn lại: chỉ ẩn, và gỡ khỏi giỏ hàng của khách để không đặt mua được nữa
    UPDATE p
    SET    p.IsVisible = 0
    FROM   dbo.Products AS p
    JOIN   deleted      AS d ON d.ProductId = p.ProductId
    WHERE  d.ProductId NOT IN (SELECT ProductId FROM @HardDelete);

    DELETE c
    FROM dbo.CartItems AS c
    JOIN deleted       AS d ON d.ProductId = c.ProductId
    WHERE d.ProductId NOT IN (SELECT ProductId FROM @HardDelete);
END
GO

/* =====================================================================
   NHÓM 2 - PHIẾU NHẬP HÀNG
   ===================================================================== */

/* ---------------------------------------------------------------------
   T4. Khi phiếu nhập chuyển Draft -> Completed:
       - Cập nhật GIÁ NHẬP BÌNH QUÂN:
           Giá mới = (Tồn * Giá cũ + SL nhập * Giá nhập) / (Tồn + SL nhập)
       - Cộng tồn kho
       - Ghi sổ kho IMPORT
     Đồng thời: phiếu đã Completed thì không được sửa (kể cả quay về Draft).

   Ứng dụng hoàn thành phiếu bằng 1 câu:
       UPDATE ImportReceipts SET Status = 'Completed', CompletedAt = SYSDATETIME()
       WHERE ReceiptId = @id AND Status = 'Draft';
   (phải set cả CompletedAt vì CK_ImportReceipts_Completed kiểm tra TRƯỚC trigger)
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_ImportReceipts_AfterUpdate
ON dbo.ImportReceipts
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    -- (a) Phiếu đã hoàn thành thì khoá hoàn toàn
    IF EXISTS (SELECT 1 FROM deleted WHERE Status = 'Completed')
    BEGIN
        THROW 50010, N'Phiếu nhập đã hoàn thành, không được sửa.', 1;
    END

    -- (b) Lấy các phiếu vừa chuyển Draft -> Completed
    DECLARE @Completed TABLE (ReceiptId INT PRIMARY KEY, CompletedAt DATETIME2(0) NOT NULL);

    INSERT INTO @Completed (ReceiptId, CompletedAt)
    SELECT i.ReceiptId, i.CompletedAt
    FROM inserted AS i
    JOIN deleted  AS d ON d.ReceiptId = i.ReceiptId
    WHERE d.Status = 'Draft' AND i.Status = 'Completed';

    IF NOT EXISTS (SELECT 1 FROM @Completed) RETURN;

    -- (c) Không cho hoàn thành phiếu rỗng
    IF EXISTS (
        SELECT 1 FROM @Completed AS c
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ImportReceiptDetails AS x WHERE x.ReceiptId = c.ReceiptId)
    )
    BEGIN
        THROW 50011, N'Phiếu nhập chưa có sản phẩm nào, không thể hoàn thành.', 1;
    END

    -- (d) Gom số lượng & thành tiền theo sản phẩm (phòng khi 1 câu UPDATE
    --     hoàn thành nhiều phiếu cùng chứa 1 sản phẩm), rồi tính bình quân.
    --     Trong UPDATE, vế phải luôn dùng giá trị CŨ của dòng -> công thức đúng.
    ;WITH Agg AS
    (
        SELECT x.ProductId,
               SUM(x.Quantity)                 AS TotalQty,
               SUM(x.Quantity * x.ImportPrice) AS TotalValue
        FROM dbo.ImportReceiptDetails AS x
        JOIN @Completed AS c ON c.ReceiptId = x.ReceiptId
        GROUP BY x.ProductId
    )
    UPDATE p
    SET    p.CostPrice     = (p.StockQuantity * p.CostPrice + a.TotalValue)
                             / (p.StockQuantity + a.TotalQty),   -- TotalQty > 0 nên không chia cho 0
           p.StockQuantity = p.StockQuantity + a.TotalQty
    FROM   dbo.Products AS p
    JOIN   Agg          AS a ON a.ProductId = p.ProductId;

    -- (e) Ghi sổ kho
    INSERT INTO dbo.InventoryTransactions (ProductId, TransactionDate, TransactionType, QuantityChange, ReferenceId, Note)
    SELECT x.ProductId, c.CompletedAt, 'IMPORT', x.Quantity, x.ReceiptId,
           N'Nhập hàng theo phiếu ' + r.ReceiptCode
    FROM dbo.ImportReceiptDetails AS x
    JOIN @Completed               AS c ON c.ReceiptId = x.ReceiptId
    JOIN dbo.ImportReceipts       AS r ON r.ReceiptId = x.ReceiptId;
END
GO

/* ---------------------------------------------------------------------
   T5. Không cho xoá phiếu nhập đã hoàn thành
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_ImportReceipts_AfterDelete
ON dbo.ImportReceipts
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted WHERE Status = 'Completed')
    BEGIN
        THROW 50012, N'Không được xoá phiếu nhập đã hoàn thành.', 1;
    END
END
GO

/* ---------------------------------------------------------------------
   T6. Chỉ được thêm/sửa/xoá chi tiết khi phiếu còn Draft
   (đề: "chỉ có thể sửa phiếu nhập trước khi hoàn thành phiếu")
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_ImportReceiptDetails_LockCompleted
ON dbo.ImportReceiptDetails
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM dbo.ImportReceipts AS r
        WHERE r.Status = 'Completed'
          AND r.ReceiptId IN (SELECT ReceiptId FROM inserted
                              UNION
                              SELECT ReceiptId FROM deleted)
    )
    BEGIN
        THROW 50013, N'Phiếu nhập đã hoàn thành, không được thay đổi chi tiết.', 1;
    END
END
GO

/* =====================================================================
   NHÓM 3 - ĐƠN HÀNG
   Quyết định nghiệp vụ: TRỪ KHO NGAY KHI KHÁCH ĐẶT ĐƠN (giữ hàng cho khách),
   TRẢ LẠI KHO KHI ĐƠN BỊ HUỶ. Nhờ vậy cảnh báo "sắp hết hàng" phản ánh
   đúng số hàng còn có thể bán.
   ===================================================================== */

/* ---------------------------------------------------------------------
   T7. Thêm chi tiết đơn hàng (INSTEAD OF INSERT)
       - Đơn phải đang Pending, sản phẩm phải đang bán
       - Kiểm tra đủ tồn kho (khoá dòng để 2 khách mua cùng lúc không vượt tồn)
       - Chụp lại giá bán & giá vốn hiện tại (bỏ qua giá client gửi lên
         -> chống sửa giá từ phía trình duyệt)
       - Trừ tồn, ghi sổ kho EXPORT, tính lại TotalAmount
   WS chỉ cần: INSERT Orders (Pending) rồi INSERT OrderDetails (OrderId,
   ProductId, Quantity, UnitPrice = 0) trong cùng 1 transaction.
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_OrderDetails_InsteadOfInsert
ON dbo.OrderDetails
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    IF EXISTS (
        SELECT 1 FROM inserted AS i
        JOIN dbo.Orders AS o ON o.OrderId = i.OrderId
        WHERE o.Status <> 'Pending'
    )
    BEGIN
        THROW 50030, N'Chỉ được thêm sản phẩm vào đơn hàng đang ở trạng thái Chưa xử lý.', 1;
    END

    IF EXISTS (
        SELECT 1 FROM inserted AS i
        LEFT JOIN dbo.Products AS p ON p.ProductId = i.ProductId
        WHERE p.ProductId IS NULL      -- sản phẩm không tồn tại (nếu không chặn, JOIN bên dưới sẽ âm thầm bỏ mất dòng)
           OR p.IsVisible = 0
    )
    BEGIN
        THROW 50031, N'Đơn hàng có sản phẩm không tồn tại hoặc đã ngừng bán.', 1;
    END

    -- Tổng số lượng cần theo từng sản phẩm
    DECLARE @Need TABLE (ProductId INT PRIMARY KEY, Qty INT NOT NULL);

    INSERT INTO @Need (ProductId, Qty)
    SELECT ProductId, SUM(Quantity)
    FROM inserted
    GROUP BY ProductId;

    -- UPDLOCK + HOLDLOCK: giữ khoá trên dòng sản phẩm đến hết transaction,
    -- giao dịch khác muốn mua cùng sản phẩm phải chờ -> không bán vượt tồn.
    IF EXISTS (
        SELECT 1
        FROM dbo.Products AS p WITH (UPDLOCK, HOLDLOCK)
        JOIN @Need        AS n ON n.ProductId = p.ProductId
        WHERE p.StockQuantity < n.Qty
    )
    BEGIN
        THROW 50032, N'Không đủ hàng trong kho cho một hoặc nhiều sản phẩm.', 1;
    END

    -- Ghi chi tiết với giá chụp từ bảng Products (CHECK Quantity > 0 kiểm tra ở đây)
    INSERT INTO dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice, UnitCost)
    SELECT i.OrderId, i.ProductId, i.Quantity, p.SalePrice, p.CostPrice
    FROM inserted     AS i
    JOIN dbo.Products AS p ON p.ProductId = i.ProductId;

    -- Trừ tồn kho
    UPDATE p
    SET    p.StockQuantity = p.StockQuantity - n.Qty
    FROM   dbo.Products AS p
    JOIN   @Need        AS n ON n.ProductId = p.ProductId;

    -- Ghi sổ kho
    INSERT INTO dbo.InventoryTransactions (ProductId, TransactionType, QuantityChange, ReferenceId, Note)
    SELECT i.ProductId, 'EXPORT', -i.Quantity, i.OrderId, N'Bán hàng - đơn ' + o.OrderCode
    FROM inserted   AS i
    JOIN dbo.Orders AS o ON o.OrderId = i.OrderId;

    -- Tính lại tổng tiền đơn hàng
    UPDATE o
    SET    o.TotalAmount = t.Total
    FROM   dbo.Orders AS o
    JOIN  (SELECT od.OrderId, SUM(od.LineTotal) AS Total
           FROM dbo.OrderDetails AS od
           WHERE od.OrderId IN (SELECT OrderId FROM inserted)
           GROUP BY od.OrderId) AS t ON t.OrderId = o.OrderId;
END
GO

/* ---------------------------------------------------------------------
   T8. Chi tiết đơn hàng đã ghi thì không được sửa/xoá
   (muốn đổi -> huỷ đơn, đặt đơn mới; giữ lịch sử và sổ kho nhất quán)
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_OrderDetails_ReadOnly
ON dbo.OrderDetails
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        THROW 50033, N'Chi tiết đơn hàng không được sửa hoặc xoá.', 1;
    END
END
GO

/* ---------------------------------------------------------------------
   T9. Trạng thái đơn hàng chỉ đi 1 chiều + huỷ đơn thì trả hàng về kho
       Hợp lệ:  Pending   -> Confirmed | Cancelled
                Confirmed -> Delivered | Cancelled
       Delivered và Cancelled là trạng thái cuối, không đổi được nữa.
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Orders_StatusFlow
ON dbo.Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    -- (a) Kiểm tra chuyển trạng thái hợp lệ
    IF EXISTS (
        SELECT 1
        FROM inserted AS i
        JOIN deleted  AS d ON d.OrderId = i.OrderId
        WHERE i.Status <> d.Status
          AND NOT (
                (d.Status = 'Pending'   AND i.Status IN ('Confirmed', 'Cancelled'))
             OR (d.Status = 'Confirmed' AND i.Status IN ('Delivered', 'Cancelled'))
          )
    )
    BEGIN
        THROW 50020, N'Chuyển trạng thái đơn hàng không hợp lệ (không được quay lui hoặc nhảy bước).', 1;
    END

    -- (b) Đơn vừa bị huỷ -> trả hàng về kho
    DECLARE @Cancelled TABLE (OrderId INT PRIMARY KEY);

    INSERT INTO @Cancelled (OrderId)
    SELECT i.OrderId
    FROM inserted AS i
    JOIN deleted  AS d ON d.OrderId = i.OrderId
    WHERE d.Status <> 'Cancelled' AND i.Status = 'Cancelled';

    IF NOT EXISTS (SELECT 1 FROM @Cancelled) RETURN;

    UPDATE p
    SET    p.StockQuantity = p.StockQuantity + r.Qty
    FROM   dbo.Products AS p
    JOIN  (SELECT od.ProductId, SUM(od.Quantity) AS Qty
           FROM dbo.OrderDetails AS od
           JOIN @Cancelled       AS c ON c.OrderId = od.OrderId
           GROUP BY od.ProductId) AS r ON r.ProductId = p.ProductId;

    INSERT INTO dbo.InventoryTransactions (ProductId, TransactionType, QuantityChange, ReferenceId, Note)
    SELECT od.ProductId, 'RETURN', od.Quantity, od.OrderId, N'Huỷ đơn ' + o.OrderCode + N' - trả hàng về kho'
    FROM dbo.OrderDetails AS od
    JOIN @Cancelled       AS c ON c.OrderId = od.OrderId
    JOIN dbo.Orders       AS o ON o.OrderId = od.OrderId;
END
GO

/* ---------------------------------------------------------------------
   T10. Không cho xoá đơn hàng (chỉ được chuyển sang Cancelled)
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_Orders_NoDelete
ON dbo.Orders
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted)
    BEGIN
        THROW 50021, N'Không được xoá đơn hàng. Hãy chuyển đơn sang trạng thái Đã huỷ.', 1;
    END
END
GO

/* =====================================================================
   NHÓM 4 - GIỎ HÀNG
   ===================================================================== */

/* ---------------------------------------------------------------------
   T11. Không cho thêm sản phẩm đang ẩn vào giỏ
   --------------------------------------------------------------------- */
CREATE OR ALTER TRIGGER dbo.trg_CartItems_CheckProduct
ON dbo.CartItems
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM inserted AS i
        JOIN dbo.Products AS p ON p.ProductId = i.ProductId
        WHERE p.IsVisible = 0
    )
    BEGIN
        THROW 50040, N'Sản phẩm đã ngừng bán, không thể thêm vào giỏ hàng.', 1;
    END
END
GO

/* Xem danh sách trigger vừa tạo */
SELECT t.name AS TriggerName, OBJECT_NAME(t.parent_id) AS TableName,
       CASE WHEN t.is_instead_of_trigger = 1 THEN 'INSTEAD OF' ELSE 'AFTER' END AS Kind
FROM sys.triggers AS t
WHERE t.parent_class = 1
ORDER BY TableName, TriggerName;
GO
