using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using App67.Pages;
using System; // Thêm dòng này để dùng hàm xử lý chuỗi (Substring)

namespace App67
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            App.MainAppWindow = this;

            // =========================================================
            // 1. THÊM ĐOẠN NÀY ĐỂ LẤY THÔNG TIN ADMIN TỪ LÚC ĐĂNG NHẬP
            // =========================================================
            txtAdminName.Text = App.CurrentAdminName;
            txtAdminRole.Text = App.CurrentAdminRole;

            // Lấy chữ cái đầu tiên của tên làm Avatar (Ví dụ: "Admin" -> "A")
            if (!string.IsNullOrEmpty(App.CurrentAdminName))
            {
                txtAvatarLetter.Text = App.CurrentAdminName.Substring(0, 1).ToUpper();
            }

            // Mặc định mở trang Sản phẩm
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                string tag = item.Tag.ToString() ?? "";
                if (tag == "Dashboard")
                { ContentFrame.Navigate(typeof(Pages.DashboardPage)); }
                else if (tag == "Products")
                {
                    ContentFrame.Navigate(typeof(ProductsPage));
                }
                else if (tag == "Categories")
                {
                    ContentFrame.Navigate(typeof(Pages.CategoriesPage));
                }
                else if (tag == "Prices")
                {
                    ContentFrame.Navigate(typeof(Pages.PricesPage));
                }
                // Thêm điều hướng cho trang Đơn hàng (nếu bạn đã tạo OrdersPage)
                else if (tag == "Orders")
                {
                    ContentFrame.Navigate(typeof(OrdersPage));
                }
                else if (tag == "Users")
                {
                    ContentFrame.Navigate(typeof(Pages.UsersPage));
                }
                else if (tag == "Inventory")
                {
                    ContentFrame.Navigate(typeof(Pages.InventoryPage));
                }
            }
        }

        // =========================================================
        // 2. NÚT CÀI ĐẶT (BÁNH RĂNG): Đăng xuất
        // =========================================================
        private async void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Tài khoản",
                Content = $"Đang đăng nhập: {App.CurrentAdminName} ({App.CurrentAdminRole})",
                PrimaryButtonText = "Đăng xuất",
                CloseButtonText = "Đóng",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Xóa thông tin phiên đăng nhập, mở lại màn đăng nhập rồi đóng cửa sổ chính.
                // Phải mở LoginWindow TRƯỚC khi đóng: đóng cửa sổ cuối cùng sẽ thoát cả ứng dụng.
                App.CurrentAdminId = 0;
                App.CurrentAdminName = "Admin";
                App.CurrentAdminRole = "Quản trị viên";
                App.MainAppWindow = null;

                var loginWindow = new LoginWindow();
                loginWindow.Activate();
                this.Close();
            }
        }
    }
}