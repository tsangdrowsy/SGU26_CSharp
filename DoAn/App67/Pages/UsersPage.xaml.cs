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
    public sealed partial class UsersPage : Page
    {
        public class UserDisplayModel
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string RoleString { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        private List<UserDisplayModel> _allUsers = new();

        public UsersPage()
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
                    _allUsers = context.Users.OrderBy(u => u.UserId).ToList().Select(u => new UserDisplayModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        RoleString = u.Role == "Admin" ? "Quản trị viên" : "Khách hàng",
                        StatusString = u.IsLocked ? "Bị khóa" : "Hoạt động",
                        StatusColor = u.IsLocked ? new SolidColorBrush(Microsoft.UI.Colors.Red) : new SolidColorBrush(Microsoft.UI.Colors.Green)
                    }).ToList();

                    FilterData(); // Gọi hàm lọc sau khi tải
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Lỗi tải dữ liệu", ex.Message);
            }
        }

        // Hàm lọc dữ liệu (Tìm kiếm)
        private void FilterData()
        {
            string searchText = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                UserListView.ItemsSource = _allUsers;
            }
            else
            {
                var filtered = _allUsers.Where(u =>
                    u.Username.ToLower().Contains(searchText) ||
                    u.FullName.ToLower().Contains(searchText) ||
                    u.Email.ToLower().Contains(searchText)).ToList();
                UserListView.ItemsSource = filtered;
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }

        // ================= HÀM SỬA THÔNG TIN =================
        private async void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var user = context.Users.Find(userId);
                        if (user != null)
                        {
                            // Đổ dữ liệu vào form
                            txtEditUsername.Text = user.Username;
                            txtEditFullName.Text = user.FullName;
                            txtEditEmail.Text = user.Email;
                            txtEditPhone.Text = user.Phone;
                            txtEditAddress.Text = user.Address;
                            txtEditWard.Text = user.Ward;
                            txtEditDistrict.Text = user.District;
                            txtEditCity.Text = user.City;

                            await EditUserDialog.ShowAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", ex.Message);
                }
            }
        }

        // ================= HÀM LƯU KHI SỬA =================
        private void EditUserDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(txtEditFullName.Text))
            {
                args.Cancel = true;
                ShowMessage("Lỗi", "Họ tên không được để trống.");
                return;
            }

            try
            {
                // Tìm User đang được chọn trong danh sách hiển thị
                var selectedDisplay = UserListView.SelectedItem as UserDisplayModel;
                if (selectedDisplay == null) return;

                using (var context = new CoffeeShopContext())
                {
                    var user = context.Users.Find(selectedDisplay.UserId);
                    if (user != null)
                    {
                        user.FullName = txtEditFullName.Text.Trim();
                        user.Phone = string.IsNullOrWhiteSpace(txtEditPhone.Text) ? null : txtEditPhone.Text.Trim();
                        user.Address = string.IsNullOrWhiteSpace(txtEditAddress.Text) ? null : txtEditAddress.Text.Trim();
                        user.Ward = string.IsNullOrWhiteSpace(txtEditWard.Text) ? null : txtEditWard.Text.Trim();
                        user.District = string.IsNullOrWhiteSpace(txtEditDistrict.Text) ? null : txtEditDistrict.Text.Trim();
                        user.City = string.IsNullOrWhiteSpace(txtEditCity.Text) ? null : txtEditCity.Text.Trim();

                        context.SaveChanges();
                        LoadData();
                    }
                }
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                // Nếu vi phạm ràng buộc CK_Users_CustomerShippingInfo, SQL sẽ ném lỗi tại đây
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowMessage("Lỗi lưu dữ liệu", "Không thể lưu. Vui lòng kiểm tra lại thông tin (Đặc biệt là địa chỉ và SĐT của Khách hàng).\n\nChi tiết: " + msg);
            }
        }

        // ================= CÁC HÀM CŨ (KHÓA VÀ RESET) =================
        private void btnToggleLock_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                if (userId == App.CurrentAdminId)
                {
                    ShowMessage("Cảnh báo", "Bạn không thể tự khóa tài khoản của chính mình!");
                    return;
                }
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var user = context.Users.Find(userId);
                        if (user != null)
                        {
                            user.IsLocked = !user.IsLocked;
                            context.SaveChanges();
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi cập nhật", ex.Message);
                }
            }
        }

        private async void btnResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                if (userId == App.CurrentAdminId)
                {
                    ShowMessage("Cảnh báo", "Bạn không thể tự reset mật khẩu của chính mình!");
                    return;
                }
                try
                {
                    using (var context = new CoffeeShopContext())
                    {
                        var user = context.Users.Find(userId);
                        if (user != null)
                        {
                            if (user.Role == "Admin")
                            {
                                ShowMessage("Cảnh báo", "Không được phép reset mật khẩu của tài khoản Quản trị viên!");
                                return;
                            }

                            ContentDialog confirmDialog = new ContentDialog
                            {
                                Title = "Xác nhận Reset",
                                Content = $"Reset mật khẩu của '{user.Username}' về 123456?",
                                PrimaryButtonText = "Đồng ý",
                                CloseButtonText = "Hủy",
                                XamlRoot = this.Content.XamlRoot
                            };

                            if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
                            {
                                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                                context.SaveChanges();
                                ShowMessage("Thành công", "Đã đặt lại mật khẩu thành 123456.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Lỗi", ex.Message);
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
