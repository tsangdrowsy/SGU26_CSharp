using App67.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using WebApplication1.Models;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.EntityFrameworkCore;


namespace App67.Pages
{
    public sealed partial class OrdersPage : Page
    {
        public class OrderDisplayModel
        {
            public int OrderId { get; set; }
            public string OrderDateString { get; set; } = string.Empty;
            public string ReceiverName { get; set; } = string.Empty;
            public string TotalAmountString { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public string RawStatus { get; set; } = string.Empty;

            // Thuộc tính chứa màu sắc
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        public OrdersPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var dbOrders = context.Orders.OrderByDescending(o => o.OrderId).ToList();

                    var orders = dbOrders.Select(o => new OrderDisplayModel
                    {
                        OrderId = o.OrderId,
                        OrderDateString = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                        ReceiverName = o.ReceiverName,
                        TotalAmountString = o.TotalAmount.ToString("N0") + " ₫",
                        RawStatus = o.Status,
                        StatusString = o.Status == "Pending" ? "Chờ duyệt" :
                                       o.Status == "Confirmed" ? "Đã duyệt" :
                                       o.Status == "Delivered" ? "Đã giao" : "Đã hủy",

                        // SỬA LẠI CÚ PHÁP KHỞI TẠO MÀU
                        StatusColor = o.Status == "Pending" ? new SolidColorBrush(Microsoft.UI.Colors.Orange) :
                                      o.Status == "Confirmed" ? new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue) :
                                      o.Status == "Delivered" ? new SolidColorBrush(Microsoft.UI.Colors.Green) :
                                      new SolidColorBrush(Microsoft.UI.Colors.Red)
                    }).ToList();

                    OrderListView.ItemsSource = orders;
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu", ex.Message);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

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
                            .Include(o => o.OrderDetails)
                            .ThenInclude(od => od.Product)
                            .FirstOrDefault(o => o.OrderId == orderId);

                        if (order != null)
                        {
                            DetailContentPanel.Children.Add(new TextBlock { Text = $"Người nhận: {order.ReceiverName} - SĐT: {order.ReceiverPhone}", FontWeight = Microsoft.UI.Text.FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
                            DetailContentPanel.Children.Add(new TextBlock { Text = $"Địa chỉ: {order.ShippingAddress}, {order.Ward}, {order.District}, {order.City}", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 15) });

                            DetailContentPanel.Children.Add(new TextBlock { Text = "Danh sách sản phẩm:", FontWeight = Microsoft.UI.Text.FontWeights.Bold });

                            foreach (var detail in order.OrderDetails)
                            {
                                string productName = detail.Product?.Name ?? "Sản phẩm đã bị xóa";
                                DetailContentPanel.Children.Add(new TextBlock
                                {
                                    Text = $"- {productName} x{detail.Quantity} : {detail.UnitPrice.ToString("N0")} ₫",
                                    Margin = new Thickness(10, 5, 0, 0)
                                });
                            }

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

        private async void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int orderId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var order = context.Orders.Find(orderId);
                        if (order != null)
                        {
                            if (order.Status == "Pending")
                                order.Status = "Confirmed";
                            else if (order.Status == "Confirmed")
                                order.Status = "Delivered";
                            else
                            {
                                ShowMessage("Thông báo", "Đơn hàng này đã ở trạng thái cuối, không thể duyệt thêm.");
                                return;
                            }

                            context.SaveChanges();
                            LoadData();
                        }
                    }
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
                                if (order.Status == "Delivered")
                                {
                                    ShowMessage("Lỗi", "Đơn hàng đã giao, không thể hủy!");
                                    return;
                                }

                                // Chỉ cần đổi trạng thái, SQL Trigger sẽ tự động cộng lại hàng vào kho
                                order.Status = "Cancelled";
                                context.SaveChanges();
                                LoadData();
                            }
                        }
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
