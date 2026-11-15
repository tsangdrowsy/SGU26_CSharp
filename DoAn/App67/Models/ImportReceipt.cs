using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public partial class ImportReceipt
    {
        [Key]
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = null!;
        public DateTime ImportDate { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CompletedAt { get; set; }
        public int CreatedBy { get; set; }
        public string? Note { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User CreatedByNavigation { get; set; } = null!;

        public virtual ICollection<ImportReceiptDetail> ImportReceiptDetails { get; set; } = new List<ImportReceiptDetail>();
    }
}