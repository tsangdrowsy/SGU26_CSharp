using App67.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models;

namespace App67.Pages
{
    public sealed partial class InventoryPage : Page
    {
        // ===================== CÁC LỚP HIỂN THỊ =====================
        public class ReceiptDisplayModel
        {
            public int ReceiptId { get; set; }
            public string ReceiptCode { get; set; } = string.Empty;
            public string DateString { get; set; } = string.Empty;
            public string CreatorName { get; set; } = string.Empty;
            public int ItemCount { get; set; }
            public string TotalValueString { get; set; } = string.Empty;
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

        // Sản phẩm trong ComboBox (phiếu nhập, báo cáo)
        public class ProductOption
        {
            public int ProductId { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public int StockQuantity { get; set; }
            public decimal CostPrice { get; set; }
            public decimal ProfitRate { get; set; }
            public decimal SalePrice { get; set; }
        }

        // 1 dòng sản phẩm trong phiếu nhập nháp
        public class ReceiptLineModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal ImportPrice { get; set; }
            public string QuantityString => $"SL: {Quantity}";
            public string PriceString => $"{ImportPrice:N0} ₫";
        }

        public class StockAtDateModel
        {
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Opening { get; set; }
            public int InQuantity { get; set; }
            public int OutQuantity { get; set; }
            public int Closing { get; set; }
            public decimal CostPrice { get; set; }
            public string CostPriceString => $"{CostPrice:N0} ₫";
            public string ClosingValueString => $"{Closing * CostPrice:N0} ₫";
        }

        public class ReportLineModel
        {
            public DateTime Date { get; set; }
            public string DateString => Date.ToString("dd/MM/yyyy");
            public string ProductName { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public string TotalString => $"{Quantity * UnitPrice:N0} ₫";
        }

        public class WarningModel
        {
            public string Code { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Stock { get; set; }
            public string LevelString { get; set; } = string.Empty;
            public SolidColorBrush LevelColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Black);
            public string VisibleString { get; set; } = string.Empty;
        }

        // Danh sách sản phẩm dùng cho dialog phiếu nhập
        private List<ProductOption> _productOptions = new();

        public InventoryPage()
        {
            this.InitializeComponent();
            LoadFilterSources();
            LoadReceipts();
            LoadAllTransactions();
            LoadWarnings();
        }

        // Nạp dữ liệu cho các ComboBox lọc (danh mục, sản phẩm)
        private void LoadFilterSources()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var categories = context.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();

                    List<Category> WithAll()
                    {
                        var list = new List<Category> { new Category { CategoryId = 0, Name = "Tất cả danh mục" } };
                        list.AddRange(categories);
                        return list;
                    }

                    foreach (var cbo in new[] { cboStockCategory, cboWarningCategory })
                    {
                        cbo.ItemsSource = WithAll();
                        cbo.DisplayMemberPath = "Name";
                        cbo.SelectedValuePath = "CategoryId";
                        cbo.SelectedIndex = 0;
                    }

                    _productOptions = context.Products
                        .AsNoTracking()
                        .OrderBy(p => p.Name)
                        .Select(p => new ProductOption
                        {
                            ProductId = p.ProductId,
                            DisplayName = p.Name + " (" + p.Code + ")" + (p.IsVisible ? "" : " [đã ẩn]"),
                            StockQuantity = p.StockQuantity,
                            CostPrice = p.CostPrice,
                            ProfitRate = p.ProfitRate,
                            SalePrice = p.SalePrice
                        })
                        .ToList();

                    var reportProducts = new List<ProductOption> { new ProductOption { ProductId = 0, DisplayName = "Tất cả sản phẩm" } };
                    reportProducts.AddRange(_productOptions);
                    cboReportProduct.ItemsSource = reportProducts;
                    cboReportProduct.DisplayMemberPath = "DisplayName";
                    cboReportProduct.SelectedValuePath = "ProductId";
                    cboReportProduct.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải bộ lọc kho: " + ex.Message);
            }
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
                    var query = context.ImportReceipts.AsNoTracking().AsQueryable();

                    if (start.HasValue) query = query.Where(r => r.ImportDate >= start.Value);
                    if (end.HasValue) query = query.Where(r => r.ImportDate <= end.Value);

