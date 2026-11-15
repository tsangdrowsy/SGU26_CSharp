using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services.VnPay;

namespace WebApplication1.Pages.View
{
    public class OrderSuccessModel : PageModel
    {
        private readonly CoffeeShopContext _context;
        private readonly OrderViewTokenService _orderToken;

        public OrderSuccessModel(CoffeeShopContext context, OrderViewTokenService orderToken)
        {
            _context = context;
            _orderToken = orderToken;
        }

        public Order CurrentOrder { get; set; } = default!;

        // t: token do PaymentController tạo sau khi thanh toán VNPay (có hạn 30 phút)
        public async Task<IActionResult> OnGetAsync(int orderId, string? t)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            bool hasValidToken = _orderToken.IsValid(t, orderId);

            // Cho xem nếu: đang đăng nhập đúng chủ đơn, HOẶC có token hợp lệ cho đúng đơn này
            if (userId == null && !hasValidToken) return RedirectToPage("/Index");

            // Lấy thông tin đơn hàng vừa tạo
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && (hasValidToken || o.UserId == userId));

            if (order == null) return RedirectToPage("/Index");

            CurrentOrder = order;
            return Page();
        }
    }
}