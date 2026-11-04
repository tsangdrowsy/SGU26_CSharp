/* =====================================================================
   ĐỒ ÁN: HỆ THỐNG BÁN BỘT CÀ PHÊ TRỰC TUYẾN
   File 01 - Tạo database, bảng và RÀNG BUỘC (constraint)
   Hệ quản trị: Microsoft SQL Server 2019 trở lên

   Cách dùng:
     - PHƯƠNG ÁN A (chỉ ràng buộc): chạy file này. Mọi nghiệp vụ
       (giá nhập bình quân, trừ/cộng tồn kho, trạng thái đơn 1 chiều...)
       do Web Service (C#) xử lý trong transaction.
     - PHƯƠNG ÁN B (có trigger): chạy file này TRƯỚC, sau đó chạy
       02_triggers.sql. Khi đó DB tự xử lý các nghiệp vụ trên.

   Quy ước:
     PK_ = khoá chính, FK_ = khoá ngoại, UQ_ = duy nhất,
     CK_ = kiểm tra (CHECK), DF_ = giá trị mặc định, IX_ = chỉ mục
   ===================================================================== */

USE master;
GO

IF DB_ID(N'CoffeeShopDb') IS NOT NULL
BEGIN
    ALTER DATABASE CoffeeShopDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CoffeeShopDb;
END
GO

CREATE DATABASE CoffeeShopDb;
GO

USE CoffeeShopDb;
GO

