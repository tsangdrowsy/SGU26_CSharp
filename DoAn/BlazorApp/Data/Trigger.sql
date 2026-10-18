CREATE TRIGGER trg_RestoreStock_OnCancel
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Chỉ thực hiện khi cột OrderStatus có sự thay đổi
    IF UPDATE(OrderStatus)
    BEGIN
        -- Nếu trạng thái CŨ không phải là Cancelled và trạng thái MỚI là Cancelled
        IF EXISTS (
            SELECT 1 
            FROM inserted i 
            JOIN deleted d ON i.OrderID = d.OrderID 
            WHERE i.OrderStatus = 'Cancelled' AND d.OrderStatus <> 'Cancelled'
        )
        BEGIN
            -- Cộng lại tồn kho cho từng sản phẩm trong đơn hàng bị hủy
            UPDATE p
            SET p.StockQuantity = p.StockQuantity + od.Quantity
            FROM Products p
            JOIN OrderDetails od ON p.ProductID = od.ProductID
            JOIN inserted i ON od.OrderID = i.OrderID
            JOIN deleted d ON i.OrderID = d.OrderID
            WHERE i.OrderStatus = 'Cancelled' AND d.OrderStatus <> 'Cancelled';
        END
    END
END;
GO
CREATE TRIGGER trg_CheckAndDeductStock
ON OrderDetails
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Kiểm tra xem có sản phẩm nào bị đặt vượt quá số lượng tồn không
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN Products p ON i.ProductID = p.ProductID
        WHERE i.Quantity > p.StockQuantity
    )
    BEGIN
        -- Báo lỗi và rollback transaction nếu không đủ hàng
        RAISERROR ('Lỗi: Số lượng đặt hàng vượt quá số lượng tồn kho hiện tại.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 2. Nếu hợp lệ, tự động trừ số lượng tồn kho
    UPDATE p
    SET p.StockQuantity = p.StockQuantity - i.Quantity
    FROM Products p
    JOIN inserted i ON p.ProductID = i.ProductID;
END;
GO