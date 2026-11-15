using App67.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models;

namespace App67.Pages
{
    // Tương ứng price.php: xem giá vốn / % lãi / giá bán, sửa % lãi từng sản phẩm hoặc cả danh mục.
    // Khác PHP: cột SalePrice là CỘT TÍNH TOÁN trong SQL Server
    //   SalePrice = ROUND(CostPrice * (100 + ProfitRate) / 100, 2)
    // nên app CHỈ cập nhật ProfitRate, DB tự tính lại giá bán -> giá bán luôn đúng công thức.
    public sealed partial class PricesPage : Page
    {
        public class PriceDisplayModel
        {
            public int ProductId { get; set; }
            public string Code { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public decimal CostPrice { get; set; }
            public decimal ProfitRate { get; set; }
            public decimal SalePrice { get; set; }
            public string CostPriceString => CostPrice.ToString("N0") + " ₫";
            public string ProfitRateString => ProfitRate.ToString("0.##") + " %";
            public string SalePriceString => SalePrice.ToString("N0") + " ₫";
        }

        private List<PriceDisplayModel> _allProducts = new();
        private PriceDisplayModel? _editingProduct;
        private bool _isReady;

        public PricesPage()
        {
            this.InitializeComponent();
            LoadCategories();
            _isReady = true;
            LoadData();
        }

        // Công thức giống cột tính toán trong DB, dùng để XEM TRƯỚC giá bán trên form
        private static decimal CalcSalePrice(decimal cost, decimal rate) =>
            Math.Round(cost * (100 + rate) / 100, 2);

        private void LoadCategories()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    var categories = context.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();

                    var filterList = new List<Category> { new Category { CategoryId = 0, Name = "Tất cả danh mục" } };
                    filterList.AddRange(categories);
                    cboFilterCategory.ItemsSource = filterList;
                    cboFilterCategory.DisplayMemberPath = "Name";
                    cboFilterCategory.SelectedValuePath = "CategoryId";
                    cboFilterCategory.SelectedIndex = 0;

                    cboProfitCategory.ItemsSource = categories;
                    cboProfitCategory.DisplayMemberPath = "Name";
                    cboProfitCategory.SelectedValuePath = "CategoryId";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải danh mục: " + ex.Message);
            }
        }

        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    _allProducts = context.Products
                        .AsNoTracking()
                        .OrderByDescending(p => p.ProductId)
                        .Select(p => new PriceDisplayModel
                        {
                            ProductId = p.ProductId,
                            Code = p.Code,
                            Name = p.Name,
                            CategoryId = p.CategoryId,
                            CategoryName = p.Category.Name,
                            CostPrice = p.CostPrice,
                            ProfitRate = p.ProfitRate,
                            SalePrice = p.SalePrice
                        })
                        .ToList();
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu", ex.Message);
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<PriceDisplayModel> query = _allProducts;

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

            var result = query.ToList();
            PriceListView.ItemsSource = result;
            txtResultCount.Text = $"{result.Count}/{_allProducts.Count} sản phẩm";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void cboFilterCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady) ApplyFilters();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadData();

        // ================= SỬA % LÃI 1 SẢN PHẨM =================
        private async void btnEditProfit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                _editingProduct = _allProducts.FirstOrDefault(p => p.ProductId == productId);
                if (_editingProduct == null) return;

                txtProfitProductName.Text = $"{_editingProduct.Name} ({_editingProduct.Code})";
                txtProfitCostPrice.Text = $"Giá nhập bình quân hiện tại: {_editingProduct.CostPriceString}";
                txtProfitRate.Text = _editingProduct.ProfitRate.ToString("0.##");
                txtProfitError.Visibility = Visibility.Collapsed;
                UpdatePreview();

                await ProductProfitDialog.ShowAsync();
            }
        }

        private void txtProfitRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady) UpdatePreview();
        }

        // Hiển thị giá bán mới ngay khi gõ % lãi
        private void UpdatePreview()
        {
            if (_editingProduct != null && decimal.TryParse(txtProfitRate.Text, out decimal rate))
            {
                txtProfitPreview.Text = $"Giá bán mới: {CalcSalePrice(_editingProduct.CostPrice, rate):N0} ₫ (hiện tại {_editingProduct.SalePriceString})";
            }
            else
            {
                txtProfitPreview.Text = "";
            }
        }

        private static bool TryParseRate(string text, out decimal rate) =>
            decimal.TryParse(text, out rate) && rate >= 0 && rate <= 500; // CK_Products_ProfitRate

        private void ProductProfitDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_editingProduct == null) return;
            if (!TryParseRate(txtProfitRate.Text, out decimal rate))
            {
                args.Cancel = true;
                txtProfitError.Text = "Tỷ lệ lãi phải là số từ 0 đến 500.";
                txtProfitError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Chỉ cập nhật ProfitRate (không đụng CostPrice/Stock -> không bị trigger GuardStock chặn)
                    context.Products
                        .Where(p => p.ProductId == _editingProduct.ProductId)
                        .ExecuteUpdate(s => s.SetProperty(p => p.ProfitRate, rate));
                }
                LoadData();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                txtProfitError.Text = "Lỗi lưu: " + (ex.InnerException?.Message ?? ex.Message);
                txtProfitError.Visibility = Visibility.Visible;
            }
        }

        // ================= ĐẶT % LÃI CHO CẢ DANH MỤC =================
        private async void btnCategoryProfit_Click(object sender, RoutedEventArgs e)
        {
            cboProfitCategory.SelectedIndex = -1;
            if (cboFilterCategory.SelectedValue is int categoryId && categoryId > 0)
            {
                cboProfitCategory.SelectedValue = categoryId; // gợi ý danh mục đang lọc
            }
            txtCategoryProfitRate.Text = "";
            txtCategoryProfitError.Visibility = Visibility.Collapsed;
            await CategoryProfitDialog.ShowAsync();
        }

        private async void CategoryProfitDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (cboProfitCategory.SelectedValue is not int categoryId)
            {
                args.Cancel = true;
                txtCategoryProfitError.Text = "Vui lòng chọn danh mục.";
                txtCategoryProfitError.Visibility = Visibility.Visible;
                return;
            }
            if (!TryParseRate(txtCategoryProfitRate.Text, out decimal rate))
            {
                args.Cancel = true;
                txtCategoryProfitError.Text = "Tỷ lệ lãi phải là số từ 0 đến 500.";
                txtCategoryProfitError.Visibility = Visibility.Visible;
                return;
            }

            // Lấy deferral: chờ cập nhật DB xong mới đóng dialog
            var deferral = args.GetDeferral();
            try
            {
                int updated;
                using (var context = new CoffeeShopContext())
                {
                    // 1 câu UPDATE ... WHERE CategoryId = @id cho cả danh mục (PHP lặp từng sản phẩm)
                    updated = await context.Products
                        .Where(p => p.CategoryId == categoryId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.ProfitRate, rate));
                }

                if (updated == 0)
                {
                    args.Cancel = true;
                    txtCategoryProfitError.Text = "Danh mục này chưa có sản phẩm nào.";
                    txtCategoryProfitError.Visibility = Visibility.Visible;
                    return;
                }
                LoadData();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                txtCategoryProfitError.Text = "Lỗi lưu: " + (ex.InnerException?.Message ?? ex.Message);
                txtCategoryProfitError.Visibility = Visibility.Visible;
            }
            finally
            {
                deferral.Complete();
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
