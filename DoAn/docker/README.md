# CSDL CoffeeShopDb chạy bằng Docker

## Yêu cầu
- Docker Desktop (Windows/macOS) hoặc Docker Engine (Linux).

## Các bước
1. Chép thư mục `docker/` vào `DoAn/` (cùng cấp với `WebApplication1/`).
2. `cd DoAn/docker` rồi tạo file `.env` từ `.env.example` (đổi mật khẩu).
3. `docker compose --env-file .env up -d`
4. `docker compose logs -f db-init` → chờ dòng `Khởi tạo CoffeeShopDb xong.` và danh sách 10 bảng, 11 trigger.

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
| Chạy lại | `docker compose start` |
| Xoá cả dữ liệu, tạo lại từ đầu | `docker compose down -v` rồi `up -d` |
| Chạy script kiểm thử trigger | `docker cp ../WebApplication1/DB/03_test_triggers.sql coffeeshop-db:/tmp/03_test_triggers.sql` rồi `docker exec coffeeshop-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<mk>" -C -I -f 65001 -i /tmp/03_test_triggers.sql` |