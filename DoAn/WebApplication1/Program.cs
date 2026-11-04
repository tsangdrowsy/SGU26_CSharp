using Microsoft.EntityFrameworkCore;
using WebApplication1.Models; // Đổi WebApplication1 thành tên dự án của bạn nếu khác

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. CẤU HÌNH KẾT NỐI DATABASE
// =========================================================
builder.Services.AddDbContext<CoffeeShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================================
// 2. BẬT TÍNH NĂNG SESSION (Dùng để lưu trạng thái đăng nhập)
// =========================================================
builder.Services.AddSession();

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

// =========================================================dotnet add package BCrypt.Net-Next
// 3. KÍCH HOẠT SESSION (Phải đặt trước UseRouting)
// =========================================================
app.UseSession();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();