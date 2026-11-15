using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models // Đổi thành tên dự án của bạn nếu khác
{
    [Table("CartItems")]
    [PrimaryKey(nameof(UserId), nameof(ProductId))] // Khai báo khóa chính phức hợp
    public partial class CartItem
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime AddedAt { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}