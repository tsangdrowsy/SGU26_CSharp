using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using App67.Data; // <-- Sửa lại namespace này nếu bạn đã đổi tên

namespace App67
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
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
                    var products = context.Products.ToList();
                    ProductListView.ItemsSource = products;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("LỖI KẾT NỐI: " + ex.Message);
            }
        }
    }
}