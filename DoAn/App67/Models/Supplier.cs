namespace WebApplication1.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}