/* ---------------------------------------------------------------------
   1. USERS - tài khoản khách hàng và quản trị
  
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    UserId        INT IDENTITY(1,1) NOT NULL,
    Username      VARCHAR(50)       NOT NULL,
    PasswordHash  VARCHAR(255)      NOT NULL,   
    FullName      NVARCHAR(100)     NOT NULL,
    Email         VARCHAR(100)      NOT NULL,
    Phone         VARCHAR(15)       NULL,
    Address       NVARCHAR(255)     NULL,     
    Ward          NVARCHAR(100)     NULL,       
    District      NVARCHAR(100)     NULL,       
    City          NVARCHAR(100)     NULL,       
    Role          VARCHAR(20)       NOT NULL CONSTRAINT DF_Users_Role      DEFAULT ('Customer'),
    IsLocked      BIT               NOT NULL CONSTRAINT DF_Users_IsLocked  DEFAULT (0),
    CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_Users          PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role     CHECK (Role IN ('Admin', 'Customer')),
    CONSTRAINT CK_Users_Phone    CHECK (Phone IS NULL OR (LEN(Phone) BETWEEN 9 AND 15 AND Phone NOT LIKE '%[^0-9+]%')),
    CONSTRAINT CK_Users_Email    CHECK (Email LIKE '_%@_%._%'),
    
    CONSTRAINT CK_Users_CustomerShippingInfo CHECK
    (
        Role = 'Admin'
        OR (Phone IS NOT NULL AND Address IS NOT NULL AND Ward IS NOT NULL
            AND District IS NOT NULL AND City IS NOT NULL)
    )
);
GO

/* ---------------------------------------------------------------------
   2. CATEGORIES - loại sản phẩm 
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Categories
(
    CategoryId   INT IDENTITY(1,1) NOT NULL,
    Name         NVARCHAR(100)     NOT NULL,
   
    IsActive     BIT               NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),

    CONSTRAINT PK_Categories      PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name),
    CONSTRAINT CK_Categories_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
);
GO

/* ---------------------------------------------------------------------
   3. SUPPLIERS - nhà cung cấp (thông tin tham khảo của sản phẩm;
      đề cho phép phiếu nhập bỏ qua nhà cung cấp)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Suppliers
(
    SupplierId  INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(150)     NOT NULL,
   

    CONSTRAINT PK_Suppliers      PRIMARY KEY (SupplierId),
    CONSTRAINT UQ_Suppliers_Name UNIQUE (Name)
);
GO

/* ---------------------------------------------------------------------
   4. PRODUCTS - sản phẩm bột cà phê
   - CostPrice  : giá nhập BÌNH QUÂN, cập nhật mỗi khi hoàn thành phiếu nhập.
                  Để 4 chữ số thập phân cho phép bình quân không bị làm tròn sớm.
   - ProfitRate : tỉ lệ lợi nhuận mong muốn (%).
   - SalePrice  : CỘT TÍNH TOÁN (computed column) = giá nhập * (100% + lợi nhuận).
                  DB tự tính, ứng dụng không cần (và không thể) ghi vào cột này,
                  nên giá bán luôn khớp với công thức của đề.
   - IsVisible  : 1 = đang bán (hiển thị), 0 = ẩn.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Products
(
    ProductId      INT IDENTITY(1,1) NOT NULL,
    Code           VARCHAR(20)       NOT NULL,
    Name           NVARCHAR(150)     NOT NULL,
    CategoryId     INT               NOT NULL,
    SupplierId     INT               NULL,
    Description    NVARCHAR(MAX)     NULL,
    Unit           NVARCHAR(30)      NOT NULL CONSTRAINT DF_Products_Unit DEFAULT (N'Gói'),
   
    Origin         NVARCHAR(100)     NULL,          -- xuất xứ: Buôn Ma Thuột, Cầu Đất...
    
    ImageUrl       NVARCHAR(500)     NULL,
    CostPrice      DECIMAL(18,4)     NOT NULL CONSTRAINT DF_Products_CostPrice  DEFAULT (0),
    ProfitRate     DECIMAL(5,2)      NOT NULL CONSTRAINT DF_Products_ProfitRate DEFAULT (0),
    SalePrice      AS CAST(ROUND(CostPrice * (100 + ProfitRate) / 100, 2) AS DECIMAL(18,2)) PERSISTED,
    StockQuantity  INT               NOT NULL CONSTRAINT DF_Products_Stock      DEFAULT (0),
    IsVisible      BIT               NOT NULL CONSTRAINT DF_Products_IsVisible  DEFAULT (1),
    CreatedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_Products_CreatedAt  DEFAULT (SYSDATETIME()),

    WeightGram     INT               NULL,
    RoastLevel     VARCHAR(20)       NULL,

    CONSTRAINT PK_Products            PRIMARY KEY (ProductId),
    CONSTRAINT UQ_Products_Code       UNIQUE (Code),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (CategoryId),
    CONSTRAINT FK_Products_Suppliers  FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers  (SupplierId),
    CONSTRAINT CK_Products_Weight     CHECK (WeightGram > 0),
    CONSTRAINT CK_Products_CostPrice  CHECK (CostPrice >= 0),
    CONSTRAINT CK_Products_ProfitRate CHECK (ProfitRate >= 0 AND ProfitRate <= 500),
    CONSTRAINT CK_Products_Stock      CHECK (StockQuantity >= 0),   -- chốt chặn cuối: không bao giờ âm kho
    CONSTRAINT CK_Products_RoastLevel CHECK (RoastLevel IS NULL OR RoastLevel IN ('Light', 'Medium', 'Dark'))
);
GO
-- Lý do không dùng ON DELETE CASCADE ở FK_Products_Categories:
-- xoá 1 loại mà kéo theo xoá hết sản phẩm là quá nguy hiểm. Mặc định NO ACTION
-- sẽ báo lỗi nếu loại còn sản phẩm -> ứng dụng phải ẩn loại (IsActive = 0).

/* ---------------------------------------------------------------------
   5. IMPORT RECEIPTS - phiếu nhập hàng
   
   --------------------------------------------------------------------- */
CREATE TABLE dbo.ImportReceipts
(
    ReceiptId    INT IDENTITY(1,1) NOT NULL,
    ReceiptCode  VARCHAR(20)       NOT NULL,       -- VD: PN20261103-001
    ImportDate   DATETIME2(0)      NOT NULL CONSTRAINT DF_ImportReceipts_ImportDate DEFAULT (SYSDATETIME()),
    Status       VARCHAR(20)       NOT NULL CONSTRAINT DF_ImportReceipts_Status     DEFAULT ('Draft'),
    CompletedAt  DATETIME2(0)      NULL,
    CreatedBy    INT               NOT NULL,
    Note         NVARCHAR(500)     NULL,

    CONSTRAINT PK_ImportReceipts       PRIMARY KEY (ReceiptId),
    CONSTRAINT UQ_ImportReceipts_Code  UNIQUE (ReceiptCode),
    CONSTRAINT FK_ImportReceipts_Users FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_ImportReceipts_Status CHECK (Status IN ('Draft', 'Completed')),
    -- Trạng thái và thời điểm hoàn thành phải khớp nhau
    CONSTRAINT CK_ImportReceipts_Completed CHECK
    (
        (Status = 'Draft'     AND CompletedAt IS NULL) OR
        (Status = 'Completed' AND CompletedAt IS NOT NULL)
    )
);
GO