                    // Đếm số mặt hàng và tổng giá trị ngay trong SQL (COUNT/SUM), không tải chi tiết về
                    var receipts = query
                        .OrderByDescending(r => r.ReceiptId)
                        .Select(r => new
                        {
                            r.ReceiptId,
                            r.ReceiptCode,
                            r.ImportDate,
                            r.Status,
                            CreatorName = r.CreatedByNavigation.FullName,
                            ItemCount = r.ImportReceiptDetails.Count(),
                            TotalValue = r.ImportReceiptDetails.Sum(d => d.Quantity * d.ImportPrice)
                        })
                        .ToList();

                    var display = receipts.Select(r => new ReceiptDisplayModel
                    {
                        ReceiptId = r.ReceiptId,
                        ReceiptCode = r.ReceiptCode,
                        DateString = r.ImportDate.ToString("dd/MM/yyyy HH:mm"),
                        CreatorName = r.CreatorName ?? "N/A",
                        ItemCount = r.ItemCount,
                        TotalValueString = r.TotalValue.ToString("N0") + " ₫",
                        StatusString = r.Status == "Completed" ? "Đã hoàn thành" : "Bản nháp",
                        StatusColor = r.Status == "Completed" ? new SolidColorBrush(Microsoft.UI.Colors.Green) : new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        CompleteButtonVisibility = r.Status == "Draft" ? Visibility.Visible : Visibility.Collapsed
                    }).ToList();

                    ReceiptListView.ItemsSource = display;
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi tải dữ liệu", ex.Message); }
        }

        private void btnFilterReceipts_Click(object sender, RoutedEventArgs e)
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
            dpImportDate.Date = DateTimeOffset.Now;
            txtReceiptError.Visibility = Visibility.Collapsed;
            txtAutoCodeNotice.Visibility = Visibility.Visible;
            CreateReceiptDialog.Title = "Tạo Phiếu Nhập Mới";
            CreateReceiptDialog.PrimaryButtonText = "Tạo phiếu";
            CreateReceiptDialog.Tag = null;
            await CreateReceiptDialog.ShowAsync();
        }

