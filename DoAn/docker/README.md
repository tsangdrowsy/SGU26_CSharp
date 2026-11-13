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
- **App67:** chuỗi kết nối đang viết cứng trong `App67/App.xaml.cs`; đổi tạm thành chuỗi trên khi chạy với Docker.

## Lệnh hay dùng
| Việc | Lệnh |
|---|---|
| Dừng | `docker compose stop` |
| Chạy lại | `docker compose start` |
| Xoá cả dữ liệu, tạo lại từ đầu | `docker compose down -v` rồi `up -d` |
| Chạy script kiểm thử trigger | `docker cp ../WebApplication1/DB/03_test_triggers.sql coffeeshop-db:/tmp/03_test_triggers.sql` rồi `docker exec coffeeshop-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "<mk>" -C -I -f 65001 -i /tmp/03_test_triggers.sql` |