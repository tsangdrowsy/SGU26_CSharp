using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace WebApplication1.Pages.View
{
    public class AddressBookModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public AddressBookModel(CoffeeShopContext context)
        {
            _context = context;
        }

        [BindProperty]
        public User CurrentUser { get; set; } = null!;

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            CurrentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (CurrentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToPage("/View/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToPage("/View/Login");

            // Xóa các lỗi validation của các trường không có trong Form
            ModelState.Remove("CurrentUser.Username");
            ModelState.Remove("CurrentUser.Email");
            ModelState.Remove("CurrentUser.PasswordHash");
            ModelState.Remove("CurrentUser.Role");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng kiểm tra lại các trường thông tin.";
                return Page();
            }

            try
            {
                var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (userToUpdate == null) return RedirectToPage("/View/Login");

                // Cập nhật các trường địa chỉ
                userToUpdate.FullName = CurrentUser.FullName;
                userToUpdate.Phone = CurrentUser.Phone;
                userToUpdate.City = CurrentUser.City;
                userToUpdate.District = CurrentUser.District;
                userToUpdate.Ward = CurrentUser.Ward;
                userToUpdate.Address = CurrentUser.Address;

                _context.Users.Update(userToUpdate);
                await _context.SaveChangesAsync();

                SuccessMessage = "Cập nhật địa chỉ giao hàng thành công!";

                CurrentUser.Username = userToUpdate.Username;
                CurrentUser.Email = userToUpdate.Email;
                CurrentUser.Role = userToUpdate.Role;
                CurrentUser.CreatedAt = userToUpdate.CreatedAt;
                CurrentUser.IsLocked = userToUpdate.IsLocked;

                HttpContext.Session.SetString("FullName", userToUpdate.FullName);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Lỗi hệ thống: " + ex.Message;
            }

            return Page();
        }
    }
}