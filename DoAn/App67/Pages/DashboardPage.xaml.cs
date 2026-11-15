using App67.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace App67.Pages
{
    // Cách tải dữ liệu (tương đương AJAX bên PHP):
    //  - Không tải trong constructor nữa: chờ trang hiển thị (Loaded) rồi mới tải -> mở trang ngay, không bị đơ.
    //  - Tải BẤT ĐỒNG BỘ (async/await): câu SQL chạy nền, giao diện vẫn bấm được, có vòng xoay báo đang tải.
    //  - Tự làm mới mỗi 30 giây (DispatcherTimer) + nút "Làm mới"; hiện giờ cập nhật gần nhất.
    //  - Lỗi hiện lên InfoBar thay vì bị nuốt (bản cũ lỗi giữa chừng -> nửa trang hiện số 0 mà không báo gì).
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

        private const double MaxBarHeight = 160;

        private readonly DashboardService _service = new();
        private readonly DispatcherTimer _refreshTimer = new() { Interval = TimeSpan.FromSeconds(30) };

        // Mỗi lần tải tăng số này. Kết quả về muộn của lần tải CŨ (vd bấm Làm mới 2 lần liên tiếp)
        // sẽ bị bỏ qua, tránh dữ liệu cũ ghi đè dữ liệu mới.
        private int _loadVersion;
        private bool _isLoading;

        public DashboardPage()
        {
            this.InitializeComponent();
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            this.Loaded += DashboardPage_Loaded;
            this.Unloaded += DashboardPage_Unloaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
            if (tglAutoRefresh.IsOn) _refreshTimer.Start();
        }

        // Rời trang thì dừng timer, nếu không timer vẫn chạy ngầm và truy vấn DB mãi
        private void DashboardPage_Unloaded(object sender, RoutedEventArgs e) => _refreshTimer.Stop();

        private async void btnRefresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

        private void tglAutoRefresh_Toggled(object sender, RoutedEventArgs e)
        {
            if (tglAutoRefresh.IsOn) _refreshTimer.Start(); else _refreshTimer.Stop();
        }

        private async Task RefreshAsync()
        {
            if (_isLoading) return; // timer bắn khi lần tải trước chưa xong -> bỏ qua
            _isLoading = true;
            int version = ++_loadVersion;
            LoadingRing.IsActive = true;
            btnRefresh.IsEnabled = false;

            try
            {
                // await: câu SQL chạy nền; khi xong, code phía dưới chạy tiếp trên luồng giao diện
                var data = await _service.LoadAsync(DateTime.Now);
                if (version != _loadVersion) return;

                Render(data);
                ErrorInfoBar.IsOpen = false;
                txtLastUpdated.Text = $"Cập nhật lúc {data.LoadedAt:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                // Giữ nguyên dữ liệu đang hiển thị, chỉ báo lỗi
                ErrorInfoBar.Message = ex.InnerException?.Message ?? ex.Message;
                ErrorInfoBar.IsOpen = true;
                txtLastUpdated.Text = "Lỗi khi tải";
            }
            finally
            {
                _isLoading = false;
                LoadingRing.IsActive = false;
                btnRefresh.IsEnabled = true;
            }
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

        private static SolidColorBrush StatusBrush(string status) => new SolidColorBrush(status switch
        {
            "Pending" => Microsoft.UI.Colors.Orange,
            "Confirmed" => Microsoft.UI.Colors.DodgerBlue,
            "Delivered" => Microsoft.UI.Colors.Green,
            _ => Microsoft.UI.Colors.Red
        });

        // Chỉ đổ dữ liệu ra giao diện, không truy vấn DB
        private void Render(DashboardData d)
        {
            // 1. KPI
            txtRevenue.Text = d.TotalRevenue.ToString("N0") + " ₫";
            txtTotalOrders.Text = d.TotalOrders.ToString();
            txtCustomers.Text = d.TotalCustomers.ToString();
            txtPendingOrders.Text = d.PendingOrders.ToString();

            txtMonthRevenue.Text = d.MonthRevenue.ToString("N0") + " ₫";
            txtMonthRevenueChange.Text = FormatChange(d.MonthRevenue, d.LastMonthRevenue, "tháng trước");
            txtOrdersToday.Text = d.OrdersToday.ToString();
            txtOrdersTodayChange.Text = FormatChange(d.OrdersToday, d.OrdersYesterday, "hôm qua");
            txtTotalProducts.Text = d.TotalProducts.ToString();
            txtProductStatus.Text = $"Đang bán: {d.VisibleProducts} | Đã ẩn: {d.TotalProducts - d.VisibleProducts}";
            txtOutOfStock.Text = $"{d.OutOfStock} hết hàng";
            txtStockStatus.Text = $"{d.LowStock} sản phẩm sắp hết (≤ {DashboardService.LowStockThreshold})";

            // 2. Đơn mới nhất
            RecentOrdersListView.ItemsSource = d.RecentOrders.Select(o => new RecentOrderModel
            {
                OrderIdString = "#" + o.OrderId,
                ReceiverName = o.ReceiverName,
                TotalAmountString = o.TotalAmount.ToString("N0") + " ₫",
                StatusString = OrdersPage.GetStatusText(o.Status),
                StatusColor = StatusBrush(o.Status)
            }).ToList();

            // 3. Cảnh báo tồn kho
            var lowStock = d.LowStockProducts.Select(p => new LowStockModel
            {
                ProductName = p.ProductName,
                StockString = p.Stock == 0 ? "Hết hàng" : $"Còn {p.Stock}"
            }).ToList();
            if (lowStock.Count == 0)
            {
                lowStock.Add(new LowStockModel { ProductName = "Tất cả sản phẩm đều đủ hàng", StockString = "" });
            }
            LowStockListView.ItemsSource = lowStock;

            // 4. Biểu đồ: chiều cao cột tỉ lệ với ngày cao nhất; cột 0 vẫn cao 2px để thấy được
            decimal max = d.DailyRevenue.Count > 0 ? d.DailyRevenue.Max(x => x.Revenue) : 0;
            RevenueChart.ItemsSource = d.DailyRevenue.Select(x => new RevenueBarModel
            {
                DayLabel = x.Day.ToString("dd/MM"),
                Value = x.Revenue,
                BarHeight = max > 0 ? Math.Max(2, (double)(x.Revenue / max) * MaxBarHeight) : 2
            }).ToList();

            // 5. Top bán chạy
            var top = d.TopProducts.Select((x, i) => new TopProductModel
            {
                RankString = $"#{i + 1}",
                ProductName = x.ProductName,
                SoldString = $"{x.Sold} đã bán"
            }).ToList();
            if (top.Count == 0)
            {
                top.Add(new TopProductModel { ProductName = "Chưa có đơn hàng nào trong tháng", SoldString = "" });
            }
            TopProductsListView.ItemsSource = top;
        }
    }
}
