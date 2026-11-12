using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using App67.Pages;

namespace App67
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
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
            }
        }
    }
}