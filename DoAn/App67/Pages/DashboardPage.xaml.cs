using App67.Data;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;

namespace App67.Pages
{
    public sealed partial class DashboardPage : Page
    {
        // Lớp phụ để hiển thị đơn hàng
        public class RecentOrderModel
        {
            public string OrderIdString { get; set; } = string.Empty;
            public string ReceiverName { get; set; } = string.Empty;
            public string TotalAmountString { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        // Lớp phụ để hiển thị cảnh báo kho
        public class LowStockModel
        {
            public string ProductName { get; set; } = string.Empty;
            public string StockString { get; set; } = string.Empty;
        }

        // 1 cột của biểu đồ doanh thu
        public class RevenueBarModel
        {
            public string DayLabel { get; set; } = string.Empty;
            public decimal Value { get; set; }
            public double BarHeight { get; set; }
            public string ShortValue => FormatShort(Value);
            public string TooltipText => $"{DayLabel}: {Value:N0} ₫";
        }

        public class TopProductModel
        {
            public string RankString { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string SoldString { get; set; } = string.Empty;
        }

        // Doanh thu "thực" = đơn đã duyệt hoặc đã giao (giống dashboard.php: TrangThai IN (1,2)).
        // Đơn chờ duyệt có thể còn bị hủy, đơn đã hủy thì không tính.
        private static readonly string[] RevenueStatuses = { "Confirmed", "Delivered" };

        private const double MaxBarHeight = 160;

        public DashboardPage()
        {
            this.InitializeComponent();
            LoadDashboardData();
        }

        // 1.500.000 -> "1.5M", 25.000 -> "25K" (giống hàm formatMoney trong PHP)
        private static string FormatShort(decimal amount)
        {
            if (amount >= 1_000_000_000) return Math.Round(amount / 1_000_000_000, 1) + "B";
            if (amount >= 1_000_000) return Math.Round(amount / 1_000_000, 1) + "M";
            if (amount >= 1_000) return Math.Round(amount / 1_000, 1) + "K";
            return amount.ToString("N0");
        }

        // % thay đổi so với kỳ trước; kỳ trước = 0 thì không chia được -> trả về mô tả riêng
        private static string FormatChange(decimal current, decimal previous, string periodName)
        {
            if (previous == 0)
                return current > 0 ? $"▲ mới phát sinh (so với {periodName}: 0)" : $"Không đổi so với {periodName}";
            decimal percent = (current - previous) / previous * 100;
            string arrow = percent >= 0 ? "▲" : "▼";
            return $"{arrow} {Math.Abs(percent):0.#}% so với {periodName}";
        }

        private void LoadDashboardData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // 1. THỐNG KÊ (KPI)
                    // Tổng doanh thu (Chỉ tính các đơn đã giao thành công)
                    decimal revenue = context.Orders
                        .Where(o => o.Status == "Delivered")
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0;
                    txtRevenue.Text = revenue.ToString("N0") + " ₫";

                    // Tổng số đơn hàng
                    int totalOrders = context.Orders.Count();
                    txtTotalOrders.Text = totalOrders.ToString();

                    // Số lượng khách hàng
                    int totalCustomers = context.Users.Count(u => u.Role == "Customer");
                    txtCustomers.Text = totalCustomers.ToString();

                    // Số đơn đang chờ duyệt
                    int pendingOrders = context.Orders.Count(o => o.Status == "Pending");
                    txtPendingOrders.Text = pendingOrders.ToString();

                    // 1B. KPI SO SÁNH THEO KỲ
                    DateTime today = DateTime.Today;
                    DateTime monthStart = new DateTime(today.Year, today.Month, 1);
                    DateTime lastMonthStart = monthStart.AddMonths(-1);

                    decimal monthRevenue = context.Orders
                        .Where(o => RevenueStatuses.Contains(o.Status) && o.OrderDate >= monthStart)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0;
                    decimal lastMonthRevenue = context.Orders
                        .Where(o => RevenueStatuses.Contains(o.Status) && o.OrderDate >= lastMonthStart && o.OrderDate < monthStart)
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0;
                    txtMonthRevenue.Text = monthRevenue.ToString("N0") + " ₫";
                    txtMonthRevenueChange.Text = FormatChange(monthRevenue, lastMonthRevenue, "tháng trước");

                    int ordersToday = context.Orders.Count(o => o.OrderDate >= today);
                    int ordersYesterday = context.Orders.Count(o => o.OrderDate >= today.AddDays(-1) && o.OrderDate < today);
                    txtOrdersToday.Text = ordersToday.ToString();
                    txtOrdersTodayChange.Text = FormatChange(ordersToday, ordersYesterday, "hôm qua");

                    int totalProducts = context.Products.Count();
                    int visibleProducts = context.Products.Count(p => p.IsVisible);
                    txtTotalProducts.Text = totalProducts.ToString();
                    txtProductStatus.Text = $"Đang bán: {visibleProducts} | Đã ẩn: {totalProducts - visibleProducts}";

                    int outOfStock = context.Products.Count(p => p.IsVisible && p.StockQuantity == 0);
                    int lowStockCount = context.Products.Count(p => p.IsVisible && p.StockQuantity > 0 && p.StockQuantity <= 10);
                    txtOutOfStock.Text = $"{outOfStock} hết hàng";
                    txtStockStatus.Text = $"{lowStockCount} sản phẩm sắp hết (≤ 10)";


                    // 2. LẤY 5 ĐƠN HÀNG MỚI NHẤT
                    var recentOrders = context.Orders
                        .OrderByDescending(o => o.OrderId)
                        .Take(5)
                        .Select(o => new { o.OrderId, o.ReceiverName, o.TotalAmount, o.Status })
                        .ToList()
                        .Select(o => new RecentOrderModel
                        {
                            OrderIdString = "#" + o.OrderId,
                            ReceiverName = o.ReceiverName,
                            TotalAmountString = o.TotalAmount.ToString("N0") + " ₫",
                            StatusString = OrdersPage.GetStatusText(o.Status),
                            StatusColor = new SolidColorBrush(o.Status switch
                            {
                                "Pending" => Microsoft.UI.Colors.Orange,
                                "Confirmed" => Microsoft.UI.Colors.DodgerBlue,
                                "Delivered" => Microsoft.UI.Colors.Green,
                                _ => Microsoft.UI.Colors.Red
                            })
                        }).ToList();
                    RecentOrdersListView.ItemsSource = recentOrders;


                    // 3. LẤY SẢN PHẨM SẮP HẾT HÀNG (Tồn kho <= 10)
                    var lowStock = context.Products
                        .Where(p => p.IsVisible && p.StockQuantity <= 10)
                        .OrderBy(p => p.StockQuantity)
                        .Select(p => new LowStockModel
                        {
                            ProductName = p.Name,
                            StockString = p.StockQuantity == 0 ? "Hết hàng" : $"Còn {p.StockQuantity}"
                        }).ToList();

                    if (lowStock.Count == 0)
                    {
                        lowStock.Add(new LowStockModel { ProductName = "Tất cả sản phẩm đều đủ hàng", StockString = "" });
                    }

                    LowStockListView.ItemsSource = lowStock;


                    // 4. BIỂU ĐỒ DOANH THU 7 NGÀY (gồm hôm nay)
                    DateTime chartStart = today.AddDays(-6);
                    var dailyRevenue = context.Orders
                        .Where(o => RevenueStatuses.Contains(o.Status) && o.OrderDate >= chartStart)
                        .Select(o => new { o.OrderDate, o.TotalAmount })
                        .ToList()                                   // gom nhóm theo ngày ở phía C#
                        .GroupBy(o => o.OrderDate.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalAmount));

                    var bars = Enumerable.Range(0, 7)
                        .Select(i => chartStart.AddDays(i))
                        .Select(day => new RevenueBarModel
                        {
                            DayLabel = day.ToString("dd/MM"),
                            Value = dailyRevenue.TryGetValue(day, out var v) ? v : 0
                        })
                        .ToList();

                    // Chiều cao cột tỉ lệ với ngày cao nhất; cột 0 vẫn cao 2px để thấy được
                    decimal max = bars.Max(b => b.Value);
                    foreach (var bar in bars)
                    {
                        bar.BarHeight = max > 0 ? Math.Max(2, (double)(bar.Value / max) * MaxBarHeight) : 2;
                    }
                    RevenueChart.ItemsSource = bars;


                    // 5. TOP 5 SẢN PHẨM BÁN CHẠY THÁNG NÀY
                    var topProducts = context.OrderDetails
                        .Where(d => RevenueStatuses.Contains(d.Order.Status) && d.Order.OrderDate >= monthStart)
                        .GroupBy(d => new { d.ProductId, d.Product.Name })
                        .Select(g => new { g.Key.Name, Sold = g.Sum(d => d.Quantity) })
                        .OrderByDescending(x => x.Sold)
                        .Take(5)
                        .ToList()
                        .Select((x, i) => new TopProductModel
                        {
                            RankString = $"#{i + 1}",
                            ProductName = x.Name,
                            SoldString = $"{x.Sold} đã bán"
                        })
                        .ToList();

                    if (topProducts.Count == 0)
                    {
                        topProducts.Add(new TopProductModel { ProductName = "Chưa có đơn hàng nào trong tháng", SoldString = "" });
                    }
                    TopProductsListView.ItemsSource = topProducts;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải Dashboard: " + ex.Message);
            }
        }
    }
}
