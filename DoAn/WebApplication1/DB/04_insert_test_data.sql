/* =====================================================================
   THÊM SẢN PHẨM MẪU ĐỂ TEST GIAO DIỆN WEB
   ===================================================================== */
USE CoffeeShopDb;
GO

-- Lấy ID của các danh mục đã tạo ở trên
DECLARE @CatRobusta INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Robusta');
DECLARE @CatArabica INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Arabica');
DECLARE @CatCuli INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = N'Culi');

-- 1. Cà phê Robusta
INSERT INTO dbo.Products 
(Code, Name, CategoryId, Description, Unit, CostPrice, ProfitRate, StockQuantity, IsVisible, WeightGram, RoastLevel, Origin, ImageUrl)
VALUES 
('CF-ROB-01', N'Cà phê Robusta Buôn Ma Thuột', @CatRobusta, 
 N'Hương vị đậm đà, đắng nhẹ, hậu vị kéo dài. Thích hợp pha phin truyền thống.', 
 N'Gói 500g', 100000, 20, 50, 1, 500, 'Dark', N'Đắk Lắk', 'img/sanpham/coffee-1.jpg');

-- 2. Cà phê Arabica
INSERT INTO dbo.Products 
(Code, Name, CategoryId, Description, Unit, CostPrice, ProfitRate, StockQuantity, IsVisible, WeightGram, RoastLevel, Origin, ImageUrl)
VALUES 
('CF-ARA-01', N'Cà phê Arabica Cầu Đất', @CatArabica, 
 N'Chua thanh, hương thơm hoa quả, ít đắng. Phù hợp pha máy Espresso.', 
 N'Gói 250g', 150000, 30, 30, 1, 250, 'Medium', N'Lâm Đồng', 'img/sanpham/coffee-2.jpg');

-- 3. Cà phê Culi
INSERT INTO dbo.Products 
(Code, Name, CategoryId, Description, Unit, CostPrice, ProfitRate, StockQuantity, IsVisible, WeightGram, RoastLevel, Origin, ImageUrl)
VALUES 
('CF-CULI-01', N'Cà phê Culi Đắk Nông', @CatCuli, 
 N'Hạt culi tròn đều, vị đậm đà, hàm lượng cafein cao.', 
 N'Gói 500g', 120000, 25, 100, 1, 500, 'Dark', N'Đắk Nông', 'img/sanpham/coffee-3.jpg');

-- 4. Cà phê Blend
INSERT INTO dbo.Products 
(Code, Name, CategoryId, Description, Unit, CostPrice, ProfitRate, StockQuantity, IsVisible, WeightGram, RoastLevel, Origin, ImageUrl)
VALUES 
('CF-BLEND-01', N'Cà phê Blend Đặc Biệt', @CatRobusta, 
 N'Sự kết hợp hoàn hảo giữa Arabica và Robusta. Vị cân bằng, dễ uống.', 
 N'Gói 500g', 130000, 20, 0, 1, 500, 'Medium', N'Việt Nam', 'img/sanpham/coffee-4.jpg');
GO