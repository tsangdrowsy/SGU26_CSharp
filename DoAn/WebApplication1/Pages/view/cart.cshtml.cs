using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data; // Đổi thành namespace dự án của bạn
using WebApplication1.Models;

namespace WebApplication1.Pages.View
{
    public class CartModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public CartModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }

        // Class phụ để hiển thị thông tin ra giao diện
        public class CartItemViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string? ImageUrl { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int Stock { get; set; }
            public decimal Subtotal => Price * Quantity;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng!";
                return RedirectToPage("/View/Login");
            }

            // 2. Tải giỏ hàng
            await LoadCartAsync(userId.Value);
            return Page();
        }

        private async Task LoadCartAsync(int userId)
        {
            var cartDb = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Chỉ lấy những sản phẩm còn đang bán (IsVisible = true)
            CartItems = cartDb
                .Where(c => c.Product != null && c.Product.IsVisible)
                .Select(c => new CartItemViewModel
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product.Name,
                    ImageUrl = c.Product.ImageUrl,
                    Price = c.Product.SalePrice,
                    Quantity = c.Quantity,
                    Stock = c.Product.StockQuantity
                }).ToList();

            TotalAmount = CartItems.Sum(c => c.Subtotal);
            TotalItems = CartItems.Sum(c => c.Quantity);
        }

        // Cập nhật số lượng
        public async Task<IActionResult> OnPostUpdateCartAsync(Dictionary<int, int> quantities)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            if (quantities != null)
            {
                foreach (var item in quantities)
                {
                    var cartItem = await _context.CartItems
                        .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == item.Key);

                    if (cartItem != null)
                    {
                        if (item.Value > 0)
                        {
                            cartItem.Quantity = item.Value;
                            _context.CartItems.Update(cartItem);
                        }
                        else
                        {
                            _context.CartItems.Remove(cartItem);
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/View/Cart");
        }

        // Xóa 1 sản phẩm
        public async Task<IActionResult> OnPostRemoveItemAsync(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/View/Cart");
        }

        // Xóa toàn bộ giỏ hàng
        public async Task<IActionResult> OnPostClearCartAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/View/Cart");
        }
    }
}