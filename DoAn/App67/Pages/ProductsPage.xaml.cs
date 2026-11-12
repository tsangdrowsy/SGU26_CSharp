using App67.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using WebApplication1.Models;

namespace App67.Pages
{
    public sealed partial class ProductsPage : Page
    {
        // Biến lưu sản phẩm đang được chọn (dùng cho Sửa/Xóa)
        private Product? _selectedProduct = null;

        public ProductsPage()
        {
            this.InitializeComponent();
            LoadData();
            LoadCategories();
        }

        // ================= TẢI DỮ LIỆU =================
        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Chỉ lấy dữ liệu gốc, không dùng Select để tạo Anonymous Type nữa
                    var products = context.Products
                        .OrderByDescending(p => p.ProductId)
                        .ToList();

                    ProductListView.ItemsSource = products;
                }
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
                    var categories = context.Categories.ToList();
                    cboCategory.ItemsSource = categories;
                    cboCategory.DisplayMemberPath = "Name"; // Hiển thị tên danh mục
                    cboCategory.SelectedValuePath = "CategoryId"; // Lấy giá trị ID
                }
            }
            catch { }
        }

        // ================= NÚT THÊM MỚI =================
        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            _selectedProduct = null;
            ClearForm();
            ProductDialog.Title = "Thêm Sản Phẩm Mới";

            txtCostPrice.IsEnabled = true;
            txtStock.IsEnabled = true;

            await ProductDialog.ShowAsync();
        }

        // ================= NÚT SỬA =================
        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (ProductListView.SelectedItem is Product product)
            {
                _selectedProduct = product;
                ProductDialog.Title = "Sửa Sản Phẩm";

                // Đổ dữ liệu cũ vào form
                txtName.Text = product.Name;
                txtCode.Text = product.Code;
                txtCostPrice.Text = product.CostPrice.ToString();
                txtProfitRate.Text = product.ProfitRate.ToString();
                txtStock.Text = product.StockQuantity.ToString();
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

                txtCostPrice.IsEnabled = false;
                txtStock.IsEnabled = false;


                await ProductDialog.ShowAsync();
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một sản phẩm trên bảng để sửa.");
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
                    Content = $"Bạn có chắc chắn muốn xóa sản phẩm '{product.Name}'?\n(Lưu ý: Nếu sản phẩm đã từng bán, hệ thống sẽ chỉ ẩn đi để bảo toàn dữ liệu).",
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
                        using (var context = new CoffeeShopContext())
                        {
                            var dbProduct = context.Products.Find(product.ProductId);
                            if (dbProduct != null)
                            {
                                context.Products.Remove(dbProduct);
                                context.SaveChanges();
                            }
                        }
                        LoadData(); // Tải lại bảng
                        ShowMessage("Thành công", "Đã xử lý xóa sản phẩm.");
                    }
                    catch (Exception ex)
                    {
                        ShowMessage("Lỗi khi xóa", ex.Message);
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
                    ShowMessage("Lỗi", "Không thể đổi trạng thái: " + ex.Message);
                }
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một sản phẩm trên bảng để đổi trạng thái.");
            }
        }

        // ================= SỰ KIỆN LƯU KHI BẤM NÚT PRIMARY TRONG DIALOG =================
        private void ProductDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtCode.Text) ||
                !decimal.TryParse(txtCostPrice.Text, out decimal costPrice) || cboCategory.SelectedValue == null)
            {
                args.Cancel = true;
                ShowMessage("Lỗi nhập liệu", "Vui lòng nhập đầy đủ Tên, Mã, Giá nhập và chọn Danh mục.");
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    if (_selectedProduct == null) // THÊM MỚI
                    {
                        var newProduct = new Product
                        {
                            Name = txtName.Text.Trim(),
                            Code = txtCode.Text.Trim(),
                            CostPrice = costPrice,
                            ProfitRate = decimal.TryParse(txtProfitRate.Text, out decimal pr) ? pr : 0,
                            StockQuantity = int.TryParse(txtStock.Text, out int st) ? st : 0,
                            CategoryId = (int)cboCategory.SelectedValue,
                            Origin = txtOrigin.Text.Trim(),
                            WeightGram = int.TryParse(txtWeight.Text, out int wg) ? wg : (int?)null,
                            RoastLevel = (cboRoastLevel.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                            ImageUrl = txtImageUrl.Text.Trim(),
                            Description = txtDesc.Text.Trim(),
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
                            // Chỉ cập nhật các thông tin cơ bản
                            dbProduct.Name = txtName.Text.Trim();
                            dbProduct.Code = txtCode.Text.Trim();
                            dbProduct.ProfitRate = decimal.TryParse(txtProfitRate.Text, out decimal pr) ? pr : 0;
                            dbProduct.CategoryId = (int)cboCategory.SelectedValue;
                            dbProduct.Origin = txtOrigin.Text.Trim();
                            dbProduct.WeightGram = int.TryParse(txtWeight.Text, out int wg) ? wg : (int?)null;
                            dbProduct.RoastLevel = (cboRoastLevel.SelectedItem as ComboBoxItem)?.Content?.ToString();
                            dbProduct.ImageUrl = txtImageUrl.Text.Trim();
                            dbProduct.Description = txtDesc.Text.Trim();

                            // XỬ LÝ LỖI TRIGGER: Chỉ cập nhật Tồn kho và Giá nhập nếu nó THỰC SỰ THAY ĐỔI
                            if (dbProduct.StockQuantity != (int.TryParse(txtStock.Text, out int newStock) ? newStock : 0))
                            {
                                dbProduct.StockQuantity = int.Parse(txtStock.Text);
                            }

                            if (dbProduct.CostPrice != costPrice)
                            {
                                dbProduct.CostPrice = costPrice;
                            }

                            // KHÔNG DÙNG context.Products.Update() NỮA
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
                ShowMessage("Lỗi lưu dữ liệu", errorMsg);
            }
        }

        // ================= CÁC HÀM HỖ TRỢ =================
        private void ClearForm()
        {
            txtName.Text = ""; txtCode.Text = ""; txtCostPrice.Text = "";
            txtProfitRate.Text = ""; txtStock.Text = ""; txtOrigin.Text = "";
            txtWeight.Text = ""; txtImageUrl.Text = ""; txtDesc.Text = "";
            cboCategory.SelectedValue = null; cboRoastLevel.SelectedItem = null;
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