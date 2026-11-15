/* =====================================================================
   File 03 - KIỂM THỬ TRIGGER + CÁC TRUY VẤN BÁO CÁO MẪU
   Chạy trên DB MỚI: 01_create_tables_constraints.sql -> 02_triggers.sql -> file này
   Mỗi bước có ghi "MONG ĐỢI" để đối chiếu kết quả (dùng cho mục 5.2 báo cáo).
   ===================================================================== */
USE CoffeeShopDb;
GO
SET NOCOUNT ON;

DECLARE @AdminId INT = (SELECT UserId FROM dbo.Users WHERE Username = 'admin');
DECLARE @CatId   INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Robusta');

/* ---------- Dữ liệu: 1 sản phẩm mới (giá 0, tồn 0, lợi nhuận 20%) ---------- */
INSERT INTO dbo.Products (Code, Name, CategoryId, Unit,  Origin,  ProfitRate)
VALUES ('RB-BMT-500', N'Robusta Buôn Ma Thuột 500g', @CatId, N'Gói', N'Buôn Ma Thuột', 20);
DECLARE @P INT = SCOPE_IDENTITY();

INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Phone, Address, Ward, District, City)
VALUES ('khach01', 'HASH', N'Nguyễn Văn A', 'a@mail.com', '0901234567', N'12 Lê Lợi', N'Bến Nghé', N'Quận 1', N'TP.HCM');
DECLARE @U INT = SCOPE_IDENTITY();

/* ---------- TC01: Nhập lần 1 - 10 sp giá 20 ---------- */
INSERT INTO dbo.ImportReceipts (ReceiptCode, CreatedBy) VALUES ('PN-001', @AdminId);
DECLARE @R1 INT = SCOPE_IDENTITY();
INSERT INTO dbo.ImportReceiptDetails (ReceiptId, ProductId, Quantity, ImportPrice) VALUES (@R1, @P, 10, 20);
UPDATE dbo.ImportReceipts SET Status = 'Completed', CompletedAt = SYSDATETIME() WHERE ReceiptId = @R1;

SELECT 'TC01 - MONG ĐỢI: Stock=10, CostPrice=20, SalePrice=24' AS TestCase,
       StockQuantity, CostPrice, SalePrice FROM dbo.Products WHERE ProductId = @P;

/* ---------- TC02: Khách đặt 4 sp ---------- */
INSERT INTO dbo.Orders (OrderCode, UserId, ReceiverName, ReceiverPhone, ShippingAddress, Ward, District, City, PaymentMethod)
VALUES ('DH-001', @U, N'Nguyễn Văn A', '0901234567', N'12 Lê Lợi', N'Bến Nghé', N'Quận 1', N'TP.HCM', 'Cash');
DECLARE @O1 INT = SCOPE_IDENTITY();
INSERT INTO dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice) VALUES (@O1, @P, 4, 0); -- giá 0 sẽ bị trigger thay bằng giá thật

SELECT 'TC02 - MONG ĐỢI: Stock=6, UnitPrice=24, Total=96' AS TestCase,
       p.StockQuantity, od.UnitPrice, o.TotalAmount
FROM dbo.Products p JOIN dbo.OrderDetails od ON od.ProductId = p.ProductId JOIN dbo.Orders o ON o.OrderId = od.OrderId
WHERE p.ProductId = @P;

/* ---------- TC03: Nhập lần 2 - 10 sp giá 15 (ví dụ trong đề) ---------- */
INSERT INTO dbo.ImportReceipts (ReceiptCode, CreatedBy) VALUES ('PN-002', @AdminId);
DECLARE @R2 INT = SCOPE_IDENTITY();
INSERT INTO dbo.ImportReceiptDetails (ReceiptId, ProductId, Quantity, ImportPrice) VALUES (@R2, @P, 10, 15);
UPDATE dbo.ImportReceipts SET Status = 'Completed', CompletedAt = SYSDATETIME() WHERE ReceiptId = @R2;

SELECT 'TC03 - MONG ĐỢI: Stock=16, CostPrice=(6*20+10*15)/16=16.875, SalePrice=20.25' AS TestCase,
       StockQuantity, CostPrice, SalePrice FROM dbo.Products WHERE ProductId = @P;

/* ---------- Các ca PHẢI BÁO LỖI ---------- */
BEGIN TRY  -- TC04
    UPDATE dbo.ImportReceiptDetails SET Quantity = 99 WHERE ReceiptId = @R1;
    PRINT 'TC04 FAIL - lẽ ra phải bị chặn';
END TRY BEGIN CATCH PRINT CONCAT('TC04 PASS (sửa chi tiết phiếu đã hoàn thành) -> ', ERROR_NUMBER(), ': ', ERROR_MESSAGE()); END CATCH;

