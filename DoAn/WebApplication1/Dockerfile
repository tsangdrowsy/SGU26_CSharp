# syntax=docker/dockerfile:1
# Image cho Web CoffeeShop. Build bằng docker compose (xem DoAn/docker/README.md),
# hoặc thủ công:  docker build -t coffeeshop-web DoAn/WebApplication1

# ---------- Giai đoạn 1: BUILD (image SDK ~ 800MB, chỉ dùng để biên dịch) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy file .csproj và restore TRƯỚC khi copy code: Docker cache theo từng lệnh, nên nếu
# chỉ sửa code (không đổi package) thì bước restore (tải NuGet) được dùng lại, build nhanh hơn.
COPY WebApplication1.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish WebApplication1.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Giai đoạn 2: RUNTIME (image ASP.NET ~ 220MB, không có SDK/source code) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Giờ Việt Nam: Web ghi OrderDate bằng DateTime.Now -> phải cùng múi giờ với SQL Server (container db)
ENV TZ=Asia/Ho_Chi_Minh \
    ASPNETCORE_HTTP_PORTS=8080

# Thư mục lưu khóa Data Protection (cookie session + token OrderSuccess). Tạo sẵn và cấp quyền cho
# user "app" (không phải root): volume gắn vào đây sẽ thừa hưởng quyền này ở lần tạo đầu tiên.
RUN mkdir -p /app/keys && chown "$APP_UID" /app/keys

COPY --from=build /app/publish .

# Chạy bằng user thường (có sẵn trong image .NET 8+), không chạy bằng root
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
