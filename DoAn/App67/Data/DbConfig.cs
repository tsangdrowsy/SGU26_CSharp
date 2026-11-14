using System;

namespace App67.Data
{
    // NƠI DUY NHẤT quyết định App67 kết nối tới DB nào.
    //
    // Trước đây có 2 chuỗi kết nối: 1 trong App.xaml.cs (không trang nào dùng) và 1 viết cứng
    // trong CoffeeShopContext.OnConfiguring (mọi trang đều dùng). Sửa nhầm chỗ thì app vẫn đọc
    // DB cũ -> số liệu khác Web mà không báo lỗi gì.
    //
    // Mặc định: SQL Server cài trên máy (Windows Authentication).
    // Dùng DB trong Docker: đặt biến môi trường COFFEESHOP_DB (không cần sửa code, không lộ mật khẩu lên Git):
    //   setx COFFEESHOP_DB "Server=localhost,1433;Database=CoffeeShopDb;User Id=sa;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"
    // rồi mở lại Visual Studio (biến môi trường chỉ áp dụng cho tiến trình mở SAU lệnh setx).
    public static class DbConfig
    {
        public const string EnvironmentVariable = "COFFEESHOP_DB";

        public const string DefaultConnection =
            "Server=localhost;Database=CoffeeShopDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        public static string ConnectionString
        {
            get
            {
                var fromEnv = Environment.GetEnvironmentVariable(EnvironmentVariable);
                return string.IsNullOrWhiteSpace(fromEnv) ? DefaultConnection : fromEnv;
            }
        }

        // Hiển thị nguồn DB (KHÔNG in mật khẩu) để dễ biết app đang trỏ vào đâu
        public static string Describe()
        {
            try
            {
                var b = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(ConnectionString);
                string source = Environment.GetEnvironmentVariable(EnvironmentVariable) is { Length: > 0 } ? EnvironmentVariable : "mặc định";
                return $"{b.DataSource} / {b.InitialCatalog} ({source})";
            }
            catch
            {
                return "(chuỗi kết nối không hợp lệ)";
            }
        }
    }
}