/* ---------------------------------------------------------------------
   6. IMPORT RECEIPT DETAILS - chi tiết phiếu nhập
  
   --------------------------------------------------------------------- */
CREATE TABLE dbo.ImportReceiptDetails
(
    ReceiptId    INT           NOT NULL,
    ProductId    INT           NOT NULL,
    Quantity     INT           NOT NULL,
    ImportPrice  DECIMAL(18,2) NOT NULL,

    CONSTRAINT PK_ImportReceiptDetails PRIMARY KEY (ReceiptId, ProductId),
    CONSTRAINT FK_ImportReceiptDetails_Receipts FOREIGN KEY (ReceiptId)
        REFERENCES dbo.ImportReceipts (ReceiptId) ON DELETE CASCADE,
    CONSTRAINT FK_ImportReceiptDetails_Products FOREIGN KEY (ProductId)
        REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_ImportReceiptDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_ImportReceiptDetails_Price    CHECK (ImportPrice > 0)
);
GO

/* ---------------------------------------------------------------------
   7. ORDERS - đơn đặt hàng
   - Thông tin người nhận lưu RIÊNG trong đơn (không tham chiếu Users)
     vì đề cho phép giao cho người khác ở địa chỉ khác, và địa chỉ
     trong đơn cũ không được đổi khi khách sửa hồ sơ.
   - Status: Pending (chưa xử lý) -> Confirmed (đã xác nhận)
             -> Delivered (đã giao) ; hoặc -> Cancelled (đã huỷ).
     Ràng buộc CHECK chỉ kiểm tra được GIÁ TRỊ hợp lệ, không kiểm tra
     được "không quay lui" -> phần đó cần WS hoặc trigger (file 02).
   --------------------------------------------------------------------- */
CREATE TABLE dbo.Orders
(
    OrderId          INT IDENTITY(1,1) NOT NULL,
    OrderCode        VARCHAR(20)       NOT NULL,       -- VD: DH20261103-0001
    UserId           INT               NOT NULL,
    OrderDate        DATETIME2(0)      NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT (SYSDATETIME()),
    ReceiverName     NVARCHAR(100)     NOT NULL,
    ReceiverPhone    VARCHAR(15)       NOT NULL,
    ShippingAddress  NVARCHAR(255)     NOT NULL,
    Ward             NVARCHAR(100)     NOT NULL,
    District         NVARCHAR(100)     NOT NULL,
    City             NVARCHAR(100)     NOT NULL,
    PaymentMethod    VARCHAR(20)       NOT NULL,
    Status           VARCHAR(20)       NOT NULL CONSTRAINT DF_Orders_Status DEFAULT ('Pending'),
    TotalAmount      DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Orders_Total  DEFAULT (0),
    Note             NVARCHAR(500)     NULL,
    UpdatedAt        DATETIME2(0)      NULL,

    CONSTRAINT PK_Orders        PRIMARY KEY (OrderId),
    CONSTRAINT UQ_Orders_Code   UNIQUE (OrderCode),
    CONSTRAINT FK_Orders_Users  FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Orders_Payment CHECK (PaymentMethod IN ('Cash', 'BankTransfer', 'Online')),
    CONSTRAINT CK_Orders_Status  CHECK (Status IN ('Pending', 'Confirmed', 'Delivered', 'Cancelled')),
    CONSTRAINT CK_Orders_Total   CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Orders_Phone   CHECK (LEN(ReceiverPhone) BETWEEN 9 AND 15 AND ReceiverPhone NOT LIKE '%[^0-9+]%')
);
GO

