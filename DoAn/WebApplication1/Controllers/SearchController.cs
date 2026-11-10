using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly CoffeeShopContext _context;

        public SearchController(CoffeeShopContext context)
        {
            _context = context;
        }

        // GET: /api/search?q=keyword
        [HttpGet]
        public async Task<IActionResult> GetSuggestions([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 1)
            {
                return Ok(new { success = false, data = new List<object>() });
            }

            // Tìm kiếm sản phẩm trong DB (giới hạn 8 kết quả)
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.Name.Contains(q) && p.IsVisible)
                .OrderByDescending(p => p.ProductId)
                .Take(8)
                .ToListAsync();

            // Map dữ liệu sang JSON để JS đọc được
            var data = products.Select(p => new
            {
                id = p.ProductId,
                name = p.Name,
                price = p.SalePrice.ToString("N0") + "₫",
                old_price = (string)null, // Database của bạn không có giá cũ, để null
                discount = 0,             // Để 0
                category = p.Category?.Name ?? "Không rõ",
                brand = p.Supplier?.Name ?? "",
                image = string.IsNullOrEmpty(p.ImageUrl) ? "img/sanpham/placeholder.png" : p.ImageUrl,
                url = $"/View/Product?id={p.ProductId}", // Đường dẫn tới trang chi tiết
                inStock = p.StockQuantity > 0,
                stock_quantity = p.StockQuantity
            }).ToList();

            return Ok(new
            {
                success = true,
                data = data,
                count = data.Count
            });
        }
    }
}