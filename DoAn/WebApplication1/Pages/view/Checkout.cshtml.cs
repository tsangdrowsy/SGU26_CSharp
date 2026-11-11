using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Pages.View
{
    public class CheckoutModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public CheckoutModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public List<CartItem> CartItems { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public User CurrentUser { get; set; } = default!;

        // Các trường dữ liệu từ Form
        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string ReceiverName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10 số")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Tỉnh/Thành phố")]
        public string City { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Quận/Huyện")]
        public string District { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Phường/Xã")]
        public string Ward { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ chi tiết")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        public string PaymentMethod { get; set; } = "Cash"; // Mặc định là COD

        [BindProperty]
        public string? Note { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToPage("/View/Login", new { error = "login_required" });
            }

            // Lấy thông tin user để điền sẵn vào form
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return RedirectToPage("/Index");

            CurrentUser = user;
            ReceiverName = user.FullName;
            ReceiverPhone = user.Phone ?? "";
            City = user.City ?? "";
            District = user.District ?? "";
            Ward = user.Ward ?? "";
            Address = user.Address ?? "";

            // Lấy giỏ hàng từ Database
            CartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!CartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                return RedirectToPage("/View/Cart");
            }

            TotalAmount = CartItems.Sum(c => (c.Product?.SalePrice ?? 0) * c.Quantity);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            if (!ModelState.IsValid)
            {
                await LoadDataForPage(userId.Value);
                return Page();
            }

            // Lấy lại giỏ hàng để kiểm tra
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToPage("/View/Cart");

            try
            {
                // 1. Tạo đơn hàng mới (Bảng Orders)
                var order = new Order
                {
                    OrderCode = "DH" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(10, 99),
                    UserId = userId.Value,
                    OrderDate = DateTime.Now,
                    ReceiverName = ReceiverName,
                    ReceiverPhone = ReceiverPhone,
                    ShippingAddress = Address,
                    Ward = Ward,
                    District = District,
                    City = City,
                    PaymentMethod = PaymentMethod,
                    Status = "Pending", // Trạng thái mặc định
                    Note = Note,
                    TotalAmount = 0 // Tạm tính, Trigger sẽ tự cập nhật lại sau
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Lưu để lấy OrderId

                // 2. Tạo chi tiết đơn hàng (Bảng OrderDetails)
                foreach (var item in cartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = 0, // Trigger sẽ tự lấy giá đúng
                        UnitCost = 0   // Trigger sẽ tự lấy giá vốn
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // Lưu ý: Lúc này SQL Trigger sẽ tự động chạy: 
                // - Trừ tồn kho
                // - Cập nhật TotalAmount của Order
                // - Ghi log vào InventoryTransactions
                await _context.SaveChangesAsync();

                // 3. Xóa giỏ hàng sau khi đặt thành công
                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                // 4. Chuyển sang trang Thông báo thành công
                return RedirectToPage("/View/OrderSuccess", new { orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống khi đặt hàng: " + ex.Message;
                await LoadDataForPage(userId.Value);
                return Page();
            }
        }

        // Hàm hỗ trợ load lại dữ liệu nếu có lỗi
        private async Task LoadDataForPage(int userId)
        {
            CurrentUser = await _context.Users.FindAsync(userId) ?? new User();
            CartItems = await _context.CartItems.Include(c => c.Product).Where(c => c.UserId == userId).ToListAsync();
            TotalAmount = CartItems.Sum(c => (c.Product?.SalePrice ?? 0) * c.Quantity);
        }
    }
}