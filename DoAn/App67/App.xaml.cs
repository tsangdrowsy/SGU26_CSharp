using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using App67.Data;
using Microsoft.EntityFrameworkCore;

namespace App67
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        private Window? _window;

        public static string CurrentAdminName { get; set; } = "Admin";
        public static string CurrentAdminRole { get; set; } = "Quản trị viên";
        public static int CurrentAdminId { get; set; } = 0;

        // Cửa sổ chính đang mở (dùng để lấy HWND cho FileOpenPicker và để Đăng xuất)
        public static Window? MainAppWindow { get; set; }

        public App()
        {
            this.InitializeComponent();
            this.RequestedTheme = ApplicationTheme.Light; // Ép giao diện sáng

            // Cấu hình Dependency Injection
            var services = new ServiceCollection();
            // Dùng chung chuỗi kết nối với mọi trang (xem Data/DbConfig.cs)
            services.AddDbContext<CoffeeShopContext>(options => options.UseSqlServer(DbConfig.ConnectionString));

            // Đăng ký các Page sau này (nếu cần)
            services.AddTransient<Pages.ProductsPage>();

            Services = services.BuildServiceProvider();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new LoginWindow();
            _window.Activate();
        }
    }
}