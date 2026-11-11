using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.View
{
    public class OrderSuccessModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public OrderSuccessModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public Order CurrentOrder { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/Index");

            // Lấy thông tin đơn hàng vừa tạo
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null) return RedirectToPage("/Index");

            CurrentOrder = order;
            return Page();
        }
    }
}