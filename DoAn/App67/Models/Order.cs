using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public partial class Order
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public string ReceiverName { get; set; } = null!;
        public string ReceiverPhone { get; set; } = null!;
        public string ShippingAddress { get; set; } = null!;
        public string Ward { get; set; } = null!;
        public string District { get; set; } = null!;
        public string City { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}