        private void CreateReceiptDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Ngày nhập: giữ giờ hiện tại nếu là hôm nay; không cho chọn ngày tương lai
            DateTime importDate = dpImportDate.Date?.DateTime.Date ?? DateTime.Today;
            if (importDate > DateTime.Today)
            {
                args.Cancel = true;
                txtReceiptError.Text = "Ngày nhập không được ở tương lai.";
                txtReceiptError.Visibility = Visibility.Visible;
                return;
            }
            importDate = importDate == DateTime.Today ? DateTime.Now : importDate.AddHours(8);

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
                            if (receipt.ImportDate.Date != importDate.Date) receipt.ImportDate = importDate;
                            context.SaveChanges();
                        }
                    }
                    else
                    {
                        string newCode = "PN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(10, 99);
                        var newReceipt = new ImportReceipt
                        {
                            ReceiptCode = newCode,
                            ImportDate = importDate,
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
                txtReceiptError.Text = "Không thể lưu: " + (ex.InnerException?.Message ?? ex.Message);
                txtReceiptError.Visibility = Visibility.Visible;
            }
        }

        private async void btnCompleteReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                try
                {
                    // Kiểm tra trước để báo lỗi dễ hiểu (trigger cũng chặn phiếu rỗng)
                    using (var context = new CoffeeShopContext())
                    {
                        if (!context.ImportReceiptDetails.Any(d => d.ReceiptId == receiptId))
                        {
                            ShowMessage("Không thể hoàn thành", "Phiếu nhập chưa có sản phẩm nào. Bấm \"Sản phẩm\" để thêm trước.");
                            return;
                        }
                    }
                }
                catch (Exception ex) { ShowMessage("Lỗi", ex.Message); return; }

                ContentDialog confirm = new ContentDialog
                {
                    Title = "Xác nhận",
                    Content = "Hoàn thành phiếu nhập này? Hàng sẽ được tự động cộng vào kho và tính lại giá vốn.\nSau khi hoàn thành, phiếu KHÔNG sửa hoặc xóa được nữa.",
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
                            if (receipt != null && receipt.Status == "Draft")
                            {
                                // Trigger trg_ImportReceipts_AfterUpdate tự: cộng tồn kho, tính giá nhập bình quân,
                                // ghi sổ kho IMPORT. Phải set CompletedAt vì CHECK constraint yêu cầu.
                                receipt.Status = "Completed";
                                receipt.CompletedAt = DateTime.Now;
                                context.SaveChanges();
                            }
                        }
                        LoadFilterSources(); // tồn kho & giá vốn đã đổi
                        LoadReceipts();
                        LoadAllTransactions();
                        LoadWarnings();
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ShowMessage("Không thể hoàn thành", "Chi tiết: " + msg);
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
                            dpImportDate.Date = new DateTimeOffset(receipt.ImportDate);
                            txtReceiptError.Visibility = Visibility.Collapsed;
                            txtAutoCodeNotice.Visibility = Visibility.Collapsed;
                            CreateReceiptDialog.Title = $"Sửa Phiếu Nháp {receipt.ReceiptCode}";
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
                                // Chi tiết phiếu tự xóa theo ON DELETE CASCADE
                                context.ImportReceipts.Remove(receipt);
                                context.SaveChanges();
                            }
                        }
                        LoadReceipts();
                    }
                    catch (Exception ex) { ShowMessage("Lỗi xóa", ex.InnerException?.Message ?? ex.Message); }
                }
            }
        }

        // =================================================================
        // QUẢN LÝ SẢN PHẨM TRONG PHIẾU NHÁP (thêm / sửa / xóa dòng)
        // =================================================================
        private async void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                try
                {
                    cboProducts.ItemsSource = _productOptions;
                    cboProducts.SelectedIndex = -1;
                    txtImportQuantity.Text = "1";
                    txtImportPrice.Text = "0";
                    txtProductInfo.Text = "";
                    txtProductError.Visibility = Visibility.Collapsed;

                    AddProductDialog.Tag = receiptId;
                    LoadReceiptLines(receiptId);
                    await AddProductDialog.ShowAsync();

                    LoadReceipts(); // cập nhật cột Số MH / Tổng giá trị sau khi đóng dialog
                }
                catch (Exception ex) { ShowMessage("Lỗi", ex.Message); }
            }
        }

        private List<ReceiptLineModel> _currentLines = new();

        private void LoadReceiptLines(int receiptId)
        {
            using (var context = new CoffeeShopContext())
            {
                _currentLines = context.ImportReceiptDetails
                    .AsNoTracking()
                    .Where(d => d.ReceiptId == receiptId)
                    .Select(d => new ReceiptLineModel
                    {
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        Quantity = d.Quantity,
                        ImportPrice = d.ImportPrice
                    })
                    .ToList();
            }
            ReceiptLinesListView.ItemsSource = _currentLines;
            decimal total = _currentLines.Sum(l => l.Quantity * l.ImportPrice);
            txtLinesTotal.Text = _currentLines.Count == 0
                ? "(Phiếu chưa có sản phẩm nào)"
                : $"{_currentLines.Count} mặt hàng - Tổng giá trị: {total:N0} ₫";
        }

        // Chọn sản phẩm: nếu đã có trong phiếu thì đổ số lượng & giá cũ để sửa
        private void cboProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboProducts.SelectedItem is ProductOption option)
            {
                var existing = _currentLines.FirstOrDefault(l => l.ProductId == option.ProductId);
                if (existing != null)
                {
                    txtImportQuantity.Text = existing.Quantity.ToString();
                    txtImportPrice.Text = existing.ImportPrice.ToString("0.##");
                }
                else if (option.CostPrice > 0)
                {
                    txtImportPrice.Text = option.CostPrice.ToString("0.##"); // gợi ý theo giá nhập BQ hiện tại
                }
            }
            UpdateProductInfo();
        }

        private void ImportInput_TextChanged(object sender, TextChangedEventArgs e) => UpdateProductInfo();

        // Ước tính giá nhập bình quân & giá bán SAU khi hoàn thành phiếu (giống công thức trigger T4)
        //   Giá BQ mới = (Tồn × Giá BQ cũ + SL nhập × Giá nhập) / (Tồn + SL nhập)
        private void UpdateProductInfo()
        {
            if (cboProducts?.SelectedItem is not ProductOption p) { if (txtProductInfo != null) txtProductInfo.Text = ""; return; }

            string info = $"Tồn hiện tại: {p.StockQuantity} | Giá nhập BQ: {p.CostPrice:N0} ₫ | Lãi: {p.ProfitRate:0.##}% | Giá bán: {p.SalePrice:N0} ₫";
            if (int.TryParse(txtImportQuantity.Text, out int qty) && qty > 0 &&
                decimal.TryParse(txtImportPrice.Text, out decimal price) && price > 0)
            {
                decimal newCost = (p.StockQuantity * p.CostPrice + qty * price) / (p.StockQuantity + qty);
                decimal newSale = Math.Round(newCost * (100 + p.ProfitRate) / 100, 2);
                info += $"\n→ Ước tính sau khi hoàn thành phiếu: Giá nhập BQ {newCost:N0} ₫, Giá bán {newSale:N0} ₫";
            }
            txtProductInfo.Text = info;
        }

        private void AddProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Luôn giữ dialog mở để nhập tiếp nhiều sản phẩm; bấm "Xong" để đóng
            args.Cancel = true;

            // CK_ImportReceiptDetails_Quantity (> 0) và CK_ImportReceiptDetails_Price (> 0)
            if (cboProducts.SelectedValue is not int productId || !int.TryParse(txtImportQuantity.Text, out int qty) || qty <= 0 || !decimal.TryParse(txtImportPrice.Text, out decimal price) || price <= 0)
            {
                txtProductError.Text = "Vui lòng chọn sản phẩm, nhập Số lượng > 0 và Giá nhập > 0.";
                txtProductError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                int receiptId = (int)AddProductDialog.Tag;

                using (var context = new CoffeeShopContext())
                {
                    var existingDetail = context.ImportReceiptDetails
                        .FirstOrDefault(d => d.ReceiptId == receiptId && d.ProductId == productId);

                    if (existingDetail != null)
                    {
                        // Sửa dòng: GHI ĐÈ số lượng & giá (bản cũ cộng dồn số lượng nên không sửa được)
                        existingDetail.Quantity = qty;
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

                LoadReceiptLines(receiptId);
                cboProducts.SelectedIndex = -1;
                txtImportQuantity.Text = "1";
                txtImportPrice.Text = "0";
                txtProductError.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                txtProductError.Text = "Lỗi: " + msg;
                txtProductError.Visibility = Visibility.Visible;
            }
        }

        private void btnRemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId && AddProductDialog.Tag is int receiptId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        // Trigger trg_ImportReceiptDetails_LockCompleted chặn nếu phiếu đã hoàn thành
                        context.ImportReceiptDetails
                            .Where(d => d.ReceiptId == receiptId && d.ProductId == productId)
                            .ExecuteDelete();
                    }
                    LoadReceiptLines(receiptId);
                }
                catch (Exception ex)
                {
                    txtProductError.Text = "Lỗi xóa dòng: " + (ex.InnerException?.Message ?? ex.Message);
                    txtProductError.Visibility = Visibility.Visible;
                }
            }
        }

        private async void btnViewReceiptDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int receiptId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        // Lấy thông tin phiếu kèm theo danh sách chi tiết và tên sản phẩm
                        var receipt = context.ImportReceipts
                            .AsNoTracking()
                            .Include(r => r.CreatedByNavigation)
                            .Include(r => r.ImportReceiptDetails)
                            .ThenInclude(d => d.Product)
                            .FirstOrDefault(r => r.ReceiptId == receiptId);

                        if (receipt != null)
                        {
                            // Tạo nội dung hiển thị
                            string detailText = $"Mã phiếu: {receipt.ReceiptCode}\n";
                            detailText += $"Ngày nhập: {receipt.ImportDate:dd/MM/yyyy HH:mm}\n";
                            detailText += $"Người tạo: {receipt.CreatedByNavigation?.FullName}\n";
                            detailText += $"Trạng thái: {(receipt.Status == "Completed" ? $"Đã hoàn thành ({receipt.CompletedAt:dd/MM/yyyy HH:mm})" : "Bản nháp")}\n";
                            detailText += $"Ghi chú: {receipt.Note ?? "Không có"}\n";
                            detailText += "----------------------------------------\n";
                            detailText += "DANH SÁCH SẢN PHẨM:\n";

                            decimal totalAmount = 0;

                            foreach (var item in receipt.ImportReceiptDetails)
                            {
                                string productName = item.Product?.Name ?? "SP đã xóa";
                                decimal lineTotal = item.Quantity * item.ImportPrice;
                                totalAmount += lineTotal;
                                detailText += $"- {productName} | SL: {item.Quantity} | Giá: {item.ImportPrice:N0}đ | T.Tiền: {lineTotal:N0}đ\n";
                            }

                            if (!receipt.ImportReceiptDetails.Any())
                            {
                                detailText += "(Phiếu này chưa có sản phẩm nào)\n";
                            }

                            detailText += "----------------------------------------\n";
                            detailText += $"TỔNG TIỀN NHẬP: {totalAmount:N0} VNĐ";

                            // Hiển thị lên màn hình bằng ContentDialog
                            ContentDialog dialog = new ContentDialog
                            {
                                Title = "Chi Tiết Phiếu Nhập",
                                Content = new ScrollViewer
                                {
                                    Content = new TextBlock { Text = detailText, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Consolas") },
                                    MaxHeight = 400
                                },
                                CloseButtonText = "Đóng",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await dialog.ShowAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", ex.Message);
                }
            }
        }

        // =================================================================
        // XỬ LÝ TAB 2: LỊCH SỬ KHO
        // =================================================================
        private static string GetTransactionTypeText(string type) => type switch
        {
            "INITIAL" => "Tồn ban đầu",
            "IMPORT" => "Nhập hàng",
            "EXPORT" => "Bán hàng",
            "RETURN" => "Trả về (hủy đơn)",
            _ => type
        };

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
                    var query = context.InventoryTransactions.AsNoTracking().Include(t => t.Product).AsQueryable();

                    if (start.HasValue) query = query.Where(t => t.TransactionDate >= start.Value);
                    if (end.HasValue) query = query.Where(t => t.TransactionDate <= end.Value);

                    var transactions = query.OrderByDescending(t => t.TransactionDate).ToList();

                    var displayList = transactions.Select(t => new TransactionDisplayModel
                    {
                        DateString = t.TransactionDate.ToString("dd/MM/yyyy HH:mm"),
                        ProductName = t.Product?.Name ?? "Sản phẩm đã bị xóa",
                        TypeString = GetTransactionTypeText(t.TransactionType),
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

        private void btnFilterTransactions_Click(object sender, RoutedEventArgs e)
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

        // =================================================================
        // TAB 3: TỒN KHO THEO NGÀY
        // Tồn cuối ngày D = SUM(QuantityChange) của sổ kho có TransactionDate < D + 1 ngày
        //   Tồn đầu ngày = phần trước 00:00 ngày D ; Nhập/Xuất = phần dương/âm TRONG ngày D
        // =================================================================
        private void btnViewStockAtDate_Click(object sender, RoutedEventArgs e)
        {
            DateTime dayStart = dpStockDate.Date?.DateTime.Date ?? DateTime.Today;
            if (dayStart > DateTime.Today)
            {
                ShowMessage("Lỗi thời gian", "Không thể xem tồn kho của ngày trong tương lai.");
                return;
            }
            dpStockDate.Date = new DateTimeOffset(dayStart);
            DateTime dayEnd = dayStart.AddDays(1);
            int categoryId = cboStockCategory.SelectedValue is int c ? c : 0;

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Gom sổ kho theo sản phẩm bằng 1 câu GROUP BY (SUM có điều kiện = SUM(CASE WHEN ...))
                    var ledger = context.InventoryTransactions
                        .Where(t => t.TransactionDate < dayEnd)
                        .GroupBy(t => t.ProductId)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            Opening = g.Sum(t => t.TransactionDate < dayStart ? t.QuantityChange : 0),
                            In = g.Sum(t => t.TransactionDate >= dayStart && t.QuantityChange > 0 ? t.QuantityChange : 0),
                            Out = g.Sum(t => t.TransactionDate >= dayStart && t.QuantityChange < 0 ? -t.QuantityChange : 0)
                        })
                        .ToDictionary(x => x.ProductId);

                    var products = context.Products
                        .AsNoTracking()
                        .Where(p => categoryId == 0 || p.CategoryId == categoryId)
                        .OrderBy(p => p.Name)
                        .Select(p => new { p.ProductId, p.Code, p.Name, CategoryName = p.Category.Name, p.CostPrice })
                        .ToList();

                    var rows = products.Select(p =>
                    {
                        ledger.TryGetValue(p.ProductId, out var l);
                        int opening = l?.Opening ?? 0, inQty = l?.In ?? 0, outQty = l?.Out ?? 0;
                        return new StockAtDateModel
                        {
                            Code = p.Code,
                            ProductName = p.Name,
                            CategoryName = p.CategoryName,
                            Opening = opening,
                            InQuantity = inQty,
                            OutQuantity = outQty,
                            Closing = opening + inQty - outQty,
                            CostPrice = p.CostPrice
                        };
                    }).ToList();

                    StockAtDateListView.ItemsSource = rows;

                    decimal totalValue = rows.Sum(r => r.Closing * r.CostPrice);
                    txtStockAtDateSummary.Text =
                        $"Tồn kho cuối ngày {dayStart:dd/MM/yyyy}: {rows.Sum(r => r.Closing)} sản phẩm, " +
                        $"giá trị ≈ {totalValue:N0} ₫ (tính theo giá nhập bình quân HIỆN TẠI). " +
                        $"Trong ngày: nhập {rows.Sum(r => r.InQuantity)}, xuất {rows.Sum(r => r.OutQuantity)}.";
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi", ex.InnerException?.Message ?? ex.Message); }
        }

        // =================================================================
        // TAB 4: BÁO CÁO NHẬP - XUẤT TRONG KHOẢNG THỜI GIAN
        // =================================================================
        private void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            // Không chọn ngày bắt đầu = từ đầu ; không chọn ngày kết thúc = đến hôm nay
            DateTime from = dpReportFrom.Date?.DateTime.Date ?? new DateTime(2000, 1, 1);
            DateTime toExclusive = (dpReportTo.Date?.DateTime.Date ?? DateTime.Today).AddDays(1);
            if (from >= toExclusive)
            {
                ShowMessage("Lỗi thời gian", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return;
            }
            int productId = cboReportProduct.SelectedValue is int p ? p : 0;

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // ---- 1. Tóm tắt từ sổ kho ----
                    var ledger = context.InventoryTransactions.Where(t => productId == 0 || t.ProductId == productId);
                    var inRange = ledger.Where(t => t.TransactionDate >= from && t.TransactionDate < toExclusive);

                    int opening = ledger.Where(t => t.TransactionDate < from).Sum(t => (int?)t.QuantityChange) ?? 0;
                    int imported = inRange.Where(t => t.TransactionType == "IMPORT" || t.TransactionType == "INITIAL").Sum(t => (int?)t.QuantityChange) ?? 0;
                    int returned = inRange.Where(t => t.TransactionType == "RETURN").Sum(t => (int?)t.QuantityChange) ?? 0;
                    int exported = -(inRange.Where(t => t.TransactionType == "EXPORT").Sum(t => (int?)t.QuantityChange) ?? 0);
                    int closing = opening + imported + returned - exported;

                    txtReportOpening.Text = opening.ToString("N0");
                    txtReportIn.Text = "+" + imported.ToString("N0");
                    txtReportOut.Text = "-" + exported.ToString("N0");
                    txtReportReturn.Text = "+" + returned.ToString("N0");
                    txtReportClosing.Text = closing.ToString("N0");

                    if (productId > 0)
                    {
                        decimal cost = context.Products.Where(x => x.ProductId == productId).Select(x => x.CostPrice).FirstOrDefault();
                        txtReportClosingValue.Text = $"≈ {closing * cost:N0} ₫ (giá BQ {cost:N0} ₫)";
                    }
                    else
                    {
                        txtReportClosingValue.Text = "";
                    }

                    // ---- 2. Chi tiết nhập: phiếu đã hoàn thành trong kỳ (tính theo ngày hoàn thành = ngày cộng kho) ----
                    var imports = context.ImportReceiptDetails
                        .AsNoTracking()
                        .Where(d => d.Receipt.Status == "Completed"
                                    && d.Receipt.CompletedAt >= from && d.Receipt.CompletedAt < toExclusive
                                    && (productId == 0 || d.ProductId == productId))
                        .OrderBy(d => d.Receipt.CompletedAt)
                        .Select(d => new ReportLineModel
                        {
                            Date = d.Receipt.CompletedAt ?? d.Receipt.ImportDate,
                            ProductName = d.Product.Name,
                            Reference = "Phiếu " + d.Receipt.ReceiptCode,
                            Quantity = d.Quantity,
                            UnitPrice = d.ImportPrice
                        })
                        .ToList();

                    // ---- 3. Chi tiết xuất: đơn hàng đặt trong kỳ (kho bị trừ ngay lúc đặt) ----
                    var exports = context.OrderDetails
                        .AsNoTracking()
                        .Where(d => d.Order.OrderDate >= from && d.Order.OrderDate < toExclusive
                                    && (productId == 0 || d.ProductId == productId))
                        .OrderBy(d => d.Order.OrderDate)
                        .Select(d => new { d.Order.OrderDate, d.Order.OrderCode, d.Order.Status, ProductName = d.Product.Name, d.Quantity, d.UnitPrice })
                        .ToList()
                        .Select(d => new ReportLineModel
                        {
                            Date = d.OrderDate,
                            ProductName = d.ProductName,
                            Reference = $"Đơn {d.OrderCode} - {OrdersPage.GetStatusText(d.Status)}",
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice
                        })
                        .ToList();

                    ReportImportListView.ItemsSource = imports;
                    ReportExportListView.ItemsSource = exports;
                    txtImportTitle.Text = $"Chi tiết nhập hàng ({imports.Count} dòng, {imports.Sum(x => x.Quantity * x.UnitPrice):N0} ₫)";
                    txtExportTitle.Text = $"Chi tiết xuất hàng - đơn hàng ({exports.Count} dòng, {exports.Sum(x => x.Quantity * x.UnitPrice):N0} ₫)";
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi", ex.InnerException?.Message ?? ex.Message); }
        }

        // =================================================================
        // TAB 5: CẢNH BÁO TỒN KHO
        // DB chưa có cột ngưỡng riêng cho từng sản phẩm (PHP: CanhBaoTon),
        // nên dùng 1 ngưỡng chung do admin nhập (mặc định 10).
        // =================================================================
        private void btnFilterWarning_Click(object sender, RoutedEventArgs e) => LoadWarnings();

        private void LoadWarnings()
        {
            if (!int.TryParse(txtWarningThreshold.Text, out int threshold) || threshold < 0)
            {
                ShowMessage("Lỗi nhập liệu", "Ngưỡng cảnh báo phải là số nguyên không âm.");
                return;
            }
            int categoryId = cboWarningCategory.SelectedValue is int c ? c : 0;
            string keyword = txtWarningKeyword.Text.Trim();
            bool visibleOnly = chkWarningVisibleOnly.IsChecked == true;

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var query = context.Products.AsNoTracking().AsQueryable();
                    if (categoryId > 0) query = query.Where(p => p.CategoryId == categoryId);
                    if (visibleOnly) query = query.Where(p => p.IsVisible);
                    if (!string.IsNullOrEmpty(keyword)) query = query.Where(p => p.Name.Contains(keyword) || p.Code.Contains(keyword));

                    txtCountOutOfStock.Text = query.Count(p => p.StockQuantity == 0).ToString();
                    txtCountLowStock.Text = query.Count(p => p.StockQuantity > 0 && p.StockQuantity <= threshold).ToString();
                    txtCountOkStock.Text = query.Count(p => p.StockQuantity > threshold).ToString();

                    var warnings = query
                        .Where(p => p.StockQuantity <= threshold)
                        .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                        .Select(p => new { p.Code, p.Name, CategoryName = p.Category.Name, p.StockQuantity, p.IsVisible })
                        .ToList()
                        .Select(p => new WarningModel
                        {
                            Code = p.Code,
                            ProductName = p.Name,
                            CategoryName = p.CategoryName,
                            Stock = p.StockQuantity,
                            LevelString = p.StockQuantity == 0 ? "Hết hàng" : "Sắp hết",
                            LevelColor = new SolidColorBrush(p.StockQuantity == 0 ? Microsoft.UI.Colors.Red : Microsoft.UI.Colors.Orange),
                            VisibleString = p.IsVisible ? "Đang bán" : "Đã ẩn"
                        })
                        .ToList();

                    WarningListView.ItemsSource = warnings;
                }
            }
            catch (Exception ex) { ShowMessage("Lỗi", ex.InnerException?.Message ?? ex.Message); }
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
