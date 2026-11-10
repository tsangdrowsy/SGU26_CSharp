/* =====================================================================
   File 04 - DỮ LIỆU MẪU: 10 sản phẩm bột cà phê Trung Nguyên
   Chạy SAU: 01_create_tables_constraints.sql (và 02_triggers.sql nếu dùng Phương án B)
   - Chạy được nhiều lần: sản phẩm đã có Code thì bỏ qua (không lỗi UNIQUE).
   - Chạy được cho cả Phương án A (không trigger) và B (có trigger).
   - Giá nhập / giá bán là GIÁ GIẢ LẬP để test, không phải giá niêm yết.
   ===================================================================== */
USE CoffeeShopDb;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;   -- có lỗi bất kỳ -> tự ROLLBACK toàn bộ, không để dữ liệu dở dang

/* ---------- 0. Kiểm tra schema có đủ cột (file 01 phải đã được sửa) ---------- */
IF COL_LENGTH('dbo.Products', 'WeightGram') IS NULL OR COL_LENGTH('dbo.Products', 'RoastLevel') IS NULL
    THROW 50090, N'Bảng Products thiếu cột WeightGram/RoastLevel. Hãy sửa file 01 trước khi chạy file này.', 1;

BEGIN TRANSACTION;

/* ---------- 1. Loại sản phẩm: chỉ thêm nếu chưa có (so theo Name - UQ_Categories_Name) ---------- */
INSERT INTO dbo.Categories (Name, Description)
SELECT v.Name, v.Description
FROM (VALUES
    (N'Arabica',           N'Cà phê Arabica: chua thanh, hương thơm, ít đắng'),
    (N'Robusta',           N'Cà phê Robusta: đậm, đắng, nhiều caffeine'),
    (N'Culi',              N'Hạt Culi (Peaberry): hạt tròn, vị đậm đà'),
    (N'Blend - Phối trộn', N'Phối trộn Arabica và Robusta theo tỉ lệ')
) AS v(Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name);

/* ---------- 2. Nhà cung cấp ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE Name = N'Tập đoàn Trung Nguyên Legend')
    INSERT INTO dbo.Suppliers (Name) VALUES (N'Tập đoàn Trung Nguyên Legend');

DECLARE @SupplierId INT = (SELECT SupplierId FROM dbo.Suppliers WHERE Name = N'Tập đoàn Trung Nguyên Legend');

/* ---------- 3. Sản phẩm ----------
   Dữ liệu viết dạng bảng VALUES, sau đó JOIN với Categories để lấy CategoryId
   theo TÊN loại -> không phụ thuộc vào giá trị IDENTITY (1,2,3...) của từng máy.
   Không ghi SalePrice: đó là cột tính toán, DB tự tính = CostPrice * (100 + ProfitRate) / 100.
   INSERT được phép ghi StockQuantity/CostPrice (trigger T2 chỉ chặn UPDATE). */
INSERT INTO dbo.Products
    (Code, Name, CategoryId, SupplierId, Description, Unit, WeightGram, RoastLevel,
     Origin, ImageUrl, CostPrice, ProfitRate, StockQuantity, IsVisible)
SELECT v.Code, v.Name, c.CategoryId, @SupplierId, v.Description, v.Unit, v.WeightGram, v.RoastLevel,
       N'Buôn Ma Thuột, Đắk Lắk', v.ImageUrl, v.CostPrice, v.ProfitRate, v.StockQuantity, v.IsVisible
