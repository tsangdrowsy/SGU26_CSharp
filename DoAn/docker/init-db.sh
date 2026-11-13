#!/bin/bash
# Khởi tạo CoffeeShopDb từ các script trong WebApplication1/DB (mount vào /scripts).
set -euo pipefail
# -I: bật QUOTED_IDENTIFIER (sqlcmd mặc định TẮT, SSMS mặc định BẬT). Bảng Products có cột tính toán
#     PERSISTED nên tạo khoá/chỉ mục trên bảng này bắt buộc QUOTED_IDENTIFIER ON, thiếu -I sẽ lỗi.
# -b: dừng ngay khi gặp lỗi.  -f 65001: đọc file UTF-8 (tiếng Việt).
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I -f 65001)

# Coi là đã khởi tạo xong khi có bảng Products VÀ có trigger (DB tạo dở thì không tính).
READY=$("${SQLCMD[@]}" -h -1 -W -Q "SET NOCOUNT ON;
  IF DB_ID('CoffeeShopDb') IS NULL SELECT 0
  ELSE SELECT CASE WHEN OBJECT_ID('CoffeeShopDb.dbo.Products') IS NOT NULL
       AND (SELECT COUNT(*) FROM CoffeeShopDb.sys.triggers) >= 11 THEN 1 ELSE 0 END" | tr -d '[:space:]')
if [ "$READY" = "1" ]; then
  echo "CoffeeShopDb đã khởi tạo đầy đủ -> bỏ qua (muốn tạo lại: docker compose down -v)."
  exit 0
fi

# Script 01 tự DROP CoffeeShopDb nếu đã tồn tại, nên DB tạo dở sẽ được tạo lại từ đầu.
for f in 01_create_tables_constraints.sql 02_triggers.sql 04_insert_test_data.sql 04_seed_trungnguyen_products.sql; do
  echo ">>> Chạy $f"
  "${SQLCMD[@]}" -i "/scripts/$f"
done
echo "Khởi tạo CoffeeShopDb xong."
"${SQLCMD[@]}" -d CoffeeShopDb -Q "SELECT name AS BangDuLieu FROM sys.tables ORDER BY name; SELECT COUNT(*) AS SoTrigger FROM sys.triggers;"