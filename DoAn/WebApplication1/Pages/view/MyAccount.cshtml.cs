using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Data;
using WebApplication1.Models; // Đổi thành namespace dự án của bạn

namespace WebApplication1.Pages.View
{
    public class MyAccountModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public MyAccountModel(CoffeeShopContext context)
        {
            _context = context;
        }

        public User CurrentUser { get; set; } = default!;

        [BindProperty]
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10 số")]
        public string Phone { get; set; } = string.Empty;

        public string SuccessMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Kiểm tra đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Chưa đăng nhập thì chuyển về trang Login
                return RedirectToPage("/View/Login");
            }

            // 2. Lấy thông tin user mới nhất từ Database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                // User không tồn tại trong DB -> Xóa session và về trang chủ
                HttpContext.Session.Clear();
                return RedirectToPage("/Index");
            }

            // Cập nhật lại Session nếu có thay đổi
            HttpContext.Session.SetString("Username", user.Username);
            CurrentUser = user;

            // Gán dữ liệu ra Form
            FullName = user.FullName;
            Phone = user.Phone ?? "";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            if (!ModelState.IsValid)
            {
                // Nếu có lỗi nhập liệu, load lại thông tin user để hiển thị form
                CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId) ?? new User();
                return Page();
            }

            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return RedirectToPage("/Index");

                // Cập nhật thông tin
                user.FullName = FullName;
                user.Phone = Phone;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                SuccessMessage = "Cập nhật thông tin thành công!";
                CurrentUser = user; // Cập nhật lại view

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
                CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId) ?? new User();
                return Page();
            }
        }
    }
}