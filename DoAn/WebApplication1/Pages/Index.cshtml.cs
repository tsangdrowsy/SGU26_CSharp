using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public IndexModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public IList<Category> Categories { get; set; } = default!;
        public IList<Product> FeaturedProducts { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // 1. Lấy 4 danh mục đang hoạt động
            Categories = await _context.Categories
                .Where(c => c.IsActive)
                .Take(4)
                .ToListAsync();

            // 2. Lấy 4 sản phẩm mới nhất đang được bán (IsVisible = true)
            FeaturedProducts = await _context.Products
                .Include(p => p.Category) // Lấy kèm tên danh mục
                .Where(p => p.IsVisible)
                .OrderByDescending(p => p.CreatedAt) // Sản phẩm mới nhất lên đầu
                .Take(4)
                .ToListAsync();
        }

        public IActionResult OnGetLogout()
        {
            // Xóa toàn bộ Session
            HttpContext.Session.Clear();

            // Chuyển hướng về trang chủ
            return RedirectToPage("/Index");
        }
    }
}