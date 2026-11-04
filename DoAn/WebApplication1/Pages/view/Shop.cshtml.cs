using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Data; // Thêm dòng này để nhận diện CoffeeShopContext

namespace WebApplication1.Pages.View // Tên namespace phải khớp với thư mục
{
    // Tên class BẮT BUỘC phải là ShopModel (khớp với @model trong file cshtml)
    public class ShopModel : PageModel
    {
        private readonly CoffeeShopContext _context;

        public ShopModel(CoffeeShopContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)] public string? Search { get; set; }
        [BindProperty(SupportsGet = true)] public int[]? CategoryIds { get; set; }
        [BindProperty(SupportsGet = true)] public int[]? SupplierIds { get; set; }
        [BindProperty(SupportsGet = true)] public decimal MinPrice { get; set; } = 0;
        [BindProperty(SupportsGet = true)] public decimal MaxPrice { get; set; } = 50000000;
        [BindProperty(SupportsGet = true)] public string Sort { get; set; } = "newest";
        [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;

        public IList<Product> Products { get; set; } = default!;
        public IList<Category> Categories { get; set; } = default!;
        public IList<Supplier> Suppliers { get; set; } = default!;

        public int TotalPages { get; set; }
        public int TotalProducts { get; set; }
        public int PageSize { get; set; } = 12;

        public async Task OnGetAsync()
        {
            Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            Suppliers = await _context.Suppliers.ToListAsync();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.IsVisible == true)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
                query = query.Where(p => p.Name.Contains(Search));

            if (CategoryIds != null && CategoryIds.Length > 0)
                query = query.Where(p => CategoryIds.Contains(p.CategoryId));

            if (SupplierIds != null && SupplierIds.Length > 0)
                query = query.Where(p => p.SupplierId.HasValue && SupplierIds.Contains(p.SupplierId.Value));

            query = query.Where(p => p.SalePrice >= MinPrice && p.SalePrice <= MaxPrice);

            TotalProducts = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalProducts / (double)PageSize);

            query = Sort switch
            {
                "price_asc" => query.OrderBy(p => p.SalePrice),
                "price_desc" => query.OrderByDescending(p => p.SalePrice),
                "name_asc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            if (PageNumber < 1) PageNumber = 1;
            Products = await query.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToListAsync();
        }
    }
}