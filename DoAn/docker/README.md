# CoffeeShop chạy bằng Docker (SQL Server + Web)

## Yêu cầu
- Docker Desktop (Windows/macOS) hoặc Docker Engine (Linux).

## Các bước
1. Chép thư mục `docker/` vào `DoAn/` (cùng cấp với `WebApplication1/`).
2. `cd DoAn/docker` rồi tạo file `.env` từ `.env.example` (đổi mật khẩu).
3. (Tuỳ chọn) điền `VNPAY_TMN_CODE`, `VNPAY_HASH_SECRET` trong `.env` để Web trong container thanh toán được VNPay.
4. `docker compose --env-file .env up -d --build`
5. `docker compose logs -f db-init` → chờ dòng `Khởi tạo CoffeeShopDb xong.` và danh sách 10 bảng, 11 trigger.
6. Mở Web: http://localhost:5144 (container `web` chỉ chạy sau khi `db-init` xong).

## Các container
| Service | Làm gì | Cổng trên máy |
|---|---|---|
| `db` | SQL Server 2022, dữ liệu trong volume `mssql-data` | 1433 |
| `db-init` | Chạy 1 lần: tạo CoffeeShopDb từ `WebApplication1/DB/*.sql` | – |
| `web` | Web ASP.NET Core, build từ `WebApplication1/Dockerfile` | 5144 |

- Web trong container kết nối DB bằng tên service `db` (`Server=db,1433`), không phải `localhost`.
- Cổng 5144 trùng với `dotnet run`, nên ngrok/IPN VNPay giữ nguyên. **Đừng chạy `dotnet run` cùng lúc** (trùng cổng).
- Ảnh sản phẩm: thư mục `WebApplication1/wwwroot/img/sanpham` trên máy được gắn thẳng vào container,
  ảnh App WinUI vừa thêm hiện ngay trên Web, không cần build lại.
- Sửa code Web xong: `docker compose --env-file .env up -d --build web` để build lại image.
- Khoá Data Protection lưu ở volume `web-keys`. Dữ liệu đăng nhập (session) vẫn nằm trong RAM:
  restart container `web` thì người dùng phải đăng nhập lại.

## Trỏ ứng dụng vào container
Chuỗi kết nối dùng SQL login thay cho Windows Authentication:

```
Server=localhost,1433;Database=CoffeeShopDb;User Id=sa;Password=<mật khẩu trong .env>;TrustServerCertificate=True;MultipleActiveResultSets=true
```

- **Web:** không sửa appsettings.json, đặt bằng user-secrets:
  `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<chuỗi trên>" --project WebApplication1`
- **App67:** đặt biến môi trường `COFFEESHOP_DB` (đọc trong `App67/Data/DbConfig.cs`), rồi mở lại Visual Studio:
  `setx COFFEESHOP_DB "<chuỗi trên>"` (xoá: `reg delete HKCU\Environment /v COFFEESHOP_DB /f`).
  Không đặt thì App67 dùng SQL Server cài trên máy (Windows Authentication).

## Múi giờ
Container đặt `TZ: Asia/Ho_Chi_Minh` vì trigger ghi sổ kho bằng giờ của SQL Server còn ứng dụng ghi giờ máy.
Nếu đã tạo container trước khi có dòng này: `docker compose --env-file .env up -d db` để tạo lại (dữ liệu vẫn giữ).
Kiểm tra: `SELECT SYSDATETIME()` phải ra đúng giờ Việt Nam.

## Lệnh hay dùng
| Việc | Lệnh |
|---|---|
| Dừng | `docker compose stop` |
| Xem log Web | `docker compose logs -f web` |
| Build lại Web sau khi sửa code | `docker compose --env-file .env up -d --build web` |
| Chạy lại | `docker compose start` |
| Xoá cả dữ liệu, tạo lại từ đầu | `docker compose down -v` rồi `up -d` |
| Chạy script kiểm thử trigger | `docker cp ../WebApplication1/DB/03_test_triggers.sql coffeeshop-db:/tmp/03_test_triggers.sql` rồi `docker exec coffeeshop-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<mk>" -C -I -f 65001 -i /tmp/03_test_triggers.sql` |