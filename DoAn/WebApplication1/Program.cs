using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Services.VnPay;

var builder = WebApplication.CreateBuilder(args);

// 1. CẤU HÌNH KẾT NỐI DATABASE
builder.Services.AddDbContext<CoffeeShopContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. BẬT TÍNH NĂNG SESSION
builder.Services.AddSession();

// 3. CẤU HÌNH VNPAY: đọc mục "VnPay" (appsettings.json + user-secrets) vào VnPayOptions
builder.Services.Configure<VnPayOptions>(builder.Configuration.GetSection("VnPay"));
builder.Services.AddSingleton<IVnPayService, VnPayService>();
builder.Services.AddScoped<VnPayPaymentHandler>(); // Scoped vì dùng DbContext
builder.Services.AddDataProtection();                  // khóa ký token xem OrderSuccess
builder.Services.AddSingleton<OrderViewTokenService>();

// Khi chạy sau ngrok/reverse proxy: tin header X-Forwarded-Proto/For do proxy (chạy ở localhost) gửi tới.
// Nhờ vậy app biết request gốc là https -> UseHttpsRedirection không chuyển hướng sai,
// và RemoteIpAddress là IP thật của khách (dùng cho vnp_IpAddr).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

// Add services to the container.
builder.Services.AddRazorPages();
//Add services for controllers
builder.Services.AddControllers();

var app = builder.Build();
app.UseForwardedHeaders(); // phải đặt đầu tiên, trước các middleware đọc Scheme/IP
// Configure the HTTP request pipeline.
app.MapControllers();

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