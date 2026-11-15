using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public partial class InventoryTransaction
    {
        [Key]
        public long TransactionId { get; set; }
        public int ProductId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = null!;
        public int QuantityChange { get; set; }
        public int? ReferenceId { get; set; }
        public string? Note { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;
    }
}