/* ---------------------------------------------------------------------
   8. ORDER DETAILS - chi tiết đơn hàng
   - UnitPrice : giá bán TẠI THỜI ĐIỂM MUA (chụp lại - snapshot), vì giá
                 sản phẩm có thể đổi sau này nhưng đơn cũ phải giữ nguyên.
   - UnitCost  : giá vốn tại thời điểm mua, phục vụ báo cáo lợi nhuận.
   - LineTotal : cột tính toán = số lượng * đơn giá.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.OrderDetails
(
    OrderId    INT           NOT NULL,
    ProductId  INT           NOT NULL,
    Quantity   INT           NOT NULL,
    UnitPrice  DECIMAL(18,2) NOT NULL,
    UnitCost   DECIMAL(18,4) NOT NULL CONSTRAINT DF_OrderDetails_UnitCost DEFAULT (0),
    LineTotal  AS CAST(Quantity * UnitPrice AS DECIMAL(18,2)) PERSISTED,

    CONSTRAINT PK_OrderDetails PRIMARY KEY (OrderId, ProductId),
    CONSTRAINT FK_OrderDetails_Orders   FOREIGN KEY (OrderId)   REFERENCES dbo.Orders   (OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_OrderDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_Price    CHECK (UnitPrice >= 0 AND UnitCost >= 0)
);
GO

/* ---------------------------------------------------------------------
   9. CART ITEMS - giỏ hàng (lưu DB để khách đăng nhập máy khác vẫn còn)
   - Khoá chính ghép (UserId, ProductId): 1 sản phẩm chỉ 1 dòng/giỏ,
     thêm lần nữa thì cộng Quantity.
   --------------------------------------------------------------------- */
CREATE TABLE dbo.CartItems
(
    UserId     INT          NOT NULL,
    ProductId  INT          NOT NULL,
    Quantity   INT          NOT NULL,
    AddedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_CartItems_AddedAt DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_CartItems PRIMARY KEY (UserId, ProductId),
    CONSTRAINT FK_CartItems_Users    FOREIGN KEY (UserId)    REFERENCES dbo.Users    (UserId)    ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId) ON DELETE CASCADE,
    CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0 AND Quantity <= 100)
);
GO

/* ---------------------------------------------------------------------
   10. INVENTORY TRANSACTIONS - sổ kho (nhật ký nhập/xuất)
   Đây là bảng giúp làm 2 yêu cầu khó của đề:
     - "Tra cứu tồn kho tại 1 thời điểm do người dùng định"
         => Tồn(T) = SUM(QuantityChange) với TransactionDate <= T
     - "Báo cáo tổng nhập - xuất trong khoảng thời gian"
         => SUM theo TransactionType trong [From, To]
   Products.StockQuantity chỉ cho biết tồn HIỆN TẠI, không trả lời được
   câu hỏi "tồn ngày 01/10 là bao nhiêu" -> cần sổ kho.

   QuantityChange: dương = vào kho, âm = ra kho.
     INITIAL  : số lượng ban đầu khi tạo sản phẩm (+)
     IMPORT   : hoàn thành phiếu nhập (+)
     EXPORT   : bán hàng - khi khách đặt đơn (-)
     RETURN   : đơn bị huỷ, trả hàng về kho (+)
   --------------------------------------------------------------------- */
CREATE TABLE dbo.InventoryTransactions
(
    TransactionId    BIGINT IDENTITY(1,1) NOT NULL,
    ProductId        INT                  NOT NULL,
    TransactionDate  DATETIME2(0)         NOT NULL CONSTRAINT DF_InvTrans_Date DEFAULT (SYSDATETIME()),
    TransactionType  VARCHAR(10)          NOT NULL,
    QuantityChange   INT                  NOT NULL,
    ReferenceId      INT                  NULL,     -- ReceiptId hoặc OrderId tuỳ loại
    Note             NVARCHAR(255)        NULL,

    CONSTRAINT PK_InventoryTransactions PRIMARY KEY (TransactionId),
    CONSTRAINT FK_InvTrans_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (ProductId),
    CONSTRAINT CK_InvTrans_Type     CHECK (TransactionType IN ('INITIAL', 'IMPORT', 'EXPORT', 'RETURN')),
    CONSTRAINT CK_InvTrans_Change   CHECK (QuantityChange <> 0),
    -- Dấu của số lượng phải khớp loại giao dịch
    CONSTRAINT CK_InvTrans_Sign CHECK
    (
        (TransactionType IN ('INITIAL', 'IMPORT', 'RETURN') AND QuantityChange > 0) OR
        (TransactionType = 'EXPORT' AND QuantityChange < 0)
    )
);
GO

/* ---------------------------------------------------------------------
   11. CHỈ MỤC (INDEX) cho các truy vấn hay dùng
   SQL Server tự tạo index cho PRIMARY KEY và UNIQUE, nhưng KHÔNG tự tạo
   cho FOREIGN KEY -> tạo thêm cho các cột FK và cột hay lọc/sắp xếp.
   --------------------------------------------------------------------- */
