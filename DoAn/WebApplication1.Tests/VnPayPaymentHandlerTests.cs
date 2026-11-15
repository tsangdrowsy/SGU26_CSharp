using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Tests;

// UT_06 - UT_09: cần SQL Server thật (Docker) vì handler dùng transaction, ExecuteUpdateAsync
// và trigger trg_Orders_StatusFlow trả hàng về kho. Provider InMemory KHÔNG chạy được các lệnh này.
//
// Trước khi chạy:  set biến môi trường COFFEESHOP_TEST_DB, ví dụ (PowerShell):
//   $env:COFFEESHOP_TEST_DB="Server=localhost,1433;Database=CoffeeShopDb;User Id=sa;Password=<mk>;TrustServerCertificate=True"
// Dữ liệu cần có: tài khoản khachhang1 và ít nhất 1 sản phẩm đang bán còn >= 5 trong kho (script 01 + 04).
// Lưu ý: test tạo thêm đơn hàng "UT..." thật trong CSDL (đơn không xoá được do trigger) -> nên chạy trên CSDL Docker,
// xong thì "docker compose down -v" để làm sạch.
public class VnPayPaymentHandlerTests
{
    private static CoffeeShopContext NewContext()
    {
        var cs = Environment.GetEnvironmentVariable("COFFEESHOP_TEST_DB")
                 ?? throw new InvalidOperationException("Chưa đặt biến môi trường COFFEESHOP_TEST_DB.");
        return new CoffeeShopContext(new DbContextOptionsBuilder<CoffeeShopContext>().UseSqlServer(cs).Options);
    }

    private static VnPayPaymentHandler NewHandler(CoffeeShopContext ctx) =>
        new(ctx, NullLogger<VnPayPaymentHandler>.Instance);

    // Tạo dữ liệu giống luồng Checkout: 1 dòng giỏ hàng + 1 đơn Online Pending (trigger trừ kho, tính tổng tiền)
    private static async Task<(Order order, int productId, int userId, int stockBefore)> CreatePendingOnlineOrderAsync()
    {
        await using var ctx = NewContext();
        var user = await ctx.Users.SingleAsync(u => u.Username == "khachhang1");
        var product = await ctx.Products.AsNoTracking()
            .FirstAsync(p => p.IsVisible && p.StockQuantity >= 5);

        if (!await ctx.CartItems.AnyAsync(c => c.UserId == user.UserId && c.ProductId == product.ProductId))
        {
            ctx.CartItems.Add(new CartItem { UserId = user.UserId, ProductId = product.ProductId, Quantity = 1, AddedAt = DateTime.Now });
        }

        var order = new Order
        {
            OrderCode = ("UT" + Guid.NewGuid().ToString("N"))[..20],   // cột OrderCode VARCHAR(20)
            UserId = user.UserId, OrderDate = DateTime.Now,
            ReceiverName = "Unit Test", ReceiverPhone = "0901234567",
            ShippingAddress = "1 Test", Ward = "P1", District = "Q1", City = "HCM",
            PaymentMethod = "Online", Status = "Pending", TotalAmount = 0,
        };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        ctx.OrderDetails.Add(new OrderDetail { OrderId = order.OrderId, ProductId = product.ProductId, Quantity = 1 });
        await ctx.SaveChangesAsync();
        await ctx.Entry(order).ReloadAsync();                            // lấy TotalAmount do trigger tính

        return (order, product.ProductId, user.UserId, product.StockQuantity);
    }

    private static VnPayResult Result(Order o, bool success, long? amount = null) => new(
        IsValidSignature: true, IsSuccess: success, TxnRef: o.OrderCode,
        Amount: amount ?? (long)(o.TotalAmount * 100),
        ResponseCode: success ? "00" : "24", TransactionNo: "14123456");

    [Fact] // UT_06
    public async Task ThanhToanThanhCong_DonConfirmed_XoaGioHang()
    {
        var (order, _, userId, _) = await CreatePendingOnlineOrderAsync();

        await using var ctx = NewContext();
        var r = await NewHandler(ctx).HandleAsync(Result(order, success: true), "IPN");

        await using var check = NewContext();
        Assert.Equal(PaymentOutcome.Paid, r.Outcome);
        Assert.Equal("Confirmed", (await check.Orders.SingleAsync(o => o.OrderId == order.OrderId)).Status);
        Assert.False(await check.CartItems.AnyAsync(c => c.UserId == userId));
    }

    [Fact] // UT_07
    public async Task GoiLaiLanHai_AlreadyProcessed()
    {
        var (order, _, _, _) = await CreatePendingOnlineOrderAsync();

        await using (var ctx1 = NewContext()) await NewHandler(ctx1).HandleAsync(Result(order, true), "IPN");
        await using var ctx2 = NewContext();
        var second = await NewHandler(ctx2).HandleAsync(Result(order, true), "Return");

        Assert.Equal(PaymentOutcome.AlreadyProcessed, second.Outcome);
    }

    [Fact] // UT_08
    public async Task LechSoTien_AmountMismatch_DonVanPending()
    {
        var (order, _, _, _) = await CreatePendingOnlineOrderAsync();

        await using var ctx = NewContext();
        var r = await NewHandler(ctx).HandleAsync(Result(order, true, amount: 100), "IPN");

        await using var check = NewContext();
        Assert.Equal(PaymentOutcome.AmountMismatch, r.Outcome);
        Assert.Equal("Pending", (await check.Orders.SingleAsync(o => o.OrderId == order.OrderId)).Status);
    }

    [Fact] // UT_09
    public async Task ThanhToanThatBai_DonCancelled_TraHang_GiuGio()
    {
        var (order, productId, userId, stockBefore) = await CreatePendingOnlineOrderAsync();

        await using var ctx = NewContext();
        var r = await NewHandler(ctx).HandleAsync(Result(order, success: false), "IPN");

        await using var check = NewContext();
        Assert.Equal(PaymentOutcome.Failed, r.Outcome);
        Assert.Equal("Cancelled", (await check.Orders.SingleAsync(o => o.OrderId == order.OrderId)).Status);
        Assert.Equal(stockBefore, (await check.Products.SingleAsync(p => p.ProductId == productId)).StockQuantity);
        Assert.True(await check.CartItems.AnyAsync(c => c.UserId == userId));
    }
}
