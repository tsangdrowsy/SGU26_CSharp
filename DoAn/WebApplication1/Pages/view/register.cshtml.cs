using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data;// Nhớ đổi thành namespace dự án của bạn

namespace WebApplication1.Pages.View
{
    public class RegisterModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public RegisterModel(CoffeeShopContext context)
        {
            _context = context;
        }

        // --- CÁC TRƯỜNG DỮ LIỆU TỪ FORM ---
        [BindProperty]
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ và số")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^09[0-9]{8}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 09 và có 10 số")]
        public string Phone { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Tỉnh/Thành phố không được để trống")]
        public string City { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Quận/Huyện không được để trống")]
        public string District { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Phường/Xã không được để trống")]
        public string Ward { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Địa chỉ chi tiết không được để trống")]
        [StringLength(255, MinimumLength = 5, ErrorMessage = "Địa chỉ phải có ít nhất 5 ký tự")]
        public string Address { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        [Range(typeof(bool), "true", "true", ErrorMessage = "Bạn phải đồng ý với điều khoản sử dụng")]
        public bool Terms { get; set; }

        // Biến hiển thị lỗi chung
        public string ErrorMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            // Nếu đã đăng nhập thì không cho vào trang đăng ký
            if (HttpContext.Session.GetString("Username") != null)
            {
                Response.Redirect("/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Kiểm tra validate form cơ bản
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 2. Kiểm tra trùng lặp dữ liệu trong Database
            if (await _context.Users.AnyAsync(u => u.Username == Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                return Page();
            }
            if (await _context.Users.AnyAsync(u => u.Email == Email))
            {
                ModelState.AddModelError("Email", "Email đã được đăng ký.");
                return Page();
            }
            if (await _context.Users.AnyAsync(u => u.Phone == Phone))
            {
                ModelState.AddModelError("Phone", "Số điện thoại đã được đăng ký.");
                return Page();
            }

            try
            {
                // 3. Tạo user mới
                var newUser = new User
                {
                    Username = Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), // Băm mật khẩu
                    FullName = FullName,
                    Email = Email,
                    Phone = Phone,
                    City = City,
                    District = District,
                    Ward = Ward,
                    Address = Address,
                    Role = "Customer", // Mặc định là Khách hàng
                    IsLocked = false,
                    CreatedAt = DateTime.Now
                };

                // 4. Lưu vào Database
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 5. Chuyển hướng sang trang Đăng nhập
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToPage("/View/Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
                return Page();
            }
        }
    }
}