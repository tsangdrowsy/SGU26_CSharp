using App67.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Linq;
using WebApplication1.Models;

namespace App67.Pages
{
    public sealed partial class CategoriesPage : Page
    {
        // Lớp hiển thị: thêm số sản phẩm và màu trạng thái (không có trong bảng Categories)
        public class CategoryDisplayModel
        {
            public int CategoryId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public int ProductCount { get; set; }
            public string StatusString => IsActive ? "Đang bật" : "Đang tắt";
            public SolidColorBrush StatusColor => new SolidColorBrush(IsActive ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Gray);
        }

        private CategoryDisplayModel? _selectedCategory = null;

        public CategoriesPage()
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
                    // Đếm số sản phẩm ngay trong câu SQL (COUNT), không tải hết sản phẩm về
                    var categories = context.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.CategoryId)
                        .Select(c => new CategoryDisplayModel
                        {
                            CategoryId = c.CategoryId,
                            Name = c.Name,
                            Description = c.Description,
                            IsActive = c.IsActive,
                            ProductCount = context.Products.Count(p => p.CategoryId == c.CategoryId)
                        })
                        .ToList();
                    CategoryListView.ItemsSource = categories;
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu", ex.Message);
            }
        }

        private async void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            _selectedCategory = null;
            txtName.Text = "";
            txtDescription.Text = "";
            HideFormError();
            CategoryDialog.Title = "Thêm Danh Mục Mới";
            await CategoryDialog.ShowAsync();
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItem is CategoryDisplayModel category)
            {
                _selectedCategory = category;
                CategoryDialog.Title = "Sửa Danh Mục";
                txtName.Text = category.Name;
                txtDescription.Text = category.Description;
                HideFormError();
                await CategoryDialog.ShowAsync();
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một danh mục trên bảng để sửa.");
            }
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItem is CategoryDisplayModel category)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var dbCategory = context.Categories.Find(category.CategoryId);
                        if (dbCategory != null)
                        {
                            // Đảo ngược trạng thái IsActive
                            dbCategory.IsActive = !dbCategory.IsActive;
                            context.SaveChanges();
                        }
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", "Không thể đổi trạng thái: " + ex.Message);
                }
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một danh mục để đổi trạng thái.");
            }
        }

        // ================= XÓA DANH MỤC =================
        // Chỉ xóa được khi danh mục KHÔNG còn sản phẩm nào (kể cả sản phẩm đã ẩn),
        // vì Products.CategoryId là khóa ngoại bắt buộc. Muốn ngừng dùng thì "Tắt" danh mục.
        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItem is not CategoryDisplayModel category)
            {
                ShowMessage("Thông báo", "Vui lòng chọn một danh mục để xóa.");
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    int productCount = context.Products.Count(p => p.CategoryId == category.CategoryId);
                    if (productCount > 0)
                    {
                        ShowMessage("Không thể xóa",
                            $"Danh mục '{category.Name}' đang có {productCount} sản phẩm. " +
                            "Hãy chuyển sản phẩm sang danh mục khác, hoặc dùng nút \"Bật / Tắt\" để ngừng hiển thị.");
                        return;
                    }
                }

                ContentDialog confirm = new ContentDialog
                {
                    Title = "Xác nhận xóa",
                    Content = $"Xóa vĩnh viễn danh mục '{category.Name}'?",
                    PrimaryButtonText = "Xóa",
                    CloseButtonText = "Hủy",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.Content.XamlRoot
                };
                if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

                using (var context = new CoffeeShopContext())
                {
                    context.Categories.Where(c => c.CategoryId == category.CategoryId).ExecuteDelete();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowMessage("Lỗi xóa", msg);
            }
        }

        private void CategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string name = txtName.Text.Trim();
            if (name.Length < 2 || name.Length > 100)
            {
                args.Cancel = true;
                ShowFormError("Tên danh mục phải từ 2 đến 100 ký tự.");
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // UQ_Categories_Name: kiểm tra trùng tên trước để báo lỗi dễ hiểu
                    int currentId = _selectedCategory?.CategoryId ?? 0;
                    if (context.Categories.Any(c => c.Name == name && c.CategoryId != currentId))
                    {
                        args.Cancel = true;
                        ShowFormError($"Danh mục '{name}' đã tồn tại.");
                        return;
                    }

                    string? description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();

                    if (_selectedCategory == null) // THÊM
                    {
                        var newCategory = new Category
                        {
                            Name = name,
                            Description = description,
                            IsActive = true
                        };
                        context.Categories.Add(newCategory);
                    }
                    else // SỬA
                    {
                        var dbCategory = context.Categories.Find(_selectedCategory.CategoryId);
                        if (dbCategory != null)
                        {
                            dbCategory.Name = name;
                            dbCategory.Description = description;
                        }
                    }
                    context.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowFormError("Lỗi lưu dữ liệu: " + msg);
            }
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
