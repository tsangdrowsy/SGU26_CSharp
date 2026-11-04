using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class CoffeeShopContext : DbContext
    {
        public CoffeeShopContext(DbContextOptions<CoffeeShopContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình bảng theo đúng tên trong SQL script
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");

            // Cấu hình để EF Core không cố gắng INSERT/UPDATE vào cột SalePrice (vì đây là Computed Column)
            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasComputedColumnSql("CAST(ROUND(CostPrice * (100 + ProfitRate) / 100, 2) AS DECIMAL(18,2)) PERSISTED");

            // LƯU Ý: Vì bạn có Trigger trên bảng Products, theo cảnh báo trong file SQL số 2:
            modelBuilder.Entity<Product>().ToTable(tb => tb.HasTrigger("trg_Products_InitialStock"));
            // Nếu có lỗi với các trigger khác, bạn phải khai báo HasTrigger tiếp ở đây.
        }
    }
}