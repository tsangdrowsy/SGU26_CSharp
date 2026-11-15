using Microsoft.AspNetCore.DataProtection;
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
// Data Protection: khóa ký/mã hóa token chống giả mạo form (antiforgery), cookie session
// và token xem OrderSuccess sau khi thanh toán VNPay.
// Trong Docker, nếu không lưu khóa ra volume thì mỗi lần container khởi động lại sẽ sinh khóa mới
// -> form đang mở báo lỗi khi gửi, link OrderSuccess cũ mất hiệu lực. DataProtection:KeysPath do compose đặt.
// LƯU Ý: dữ liệu session (UserId) vẫn nằm trong RAM (AddSession dùng bộ nhớ trong tiến trình),
// nên restart container thì người dùng vẫn phải đăng nhập lại. Muốn giữ cả session cần cache dùng chung
// (vd. SQL Server/Redis distributed cache) - cũng là điều kiện để chạy nhiều bản Web cùng lúc.
var dataProtection = builder.Services.AddDataProtection();
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
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

// Phục vụ file tĩnh có trong wwwroot LÚC CHẠY. MapStaticAssets (bên dưới) chỉ biết các file có lúc build,
// nên ảnh sản phẩm do App WinUI copy vào wwwroot/img/sanpham sau đó sẽ bị 404 nếu thiếu dòng này.
app.UseStaticFiles();

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