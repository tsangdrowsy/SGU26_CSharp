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
using BCrypt.Net;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App67
{
    /// <summary>
    /// An empty top-level window that can be activated on its own (e.g. as a
    /// secondary window of the app).
    /// </summary>
    public sealed partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            this.InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nhập đầy đủ tài khoản và mật khẩu.");
                return;
            }

            try
            {
                using (var context = new CoffeeShopContext())
                {
                    // Tìm user có Role là Admin
                    var user = context.Users.FirstOrDefault(u => u.Username == username && u.Role == "Admin");

                    if (user == null)
                    {
                        ShowError("Tài khoản không tồn tại hoặc không có quyền Admin.");
                        return;
                    }

                    // Kiểm tra mật khẩu bằng BCrypt
                    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    if (!isPasswordValid)
                    {
                        ShowError("Sai mật khẩu.");
                        return;
                    }

                    if (user.IsLocked)
                    {
                        ShowError("Tài khoản này đã bị khóa.");
                        return;
                    }

                    // Đăng nhập thành công -> Lưu thông tin Admin và mở MainWindow
                    App.CurrentAdminName = user.FullName; // Lưu tên Admin để hiển thị ở góc trái
                    App.CurrentAdminRole = user.Role;
                    App.CurrentAdminId = user.UserId;

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Activate();

                    this.Close(); // Đóng cửa sổ đăng nhập
                }
            }
            catch (Exception ex)
            {
                ShowError("Lỗi kết nối Database: " + ex.Message);
            }
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
