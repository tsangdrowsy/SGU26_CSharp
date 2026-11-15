using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public int? SupplierId { get; set; }
        public virtual Supplier Supplier { get; set; }

        public string? Description { get; set; }
        public string Unit { get; set; }
        public string? Origin { get; set; }
        public string? ImageUrl { get; set; }

        public int? WeightGram { get; set; }
        public string? RoastLevel { get; set; }

        public decimal CostPrice { get; set; }
        public decimal ProfitRate { get; set; }

        // Cột này Database tự tính, Entity Framework sẽ tự động map khi đọc dữ liệu
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal SalePrice { get; set; }

        public int StockQuantity { get; set; }
        public bool IsVisible { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}