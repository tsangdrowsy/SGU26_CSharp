using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Services.VnPay
{
    public enum PaymentOutcome
    {
        InvalidSignature,   // chữ ký sai -> dữ liệu bị sửa, không tin
        OrderNotFound,
        AmountMismatch,     // số tiền VNPay báo khác tổng tiền trong DB
        AlreadyProcessed,   // đơn đã xử lý rồi (IPN và Return cùng tới, hoặc VNPay gọi lại IPN)
        Paid,               // vừa xác nhận thanh toán thành công
        Failed              // khách hủy / thẻ lỗi -> đã hủy đơn
    }

    public record PaymentHandleResult(PaymentOutcome Outcome, int? OrderId, string? OrderStatus, string ResponseCode);

    // Logic dùng chung cho cả Return URL (trình duyệt) và IPN (server VNPay gọi).
    // Cả hai có thể tới gần như cùng lúc nên mọi thao tác phải idempotent (gọi nhiều lần vẫn đúng).
    public class VnPayPaymentHandler
    {
        private readonly CoffeeShopContext _context;
        private readonly ILogger<VnPayPaymentHandler> _logger;

        public VnPayPaymentHandler(CoffeeShopContext context, ILogger<VnPayPaymentHandler> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentHandleResult> HandleAsync(VnPayResult result, string source)
        {
            if (!result.IsValidSignature)
            {
                _logger.LogWarning("[{Source}] VNPay sai chữ ký, TxnRef={TxnRef}", source, result.TxnRef);
                return new(PaymentOutcome.InvalidSignature, null, null, result.ResponseCode);
            }

            var order = await _context.Orders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderCode == result.TxnRef && o.PaymentMethod == "Online");
            if (order == null)
            {
                return new(PaymentOutcome.OrderNotFound, null, null, result.ResponseCode);
            }

            if (order.Status != "Pending")
            {
                return new(PaymentOutcome.AlreadyProcessed, order.OrderId, order.Status, result.ResponseCode);
            }

            if (result.Amount != (long)(order.TotalAmount * 100))
            {
                // Không tự đổi trạng thái: để admin kiểm tra thủ công
                _logger.LogWarning("[{Source}] VNPay lệch số tiền đơn {OrderCode}: VNPay={Amount}, DB={Total}",
                    source, order.OrderCode, result.Amount, order.TotalAmount);
                return new(PaymentOutcome.AmountMismatch, order.OrderId, order.Status, result.ResponseCode);
            }

            var newStatus = result.IsSuccess ? "Confirmed" : "Cancelled";

            await using var tx = await _context.Database.BeginTransactionAsync();

            // UPDATE ... WHERE Status = 'Pending': câu lệnh nguyên tử (atomic) ở DB.
            // Nếu Return và IPN chạy song song, chỉ 1 bên cập nhật được (rows = 1), bên kia nhận rows = 0.
            // Đọc rồi mới ghi (if Status == Pending -> SaveChanges) sẽ bị race condition: cả 2 cùng thấy Pending.
            var rows = await _context.Orders
                .Where(o => o.OrderId == order.OrderId && o.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.Status, newStatus)
                    .SetProperty(o => o.UpdatedAt, DateTime.Now));

            if (rows == 0)
            {
                await tx.RollbackAsync();
                return new(PaymentOutcome.AlreadyProcessed, order.OrderId, null, result.ResponseCode);
            }

            if (result.IsSuccess)
            {
                // Đã trả tiền -> xóa giỏ hàng. Thất bại thì giữ giỏ để khách đặt lại.
                await _context.CartItems.Where(c => c.UserId == order.UserId).ExecuteDeleteAsync();
            }
            // Status = Cancelled -> trigger trg_Orders_StatusFlow tự trả hàng về kho

            await tx.CommitAsync();

            _logger.LogInformation("[{Source}] VNPay đơn {OrderCode} -> {Status} (mã GD {TransactionNo}, RspCode {Code})",
                source, order.OrderCode, newStatus, result.TransactionNo, result.ResponseCode);

            return new(result.IsSuccess ? PaymentOutcome.Paid : PaymentOutcome.Failed,
                order.OrderId, newStatus, result.ResponseCode);
        }
    }
}
