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
                if (tag == "Products")
                {
                    ContentFrame.Navigate(typeof(ProductsPage));
                }
                else if (tag == "Categories")
                {
                    ContentFrame.Navigate(typeof(Pages.CategoriesPage));
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
        // 2. THÊM HÀM NÀY ĐỂ XỬ LÝ KHI BẤM NÚT CÀI ĐẶT (BÁNH RĂNG)
        // =========================================================
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Cài đặt hệ thống",
                Content = "Chức năng cài đặt đang được phát triển.",
                CloseButtonText = "Đóng",
                XamlRoot = this.Content.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }
}