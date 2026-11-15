using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public partial class OrderDetail
    {
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }

        // Cột LineTotal là cột tự tính trong SQL
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public decimal? LineTotal { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}