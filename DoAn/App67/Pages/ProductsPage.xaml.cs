using App67.Data;
using App67.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models;

namespace App67.Pages
{
    public sealed partial class ProductsPage : Page
    {
        // Biến lưu sản phẩm đang được chọn (dùng cho Sửa/Xóa)
        private Product? _selectedProduct = null;

        // Danh sách gốc tải từ DB, bộ lọc chạy trên danh sách này
        private List<Product> _allProducts = new();

        private bool _isReady;

        public ProductsPage()
        {
            this.InitializeComponent();
            _isReady = true;
            LoadCategories();
            LoadSuppliers();
            LoadData();
        }

        // ================= TẢI DỮ LIỆU =================
        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Include Category để hiển thị cột "Danh mục" ({Binding Category.Name})
                    _allProducts = context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .OrderByDescending(p => p.ProductId)
                        .ToList();
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải dữ liệu: " + ex.Message);
            }
        }

        private void LoadCategories()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var categories = context.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
                    cboCategory.ItemsSource = categories;
                    cboCategory.DisplayMemberPath = "Name"; // Hiển thị tên danh mục
                    cboCategory.SelectedValuePath = "CategoryId"; // Lấy giá trị ID

                    // Combobox lọc: thêm mục "Tất cả" (Id = 0) ở đầu
                    var filterList = new List<Category> { new Category { CategoryId = 0, Name = "Tất cả danh mục" } };
                    filterList.AddRange(categories);
                    cboFilterCategory.ItemsSource = filterList;
                    cboFilterCategory.DisplayMemberPath = "Name";
                    cboFilterCategory.SelectedValuePath = "CategoryId";
                    cboFilterCategory.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void LoadSuppliers()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    cboSupplier.ItemsSource = context.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToList();
                    cboSupplier.DisplayMemberPath = "Name";
                    cboSupplier.SelectedValuePath = "SupplierId";
                }
            }
            catch { }
        }

        // ================= BỘ LỌC =================
        private void ApplyFilters()
        {
            IEnumerable<Product> query = _allProducts;

            string keyword = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p =>
                    p.Name.ToLower().Contains(keyword) ||
                    p.Code.ToLower().Contains(keyword) ||
                    p.ProductId.ToString() == keyword);
            }

            if (cboFilterCategory.SelectedValue is int categoryId && categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            string status = (cboFilterStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (status == "visible") query = query.Where(p => p.IsVisible);
            else if (status == "hidden") query = query.Where(p => !p.IsVisible);

            var result = query.ToList();
            ProductListView.ItemsSource = result;
            txtResultCount.Text = $"{result.Count}/{_allProducts.Count} sản phẩm";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            _isReady = false;
            txtSearch.Text = "";
            cboFilterCategory.SelectedIndex = 0;
            cboFilterStatus.SelectedIndex = 0;
            _isReady = true;
            ApplyFilters();
        }

        // ================= NÚT THÊM MỚI =================
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = null;
            ClearForm();
            ProductDialog.Title = "Thêm Sản Phẩm Mới";

            // Khi tạo mới được nhập giá nhập + tồn ban đầu (trigger ghi sổ kho INITIAL)
            txtCostPrice.IsEnabled = true;
            txtStock.IsEnabled = true;
            txtStockNote.Visibility = Visibility.Collapsed;

            await ProductDialog.ShowAsync();
        }

        // ================= NÚT SỬA =================
        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product product)
            {
                _selectedProduct = product;
                ProductDialog.Title = "Sửa Sản Phẩm";
                HideFormError();

                // Đổ dữ liệu cũ vào form
                txtName.Text = product.Name;
                txtCode.Text = product.Code;
                txtCostPrice.Text = product.CostPrice.ToString("0.##");
                txtProfitRate.Text = product.ProfitRate.ToString("0.##");
                txtStock.Text = product.StockQuantity.ToString();
                txtUnit.Text = product.Unit;
                txtOrigin.Text = product.Origin;
                txtWeight.Text = product.WeightGram?.ToString();
                txtImageUrl.Text = product.ImageUrl;
                txtDesc.Text = product.Description;

                if (!string.IsNullOrEmpty(product.RoastLevel))
                {
                    foreach (ComboBoxItem item in cboRoastLevel.Items)
                    {
                        if (item.Content.ToString() == product.RoastLevel) cboRoastLevel.SelectedItem = item;
                    }
                }
                else cboRoastLevel.SelectedItem = null;

                if (cboCategory.ItemsSource != null)
                {
                    cboCategory.SelectedValue = product.CategoryId;
                }
                cboSupplier.SelectedValue = product.SupplierId;

                // Trigger trg_Products_GuardStock chặn sửa tay tồn kho & giá nhập -> khóa 2 ô này
                txtCostPrice.IsEnabled = false;
                txtStock.IsEnabled = false;
                txtStockNote.Visibility = Visibility.Visible;

                await ProductDialog.ShowAsync();
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một sản phẩm trên bảng để sửa.");
            }
        }

        // ================= CHỌN ẢNH =================
        // Mở hộp thoại chọn file, copy ảnh vào wwwroot/img/sanpham của Web để Web hiển thị được.
        private async void btnBrowseImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                foreach (var ext in new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" })
                {
                    picker.FileTypeFilter.Add(ext);
                }

                // WinUI 3 (desktop): picker phải được gắn với cửa sổ (HWND), nếu không sẽ báo lỗi
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();
                if (file == null) return; // người dùng bấm Hủy

                txtImageUrl.Text = WebImageHelper.CopyToWeb(file.Path);
                HideFormError();
            }
            catch (Exception ex)
            {
                ShowFormError("Không thể chọn ảnh: " + ex.Message);
            }
        }

        // ================= NÚT XÓA / ẨN =================
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product product)
            {
                ContentDialog confirmDialog = new ContentDialog
                {
                    Title = "Xác nhận",
                    Content = $"Bạn có chắc chắn muốn xóa sản phẩm '{product.Name}'?\n(Lưu ý: Nếu sản phẩm đã từng nhập hàng hoặc đã bán, hệ thống sẽ chỉ ẩn đi để bảo toàn dữ liệu).",
                    PrimaryButtonText = "Đồng ý",
                    CloseButtonText = "Hủy",
                    XamlRoot = this.Content.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        string message;
                        using (var context = new CoffeeShopContext())
                        {
                            // Trigger INSTEAD OF DELETE (trg_Products_InsteadOfDelete) quyết định:
                            //   chưa từng nhập/bán -> xóa hẳn ; ngược lại -> chỉ ẩn (IsVisible = 0).
                            // Dùng ExecuteDelete (gửi thẳng câu DELETE) thay vì Remove + SaveChanges,
                            // vì SaveChanges kiểm tra "số dòng bị ảnh hưởng" và có thể báo lỗi khi trigger chỉ ẩn.
                            context.Products.Where(p => p.ProductId == product.ProductId).ExecuteDelete();

                            // Kiểm tra lại để báo đúng kết quả cho người dùng
                            bool stillExists = context.Products.Any(p => p.ProductId == product.ProductId);
                            message = stillExists
                                ? $"Sản phẩm '{product.Name}' đã có lịch sử nhập/bán nên chỉ được ẨN (không xóa hẳn)."
                                : $"Đã xóa vĩnh viễn sản phẩm '{product.Name}'.";
                        }
                        LoadData(); // Tải lại bảng
                        ShowMessage("Thành công", message);
                    }
                    catch (Exception ex)
                    {
                        string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                        ShowMessage("Lỗi khi xóa", msg);
                    }
                }
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một sản phẩm để xóa.");
            }
        }

        // Hàm xử lý khi bấm nút "Hiện / Ẩn"
        private void btnToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product product)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var dbProduct = context.Products.Find(product.ProductId);
                        if (dbProduct != null)
                        {
                            // Đảo ngược trạng thái: Đang ẩn -> Hiện, Đang hiện -> Ẩn
                            dbProduct.IsVisible = !dbProduct.IsVisible;
                            context.SaveChanges();
                        }
                    }
                    LoadData(); // Tải lại bảng để thấy trạng thái mới
                }
                catch (Exception ex)
                {
                    string errorDetail = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    ShowMessage("Lỗi", "Không thể đổi trạng thái: " + errorDetail);
                }
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một sản phẩm trên bảng để đổi trạng thái.");
            }
        }

        // ================= KIỂM TRA DỮ LIỆU FORM =================
        // Khớp với ràng buộc DB: UQ_Products_Code, CK_Products_ProfitRate (0-500),
        // CK_Products_Weight (> 0), CK_Products_CostPrice / Stock (>= 0)
        private string? ValidateForm(out decimal costPrice, out decimal profitRate, out int stock, out int? weight)
        {
            costPrice = 0; profitRate = 0; stock = 0; weight = null;

            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCode.Text) || cboCategory.SelectedValue == null)
                return "Vui lòng nhập đầy đủ Tên, Mã và chọn Danh mục.";
            if (txtCode.Text.Trim().Length > 20)
                return "Mã sản phẩm tối đa 20 ký tự.";
            if (!decimal.TryParse(txtCostPrice.Text, out costPrice) || costPrice < 0)
                return "Giá nhập phải là số không âm.";
            if (!string.IsNullOrWhiteSpace(txtProfitRate.Text) &&
                (!decimal.TryParse(txtProfitRate.Text, out profitRate) || profitRate < 0 || profitRate > 500))
                return "Tỷ lệ lãi phải từ 0 đến 500 (%).";
            if (_selectedProduct == null && !string.IsNullOrWhiteSpace(txtStock.Text) &&
                (!int.TryParse(txtStock.Text, out stock) || stock < 0))
                return "Số lượng tồn phải là số nguyên không âm.";
            if (!string.IsNullOrWhiteSpace(txtWeight.Text))
            {
                if (!int.TryParse(txtWeight.Text, out int w) || w <= 0) return "Khối lượng phải là số nguyên lớn hơn 0.";
                weight = w;
            }
            return null;
        }

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ================= SỰ KIỆN LƯU KHI BẤM NÚT PRIMARY TRONG DIALOG =================
        private void ProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string? error = ValidateForm(out decimal costPrice, out decimal profitRate, out int stock, out int? weight);
            if (error != null)
            {
                args.Cancel = true;
                ShowFormError(error);
                return;
            }

            string code = txtCode.Text.Trim();
            string unit = string.IsNullOrWhiteSpace(txtUnit.Text) ? "Gói" : txtUnit.Text.Trim();

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    int currentId = _selectedProduct?.ProductId ?? 0;
                    if (context.Products.Any(p => p.Code == code && p.ProductId != currentId))
                    {
                        args.Cancel = true;
                        ShowFormError($"Mã sản phẩm '{code}' đã tồn tại.");
                        return;
                    }

                    if (_selectedProduct == null) // THÊM MỚI
                    {
                        var newProduct = new Product
                        {
                            Name = txtName.Text.Trim(),
                            Code = code,
                            CostPrice = costPrice,
                            ProfitRate = profitRate,
                            StockQuantity = stock,
                            CategoryId = (int)cboCategory.SelectedValue,
                            SupplierId = cboSupplier.SelectedValue as int?,
                            Unit = unit,
                            Origin = NullIfEmpty(txtOrigin.Text),
                            WeightGram = weight,
                            RoastLevel = (cboRoastLevel.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                            ImageUrl = NullIfEmpty(txtImageUrl.Text),
                            Description = NullIfEmpty(txtDesc.Text),
                            IsVisible = true,
                            CreatedAt = DateTime.Now
                        };
                        context.Products.Add(newProduct);
                    }
                    else // SỬA
                    {
                        var dbProduct = context.Products.Find(_selectedProduct.ProductId);
                        if (dbProduct != null)
                        {
                            // Chỉ cập nhật các thông tin cơ bản.
                            // KHÔNG đụng tới StockQuantity / CostPrice: trigger GuardStock sẽ chặn.
                            dbProduct.Name = txtName.Text.Trim();
                            dbProduct.Code = code;
                            dbProduct.ProfitRate = profitRate;
                            dbProduct.CategoryId = (int)cboCategory.SelectedValue;
                            dbProduct.SupplierId = cboSupplier.SelectedValue as int?;
                            dbProduct.Unit = unit;
                            dbProduct.Origin = NullIfEmpty(txtOrigin.Text);
                            dbProduct.WeightGram = weight;
                            dbProduct.RoastLevel = (cboRoastLevel.SelectedItem as ComboBoxItem)?.Content?.ToString();
                            dbProduct.ImageUrl = NullIfEmpty(txtImageUrl.Text);
                            dbProduct.Description = NullIfEmpty(txtDesc.Text);
                        }
                    }
                    context.SaveChanges(); // Lưu thay đổi
                }
                LoadData(); // Tải lại danh sách
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                // Lấy lỗi chi tiết từ SQL (nếu có)
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowFormError("Lỗi lưu dữ liệu: " + errorMsg);
            }
        }

        // ================= CÁC HÀM HỖ TRỢ =================
        private void ClearForm()
        {
            txtName.Text = ""; txtCode.Text = ""; txtCostPrice.Text = "";
            txtProfitRate.Text = ""; txtStock.Text = ""; txtOrigin.Text = "";
            txtWeight.Text = ""; txtImageUrl.Text = ""; txtDesc.Text = "";
            txtUnit.Text = "Gói";
            cboCategory.SelectedValue = null; cboRoastLevel.SelectedItem = null;
            cboSupplier.SelectedValue = null;
            HideFormError();
        }

        private void ShowFormError(string message)
        {
            txtFormError.Text = message;
            txtFormError.Visibility = Visibility.Visible;
        }

        private void HideFormError()
        {
            txtFormError.Text = "";
            txtFormError.Visibility = Visibility.Collapsed;
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
