using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data; // Đổi thành namespace dự án của bạn
using WebApplication1.Models;

namespace WebApplication1.Pages.View
{
    public class ProductModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public ProductModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public Product CurrentProduct { get; set; } = default!;
        public List<Product> RelatedProducts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public int Quantity { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id <= 0) return RedirectToPage("/View/Shop");

            // Lấy thông tin sản phẩm (kèm theo Category và Supplier)
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == Id && p.IsVisible);

            if (product == null)
            {
                return RedirectToPage("/View/Shop");
            }

            CurrentProduct = product;

            // Lấy sản phẩm tương tự (cùng danh mục, trừ sản phẩm hiện tại)
            RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != Id && p.IsVisible)
                .OrderBy(p => Guid.NewGuid()) // Tương đương ORDER BY RAND()
                .Take(5)
                .ToListAsync();

            return Page();
        }

        // Xử lý khi bấm nút "Thêm vào giỏ"
        public async Task<IActionResult> OnPostAddToCartAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/View/Login", new { error = "login_required" });
            }

            var product = await _context.Products.FindAsync(Id);
            if (product == null || product.StockQuantity < Quantity)
            {
                TempData["Error"] = "Sản phẩm không đủ số lượng trong kho!";
                return RedirectToPage(new { id = Id });
            }

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == Id);

            if (cartItem != null)
            {
                // Nếu có rồi thì cộng thêm số lượng
                if (cartItem.Quantity + Quantity > product.StockQuantity)
                {
                    TempData["Error"] = "Số lượng vượt quá tồn kho!";
                    return RedirectToPage(new { id = Id });
                }
                cartItem.Quantity += Quantity;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                // Nếu chưa có thì tạo mới
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId.Value,
                    ProductId = Id,
                    Quantity = Quantity,
                    AddedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";
            return RedirectToPage(new { id = Id });
        }

        // Xử lý khi bấm nút "Mua ngay"
        public async Task<IActionResult> OnPostBuyNowAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/View/Login", new { error = "login_required" });
            }

            // Thêm vào giỏ trước, sau đó chuyển thẳng sang trang Thanh toán
            var result = await OnPostAddToCartAsync();
            if (result is RedirectToPageResult)
            {
                return RedirectToPage("/View/Cart"); // Tạm thời chuyển sang giỏ hàng
            }
            return Page();
        }
    }
}