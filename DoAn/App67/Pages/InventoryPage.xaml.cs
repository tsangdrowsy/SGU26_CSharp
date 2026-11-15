using App67.Data;
using App67.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace App67.Pages
{
    // Cách tải dữ liệu (tương đương AJAX bên inventory.php):
    //  - Không tải trong constructor: chờ trang hiển thị (Loaded) rồi mới tải bất đồng bộ.
    //  - Mỗi tab TỰ TẢI LẠI khi được chọn (giống showTab() của PHP) -> luôn thấy số liệu mới,
    //    vd hoàn thành phiếu nhập xong qua tab "Tồn kho theo ngày" là thấy ngay.
    //  - Đổi ngày / danh mục / sản phẩm / ngưỡng -> tự lọc lại, không cần bấm nút.
    //  - Truy vấn nằm trong InventoryReportService (đã kiểm thử với SQL Server thật); trang chỉ hiển thị.
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

        // Thứ tự tab trong TabView
        private const int TabReceipts = 0, TabTransactions = 1, TabStock = 2, TabReport = 3, TabWarning = 4;

        private readonly InventoryReportService _service = new();

        // Danh sách sản phẩm dùng cho dialog phiếu nhập
        private List<ProductOptionRow> _productOptions = new();

        // false cho tới khi tải xong lần đầu: chặn các sự kiện SelectionChanged/DateChanged
        // bắn ra trong lúc InitializeComponent hoặc lúc code tự gán giá trị cho bộ lọc
        private bool _isReady;
        private bool _suppressFilterEvents;

        // Mỗi tab 1 bộ đếm phiên bản: kết quả về muộn của lần tải cũ bị bỏ qua
        private readonly int[] _tabVersion = new int[5];
        private int _activeLoads;

        // Gõ ngưỡng/từ khóa: chờ ngừng gõ 400ms mới tải (debounce), không truy vấn mỗi phím
        private readonly DispatcherTimer _warningDebounce = new() { Interval = TimeSpan.FromMilliseconds(400) };

        public InventoryPage()
        {
            this.InitializeComponent();
            _warningDebounce.Tick += async (s, e) => { _warningDebounce.Stop(); await RefreshWarningsAsync(); };
            this.Loaded += InventoryPage_Loaded;
        }

        private async void InventoryPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isReady) return; // Loaded có thể bắn lại khi trang được gắn lại vào cây giao diện
            await LoadFilterSourcesAsync();
            _isReady = true;
            await RefreshTabAsync(InventoryTabs.SelectedIndex);
        }

        // ===================== KHUNG TẢI DỮ LIỆU DÙNG CHUNG =====================
        // fetch: lấy dữ liệu (chạy nền) ; render: đổ ra giao diện (luồng giao diện)
        private async Task LoadAsync<T>(int tab, Func<Task<T>> fetch, Action<T> render)
        {
            int version = ++_tabVersion[tab];
            _activeLoads++;
            LoadingRing.IsActive = true;
            try
            {
                var data = await fetch();
                if (version != _tabVersion[tab]) return; // đã có lần tải mới hơn
                render(data);
                ErrorInfoBar.IsOpen = false;
                txtLastUpdated.Text = $"Cập nhật lúc {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = ex.InnerException?.Message ?? ex.Message;
                ErrorInfoBar.IsOpen = true;
            }
            finally
            {
                if (--_activeLoads == 0) LoadingRing.IsActive = false;
            }
        }

        private Task RefreshTabAsync(int tab) => tab switch
        {
            TabReceipts => RefreshReceiptsAsync(),
            TabTransactions => RefreshTransactionsAsync(),
            TabStock => RefreshStockAsync(),
            TabReport => RefreshReportAsync(),
            TabWarning => RefreshWarningsAsync(),
            _ => Task.CompletedTask
        };

        private async void InventoryTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady) await RefreshTabAsync(InventoryTabs.SelectedIndex);
        }

        // Gán giá trị cho bộ lọc mà KHÔNG kích hoạt tải lại
        private void SetQuietly(Action set)
        {
            _suppressFilterEvents = true;
            try { set(); } finally { _suppressFilterEvents = false; }
        }

        private bool FilterEventsEnabled => _isReady && !_suppressFilterEvents;

        // Nạp dữ liệu cho các ComboBox lọc (danh mục, sản phẩm)
        private async Task LoadFilterSourcesAsync()
        {
            try
            {
                var categories = await _service.GetCategoriesAsync();
                _productOptions = await _service.GetProductOptionsAsync();

                SetQuietly(() =>
                {
                    foreach (var cbo in new[] { cboStockCategory, cboWarningCategory })
                    {
                        var list = new List<CategoryRow> { new CategoryRow(0, "Tất cả danh mục") };
                        list.AddRange(categories);
                        cbo.ItemsSource = list;
                        cbo.DisplayMemberPath = "Name";
                        cbo.SelectedValuePath = "CategoryId";
                        cbo.SelectedIndex = 0;
                    }

                    int selectedProduct = cboReportProduct.SelectedValue is int sp ? sp : 0;
                    var reportProducts = new List<ProductOptionRow> { new ProductOptionRow(0, "Tất cả sản phẩm", 0, 0, 0, 0) };
                    reportProducts.AddRange(_productOptions);
                    cboReportProduct.ItemsSource = reportProducts;
                    cboReportProduct.DisplayMemberPath = "DisplayName";
                    cboReportProduct.SelectedValuePath = "ProductId";
                    cboReportProduct.SelectedValue = selectedProduct;
                    if (cboReportProduct.SelectedIndex < 0) cboReportProduct.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                ErrorInfoBar.Message = "Không tải được danh mục/sản phẩm: " + (ex.InnerException?.Message ?? ex.Message);
                ErrorInfoBar.IsOpen = true;
            }
        }

        // Khoảng ngày từ 2 CalendarDatePicker: chọn đủ cả 2, hoặc bỏ trống cả 2 (= tất cả)
        private bool TryGetRange(CalendarDatePicker fromPicker, CalendarDatePicker toPicker, out DateTime? start, out DateTime? endExclusive)
        {
            start = fromPicker.Date?.DateTime.Date;
            endExclusive = toPicker.Date?.DateTime.Date.AddDays(1);
            if (start.HasValue != endExclusive.HasValue) return false; // mới chọn 1 ngày -> chờ chọn nốt
            if (start.HasValue && start >= endExclusive)
            {
                ShowMessage("Lỗi thời gian", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return false;
            }
            return true;
        }

        // =================================================================
        // TAB 1: PHIẾU NHẬP HÀNG
        // =================================================================
        private Task RefreshReceiptsAsync()
        {
            if (!TryGetRange(dpReceiptStartDate, dpReceiptEndDate, out var start, out var end)) return Task.CompletedTask;

            return LoadAsync(TabReceipts, () => _service.GetReceiptsAsync(start, end), rows =>
            {
                ReceiptListView.ItemsSource = rows.Select(r => new ReceiptDisplayModel
                {
                    ReceiptId = r.ReceiptId,
                    ReceiptCode = r.ReceiptCode,
                    DateString = r.ImportDate.ToString("dd/MM/yyyy HH:mm"),
                    CreatorName = r.CreatorName,
                    ItemCount = r.ItemCount,
                    TotalValueString = r.TotalValue.ToString("N0") + " ₫",
                    StatusString = r.Status == "Completed" ? "Đã hoàn thành" : "Bản nháp",
                    StatusColor = r.Status == "Completed" ? new SolidColorBrush(Microsoft.UI.Colors.Green) : new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    CompleteButtonVisibility = r.Status == "Draft" ? Visibility.Visible : Visibility.Collapsed
                }).ToList();
            });
        }

        private async void ReceiptDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (FilterEventsEnabled) await RefreshReceiptsAsync();
        }

        // Nút "Xóa lọc": bỏ khoảng ngày, hiện tất cả phiếu
        private async void btnFilterReceipts_Click(object sender, RoutedEventArgs e)
        {
            SetQuietly(() => { dpReceiptStartDate.Date = null; dpReceiptEndDate.Date = null; });
            await RefreshReceiptsAsync();
        }

        private async void btnRefreshReceipts_Click(object sender, RoutedEventArgs e) => await RefreshReceiptsAsync();

        private async void btnCreateReceipt_Click(object sender, RoutedEventArgs e)
        {
            txtReceiptNote.Text = "";
            dpImportDate.Date = DateTimeOffset.Now;
            txtReceiptError.Visibility = Visibility.Collapsed;
            txtAutoCodeNotice.Visibility = Visibility.Visible;
            CreateReceiptDialog.Title = "Tạo Phiếu Nhập Mới";
            CreateReceiptDialog.PrimaryButtonText = "Tạo phiếu";
            CreateReceiptDialog.Tag = null;
            if (await CreateReceiptDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                await RefreshReceiptsAsync();
            }
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
                        if (!await context.ImportReceiptDetails.AnyAsync(d => d.ReceiptId == receiptId))
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
                            var receipt = await context.ImportReceipts.FindAsync(receiptId);
                            if (receipt != null && receipt.Status == "Draft")
                            {
                                // Trigger trg_ImportReceipts_AfterUpdate tự: cộng tồn kho, tính giá nhập bình quân,
                                // ghi sổ kho IMPORT. Phải set CompletedAt vì CHECK constraint yêu cầu.
                                receipt.Status = "Completed";
                                receipt.CompletedAt = DateTime.Now;
                                await context.SaveChangesAsync();
                            }
                        }
                        await LoadFilterSourcesAsync(); // tồn kho & giá vốn đã đổi
                        await RefreshReceiptsAsync();   // các tab khác tự tải lại khi được chọn
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
                    ImportReceipt? receipt;
                    using (var context = new CoffeeShopContext())
                    {
                        receipt = await context.ImportReceipts.AsNoTracking().FirstOrDefaultAsync(r => r.ReceiptId == receiptId);
                    }
                    if (receipt != null && receipt.Status == "Draft")
                    {
                        txtReceiptNote.Text = receipt.Note ?? "";
                        dpImportDate.Date = new DateTimeOffset(receipt.ImportDate);
                        txtReceiptError.Visibility = Visibility.Collapsed;
                        txtAutoCodeNotice.Visibility = Visibility.Collapsed;
                        CreateReceiptDialog.Title = $"Sửa Phiếu Nháp {receipt.ReceiptCode}";
                        CreateReceiptDialog.PrimaryButtonText = "Lưu thay đổi";
                        CreateReceiptDialog.Tag = receiptId;
                        if (await CreateReceiptDialog.ShowAsync() == ContentDialogResult.Primary)
                        {
                            await RefreshReceiptsAsync();
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
                            var receipt = await context.ImportReceipts.FindAsync(receiptId);
                            if (receipt != null && receipt.Status == "Draft")
                            {
                                // Chi tiết phiếu tự xóa theo ON DELETE CASCADE
                                context.ImportReceipts.Remove(receipt);
                                await context.SaveChangesAsync();
                            }
                        }
                        await RefreshReceiptsAsync();
                    }
                    catch (Exception ex) { ShowMessage("Lỗi xóa", ex.InnerException?.Message ?? ex.Message); }
                }
            }
        }

        // =================================================================
        // QUẢN LÝ SẢN PHẨM TRONG PHIẾU NHÁP (thêm / sửa / xóa dòng)
        // =================================================================
        private List<ReceiptLineModel> _currentLines = new();

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
                    await LoadReceiptLinesAsync(receiptId);
                    await AddProductDialog.ShowAsync();

                    await RefreshReceiptsAsync(); // cập nhật cột Số MH / Tổng giá trị sau khi đóng dialog
                }
                catch (Exception ex) { ShowMessage("Lỗi", ex.Message); }
            }
        }

        private async Task LoadReceiptLinesAsync(int receiptId)
        {
            using (var context = new CoffeeShopContext())
            {
                _currentLines = await context.ImportReceiptDetails
                    .AsNoTracking()
                    .Where(d => d.ReceiptId == receiptId)
                    .Select(d => new ReceiptLineModel
                    {
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        Quantity = d.Quantity,
                        ImportPrice = d.ImportPrice
                    })
                    .ToListAsync();
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
            if (cboProducts.SelectedItem is ProductOptionRow option)
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
            if (cboProducts?.SelectedItem is not ProductOptionRow p) { if (txtProductInfo != null) txtProductInfo.Text = ""; return; }

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

        private async void AddProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
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

            // Deferral: giữ sự kiện chờ tới khi lưu DB xong (handler async)
            var deferral = args.GetDeferral();
            try
            {
                int receiptId = (int)AddProductDialog.Tag;

                using (var context = new CoffeeShopContext())
                {
                    var existingDetail = await context.ImportReceiptDetails
                        .FirstOrDefaultAsync(d => d.ReceiptId == receiptId && d.ProductId == productId);

                    if (existingDetail != null)
                    {
                        // Sửa dòng: GHI ĐÈ số lượng & giá (bản cũ cộng dồn số lượng nên không sửa được)
                        existingDetail.Quantity = qty;
                        existingDetail.ImportPrice = price;
                    }
                    else
                    {
                        context.ImportReceiptDetails.Add(new ImportReceiptDetail
                        {
                            ReceiptId = receiptId,
                            ProductId = productId,
                            Quantity = qty,
                            ImportPrice = price
                        });
                    }
                    await context.SaveChangesAsync();
                }

                await LoadReceiptLinesAsync(receiptId);
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
            finally
            {
                deferral.Complete();
            }
        }

        private async void btnRemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId && AddProductDialog.Tag is int receiptId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        // Trigger trg_ImportReceiptDetails_LockCompleted chặn nếu phiếu đã hoàn thành
                        await context.ImportReceiptDetails
                            .Where(d => d.ReceiptId == receiptId && d.ProductId == productId)
                            .ExecuteDeleteAsync();
                    }
                    await LoadReceiptLinesAsync(receiptId);
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
                    ImportReceipt? receipt;
                    using (var context = new CoffeeShopContext())
                    {
                        // Lấy thông tin phiếu kèm theo danh sách chi tiết và tên sản phẩm
                        receipt = await context.ImportReceipts
                            .AsNoTracking()
                            .Include(r => r.CreatedByNavigation)
                            .Include(r => r.ImportReceiptDetails)
                            .ThenInclude(d => d.Product)
                            .FirstOrDefaultAsync(r => r.ReceiptId == receiptId);
                    }
                    if (receipt == null) return;

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
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", ex.Message);
                }
            }
        }

        // =================================================================
        // TAB 2: LỊCH SỬ KHO
        // =================================================================
        private static string GetTransactionTypeText(string type) => type switch
        {
            "INITIAL" => "Tồn ban đầu",
            "IMPORT" => "Nhập hàng",
            "EXPORT" => "Bán hàng",
            "RETURN" => "Trả về (hủy đơn)",
            _ => type
        };

        private Task RefreshTransactionsAsync()
        {
            if (!TryGetRange(dpStartDate, dpEndDate, out var start, out var end)) return Task.CompletedTask;

            return LoadAsync(TabTransactions, () => _service.GetTransactionsAsync(start, end), rows =>
            {
                TransactionListView.ItemsSource = rows.Select(t => new TransactionDisplayModel
                {
                    DateString = t.Date.ToString("dd/MM/yyyy HH:mm"),
                    ProductName = t.ProductName,
                    TypeString = GetTransactionTypeText(t.Type),
                    QuantityString = t.QuantityChange > 0 ? $"+{t.QuantityChange}" : t.QuantityChange.ToString(),
                    Note = t.Note,
                    QuantityColor = t.QuantityChange > 0
                        ? new SolidColorBrush(Microsoft.UI.Colors.Green)
                        : new SolidColorBrush(Microsoft.UI.Colors.Red)
                }).ToList();
                txtTransactionCount.Text = $"{rows.Count} giao dịch";
            });
        }

        private async void TransactionDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (FilterEventsEnabled) await RefreshTransactionsAsync();
        }

        private async void btnReloadTransactions_Click(object sender, RoutedEventArgs e)
        {
            SetQuietly(() => { dpStartDate.Date = null; dpEndDate.Date = null; });
            await RefreshTransactionsAsync();
        }

        // =================================================================
        // TAB 3: TỒN KHO THEO NGÀY
        // =================================================================
        private async Task RefreshStockAsync()
        {
            DateTime day = dpStockDate.Date?.DateTime.Date ?? DateTime.Today;
            if (day > DateTime.Today)
            {
                ShowMessage("Lỗi thời gian", "Không thể xem tồn kho của ngày trong tương lai.");
                SetQuietly(() => dpStockDate.Date = new DateTimeOffset(DateTime.Today));
                day = DateTime.Today;
            }
            if (dpStockDate.Date == null) SetQuietly(() => dpStockDate.Date = new DateTimeOffset(day));
            int categoryId = cboStockCategory.SelectedValue is int c ? c : 0;

            await LoadAsync(TabStock, () => _service.GetStockAtDateAsync(day, categoryId), rows =>
            {
                StockAtDateListView.ItemsSource = rows.Select(r => new StockAtDateModel
                {
                    Code = r.Code,
                    ProductName = r.ProductName,
                    CategoryName = r.CategoryName,
                    Opening = r.Opening,
                    InQuantity = r.InQuantity,
                    OutQuantity = r.OutQuantity,
                    Closing = r.Closing,
                    CostPrice = r.CostPrice
                }).ToList();

                decimal totalValue = rows.Sum(r => r.Closing * r.CostPrice);
                txtStockAtDateSummary.Text =
                    $"Tồn kho cuối ngày {day:dd/MM/yyyy}: {rows.Sum(r => r.Closing)} sản phẩm, " +
                    $"giá trị ≈ {totalValue:N0} ₫ (tính theo giá nhập bình quân HIỆN TẠI). " +
                    $"Trong ngày: nhập {rows.Sum(r => r.InQuantity)}, xuất {rows.Sum(r => r.OutQuantity)}.";
            });

            // Đối chiếu sổ kho với tồn kho thực tế (chạy sau, không chặn bảng chính)
            await LoadAsync(TabStock, () => _service.GetLedgerMismatchesAsync(), mismatches =>
            {
                LedgerInfoBar.IsOpen = mismatches.Count > 0;
                if (mismatches.Count > 0)
                {
                    LedgerInfoBar.Message =
                        $"{mismatches.Count} sản phẩm có tồn kho khác tổng sổ kho, báo cáo theo ngày sẽ sai cho các sản phẩm này " +
                        "(thường do dữ liệu được nhập thẳng vào DB, không qua trigger): " +
                        string.Join("; ", mismatches.Take(5).Select(m => $"{m.Code}: tồn {m.StockQuantity}, sổ kho {m.LedgerTotal}")) +
                        (mismatches.Count > 5 ? "; ..." : "");
                }
            });
        }

        private async void StockFilter_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (FilterEventsEnabled) await RefreshStockAsync();
        }

        private async void StockFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterEventsEnabled) await RefreshStockAsync();
        }

        private async void btnViewStockAtDate_Click(object sender, RoutedEventArgs e) => await RefreshStockAsync();

        // =================================================================
        // TAB 4: BÁO CÁO NHẬP - XUẤT TRONG KHOẢNG THỜI GIAN
        // =================================================================
        private Task RefreshReportAsync()
        {
            // Lần đầu mở tab: mặc định từ đầu tháng tới hôm nay
            if (dpReportFrom.Date == null && dpReportTo.Date == null)
            {
                SetQuietly(() =>
                {
                    dpReportFrom.Date = new DateTimeOffset(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
                    dpReportTo.Date = new DateTimeOffset(DateTime.Today);
                });
            }

            DateTime? from = dpReportFrom.Date?.DateTime.Date;
            DateTime? to = dpReportTo.Date?.DateTime.Date;
            if (from.HasValue && to.HasValue && from > to)
            {
                ShowMessage("Lỗi thời gian", "Ngày bắt đầu không được lớn hơn ngày kết thúc.");
                return Task.CompletedTask;
            }
            int productId = cboReportProduct.SelectedValue is int p ? p : 0;

            return LoadAsync(TabReport, () => _service.GetReportAsync(from, to, productId, DateTime.Now), r =>
            {
                txtReportOpening.Text = r.Opening.ToString("N0");
                txtReportIn.Text = "+" + r.Imported.ToString("N0");
                txtReportOut.Text = "-" + r.Exported.ToString("N0");
                txtReportReturn.Text = "+" + r.Returned.ToString("N0");
                txtReportClosing.Text = r.Closing.ToString("N0");
                txtReportClosingValue.Text = r.CostPrice.HasValue
                    ? $"≈ {r.Closing * r.CostPrice.Value:N0} ₫ (giá BQ {r.CostPrice.Value:N0} ₫)"
                    : "";

                var imports = r.Imports.Select(x => new ReportLineModel
                {
                    Date = x.Date, ProductName = x.ProductName, Reference = x.Reference, Quantity = x.Quantity, UnitPrice = x.UnitPrice
                }).ToList();
                var exports = r.Exports.Select(x => new ReportLineModel
                {
                    Date = x.Date, ProductName = x.ProductName, Quantity = x.Quantity, UnitPrice = x.UnitPrice,
                    Reference = $"{x.Reference} - {OrdersPage.GetStatusText(x.OrderStatus ?? "")}"
                }).ToList();

                ReportImportListView.ItemsSource = imports;
                ReportExportListView.ItemsSource = exports;
                txtImportTitle.Text = $"Chi tiết nhập hàng ({imports.Count} dòng, {imports.Sum(x => x.Quantity * x.UnitPrice):N0} ₫)";
                txtExportTitle.Text = $"Chi tiết xuất hàng - đơn hàng ({exports.Count} dòng, {exports.Sum(x => x.Quantity * x.UnitPrice):N0} ₫)";
            });
        }

        private async void ReportFilter_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (FilterEventsEnabled) await RefreshReportAsync();
        }

        private async void ReportFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterEventsEnabled) await RefreshReportAsync();
        }

        private async void btnGenerateReport_Click(object sender, RoutedEventArgs e) => await RefreshReportAsync();

        // =================================================================
        // TAB 5: CẢNH BÁO TỒN KHO
        // DB chưa có cột ngưỡng riêng cho từng sản phẩm (PHP: CanhBaoTon),
        // nên dùng 1 ngưỡng chung do admin nhập (mặc định 10).
        // =================================================================
        private Task RefreshWarningsAsync()
        {
            if (!int.TryParse(txtWarningThreshold.Text, out int threshold) || threshold < 0)
            {
                ErrorInfoBar.Message = "Ngưỡng cảnh báo phải là số nguyên không âm.";
                ErrorInfoBar.IsOpen = true;
                return Task.CompletedTask;
            }
            int categoryId = cboWarningCategory.SelectedValue is int c ? c : 0;
            string keyword = txtWarningKeyword.Text;
            bool visibleOnly = chkWarningVisibleOnly.IsChecked == true;

            return LoadAsync(TabWarning, () => _service.GetWarningsAsync(threshold, categoryId, keyword, visibleOnly), r =>
            {
                txtCountOutOfStock.Text = r.OutOfStock.ToString();
                txtCountLowStock.Text = r.LowStock.ToString();
                txtCountOkStock.Text = r.OkStock.ToString();
                WarningListView.ItemsSource = r.Items.Select(p => new WarningModel
                {
                    Code = p.Code,
                    ProductName = p.ProductName,
                    CategoryName = p.CategoryName,
                    Stock = p.Stock,
                    LevelString = p.Stock == 0 ? "Hết hàng" : "Sắp hết",
                    LevelColor = new SolidColorBrush(p.Stock == 0 ? Microsoft.UI.Colors.Red : Microsoft.UI.Colors.Orange),
                    VisibleString = p.IsVisible ? "Đang bán" : "Đã ẩn"
                }).ToList();
            });
        }

        // Gõ phím: khởi động lại đồng hồ chờ 400ms (debounce)
        private void WarningFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!FilterEventsEnabled) return;
            _warningDebounce.Stop();
            _warningDebounce.Start();
        }

        private async void WarningFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilterEventsEnabled) await RefreshWarningsAsync();
        }

        private async void WarningFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FilterEventsEnabled) await RefreshWarningsAsync();
        }

        private async void btnFilterWarning_Click(object sender, RoutedEventArgs e) => await RefreshWarningsAsync();

        private async void ShowMessage(string title, string content)
        {
            if (this.Content?.XamlRoot == null) return; // trang chưa hiển thị -> không mở dialog được
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
