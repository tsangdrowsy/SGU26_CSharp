using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Controllers
{
    [Route("api/payment")]
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPay;
        private readonly VnPayPaymentHandler _handler;
        private readonly ILogger<PaymentController> _logger;
        private readonly OrderViewTokenService _orderToken;

        public PaymentController(IVnPayService vnPay, VnPayPaymentHandler handler, ILogger<PaymentController> logger,
            OrderViewTokenService orderToken)
        {
            _vnPay = vnPay;
            _handler = handler;
            _logger = logger;
            _orderToken = orderToken;
        }

        // Kèm token để OrderSuccess hiển thị được cả khi session bị mất (khác domain, cookie hết hạn)
        private IActionResult RedirectToOrderSuccess(int orderId) =>
            RedirectToPage("/View/OrderSuccess", new { orderId, t = _orderToken.Create(orderId) });

        // ===== RETURN URL: trình duyệt của khách được VNPay chuyển về đây =====
        // Mục đích chính: hiển thị kết quả cho khách. Vẫn cập nhật đơn (idempotent) để chạy được cả khi IPN chưa tới.
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var r = await _handler.HandleAsync(_vnPay.ValidateResponse(Request.Query), "Return");

            switch (r.Outcome)
            {
                case PaymentOutcome.Paid:
                    return RedirectToOrderSuccess(r.OrderId!.Value);

                case PaymentOutcome.AlreadyProcessed when r.OrderStatus != "Cancelled":
                    // IPN đã xử lý trước (hoặc khách bấm F5)
                    return RedirectToOrderSuccess(r.OrderId!.Value);

                case PaymentOutcome.Failed:
                case PaymentOutcome.AlreadyProcessed:
                    TempData["PaymentError"] = VnPayService.DescribeResponseCode(r.ResponseCode)
                        + ". Đơn hàng đã được hủy, giỏ hàng vẫn được giữ để bạn đặt lại.";
                    break;

                case PaymentOutcome.AmountMismatch:
                    TempData["PaymentError"] = "Số tiền thanh toán không khớp với đơn hàng. Vui lòng liên hệ cửa hàng.";
                    break;

                default: // InvalidSignature, OrderNotFound
                    TempData["PaymentError"] = "Dữ liệu thanh toán không hợp lệ.";
                    break;
            }
            return RedirectToPage("/View/Checkout");
        }

        // ===== IPN: server VNPay gọi thẳng vào đây (server-to-server), không qua trình duyệt =====
        // Phải trả JSON { RspCode, Message } đúng mã VNPay quy định. Nếu không nhận được "00"/"02",
        // VNPay sẽ gọi lại nhiều lần.
        [HttpGet("vnpay-ipn")]
        public async Task<IActionResult> VnPayIpn()
        {
            try
            {
                var r = await _handler.HandleAsync(_vnPay.ValidateResponse(Request.Query), "IPN");

                return r.Outcome switch
                {
                    PaymentOutcome.InvalidSignature => Ipn("97", "Invalid signature"),
                    PaymentOutcome.OrderNotFound => Ipn("01", "Order not found"),
                    PaymentOutcome.AmountMismatch => Ipn("04", "Invalid amount"),
                    PaymentOutcome.AlreadyProcessed => Ipn("02", "Order already confirmed"),
                    _ => Ipn("00", "Confirm Success") // Paid hoặc Failed: đã ghi nhận kết quả
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VNPay IPN lỗi");
                return Ipn("99", "Unknown error");
            }
        }

        // new JsonSerializerOptions(): giữ nguyên tên "RspCode". Mặc định ASP.NET đổi sang camelCase ("rspCode")
        // và VNPay sẽ không đọc được -> coi như IPN thất bại.
        private static JsonResult Ipn(string code, string message) =>
            new(new { RspCode = code, Message = message }, new JsonSerializerOptions());
    }
}
