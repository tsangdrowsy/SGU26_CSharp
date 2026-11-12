using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Pages.View
{
    public class OrdersModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public OrdersModel(CoffeeShopContext context)
        {
            _context = context;
        }

        // Thêm = null!; để tránh cảnh báo
        public User CurrentUser { get; set; } = null!;
        public IList<Order> OrdersList { get; set; } = new List<Order>();

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }
        public int PageSize { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/View/Login");
            }

            // --- BỔ SUNG: Lấy thông tin User cho Sidebar ---
            CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (CurrentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/View/Login");
            }

            // --- Xây dựng câu truy vấn Đơn hàng ---
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            if (StatusFilter != "all" && !string.IsNullOrEmpty(StatusFilter))
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            if (!string.IsNullOrEmpty(SearchQuery) && int.TryParse(SearchQuery, out int searchId))
            {
                query = query.Where(o => o.OrderId == searchId);
            }

            TotalOrders = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalOrders / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            OrdersList = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return Page();
        }

        // --- BỔ SUNG: Hàm xử lý Hủy Đơn ---
        public async Task<IActionResult> OnPostCancelAsync(int orderId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            // Chỉ cho phép hủy nếu đơn đang ở trạng thái Pending
            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;

                // (Tùy chọn) Hoàn lại số lượng tồn kho nếu bạn có xử lý trừ kho lúc đặt hàng
                // foreach (var detail in order.OrderDetails) { ... cộng lại StockQuantity ... }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{orderId} thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng này.";
            }

            // Tải lại trang hiện tại
            return RedirectToPage(new { status = StatusFilter, search = SearchQuery, page = CurrentPage });
        }

        // Helpers
        public string GetStatusText(string status) => status switch { "Pending" => "Chờ xác nhận", "Confirmed" => "Đã xác nhận", "Delivered" => "Đã giao hàng", "Cancelled" => "Đã hủy", _ => "Không xác định" };
        public string GetStatusBadgeClass(string status) => status switch { "Pending" => "bg-yellow-100 text-yellow-800", "Confirmed" => "bg-blue-100 text-blue-800", "Delivered" => "bg-green-100 text-green-800", "Cancelled" => "bg-red-100 text-red-800", _ => "bg-gray-100 text-gray-800" };
        public string GetStatusIcon(string status) => status switch { "Pending" => "fa-clock", "Confirmed" => "fa-check-circle", "Delivered" => "fa-truck", "Cancelled" => "fa-times-circle", _ => "fa-question-circle" };
    }
}