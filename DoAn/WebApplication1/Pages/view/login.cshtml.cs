using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore; // Cần thiết để dùng FirstOrDefaultAsync
using WebApplication1.Models;
using WebApplication1.Data;
using BCrypt.Net;

namespace WebApplication1.Pages.View
{
    public class LoginModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        // Tiêm Database Context vào trang thông qua Constructor
        public LoginModel(CoffeeShopContext context)
        {
            _context = context;
        }

        // Ràng buộc dữ liệu từ Form gửi lên
        [BindProperty]
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public bool RememberMe { get; set; }

        // Biến hiển thị thông báo
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Tự động điền username nếu có cookie "remember_user"
            if (Request.Cookies.TryGetValue("remember_user", out string savedUsername))
            {
                Username = savedUsername;
                RememberMe = true;
            }
        }

        // Chuyển OnPost thành OnPostAsync vì phải chờ truy vấn Database
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. Tìm user trong Database theo Username
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username);

            if (user == null)
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
                return Page();
            }

            // 2. Kiểm tra mật khẩu (Dùng BCrypt để so sánh mật khẩu nhập vào với Hash trong DB)
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
                return Page();
            }

            // 3. Kiểm tra tài khoản có bị khóa không
            if (user.IsLocked)
            {
                ErrorMessage = "Tài khoản của bạn đã bị khóa.";
                return Page();
            }

            // 4. Chặn Admin đăng nhập vào khu vực khách hàng
            // Theo database của bạn, Role là string ('Admin' hoặc 'Customer')
            if (user.Role == "Admin")
            {
                ErrorMessage = "Tài khoản Admin không được đăng nhập vào trang khách hàng.";
                return Page();
            }

            // 5. Đăng nhập thành công -> Lưu Session
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            // Xử lý Remember Me (Lưu Cookie 30 ngày)
            if (RememberMe)
            {
                var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(30) };
                Response.Cookies.Append("remember_user", Username, cookieOptions);
            }
            else
            {
                // Nếu không chọn ghi nhớ thì xóa cookie cũ
                Response.Cookies.Delete("remember_user");
            }

            // 6. Chuyển hướng về trang chủ
            return RedirectToPage("/Index");
        }
    }
}