BEGIN TRY  -- TC05: tạo đơn + chi tiết trong 1 TRANSACTION (đúng cách WS phải làm),
           -- lỗi ở chi tiết thì phần đơn hàng cũng bị huỷ theo, không để lại đơn rỗng
    BEGIN TRANSACTION;
    INSERT INTO dbo.Orders (OrderCode, UserId, ReceiverName, ReceiverPhone, ShippingAddress, Ward, District, City, PaymentMethod)
    VALUES ('DH-002', @U, N'Trần B', '0907654321', N'5 Hai Bà Trưng', N'Đa Kao', N'Quận 1', N'TP.HCM', 'BankTransfer');
    INSERT INTO dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice) VALUES (SCOPE_IDENTITY(), @P, 1000, 0);
    COMMIT TRANSACTION;
    PRINT 'TC05 FAIL - lẽ ra phải bị chặn';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION; PRINT CONCAT('TC05 PASS (đặt quá tồn kho) -> ', ERROR_NUMBER(), ': ', ERROR_MESSAGE()); END CATCH;

UPDATE dbo.Orders SET Status = 'Confirmed' WHERE OrderId = @O1;
BEGIN TRY  -- TC06
    UPDATE dbo.Orders SET Status = 'Pending' WHERE OrderId = @O1;
    PRINT 'TC06 FAIL - lẽ ra phải bị chặn';
END TRY BEGIN CATCH PRINT CONCAT('TC06 PASS (quay lui trạng thái đơn) -> ', ERROR_NUMBER(), ': ', ERROR_MESSAGE()); END CATCH;

BEGIN TRY  -- TC07
    UPDATE dbo.Products SET StockQuantity = 1000 WHERE ProductId = @P;
    PRINT 'TC07 FAIL - lẽ ra phải bị chặn';
END TRY BEGIN CATCH PRINT CONCAT('TC07 PASS (sửa tay tồn kho) -> ', ERROR_NUMBER(), ': ', ERROR_MESSAGE()); END CATCH;

/* ---------- TC08: Huỷ đơn -> trả 4 sp về kho ---------- */
UPDATE dbo.Orders SET Status = 'Cancelled' WHERE OrderId = @O1;
SELECT 'TC08 - MONG ĐỢI: Stock=20 (16 + 4 trả về)' AS TestCase, StockQuantity FROM dbo.Products WHERE ProductId = @P;

/* ---------- TC09, TC10: Xoá sản phẩm ---------- */
DELETE FROM dbo.Products WHERE ProductId = @P;   -- đã nhập hàng -> chỉ ẩn
SELECT 'TC09 - MONG ĐỢI: vẫn còn, IsVisible=0' AS TestCase, ProductId, IsVisible FROM dbo.Products WHERE ProductId = @P;

INSERT INTO dbo.Products (Code, Name, CategoryId, WeightGram, StockQuantity) VALUES ('TMP-01', N'SP tạm', @CatId, 250, 5);
DECLARE @Tmp INT = SCOPE_IDENTITY();
DELETE FROM dbo.Products WHERE ProductId = @Tmp;   -- chưa nhập hàng -> xoá hẳn
SELECT 'TC10 - MONG ĐỢI: 0 dòng' AS TestCase, COUNT(*) AS SoDong FROM dbo.Products WHERE ProductId = @Tmp;
GO

/* =====================================================================
   TRUY VẤN BÁO CÁO MẪU (WS sẽ dùng lại, thay hằng số bằng tham số)
   ===================================================================== */

-- Q1. Tồn kho của các sản phẩm thuộc 1 loại TẠI THỜI ĐIỂM @At
DECLARE @At DATETIME2(0) = SYSDATETIME(), @CategoryId INT = 2;
SELECT p.Code, p.Name, ISNULL(SUM(it.QuantityChange), 0) AS TonTaiThoiDiem
FROM dbo.Products p
LEFT JOIN dbo.InventoryTransactions it ON it.ProductId = p.ProductId AND it.TransactionDate <= @At
WHERE p.CategoryId = @CategoryId
GROUP BY p.Code, p.Name;

-- Q2. Báo cáo tổng nhập - xuất trong khoảng [@From, @To]
DECLARE @From DATETIME2(0) = DATEADD(DAY, -30, SYSDATETIME()), @To DATETIME2(0) = SYSDATETIME();
SELECT p.Code, p.Name,
       SUM(CASE WHEN it.TransactionType IN ('INITIAL','IMPORT') THEN it.QuantityChange ELSE 0 END) AS TongNhap,
       SUM(CASE WHEN it.TransactionType = 'EXPORT' THEN -it.QuantityChange ELSE 0 END)            AS TongXuat,
       SUM(CASE WHEN it.TransactionType = 'RETURN' THEN it.QuantityChange ELSE 0 END)             AS TraVe
FROM dbo.Products p
JOIN dbo.InventoryTransactions it ON it.ProductId = p.ProductId
WHERE it.TransactionDate BETWEEN @From AND @To
GROUP BY p.Code, p.Name;

-- Q3. Cảnh báo sắp hết hàng, ngưỡng do người dùng nhập
DECLARE @Threshold INT = 10;
SELECT Code, Name, StockQuantity FROM dbo.Products
WHERE IsVisible = 1 AND StockQuantity <= @Threshold
ORDER BY StockQuantity;

-- Q4. Danh sách sản phẩm theo loại, có phân trang (trang 1, 12 sp/trang)
DECLARE @Page INT = 1, @PageSize INT = 12;
SELECT ProductId, Code, Name, SalePrice, ImageUrl
FROM dbo.Products
WHERE CategoryId = @CategoryId AND IsVisible = 1
ORDER BY Name
OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
GO
