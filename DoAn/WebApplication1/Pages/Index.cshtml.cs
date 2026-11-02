using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // Sau này bạn sẽ viết code truy vấn Database tại đây
            // Ví dụ: var products = _context.SanPhams.ToList();
        }
    }
}