FROM (VALUES
    ('TN-ST1-340', N'Cà phê Sáng Tạo 1 - 340g', N'Culi', N'Dòng Sáng Tạo cơ bản, hạt Culi Robusta, vị đậm, đắng mạnh, hợp pha phin truyền thống.', N'Gói', 340, 'Dark', N'/images/products/tn-st1-340.jpg', 52000, 30, 120, 1),
    ('TN-ST2-340', N'Cà phê Sáng Tạo 2 - 340g', N'Blend - Phối trộn', N'Phối trộn Robusta và Arabica, vị đậm vừa, hậu ngọt nhẹ.', N'Gói', 340, 'Medium', N'/images/products/tn-st2-340.jpg', 62000, 30, 80, 1),
    ('TN-ST3-340', N'Cà phê Sáng Tạo 3 - 340g', N'Blend - Phối trộn', N'Phối trộn Arabica và Robusta, hương thơm rõ, vị cân bằng.', N'Gói', 340, 'Medium', N'/images/products/tn-st3-340.jpg', 70000, 30, 8, 1),
    ('TN-ST4-340', N'Cà phê Sáng Tạo 4 - 340g', N'Culi', N'Hạt Culi chọn lọc, vị đậm đà, thơm nồng, nước pha sánh.', N'Gói', 340, 'Dark', N'/images/products/tn-st4-340.jpg', 78000, 30, 60, 1),
    ('TN-ST5-340', N'Cà phê Sáng Tạo 5 - 340g', N'Blend - Phối trộn', N'Phối trộn nhiều giống cà phê, hương thơm phức hợp, vị êm.', N'Gói', 340, 'Medium', N'/images/products/tn-st5-340.jpg', 90000, 25, 45, 1),
    ('TN-ST8-500', N'Cà phê Sáng Tạo 8 - 500g', N'Blend - Phối trộn', N'Dòng cao cấp nhất của Sáng Tạo, hương vị tinh tế, phù hợp làm quà biếu.', N'Hộp', 500, 'Medium', N'/images/products/tn-st8-500.jpg', 280000, 25, 5, 1),
    ('TN-GB-500', N'Cà phê Gourmet Blend - 500g', N'Blend - Phối trộn', N'Phối trộn theo phong cách gourmet, vị hài hoà, dùng pha phin hoặc pha máy.', N'Gói', 500, 'Medium', N'/images/products/tn-gb-500.jpg', 105000, 30, 100, 1),
    ('TN-HB-500', N'Cà phê House Blend - 500g', N'Blend - Phối trộn', N'Phối trộn phổ thông cho quán và gia đình, vị đậm vừa, giá hợp lý.', N'Gói', 500, 'Dark', N'/images/products/tn-hb-500.jpg', 85000, 30, 0, 1),
    ('TN-CP1-500', N'Cà phê Chế Phin 1 - 500g', N'Robusta', N'Robusta rang đậm, vị đắng mạnh, nhiều caffeine, hợp pha cà phê sữa đá.', N'Gói', 500, 'Dark', N'/images/products/tn-cp1-500.jpg', 60000, 35, 150, 1),
    ('TN-CP2-500', N'Cà phê Chế Phin 2 - 500g', N'Robusta', N'Robusta phối Culi, vị đậm, hậu vị kéo dài.', N'Gói', 500, 'Dark', N'/images/products/tn-cp2-500.jpg', 65000, 30, 30, 0)
) AS v (Code, Name, CategoryName, Description, Unit, WeightGram, RoastLevel, ImageUrl,
        CostPrice, ProfitRate, StockQuantity, IsVisible)
JOIN dbo.Categories AS c ON c.Name = v.CategoryName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Products p WHERE p.Code = v.Code);

/* ---------- 4. Sổ kho INITIAL ----------
   Phương án B: trigger trg_Products_InitialStock đã tự ghi -> bỏ qua.
   Phương án A: không có trigger -> ghi tay để báo cáo "tồn tại thời điểm" vẫn đúng. */
IF OBJECT_ID('dbo.trg_Products_InitialStock', 'TR') IS NULL
BEGIN
    INSERT INTO dbo.InventoryTransactions (ProductId, TransactionType, QuantityChange, Note)
    SELECT p.ProductId, 'INITIAL', p.StockQuantity, N'Số lượng ban đầu khi tạo sản phẩm'
    FROM dbo.Products p
    WHERE p.Code LIKE 'TN-%' AND p.StockQuantity > 0
      AND NOT EXISTS (SELECT 1 FROM dbo.InventoryTransactions it
                      WHERE it.ProductId = p.ProductId AND it.TransactionType = 'INITIAL');
END

COMMIT TRANSACTION;
GO

/* ---------- 5. KIỂM TRA KẾT QUẢ ----------
   MONG ĐỢI: 10 dòng; SalePrice khớp cột "Giá bán" trong file Excel;
   SoKho = StockQuantity (riêng TN-HB-500 tồn 0 nên SoKho = 0). */
SELECT p.Code, p.Name, c.Name AS Category, p.WeightGram, p.RoastLevel,
       p.CostPrice, p.ProfitRate, p.SalePrice, p.StockQuantity, p.IsVisible,
       ISNULL((SELECT SUM(it.QuantityChange) FROM dbo.InventoryTransactions it
               WHERE it.ProductId = p.ProductId), 0) AS SoKho
FROM dbo.Products p
JOIN dbo.Categories c ON c.CategoryId = p.CategoryId
WHERE p.Code LIKE 'TN-%'
ORDER BY p.Code;
GO
