

-- 1.1 Ràng buộc cho bảng Products (Sản phẩm)
-- Giá bán phải lớn hơn 0 và số lượng tồn kho không được phép âm
ALTER TABLE Products
ADD CONSTRAINT CHK_Product_Price CHECK (Price > 0),
    CONSTRAINT CHK_Product_Stock CHECK (StockQuantity >= 0);
GO

-- 1.2 Ràng buộc cho bảng CartItems (Chi tiết giỏ hàng)
-- Số lượng thêm vào giỏ phải từ 1 trở lên
ALTER TABLE CartItems
ADD CONSTRAINT CHK_CartItem_Quantity CHECK (Quantity > 0);
GO

-- 1.3 Ràng buộc cho bảng OrderDetails (Chi tiết đơn hàng)
-- Số lượng mua phải lớn hơn 0, đơn giá lúc mua không được âm
ALTER TABLE OrderDetails
ADD CONSTRAINT CHK_OrderDetail_Quantity CHECK (Quantity > 0),
    CONSTRAINT CHK_OrderDetail_Price CHECK (UnitPrice >= 0);
GO

-- 1.4 Ràng buộc cho bảng Orders (Đơn hàng)
-- Tổng tiền không được âm và Trạng thái đơn hàng phải nằm trong danh sách cho phép
ALTER TABLE Orders
ADD CONSTRAINT CHK_Order_Total CHECK (TotalAmount >= 0),
    CONSTRAINT CHK_Order_Status CHECK (OrderStatus IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'));
GO

-- 1.5 Ràng buộc cơ bản cho bảng Users (Người dùng)
-- Kiểm tra định dạng Email có chứa ký tự '@' và dấu '.'
ALTER TABLE Users
ADD CONSTRAINT CHK_User_Email CHECK (Email LIKE '%_@__%.__%');
GO