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
    public sealed partial class InventoryPage : Page
    {
        public class ReceiptDisplayModel
        {
            public int ReceiptId { get; set; }
            public string ReceiptCode { get; set; } = string.Empty;
            public string DateString { get; set; } = string.Empty;
            public string CreatorName { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Black);
            public Visibility CompleteButtonVisibility { get; set; } = Visibility.Collapsed;
        }

        public class TransactionDisplayModel
        {
            public string DateString { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string TypeString { get; set; } = string.Empty;
            public string QuantityString { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
            public SolidColorBrush QuantityColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Black);
        }

        public InventoryPage()
        {
            this.InitializeComponent();
            LoadReceipts();
            LoadAllTransactions();
        }

        // =================================================================
        // XỬ LÝ TAB 1: PHIẾU NHẬP HÀNG
        // =================================================================
        private void LoadReceipts()
        {
            dpReceiptStartDate.Date = null;
            dpReceiptEndDate.Date = null;
            FilterReceipts(null, null);
        }

        private void FilterReceipts(DateTime? start, DateTime? end)
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var query = context.ImportReceipts.Include(r => r.CreatedByNavigation).AsQueryable();

                    if (start.HasValue) query = query.Where(r => r.ImportDate >= start.Value);
                    if (end.HasValue) query = query.Where(r => r.ImportDate <= end.Value);

                    var receipts = query.OrderByDescending(r => r.ReceiptId).ToList();

                    var display = receipts.Select(r => new ReceiptDisplayModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptCode = r.ReceiptCode,
                        DateString = r.ImportDate.ToString("dd/MM/yyyy HH:mm"),
                        CreatorName = r.CreatedByNavigation?.FullName ?? "N/A",
                        StatusString = r.Status == "Completed" ? "Đã hoàn thành" : "Bản nháp",
                        StatusColor = r.Status == "Completed" ? new SolidColorBrush(Microsoft.UI.Colors.Green) : new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        CompleteButtonVisibility = r.Status == "Draft" ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    ReceiptListView.ItemsSource = display;
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi tải dữ liệu", ex.Message); }
        }

        private async void btnFilterReceipts_Click(object sender, RoutedEventArgs e)
        {
            if (dpReceiptStartDate.Date == null || dpReceiptEndDate.Date == null)
            {
                ShowMessage("Thiếu thông tin", "Vui lòng chọn cả ngày bắt đầu và kết thúc.");
                return;
            }

            DateTime start = dpReceiptStartDate.Date.Value.DateTime.Date; // Lấy 00:00:00
            DateTime end = dpReceiptEndDate.Date.Value.DateTime.Date.AddDays(1).AddSeconds(-1); // Lấy 23:59:59

            if (start > end)
            {
                ShowMessage("Lỗi", "Ngày bắt đầu không được lớn hơn ngày kết thúc!");
                return;
            }

            FilterReceipts(start, end);
        }

        private void btnRefreshReceipts_Click(object sender, RoutedEventArgs e) => LoadReceipts();

        private async void btnCreateReceipt_Click(object sender, RoutedEventArgs e)
        {
            txtReceiptNote.Text = "";
            txtAutoCodeNotice.Visibility = Visibility.Visible;
            CreateReceiptDialog.Title = "Tạo Phiếu Nhập Mới";
            CreateReceiptDialog.PrimaryButtonText = "Tạo phiếu";
            CreateReceiptDialog.Tag = null;
            await CreateReceiptDialog.ShowAsync();
        }

        private void CreateReceiptDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    if (CreateReceiptDialog.Tag is int editId)
                    {
                        var receipt = context.ImportReceipts.Find(editId);
                        if (receipt != null)
                        {
                            receipt.Note = string.IsNullOrWhiteSpace(txtReceiptNote.Text) ? null : txtReceiptNote.Text.Trim();
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        string newCode = "PN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(10, 99);
                        var newReceipt = new ImportReceipt
                        {
                            ReceiptCode = newCode,
                            ImportDate = DateTime.Now,
                            Status = "Draft",
                            CreatedBy = App.CurrentAdminId,
                            Note = string.IsNullOrWhiteSpace(txtReceiptNote.Text) ? null : txtReceiptNote.Text.Trim()
                        };
                        context.ImportReceipts.Add(newReceipt);
                        context.SaveChanges();
                    }
                }
                LoadReceipts();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                ShowMessage("Lỗi", "Không thể lưu: " + ex.Message);
            }
        }

        private async void btnCompleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                ContentDialog confirm = new ContentDialog
                {
                    Title = "Xác nhận",
                    Content = "Hoàn thành phiếu nhập này? Hàng sẽ được tự động cộng vào kho và tính lại giá vốn.",
                    PrimaryButtonText = "Đồng ý",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var context = new CoffeeShopContext())
                        {
                            var receipt = context.ImportReceipts.Find(receiptId);
                            if (receipt != null)
                            {
                                receipt.Status = "Completed";
                                receipt.CompletedAt = DateTime.Now;
                                context.SaveChanges();
                            }
                        }
                        LoadReceipts();
                        LoadAllTransactions();
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ShowMessage("Không thể hoàn thành", "Lưu ý: Phiếu nhập phải có ít nhất 1 sản phẩm.\nChi tiết: " + msg);
                    }
                }
            }
        }

        private async void btnEditReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var receipt = context.ImportReceipts.Find(receiptId);
                        if (receipt != null && receipt.Status == "Draft")
                        {
                            txtReceiptNote.Text = receipt.Note ?? "";
                            txtAutoCodeNotice.Visibility = Visibility.Collapsed;
                            CreateReceiptDialog.Title = "Sửa Ghi Chú Phiếu Nháp";
                            CreateReceiptDialog.PrimaryButtonText = "Lưu thay đổi";
                            CreateReceiptDialog.Tag = receiptId;
                            await CreateReceiptDialog.ShowAsync();
                        }
                    }
                }
                catch (Exception ex) { ShowMessage("Lỗi", ex.Message); }
            }
        }

        private async void btnDeleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                ContentDialog confirm = new ContentDialog
                {
                    Title = "Xác nhận xóa",
                    Content = "Bạn có chắc chắn muốn xóa phiếu nháp này?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                {
                    try
                    {
                        using (var context = new CoffeeShopContext())
                        {
                            var receipt = context.ImportReceipts.Find(receiptId);
                            if (receipt != null && receipt.Status == "Draft")
                            {
                                context.ImportReceipts.Remove(receipt);
                                context.SaveChanges();
                            }
                        }
                        LoadReceipts();
                    }
                    catch (Exception ex) { ShowMessage("Lỗi xóa", ex.Message); }
                }
            }
        }

        // =================================================================
        // XỬ LÝ THÊM SẢN PHẨM VÀO PHIẾU
        // =================================================================
        private async void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        cboProducts.ItemsSource = context.Products.Where(p => p.IsVisible).ToList();
                        cboProducts.SelectedIndex = -1;
                        txtImportQuantity.Text = "1";
                        txtImportPrice.Text = "0";
                        txtProductError.Visibility = Visibility.Collapsed;

                        AddProductDialog.Tag = receiptId;
                        await AddProductDialog.ShowAsync();
                    }
                }
                catch (Exception ex) { ShowMessage("Lỗi", ex.Message); }
            }
        }

        private void AddProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (cboProducts.SelectedValue == null || !int.TryParse(txtImportQuantity.Text, out int qty) || qty <= 0 || !decimal.TryParse(txtImportPrice.Text, out decimal price) || price < 0)
            {
                args.Cancel = true;
                txtProductError.Text = "Vui lòng chọn sản phẩm và nhập Số lượng / Giá nhập hợp lệ.";
                txtProductError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                int receiptId = (int)AddProductDialog.Tag;
                int productId = (int)cboProducts.SelectedValue;

                using (var context = new CoffeeShopContext())
                {
                    var existingDetail = context.ImportReceiptDetails
                        .FirstOrDefault(d => d.ReceiptId == receiptId && d.ProductId == productId);

                    if (existingDetail != null)
                    {
                        existingDetail.Quantity += qty;
                        existingDetail.ImportPrice = price;
                    }
                    else
                    {
                        var newDetail = new ImportReceiptDetail
                        {
                            ReceiptId = receiptId,
                            ProductId = productId,
                            Quantity = qty,
                            ImportPrice = price
                        };
                        context.ImportReceiptDetails.Add(newDetail);
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                txtProductError.Text = "Lỗi: " + msg;
                txtProductError.Visibility = Visibility.Visible;
            }
        }

        // =================================================================
        // XỬ LÝ TAB 2: LỊCH SỬ KHO
        // =================================================================
        private void LoadAllTransactions()
        {
            dpStartDate.Date = null;
            dpEndDate.Date = null;
            FilterTransactions(null, null);
        }

        private void FilterTransactions(DateTime? start, DateTime? end)
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var query = context.InventoryTransactions.Include(t => t.Product).AsQueryable();

                    if (start.HasValue) query = query.Where(t => t.TransactionDate >= start.Value);
                    if (end.HasValue) query = query.Where(t => t.TransactionDate <= end.Value);

                    var transactions = query.OrderByDescending(t => t.TransactionDate).ToList();

                    var displayList = transactions.Select(t => new TransactionDisplayModel
                    {
                        DateString = t.TransactionDate.ToString("dd/MM/yyyy HH:mm"),
                        ProductName = t.Product?.Name ?? "Sản phẩm đã bị xóa",
                        TypeString = t.TransactionType,
                        QuantityString = t.QuantityChange > 0 ? $"+{t.QuantityChange}" : t.QuantityChange.ToString(),
                        Note = t.Note ?? "",
                        QuantityColor = t.QuantityChange > 0
                            ? new SolidColorBrush(Microsoft.UI.Colors.Green)
                            : new SolidColorBrush(Microsoft.UI.Colors.Red)
                    }).ToList();

                    TransactionListView.ItemsSource = displayList;
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi", ex.Message); }
        }

        private async void btnFilterTransactions_Click(object sender, RoutedEventArgs e)
        {
            if (dpStartDate.Date == null || dpEndDate.Date == null)
            {
                ShowMessage("Thiếu thông tin", "Vui lòng chọn cả ngày bắt đầu và ngày kết thúc.");
                return;
            }

            // SỬA LẠI LOGIC LẤY NGÀY KẾT THÚC: Bao gồm cả ngày cuối cùng
            DateTime start = dpStartDate.Date.Value.DateTime.Date;
            DateTime end = dpEndDate.Date.Value.DateTime.Date.AddDays(1).AddSeconds(-1);

            if (start > end)
            {
                ShowMessage("Lỗi thời gian", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return;
            }

            FilterTransactions(start, end);
        }

        private void btnReloadTransactions_Click(object sender, RoutedEventArgs e) => LoadAllTransactions();

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