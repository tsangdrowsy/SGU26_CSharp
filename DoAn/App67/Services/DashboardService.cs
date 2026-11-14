using App67.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App67.Services
{
    // Dữ liệu Dashboard dạng "thuần" (không có Brush, TextBlock...) -> trang chỉ việc hiển thị,
    // còn service này chạy/kiểm thử được độc lập với giao diện.
    public record RecentOrderRow(int OrderId, string ReceiverName, decimal TotalAmount, string Status);
    public record LowStockRow(string ProductName, int Stock);
    public record DailyRevenueRow(DateTime Day, decimal Revenue);
    public record TopProductRow(string ProductName, int Sold);

    public class DashboardData
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingOrders { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal LastMonthRevenue { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersYesterday { get; set; }
        public int TotalProducts { get; set; }
        public int VisibleProducts { get; set; }
        public int OutOfStock { get; set; }
        public int LowStock { get; set; }
        public List<RecentOrderRow> RecentOrders { get; set; } = new();
        public List<LowStockRow> LowStockProducts { get; set; } = new();
        public List<DailyRevenueRow> DailyRevenue { get; set; } = new();
        public List<TopProductRow> TopProducts { get; set; } = new();
        public DateTime LoadedAt { get; set; }
    }

    public class DashboardService
    {
        // Doanh thu = đơn ĐÃ DUYỆT + ĐÃ GIAO (giống dashboard.php: TrangThai IN (1,2)).
        // Dùng CHUNG 1 định nghĩa cho mọi thẻ. (Bản trước: "Tổng doanh thu" chỉ tính Đã giao,
        // "Doanh thu tháng" tính cả Đã duyệt -> doanh thu tháng có thể LỚN HƠN tổng doanh thu.)
        public static readonly string[] RevenueStatuses = { "Confirmed", "Delivered" };
        //
        // LƯU Ý C# 14 / .NET 10: viết RevenueStatuses.Contains(x) thì trình biên dịch chọn
        // MemoryExtensions.Contains(ReadOnlySpan<T>) thay vì Enumerable.Contains. EF Core 8 KHÔNG dịch
        // được ReadOnlySpan -> exception lúc chạy (biên dịch vẫn qua!). Vì vậy gọi thẳng Enumerable.Contains.
        public const int LowStockThreshold = 10;

        // now: truyền vào thay vì gọi DateTime.Now bên trong -> kiểm thử được với ngày cố định
        public async Task<DashboardData> LoadAsync(DateTime now, CancellationToken ct = default)
        {
            // DbContext KHÔNG an toàn đa luồng: các câu await phải chạy TUẦN TỰ trên cùng 1 context
            // (không dùng Task.WhenAll với cùng context).
            await using var context = new CoffeeShopContext();
            var orders = context.Orders.AsNoTracking();
            var products = context.Products.AsNoTracking();

            DateTime today = now.Date;
            DateTime monthStart = new DateTime(today.Year, today.Month, 1);
            DateTime lastMonthStart = monthStart.AddMonths(-1);
            DateTime chartStart = today.AddDays(-6);
            var revenueOrders = orders.Where(o => Enumerable.Contains(RevenueStatuses, o.Status));

            var data = new DashboardData { LoadedAt = now };

            data.TotalRevenue = await revenueOrders.SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
            data.TotalOrders = await orders.CountAsync(ct);
            data.TotalCustomers = await context.Users.CountAsync(u => u.Role == "Customer", ct);
            data.PendingOrders = await orders.CountAsync(o => o.Status == "Pending", ct);

            data.MonthRevenue = await revenueOrders.Where(o => o.OrderDate >= monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;
            data.LastMonthRevenue = await revenueOrders.Where(o => o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
                .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0;

            data.OrdersToday = await orders.CountAsync(o => o.OrderDate >= today, ct);
            data.OrdersYesterday = await orders.CountAsync(o => o.OrderDate >= today.AddDays(-1) && o.OrderDate < today, ct);

            data.TotalProducts = await products.CountAsync(ct);
            data.VisibleProducts = await products.CountAsync(p => p.IsVisible, ct);
            data.OutOfStock = await products.CountAsync(p => p.IsVisible && p.StockQuantity == 0, ct);
            data.LowStock = await products.CountAsync(p => p.IsVisible && p.StockQuantity > 0 && p.StockQuantity <= LowStockThreshold, ct);

            data.RecentOrders = await orders
                .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.OrderId)
                .Take(5)
                .Select(o => new RecentOrderRow(o.OrderId, o.ReceiverName, o.TotalAmount, o.Status))
                .ToListAsync(ct);

            data.LowStockProducts = await products
                .Where(p => p.IsVisible && p.StockQuantity <= LowStockThreshold)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                .Select(p => new LowStockRow(p.Name, p.StockQuantity))
                .ToListAsync(ct);

            // Doanh thu từng ngày: SQL chỉ trả về các đơn trong 7 ngày, gom theo ngày ở phía C#
            // (để ngày không có đơn vẫn có cột = 0)
            var recentRevenue = await revenueOrders
                .Where(o => o.OrderDate >= chartStart && o.OrderDate < today.AddDays(1))
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync(ct);
            var byDay = recentRevenue.GroupBy(o => o.OrderDate.Date).ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));
            data.DailyRevenue = Enumerable.Range(0, 7)
                .Select(i => chartStart.AddDays(i))
                .Select(day => new DailyRevenueRow(day, byDay.TryGetValue(day, out var v) ? v : 0))
                .ToList();

            data.TopProducts = await context.OrderDetails.AsNoTracking()
                .Where(d => Enumerable.Contains(RevenueStatuses, d.Order.Status) && d.Order.OrderDate >= monthStart)
                .GroupBy(d => new { d.ProductId, d.Product.Name })
                // Sắp xếp trên kiểu ẩn danh TRƯỚC, tạo record SAU: EF không dịch được
                // OrderBy(x => x.Sold) nếu x là record tạo bằng constructor.
                .Select(g => new { g.Key.Name, Sold = g.Sum(d => d.Quantity) })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .Select(x => new TopProductRow(x.Name, x.Sold))
                .ToListAsync(ct);

            return data;
        }
    }
}
