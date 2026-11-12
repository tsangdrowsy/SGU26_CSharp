using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using App67.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace App67.Pages
{
    public sealed partial class ProductsPage : Page
    {
        private readonly CoffeeShopContext _context;

        public ProductsPage()
        {
            this.InitializeComponent();
            _context = App.Services.GetRequiredService<CoffeeShopContext>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var products = _context.Products.OrderByDescending(p => p.ProductId).ToList();
                ProductListView.ItemsSource = products;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải dữ liệu: " + ex.Message);
            }
        }
    }
}