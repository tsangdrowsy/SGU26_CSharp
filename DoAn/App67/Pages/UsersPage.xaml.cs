using App67.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplication1.Models;

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
            public string Phone { get; set; } = string.Empty;
            public string RawRole { get; set; } = string.Empty;
            public bool IsLocked { get; set; }
            public string RoleString { get; set; } = string.Empty;
            public string StatusString { get; set; } = string.Empty;
            public SolidColorBrush StatusColor { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Gray);
        }

        private List<UserDisplayModel> _allUsers = new();

        // null = đang THÊM mới; có giá trị = đang SỬA user này.
        // (Bản cũ lấy user từ UserListView.SelectedItem, nhưng bấm nút "Sửa" trên 1 dòng
        //  KHÔNG chọn dòng đó -> có thể lưu nhầm/không lưu. Giờ lưu Id ngay khi mở form.)
        private int? _editingUserId;

        private bool _isReady;

        public UsersPage()
        {
            this.InitializeComponent();
            _isReady = true;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new CoffeeShopContext())
                {
                    _allUsers = context.Users.AsNoTracking().OrderBy(u => u.UserId).ToList().Select(u => new UserDisplayModel
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone ?? "",
                        RawRole = u.Role,
                        IsLocked = u.IsLocked,
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

        // Hàm lọc dữ liệu: từ khóa + quyền + trạng thái
        private void FilterData()
        {
            IEnumerable<UserDisplayModel> query = _allUsers;

            string searchText = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(u =>
                    u.Username.ToLower().Contains(searchText) ||
                    u.FullName.ToLower().Contains(searchText) ||
                    u.Email.ToLower().Contains(searchText) ||
                    u.Phone.Contains(searchText));
            }

            string role = (cboRoleFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.RawRole == role);

            string status = (cboStatusFilter.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            if (status == "active") query = query.Where(u => !u.IsLocked);
            else if (status == "locked") query = query.Where(u => u.IsLocked);

            var result = query.ToList();
            UserListView.ItemsSource = result;
            txtResultCount.Text = $"{result.Count}/{_allUsers.Count} tài khoản";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isReady) FilterData();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isReady) FilterData();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }

        // ================= THÊM TÀI KHOẢN =================
        private async void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            _editingUserId = null;
            EditUserDialog.Title = "Thêm tài khoản mới";
            EditUserDialog.PrimaryButtonText = "Tạo tài khoản";

            txtEditUsername.Text = "";
            txtEditUsername.IsReadOnly = false;
            txtEditPassword.Password = "";
            txtEditPassword.Visibility = Visibility.Visible;
            txtEditFullName.Text = "";
            txtEditEmail.Text = "";
            txtEditPhone.Text = "";
            txtEditAddress.Text = "";
            txtEditWard.Text = "";
            txtEditDistrict.Text = "";
            txtEditCity.Text = "";
            cboEditRole.SelectedIndex = 1; // mặc định Khách hàng
            cboEditRole.IsEnabled = true;
            HideError();

            await EditUserDialog.ShowAsync();
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
                            _editingUserId = user.UserId;
                            EditUserDialog.Title = "Sửa thông tin người dùng";
                            EditUserDialog.PrimaryButtonText = "Lưu";

                            // Đổ dữ liệu vào form
                            txtEditUsername.Text = user.Username;
                            txtEditUsername.IsReadOnly = true;            // không cho đổi tên đăng nhập
                            txtEditPassword.Visibility = Visibility.Collapsed; // đổi mật khẩu dùng nút "Reset MK"
                            txtEditFullName.Text = user.FullName;
                            txtEditEmail.Text = user.Email;
                            txtEditPhone.Text = user.Phone;
                            txtEditAddress.Text = user.Address;
                            txtEditWard.Text = user.Ward;
                            txtEditDistrict.Text = user.District;
                            txtEditCity.Text = user.City;
                            cboEditRole.SelectedIndex = user.Role == "Admin" ? 0 : 1;
                            // Không cho tự hạ quyền của chính mình (tránh mất quyền vào app)
                            cboEditRole.IsEnabled = user.UserId != App.CurrentAdminId;
                            HideError();

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

        private void cboEditRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isReady) return;
            bool isCustomer = SelectedRole() == "Customer";
            txtAddressNote.Visibility = isCustomer ? Visibility.Visible : Visibility.Collapsed;
        }

        private string SelectedRole() => (cboEditRole.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Customer";

        // ================= KIỂM TRA DỮ LIỆU =================
        // Khớp với ràng buộc DB: CK_Users_Role, CK_Users_Phone, CK_Users_Email, CK_Users_CustomerShippingInfo,
        // UQ_Users_Username, UQ_Users_Email. Kiểm tra trước để báo lỗi tiếng Việt dễ hiểu thay vì lỗi SQL.
        private string? ValidateForm(bool isNew)
        {
            string username = txtEditUsername.Text.Trim();
            string fullName = txtEditFullName.Text.Trim();
            string email = txtEditEmail.Text.Trim();
            string phone = txtEditPhone.Text.Trim();

            if (isNew)
            {
                if (!Regex.IsMatch(username, @"^[a-zA-Z0-9]{3,50}$"))
                    return "Tên đăng nhập phải từ 3-50 ký tự, chỉ gồm chữ và số.";
                if (txtEditPassword.Password.Length < 6)
                    return "Mật khẩu phải có ít nhất 6 ký tự.";
            }
            if (fullName.Length < 2 || fullName.Length > 100)
                return "Họ tên phải từ 2 đến 100 ký tự.";
            if (email.Length > 100 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                return "Email không hợp lệ.";
            if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, @"^0[0-9]{9}$"))
                return "Số điện thoại phải có 10 số và bắt đầu bằng 0.";

            if (SelectedRole() == "Customer" &&
                (string.IsNullOrEmpty(phone) ||
                 string.IsNullOrWhiteSpace(txtEditAddress.Text) || string.IsNullOrWhiteSpace(txtEditWard.Text) ||
                 string.IsNullOrWhiteSpace(txtEditDistrict.Text) || string.IsNullOrWhiteSpace(txtEditCity.Text)))
                return "Tài khoản Khách hàng phải nhập đủ SĐT, địa chỉ, phường/xã, quận/huyện, tỉnh/thành phố.";

            return null;
        }

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ================= HÀM LƯU (THÊM HOẶC SỬA) =================
        private void EditUserDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool isNew = _editingUserId == null;
            string? error = ValidateForm(isNew);
            if (error != null)
            {
                args.Cancel = true; // giữ dialog mở để sửa
                ShowError(error);
                return;
            }

            string username = txtEditUsername.Text.Trim();
            string email = txtEditEmail.Text.Trim();

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Kiểm tra trùng trước khi lưu (DB cũng có UNIQUE, đây chỉ để báo lỗi rõ ràng)
                    if (isNew && context.Users.Any(u => u.Username == username))
                    {
                        args.Cancel = true; ShowError("Tên đăng nhập đã tồn tại!"); return;
                    }
                    if (context.Users.Any(u => u.Email == email && u.UserId != (_editingUserId ?? 0)))
                    {
                        args.Cancel = true; ShowError("Email đã được tài khoản khác sử dụng!"); return;
                    }

                    User? user;
                    if (isNew)
                    {
                        user = new User
                        {
                            Username = username,
                            // Băm mật khẩu bằng BCrypt (giống Web và màn đăng nhập), KHÔNG lưu mật khẩu gốc
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword(txtEditPassword.Password),
                            IsLocked = false,
                            CreatedAt = DateTime.Now
                        };
                        context.Users.Add(user);
                    }
                    else
                    {
                        user = context.Users.Find(_editingUserId!.Value);
                        if (user == null) return;
                    }

                    user.FullName = txtEditFullName.Text.Trim();
                    user.Email = email;
                    user.Role = SelectedRole();
                    user.Phone = NullIfEmpty(txtEditPhone.Text);
                    user.Address = NullIfEmpty(txtEditAddress.Text);
                    user.Ward = NullIfEmpty(txtEditWard.Text);
                    user.District = NullIfEmpty(txtEditDistrict.Text);
                    user.City = NullIfEmpty(txtEditCity.Text);

                    context.SaveChanges();
                }
                LoadData();
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                // Nếu vi phạm ràng buộc CK_Users_CustomerShippingInfo, SQL sẽ ném lỗi tại đây
                string msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ShowError("Không thể lưu. Chi tiết: " + msg);
            }
        }

        private void ShowError(string message)
        {
            txtUserError.Text = message;
            txtUserError.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            txtUserError.Text = "";
            txtUserError.Visibility = Visibility.Collapsed;
            txtAddressNote.Visibility = SelectedRole() == "Customer" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ================= KHÓA / MỞ KHÓA VÀ RESET MẬT KHẨU =================
        private async void btnToggleLock_Click(object sender, RoutedEventArgs e)
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
                            ContentDialog confirm = new ContentDialog
                            {
                                Title = user.IsLocked ? "Mở khóa tài khoản" : "Khóa tài khoản",
                                Content = user.IsLocked
                                    ? $"Mở khóa tài khoản '{user.Username}'?"
                                    : $"Khóa tài khoản '{user.Username}'? Người dùng sẽ không đăng nhập được nữa.",
                                PrimaryButtonText = "Đồng ý",
                                CloseButtonText = "Hủy",
                                XamlRoot = this.Content.XamlRoot
                            };
                            if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

                            user.IsLocked = !user.IsLocked;
                            context.SaveChanges();
                        }
                    }
                    LoadData();
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
