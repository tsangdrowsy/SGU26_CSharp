namespace WebApplication1.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}