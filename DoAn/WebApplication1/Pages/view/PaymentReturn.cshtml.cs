using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Pages.View
{
    // VNPay chuyển trình duyệt của khách về trang này sau khi thanh toán (vnp_ReturnUrl).
    // Trang này không hiển thị gì: chỉ kiểm tra kết quả rồi chuyển hướng tiếp.
    public class PaymentReturnModel : PageModel
    {
        private readonly CoffeeShopContext _context;
        private readonly IVnPayService _vnPay;
        private readonly ILogger<PaymentReturnModel> _logger;

        public PaymentReturnModel(CoffeeShopContext context, IVnPayService vnPay, ILogger<PaymentReturnModel> logger)
        {
            _context = context;
            _vnPay = vnPay;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var result = _vnPay.ValidateResponse(Request.Query);

            // 1. Chữ ký sai = URL bị sửa (vd: đổi ResponseCode thành 00) -> không tin bất cứ tham số nào
            if (!result.IsValidSignature)
            {
                _logger.LogWarning("VNPay return: sai chữ ký, TxnRef={TxnRef}", result.TxnRef);
                TempData["PaymentError"] = "Dữ liệu thanh toán không hợp lệ.";
                return RedirectToPage("/View/Checkout");
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderCode == result.TxnRef);
            if (order == null)
            {
                TempData["PaymentError"] = "Không tìm thấy đơn hàng.";
                return RedirectToPage("/View/Checkout");
            }

            // 2. Đơn đã được xử lý trước đó (vd: khách bấm F5 trang này) -> không xử lý lại lần 2 (idempotent)
            if (order.PaymentMethod != "Online" || order.Status != "Pending")
            {
                return order.Status == "Cancelled"
                    ? RedirectToPage("/View/Checkout")
                    : RedirectToPage("/View/OrderSuccess", new { orderId = order.OrderId });
            }

            // 3. Số tiền VNPay xác nhận phải khớp với tổng tiền đơn trong DB
            var amountMatches = result.Amount == (long)(order.TotalAmount * 100);

            if (result.IsSuccess && amountMatches)
            {
                // Thanh toán thành công: Pending -> Confirmed (trạng thái hợp lệ theo trigger T9).
                // Nhờ vậy khách không tự "Hủy đơn" được nữa (nút Hủy chỉ hiện với Pending).
                order.Status = "Confirmed";
                order.UpdatedAt = DateTime.Now;

                // Chỉ xóa giỏ hàng khi đã trả tiền xong
                var cartItems = _context.CartItems.Where(c => c.UserId == order.UserId);
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();
                _logger.LogInformation("VNPay: đơn {OrderCode} thanh toán thành công, mã GD VNPay {TransactionNo}",
                    order.OrderCode, result.TransactionNo);

                return RedirectToPage("/View/OrderSuccess", new { orderId = order.OrderId });
            }

            // 4. Thất bại / khách hủy: hủy đơn -> trigger tự trả hàng về kho. Giỏ hàng vẫn còn để thử lại.
            if (!amountMatches)
            {
                _logger.LogWarning("VNPay: lệch số tiền đơn {OrderCode}: VNPay={Amount}, DB={Total}",
                    order.OrderCode, result.Amount, order.TotalAmount);
            }

            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["PaymentError"] = amountMatches
                ? VnPayService.DescribeResponseCode(result.ResponseCode) + ". Đơn hàng đã được hủy, bạn có thể đặt lại."
                : "Số tiền thanh toán không khớp với đơn hàng. Đơn hàng đã được hủy.";
            return RedirectToPage("/View/Checkout");
        }
    }
}
