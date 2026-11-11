using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class CoffeeShopContext : DbContext
    {
        public CoffeeShopContext(DbContextOptions<CoffeeShopContext> options) : base(options) { }

        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng theo đúng tên trong SQL script
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            modelBuilder.Entity<CartItem>().ToTable("CartItems");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");

            // Cấu hình Khóa chính phức hợp cho OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => new { od.OrderId, od.ProductId });



            // Cấu hình để EF Core không cố gắng INSERT/UPDATE vào cột SalePrice (vì đây là Computed Column)
            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasComputedColumnSql("CAST(ROUND(CostPrice * (100 + ProfitRate) / 100, 2) AS DECIMAL(18,2)) PERSISTED");

            // 3. CẤU HÌNH TRIGGER (Rất quan trọng để tránh lỗi Output Clause)
            modelBuilder.Entity<CartItem>()
               .ToTable(tb => tb.HasTrigger("trg_CartItems_CheckProduct"));


            // LƯU Ý: Vì bạn có Trigger trên bảng Products, theo cảnh báo trong file SQL số 2:
            modelBuilder.Entity<Product>()
                .ToTable(tb => tb.HasTrigger("trg_Products_InitialStock"))
                .ToTable(tb => tb.HasTrigger("trg_Products_GuardStock"))
                .ToTable(tb => tb.HasTrigger("trg_Products_InsteadOfDelete"));
            // Nếu có lỗi với các trigger khác, bạn phải khai báo HasTrigger tiếp ở đây.

            // Nếu sau này bạn làm đến phần Đơn hàng, hãy bỏ comment các dòng dưới đây:
            
            modelBuilder.Entity<Order>()
                .ToTable(tb => tb.HasTrigger("trg_Orders_StatusFlow"))
                .ToTable(tb => tb.HasTrigger("trg_Orders_NoDelete"));
            
            modelBuilder.Entity<OrderDetail>()
                .ToTable(tb => tb.HasTrigger("trg_OrderDetails_InsteadOfInsert"))
                .ToTable(tb => tb.HasTrigger("trg_OrderDetails_ReadOnly"));
            
        }
    }
}