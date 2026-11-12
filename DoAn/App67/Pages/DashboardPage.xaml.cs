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
using Windows.Foundation;
using Windows.Foundation.Collections;

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
        }

        // Lớp phụ để hiển thị cảnh báo kho
        public class LowStockModel
        {
            public string ProductName { get; set; } = string.Empty;
            public string StockString { get; set; } = string.Empty;
        }

        public DashboardPage()
        {
            this.InitializeComponent();
            LoadDashboardData();
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
                        .Sum(o => o.TotalAmount);
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


                    // 2. LẤY 5 ĐƠN HÀNG MỚI NHẤT
                    var recentOrders = context.Orders
                        .OrderByDescending(o => o.OrderId)
                        .Take(5)
                        .Select(o => new RecentOrderModel
                        {
                            OrderIdString = "#" + o.OrderId,
                            ReceiverName = o.ReceiverName,
                            TotalAmountString = o.TotalAmount.ToString("N0") + " ₫"
                        }).ToList();
                    RecentOrdersListView.ItemsSource = recentOrders;


                    // 3. LẤY SẢN PHẨM SẮP HẾT HÀNG (Tồn kho <= 10)
                    var lowStock = context.Products
                        .Where(p => p.IsVisible && p.StockQuantity <= 10)
                        .OrderBy(p => p.StockQuantity)
                        .Select(p => new LowStockModel
                        {
                            ProductName = p.Name,
                            StockString = $"Còn {p.StockQuantity}"
                        }).ToList();

                    if (lowStock.Count == 0)
                    {
                        lowStock.Add(new LowStockModel { ProductName = "Tất cả sản phẩm đều đủ hàng", StockString = "" });
                    }

                    LowStockListView.ItemsSource = lowStock;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải Dashboard: " + ex.Message);
            }
        }
    }
}
