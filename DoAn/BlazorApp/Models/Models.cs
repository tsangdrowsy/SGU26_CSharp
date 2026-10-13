namespace BlazorApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Url { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OldPrice { get; set; }
    public int? DiscountPercent { get; set; }
}

public class ProductSection
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string ViewAllUrl { get; set; } = "";
    public List<Product> Products { get; set; } = new();
}

public class SubCategory
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public List<SubCategory> SubCategories { get; set; } = new();
}

public class Banner
{
    public string ImageUrl { get; set; } = "";
    public string? LinkUrl { get; set; }
    public string AltText { get; set; } = "";
    public bool OpenNewTab { get; set; }
}

public class TrustBadge
{
    public string Text { get; set; } = "";
    public string SvgIcon { get; set; } = "";
}

public class BlogPost
{
    public string Title { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string Url { get; set; } = "";
    public string Date { get; set; } = "";
}

public class CartItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
}

public class FooterLink
{
    public string Text { get; set; } = "";
    public string Url { get; set; } = "";
}

public class PaymentMethod
{
    public string Name { get; set; } = "";
    public string ImageUrl { get; set; } = "";
}