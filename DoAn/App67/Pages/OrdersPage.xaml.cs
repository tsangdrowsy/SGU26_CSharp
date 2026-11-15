using App67.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace App67.Pages
{
    public sealed partial class OrdersPage : Page
    {
        public class OrderDisplayModel
        {
            public int OrderId { get; set; }
            public string OrderCode { get; set; } = string.Empty;
            public DateTime OrderDate { get; set; }
            public string OrderDateString { get; set; } = string.Empty;
            public string ReceiverName { get; set; } = string.Empty;
            public string ReceiverPhone { get; set; } = string.Empty;
            public string Ward { get; set; } = string.Empty;
            public string TotalAmountString { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public string RawStatus { get; set; } = string.Empty;

            // Nút hành động thay đổi theo trạng thái: Pending -> "Duyệt", Confirmed -> "Đã giao"
            public string NextActionText { get; set; } = string.Empty;
            // Đơn đã giao / đã hủy là trạng thái cuối -> ẩn nút Duyệt và Hủy
            public Visibility ActionVisibility { get; set; } = Visibility.Collapsed;

            // Thuộc tính chứa màu sắc
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        // Toàn bộ đơn hàng tải từ DB; bộ lọc chạy trên danh sách này (không truy vấn lại DB mỗi lần gõ phím)
        private List<OrderDisplayModel> _allOrders = new();

        // ComboBox có SelectedIndex="0" sẽ bắn SelectionChanged ngay trong InitializeComponent,
        // lúc các control khác chưa tạo xong -> dùng cờ này để bỏ qua các sự kiện đó.
        private bool _isReady;

        public OrdersPage()
        {
            this.InitializeComponent();
            _isReady = true;
            LoadData();
        }

        public static string GetStatusText(string status) => status switch
        {
            "Pending" => "Chờ duyệt",
            "Confirmed" => "Đã duyệt",
            "Delivered" => "Đã giao",
            "Cancelled" => "Đã hủy",
            _ => status
        };

        private static SolidColorBrush GetStatusColor(string status) => status switch
        {
            "Pending" => new SolidColorBrush(Microsoft.UI.Colors.Orange),
            "Confirmed" => new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue),
            "Delivered" => new SolidColorBrush(Microsoft.UI.Colors.Green),
            _ => new SolidColorBrush(Microsoft.UI.Colors.Red)
        };

        private static string GetPaymentText(string method) => method switch
        {
            "Cash" => "Thanh toán khi nhận hàng (COD)",
            "BankTransfer" => "Chuyển khoản",
            "Online" => "Thanh toán online (VNPay)",
            _ => method
        };

        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var dbOrders = context.Orders.AsNoTracking().OrderByDescending(o => o.OrderId).ToList();

                    _allOrders = dbOrders.Select(o => new OrderDisplayModel
                    {
                        OrderId = o.OrderId,
                        OrderCode = o.OrderCode,
                        OrderDate = o.OrderDate,
                        OrderDateString = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                        ReceiverName = o.ReceiverName,
                        ReceiverPhone = o.ReceiverPhone,
                        Ward = o.Ward,
                        TotalAmountString = o.TotalAmount.ToString("N0") + " ₫",
                        RawStatus = o.Status,
                        StatusString = GetStatusText(o.Status),
                        StatusColor = GetStatusColor(o.Status),
                        NextActionText = o.Status == "Pending" ? "Duyệt" : "Đã giao",
                        ActionVisibility = (o.Status == "Pending" || o.Status == "Confirmed") ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu", ex.Message);
            }
        }

        // ================= BỘ LỌC =================
        private void ApplyFilters()
        {
            IEnumerable<OrderDisplayModel> query = _allOrders;

            // 1. Tìm theo ID, mã đơn, tên người nhận, SĐT
            string keyword = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(o =>
                    o.OrderId.ToString() == keyword ||
                    o.OrderCode.ToLower().Contains(keyword) ||
                    o.ReceiverName.ToLower().Contains(keyword) ||
                    o.ReceiverPhone.Contains(keyword));
            }

            // 2. Khoảng ngày (bao gồm trọn ngày kết thúc)
            if (dpFromDate.Date.HasValue)
            {
                var from = dpFromDate.Date.Value.DateTime.Date;
                query = query.Where(o => o.OrderDate >= from);
            }
            if (dpToDate.Date.HasValue)
            {
                var toExclusive = dpToDate.Date.Value.DateTime.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            // 3. Trạng thái
            string status = (cboStatusFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.RawStatus == status);
            }

            // 4. Sắp xếp theo phường/xã (giúp gom đơn theo khu vực giao hàng)
            string sort = (cboWardSort.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (sort == "asc") query = query.OrderBy(o => o.Ward).ThenByDescending(o => o.OrderId);
            else if (sort == "desc") query = query.OrderByDescending(o => o.Ward).ThenByDescending(o => o.OrderId);

            var result = query.ToList();
            OrderListView.ItemsSource = result;
            txtResultCount.Text = $"{result.Count}/{_allOrders.Count} đơn";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void Filter_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (!_isReady) return;
            if (dpFromDate.Date.HasValue && dpToDate.Date.HasValue && dpFromDate.Date > dpToDate.Date)
            {
                ShowMessage("Lỗi thời gian", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                sender.Date = null;
                return;
            }
            ApplyFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            _isReady = false; // tránh lọc lại 4 lần khi xóa từng ô
            txtSearch.Text = "";
            dpFromDate.Date = null;
            dpToDate.Date = null;
            cboStatusFilter.SelectedIndex = 0;
            cboWardSort.SelectedIndex = 0;
            _isReady = true;
            ApplyFilters();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        // ================= CHI TIẾT ĐƠN =================
        private async void btnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                DetailContentPanel.Children.Clear();
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var order = context.Orders
                            .AsNoTracking()
                            .Include(o => o.User)
                            .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                            .FirstOrDefault(o => o.OrderId == orderId);

                        if (order != null)
                        {
                            DetailDialog.Title = $"Chi tiết đơn hàng #{order.OrderId} - {order.OrderCode}";

                            AddLine($"Ngày đặt: {order.OrderDate:dd/MM/yyyy HH:mm}");
                            AddLine($"Trạng thái: {GetStatusText(order.Status)}" +
                                    (order.UpdatedAt.HasValue ? $" (cập nhật {order.UpdatedAt:dd/MM/yyyy HH:mm})" : ""), bold: true);
                            AddLine($"Thanh toán: {GetPaymentText(order.PaymentMethod)}");
                            AddLine($"Tài khoản đặt: {order.User?.FullName} ({order.User?.Username})");
                            AddLine($"Người nhận: {order.ReceiverName} - SĐT: {order.ReceiverPhone}", bold: true, top: 10);
                            AddLine($"Địa chỉ: {order.ShippingAddress}, {order.Ward}, {order.District}, {order.City}");
                            if (!string.IsNullOrWhiteSpace(order.Note)) AddLine($"Ghi chú: {order.Note}");

                            AddLine("Danh sách sản phẩm:", bold: true, top: 10);
                            foreach (var detail in order.OrderDetails)
                            {
                                string productName = detail.Product?.Name ?? "Sản phẩm đã bị xóa";
                                decimal lineTotal = detail.Quantity * detail.UnitPrice;
                                AddLine($"- {productName} x{detail.Quantity} × {detail.UnitPrice:N0} ₫ = {lineTotal:N0} ₫", left: 10);
                            }

                            DetailContentPanel.Children.Add(new TextBlock
                            {
                                Text = $"TỔNG CỘNG: {order.TotalAmount:N0} ₫",
                                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                                FontSize = 16,
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                                Margin = new Thickness(0, 10, 0, 0)
                            });

                            await DetailDialog.ShowAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", ex.Message);
                }
            }
        }

        private void AddLine(string text, bool bold = false, double top = 0, double left = 0)
        {
            DetailContentPanel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = bold ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                Margin = new Thickness(left, top, 0, 0)
            });
        }

        // ================= CHUYỂN TRẠNG THÁI =================
        // Luồng hợp lệ (trigger trg_Orders_StatusFlow cũng kiểm tra lại ở DB):
        //   Pending -> Confirmed -> Delivered ; Pending/Confirmed -> Cancelled
        private async void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var order = context.Orders.Find(orderId);
                        if (order == null) return;

                        string nextStatus;
                        if (order.Status == "Pending") nextStatus = "Confirmed";
                        else if (order.Status == "Confirmed") nextStatus = "Delivered";
                        else
                        {
                            ShowMessage("Thông báo", "Đơn hàng này đã ở trạng thái cuối, không thể duyệt thêm.");
                            return;
                        }

                        ContentDialog confirmDialog = new ContentDialog
                        {
                            Title = "Xác nhận",
                            Content = $"Chuyển đơn #{order.OrderId} từ \"{GetStatusText(order.Status)}\" sang \"{GetStatusText(nextStatus)}\"?",
                            PrimaryButtonText = "Đồng ý",
                            CloseButtonText = "Không",
                            DefaultButton = ContentDialogButton.Primary,
                            XamlRoot = this.Content.XamlRoot
                        };
                        if (await confirmDialog.ShowAsync() != ContentDialogResult.Primary) return;

                        order.Status = nextStatus;
                        order.UpdatedAt = DateTime.Now;
                        context.SaveChanges();
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ShowMessage("Lỗi cập nhật", msg);
                }
            }
        }

        private async void btnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận hủy",
                    Content = "Bạn có chắc chắn muốn hủy đơn hàng này? Hàng sẽ được tự động trả lại kho.",
                    PrimaryButtonText = "Hủy đơn",
                    CloseButtonText = "Không",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var context = new CoffeeShopContext())
                        {
                            var order = context.Orders.Find(orderId);
                            if (order != null)
                            {
                                if (order.Status == "Delivered" || order.Status == "Cancelled")
                                {
                                    ShowMessage("Lỗi", "Đơn hàng đã giao hoặc đã hủy, không thể hủy nữa!");
                                    return;
                                }

                                // Chỉ cần đổi trạng thái, SQL Trigger sẽ tự động cộng lại hàng vào kho
                                order.Status = "Cancelled";
                                order.UpdatedAt = DateTime.Now;
                                context.SaveChanges();
                            }
                        }
                        LoadData();
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ShowMessage("Lỗi cập nhật", msg);
                    }
                }
            }
        }

        private async void ShowMessage(string title, string content)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Đóng",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
