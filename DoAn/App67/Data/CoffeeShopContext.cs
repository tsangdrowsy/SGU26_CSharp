using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models; // Nhớ sửa lại namespace này nếu bạn đã đổi tên Models

namespace App67.Data // Nhớ sửa lại namespace này cho khớp với dự án App67
{
    public class CoffeeShopContext : DbContext
    {
        // Hàm khởi tạo không tham số (Bắt buộc cho WinUI)
        public CoffeeShopContext() { }

        // Hàm khởi tạo có tham số
        public CoffeeShopContext(DbContextOptions<CoffeeShopContext> options) : base(options) { }

        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<ImportReceipt> ImportReceipts { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<ImportReceiptDetail> ImportReceiptDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình tên bảng
            modelBuilder.Entity<Product>().ToTable("Products");
            modelBuilder.Entity<Category>().ToTable("Categories");
            modelBuilder.Entity<Supplier>().ToTable("Suppliers");
            modelBuilder.Entity<CartItem>().ToTable("CartItems");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");

            // CẤU HÌNH KHÓA CHÍNH PHỨC HỢP (Đã thêm lại cho bạn)
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => new { od.OrderId, od.ProductId });

            // Cấu hình Khóa chính phức hợp cho ImportReceiptDetail
            modelBuilder.Entity<ImportReceiptDetail>()
                .HasKey(ird => new { ird.ReceiptId, ird.ProductId });

            // Cấu hình Computed Column
            modelBuilder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasComputedColumnSql("CAST(ROUND(CostPrice * (100 + ProfitRate) / 100, 2) AS DECIMAL(18,2)) PERSISTED");

            // Cấu hình Trigger
            modelBuilder.Entity<CartItem>().ToTable(tb => tb.HasTrigger("trg_CartItems_CheckProduct"));

            modelBuilder.Entity<Product>()
                .ToTable(tb => tb.HasTrigger("trg_Products_InitialStock"))
                .ToTable(tb => tb.HasTrigger("trg_Products_GuardStock"))
                .ToTable(tb => tb.HasTrigger("trg_Products_InsteadOfDelete"));

            modelBuilder.Entity<Order>()
                .ToTable(tb => tb.HasTrigger("trg_Orders_StatusFlow"))
                .ToTable(tb => tb.HasTrigger("trg_Orders_NoDelete"));

            modelBuilder.Entity<OrderDetail>()
                .ToTable(tb => tb.HasTrigger("trg_OrderDetails_InsteadOfInsert"))
                .ToTable(tb => tb.HasTrigger("trg_OrderDetails_ReadOnly"));

            modelBuilder.Entity<ImportReceipt>()
                .ToTable(tb => tb.HasTrigger("trg_ImportReceipts_AfterUpdate"))
                .ToTable(tb => tb.HasTrigger("trg_ImportReceipts_AfterDelete"));

            modelBuilder.Entity<ImportReceiptDetail>()
                .ToTable(tb => tb.HasTrigger("trg_ImportReceiptDetails_LockCompleted"));


        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Sửa lại chuỗi này cho giống hệt bên Web của bạn
                optionsBuilder.UseSqlServer("Server=localhost;Database=CoffeeShopDb;Trusted_Connection=True;TrustServerCertificate=True");
            }
        }
    }
}