using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Pages.View // Đổi WebApplication1 thành tên dự án của bạn
{
    public class LoginModel : PageModel
    {
        // Ràng buộc dữ liệu từ Form gửi lên
        [BindProperty]
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [BindProperty]
        public bool RememberMe { get; set; }

        // Biến hiển thị thông báo
        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnGet()
        {
            // Tự động điền username nếu có cookie "remember_user"
            if (Request.Cookies.TryGetValue("remember_user", out string savedUsername))
            {
                Username = savedUsername;
                RememberMe = true;
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // =========================================================
            // TODO: KẾT NỐI DATABASE (BACKEND) SẼ ĐƯỢC VIẾT Ở ĐÂY
            // =========================================================
            /*
            Ví dụ code Backend sau này:
            var user = _context.Users.FirstOrDefault(u => u.Username == Username);
            
            if (user != null && user.Password == Password) // Nên dùng hàm băm mật khẩu
            {
                if (user.Role == 1) {
                    ErrorMessage = "Tài khoản Admin không được đăng nhập ở đây.";
                    return Page();
                }
                
                // Lưu Session
                HttpContext.Session.SetInt32("user_id", user.UserId);
                HttpContext.Session.SetString("username", user.Username);
                
                // Xử lý Remember Me
                if (RememberMe) {
                    Response.Cookies.Append("remember_user", Username, new CookieOptions { Expires = DateTime.Now.AddDays(30) });
                }
                
                return RedirectToPage("/Index"); // Chuyển về trang chủ
            }
            else
            {
                ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng";
            }
            */

            // TẠM THỜI: Giả lập đăng nhập thành công để test giao diện
            SuccessMessage = "Đăng nhập thành công! (Chưa kết nối DB)";

            return Page();
        }
    }
}