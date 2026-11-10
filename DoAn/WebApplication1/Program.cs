using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH KẾT NỐI DATABASE
builder.Services.AddDbContext<CoffeeShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. BẬT TÍNH NĂNG SESSION
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

// =========================================================
// 3. THỨ TỰ MIDDLEWARE CHUẨN (Rất quan trọng)
// =========================================================
app.UseRouting();       // 1. Định tuyến (Phải đứng trước Session)
app.UseSession();       // 2. Kích hoạt Session
app.UseAuthorization(); // 3. Phân quyền

// 4. Map các Endpoint (Đặt ở cuối cùng)
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();