-- Danh sách sản phẩm theo loại (có phân trang), chỉ lấy SP đang bán
CREATE INDEX IX_Products_Category_Visible ON dbo.Products (CategoryId, IsVisible) INCLUDE (Name, SalePrice, ImageUrl);
-- Tìm kiếm nâng cao theo khoảng giá
CREATE INDEX IX_Products_SalePrice        ON dbo.Products (IsVisible, SalePrice);
-- Cảnh báo sắp hết hàng
CREATE INDEX IX_Products_Stock            ON dbo.Products (StockQuantity);

-- Lịch sử mua hàng của khách (đơn mới nhất lên trên)
CREATE INDEX IX_Orders_User_Date   ON dbo.Orders (UserId, OrderDate DESC);
-- Admin lọc đơn theo khoảng thời gian / theo trạng thái + sắp xếp theo phường
CREATE INDEX IX_Orders_Date        ON dbo.Orders (OrderDate);
CREATE INDEX IX_Orders_Status_Ward ON dbo.Orders (Status, Ward);

CREATE INDEX IX_OrderDetails_Product         ON dbo.OrderDetails (ProductId);
CREATE INDEX IX_ImportReceiptDetails_Product ON dbo.ImportReceiptDetails (ProductId);
CREATE INDEX IX_ImportReceipts_Date          ON dbo.ImportReceipts (ImportDate);
CREATE INDEX IX_CartItems_Product            ON dbo.CartItems (ProductId);
-- Tra cứu tồn tại thời điểm & báo cáo nhập - xuất
CREATE INDEX IX_InvTrans_Product_Date ON dbo.InventoryTransactions (ProductId, TransactionDate) INCLUDE (TransactionType, QuantityChange);
GO

/* ---------------------------------------------------------------------
   12. DỮ LIỆU KHỞI TẠO TỐI THIỂU
   Mật khẩu admin mẫu phải được WS băm lại khi triển khai thật.
   --------------------------------------------------------------------- */
INSERT INTO dbo.Users (Username, PasswordHash, FullName, Email, Role)
VALUES ('admin', '$2a$11$QrHImgHb/SF2pGPMj8JIPe8i/lkRh1R7qeBjYGPZWAledzOcn4HQu', N'Quản trị viên', 'admin@coffeeshop.local', 'Admin');

INSERT INTO dbo.Categories (Name, Description) VALUES
 (N'Arabica',        N'Cà phê Arabica: chua thanh, hương thơm, ít đắng'),
 (N'Robusta',        N'Cà phê Robusta: đậm, đắng, nhiều caffeine'),
 (N'Culi',           N'Hạt Culi (Peaberry): hạt tròn, vị đậm đà'),
 (N'Blend - Phối trộn', N'Phối trộn Arabica và Robusta theo tỉ lệ');
GO



INSERT INTO dbo.Users 
(
    Username, PasswordHash, FullName, Email, Phone, 
    Address, Ward, District, City, Role, IsLocked
)
VALUES 
(
    'khachhang1', 
    '$2a$11$aLSKTyjQYupOxB/op5bl2eQ9PnabsWHTGmLENB1jU.pAzZeYFOZfq', -- Mã hash của '123456'
    N'Nguyễn Văn Khách', 
    'khachhang1@gmail.com', 
    '0901234567', 
    N'123 Đường Lê Lợi', 
    N'Phường Bến Nghé', 
    N'Quận 1', 
    N'TP. Hồ Chí Minh', 
    'Customer', 
    0
);
/* =====================================================================
   NHỮNG QUY TẮC FILE NÀY KHÔNG TỰ ĐẢM BẢO ĐƯỢC
   (WS phải làm trong 1 transaction, HOẶC chạy thêm 02_triggers.sql):
   1. Hoàn thành phiếu nhập -> cập nhật giá nhập bình quân + cộng tồn kho
      + ghi sổ kho IMPORT.
   2. Phiếu nhập đã Completed thì không được sửa/xoá phiếu và chi tiết.
   3. Đặt hàng -> kiểm tra đủ hàng, trừ tồn, chụp giá, ghi sổ kho EXPORT,
      tính TotalAmount.
   4. Trạng thái đơn chỉ đi 1 chiều; huỷ đơn -> trả hàng về kho (RETURN).
   5. Xoá sản phẩm: chưa nhập/chưa bán -> xoá hẳn; ngược lại -> ẩn.
      (FK NO ACTION đã chặn xoá hẳn SP đã nhập/bán; WS bắt lỗi 547
       rồi chuyển sang UPDATE IsVisible = 0.)
   ===================================================================== */
