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

namespace App67.Pages
{
    public sealed partial class CategoriesPage : Page
    {
        private Category? _selectedCategory = null;

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
                    var categories = context.Categories.OrderBy(c => c.CategoryId).ToList();
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
            CategoryDialog.Title = "Thêm Danh Mục Mới";
            await CategoryDialog.ShowAsync();
        }

        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItem is Category category)
            {
                _selectedCategory = category;
                CategoryDialog.Title = "Sửa Danh Mục";
                txtName.Text = category.Name;
                txtDescription.Text = category.Description;
                await CategoryDialog.ShowAsync();
            }
            else
            {
                ShowMessage("Thông báo", "Vui lòng chọn một danh mục trên bảng để sửa.");
            }
        }

        private void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryListView.SelectedItem is Category category)
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

        private void CategoryDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                args.Cancel = true;
                ShowMessage("Lỗi", "Tên danh mục không được để trống.");
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    if (_selectedCategory == null) // THÊM
                    {
                        var newCategory = new Category
                        {
                            Name = txtName.Text.Trim(),
                            Description = txtDescription.Text.Trim(),
                            IsActive = true
                        };
                        context.Categories.Add(newCategory);
                    }
                    else // SỬA
                    {
                        var dbCategory = context.Categories.Find(_selectedCategory.CategoryId);
                        if (dbCategory != null)
                        {
                            dbCategory.Name = txtName.Text.Trim();
                            dbCategory.Description = txtDescription.Text.Trim();
                        }
                    }
                    context.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                ShowMessage("Lỗi lưu dữ liệu", ex.Message);
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
