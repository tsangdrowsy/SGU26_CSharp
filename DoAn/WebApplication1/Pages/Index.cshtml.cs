using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        //đăng xuất
        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Clear(); // Xóa session
            return RedirectToPage("/Index"); // Tải lại trang chủ
        }
        public void OnGet()
        {
           
        }
    }
}