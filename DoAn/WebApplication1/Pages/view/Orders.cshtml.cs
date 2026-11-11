using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.View
{
    public class OrdersModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public OrdersModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public IList<Order> OrdersList { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }
        public int TotalOrders { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/View/Login", new { error = "login_required" });
            }

            // 2. Xây dựng câu truy vấn
            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .AsQueryable();

            // 3. Lọc theo trạng thái (SQL Server lưu Status dạng chuỗi: Pending, Confirmed, Delivered, Cancelled)
            if (StatusFilter != "all" && !string.IsNullOrEmpty(StatusFilter))
            {
                query = query.Where(o => o.Status == StatusFilter);
            }

            // 4. Tìm kiếm theo mã đơn hàng (Chỉ tìm số)
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                // Ép kiểu OrderId sang chuỗi để tìm kiếm tương đối
                query = query.Where(o => o.OrderId.ToString().Contains(SearchQuery));
            }

            // 5. Đếm tổng số đơn và tính số trang
            TotalOrders = await query.CountAsync();
            int pageSize = 10;
            TotalPages = (int)Math.Ceiling(TotalOrders / (double)pageSize);

            // Đảm bảo trang hiện tại hợp lệ
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // 6. Lấy dữ liệu phân trang
            OrdersList = await query
                .OrderByDescending(o => o.OrderDate) // Đơn mới nhất lên đầu
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Page();
        }

        // Hàm hỗ trợ hiển thị màu sắc trạng thái
        public string GetStatusBadgeClass(string status)
        {
            return status switch
            {
                "Pending" => "bg-yellow-100 text-yellow-800",
                "Confirmed" => "bg-blue-100 text-blue-800",
                "Delivered" => "bg-green-100 text-green-800",
                "Cancelled" => "bg-red-100 text-red-800",
                _ => "bg-gray-100 text-gray-800"
            };
        }

        public string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "Chờ xác nhận",
                "Confirmed" => "Đã xác nhận",
                "Delivered" => "Đã giao hàng",
                "Cancelled" => "Đã hủy",
                _ => "Không xác định"
            };
        }

        public string GetStatusIcon(string status)
        {
            return status switch
            {
                "Pending" => "fa-clock",
                "Confirmed" => "fa-check-circle",
                "Delivered" => "fa-truck",
                "Cancelled" => "fa-times-circle",
                _ => "fa-question-circle"
            };
        }
    }
}