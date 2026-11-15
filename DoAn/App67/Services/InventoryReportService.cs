using App67.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App67.Services
{
    // ===================== DTO (dữ liệu thuần, không có kiểu WinUI) =====================
    public record ReceiptRow(int ReceiptId, string ReceiptCode, DateTime ImportDate, string Status,
                             string CreatorName, int ItemCount, decimal TotalValue);

    public record TransactionRow(DateTime Date, string ProductName, string Type, int QuantityChange, string Note);

    public record StockAtDateRow(int ProductId, string Code, string ProductName, string CategoryName,
                                 int Opening, int InQuantity, int OutQuantity, decimal CostPrice)
    {
        public int Closing => Opening + InQuantity - OutQuantity;
    }

    public record ReportLine(DateTime Date, string ProductName, string Reference, int Quantity, decimal UnitPrice, string? OrderStatus);

    public class ReportResult
    {
        public DateTime From { get; set; }
        public DateTime ToExclusive { get; set; }
        public int Opening { get; set; }
        public int Imported { get; set; }   // IMPORT + INITIAL trong kỳ
        public int Returned { get; set; }   // RETURN (đơn hủy) trong kỳ
        public int Exported { get; set; }   // EXPORT (bán) trong kỳ, số dương
        public int Closing => Opening + Imported + Returned - Exported;
        public decimal? CostPrice { get; set; } // chỉ có khi chọn 1 sản phẩm
        public List<ReportLine> Imports { get; set; } = new();
        public List<ReportLine> Exports { get; set; } = new();
    }

    public record WarningRow(string Code, string ProductName, string CategoryName, int Stock, bool IsVisible);

    public class WarningResult
    {
        public int OutOfStock { get; set; }
        public int LowStock { get; set; }
        public int OkStock { get; set; }
        public List<WarningRow> Items { get; set; } = new();
    }

    // Sản phẩm có tồn kho hiện tại KHÁC tổng sổ kho -> báo cáo theo ngày sẽ sai cho sản phẩm đó
    public record LedgerMismatchRow(string Code, string ProductName, int StockQuantity, int LedgerTotal);

    public record CategoryRow(int CategoryId, string Name);

    public record ProductOptionRow(int ProductId, string DisplayName, int StockQuantity,
                                   decimal CostPrice, decimal ProfitRate, decimal SalePrice);

    public class InventoryReportService
    {
        // ---------------- Dữ liệu cho các ComboBox lọc ----------------
        public async Task<List<CategoryRow>> GetCategoriesAsync(CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            return await context.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryRow(c.CategoryId, c.Name))
                .ToListAsync(ct);
        }

        // Gồm cả sản phẩm đã ẩn (vẫn nhập hàng / xem báo cáo được)
        public async Task<List<ProductOptionRow>> GetProductOptionsAsync(CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            var rows = await context.Products.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new { p.ProductId, p.Name, p.Code, p.IsVisible, p.StockQuantity, p.CostPrice, p.ProfitRate, p.SalePrice })
                .ToListAsync(ct);
            return rows.Select(p => new ProductOptionRow(p.ProductId,
                $"{p.Name} ({p.Code}){(p.IsVisible ? "" : " [đã ẩn]")}",
                p.StockQuantity, p.CostPrice, p.ProfitRate, p.SalePrice)).ToList();
        }

        // ---------------- Phiếu nhập ----------------
        public async Task<List<ReceiptRow>> GetReceiptsAsync(DateTime? start, DateTime? endExclusive, CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            var query = context.ImportReceipts.AsNoTracking().AsQueryable();
            if (start.HasValue) query = query.Where(r => r.ImportDate >= start.Value);
            if (endExclusive.HasValue) query = query.Where(r => r.ImportDate < endExclusive.Value);

            // COUNT/SUM chạy trong SQL, không tải chi tiết phiếu về
            var rows = await query
                .OrderByDescending(r => r.ReceiptId)
                .Select(r => new
                {
                    r.ReceiptId, r.ReceiptCode, r.ImportDate, r.Status,
                    CreatorName = r.CreatedByNavigation.FullName,
                    ItemCount = r.ImportReceiptDetails.Count(),
                    TotalValue = r.ImportReceiptDetails.Sum(d => d.Quantity * d.ImportPrice)
                })
                .ToListAsync(ct);

            return rows.Select(r => new ReceiptRow(r.ReceiptId, r.ReceiptCode, r.ImportDate, r.Status,
                r.CreatorName ?? "N/A", r.ItemCount, r.TotalValue)).ToList();
        }

        // ---------------- Lịch sử sổ kho ----------------
        public async Task<List<TransactionRow>> GetTransactionsAsync(DateTime? start, DateTime? endExclusive, CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            var query = context.InventoryTransactions.AsNoTracking().AsQueryable();
            if (start.HasValue) query = query.Where(t => t.TransactionDate >= start.Value);
            if (endExclusive.HasValue) query = query.Where(t => t.TransactionDate < endExclusive.Value);

            var rows = await query
                .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.TransactionId)
                .Select(t => new { t.TransactionDate, ProductName = t.Product.Name, t.TransactionType, t.QuantityChange, t.Note })
                .ToListAsync(ct);

            return rows.Select(t => new TransactionRow(t.TransactionDate, t.ProductName ?? "Sản phẩm đã bị xóa",
                t.TransactionType, t.QuantityChange, t.Note ?? "")).ToList();
        }

        // ---------------- Tồn kho theo ngày ----------------
        // Tồn cuối ngày D = SUM(QuantityChange) của sổ kho có TransactionDate < D + 1 ngày.
        public async Task<List<StockAtDateRow>> GetStockAtDateAsync(DateTime day, int categoryId, CancellationToken ct = default)
        {
            DateTime dayStart = day.Date, dayEnd = dayStart.AddDays(1);
            await using var context = new CoffeeShopContext();

            // 1 câu GROUP BY với SUM có điều kiện (SQL: SUM(CASE WHEN ... THEN ... ELSE 0 END))
            var ledger = await context.InventoryTransactions.AsNoTracking()
                .Where(t => t.TransactionDate < dayEnd)
                .GroupBy(t => t.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Opening = g.Sum(t => t.TransactionDate < dayStart ? t.QuantityChange : 0),
                    In = g.Sum(t => t.TransactionDate >= dayStart && t.QuantityChange > 0 ? t.QuantityChange : 0),
                    Out = g.Sum(t => t.TransactionDate >= dayStart && t.QuantityChange < 0 ? -t.QuantityChange : 0)
                })
                .ToDictionaryAsync(x => x.ProductId, ct);

            var products = await context.Products.AsNoTracking()
                .Where(p => categoryId == 0 || p.CategoryId == categoryId)
                .OrderBy(p => p.Name)
                .Select(p => new { p.ProductId, p.Code, p.Name, CategoryName = p.Category.Name, p.CostPrice })
                .ToListAsync(ct);

            return products.Select(p =>
            {
                ledger.TryGetValue(p.ProductId, out var l);
                return new StockAtDateRow(p.ProductId, p.Code, p.Name, p.CategoryName,
                    l?.Opening ?? 0, l?.In ?? 0, l?.Out ?? 0, p.CostPrice);
            }).ToList();
        }

        // Đối chiếu tồn kho hiện tại với tổng sổ kho. Lệch khi: dữ liệu được nhập thẳng vào DB
        // không qua trigger (VD chạy script seed TRƯỚC khi tạo trigger) -> báo cáo theo ngày sai.
        public async Task<List<LedgerMismatchRow>> GetLedgerMismatchesAsync(CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            var rows = await context.Products.AsNoTracking()
                .Select(p => new
                {
                    p.Code, p.Name, p.StockQuantity,
                    LedgerTotal = context.InventoryTransactions.Where(t => t.ProductId == p.ProductId).Sum(t => (int?)t.QuantityChange) ?? 0
                })
                .Where(x => x.StockQuantity != x.LedgerTotal)
                .ToListAsync(ct);
            return rows.Select(x => new LedgerMismatchRow(x.Code, x.Name, x.StockQuantity, x.LedgerTotal)).ToList();
        }

        // ---------------- Báo cáo nhập - xuất trong kỳ ----------------
        public async Task<ReportResult> GetReportAsync(DateTime? from, DateTime? to, int productId, DateTime now, CancellationToken ct = default)
        {
            // Không chọn ngày bắt đầu = từ đầu ; không chọn ngày kết thúc = đến hôm nay
            var result = new ReportResult
            {
                From = from?.Date ?? new DateTime(2000, 1, 1),
                ToExclusive = (to?.Date ?? now.Date).AddDays(1)
            };
            DateTime f = result.From, t = result.ToExclusive;

            await using var context = new CoffeeShopContext();

            // 1. Tóm tắt từ sổ kho
            var ledger = context.InventoryTransactions.AsNoTracking().Where(x => productId == 0 || x.ProductId == productId);
            var inRange = ledger.Where(x => x.TransactionDate >= f && x.TransactionDate < t);
            result.Opening = await ledger.Where(x => x.TransactionDate < f).SumAsync(x => (int?)x.QuantityChange, ct) ?? 0;
            result.Imported = await inRange.Where(x => x.TransactionType == "IMPORT" || x.TransactionType == "INITIAL").SumAsync(x => (int?)x.QuantityChange, ct) ?? 0;
            result.Returned = await inRange.Where(x => x.TransactionType == "RETURN").SumAsync(x => (int?)x.QuantityChange, ct) ?? 0;
            result.Exported = -(await inRange.Where(x => x.TransactionType == "EXPORT").SumAsync(x => (int?)x.QuantityChange, ct) ?? 0);

            if (productId > 0)
            {
                result.CostPrice = await context.Products.Where(p => p.ProductId == productId).Select(p => (decimal?)p.CostPrice).FirstOrDefaultAsync(ct);
            }

            // 2. Chi tiết nhập: phiếu đã hoàn thành trong kỳ (ngày hoàn thành = ngày cộng kho)
            var imports = await context.ImportReceiptDetails.AsNoTracking()
                .Where(d => d.Receipt.Status == "Completed"
                            && d.Receipt.CompletedAt >= f && d.Receipt.CompletedAt < t
                            && (productId == 0 || d.ProductId == productId))
                .OrderBy(d => d.Receipt.CompletedAt)
                .Select(d => new { Date = d.Receipt.CompletedAt ?? d.Receipt.ImportDate, d.Product.Name, d.Receipt.ReceiptCode, d.Quantity, d.ImportPrice })
                .ToListAsync(ct);
            result.Imports = imports.Select(d => new ReportLine(d.Date, d.Name, "Phiếu " + d.ReceiptCode, d.Quantity, d.ImportPrice, null)).ToList();

            // 3. Chi tiết xuất: đơn đặt trong kỳ (kho bị trừ ngay lúc đặt, kể cả đơn sau đó bị hủy)
            var exports = await context.OrderDetails.AsNoTracking()
                .Where(d => d.Order.OrderDate >= f && d.Order.OrderDate < t
                            && (productId == 0 || d.ProductId == productId))
                .OrderBy(d => d.Order.OrderDate)
                .Select(d => new { d.Order.OrderDate, d.Order.OrderCode, d.Order.Status, d.Product.Name, d.Quantity, d.UnitPrice })
                .ToListAsync(ct);
            result.Exports = exports.Select(d => new ReportLine(d.OrderDate, d.Name, "Đơn " + d.OrderCode, d.Quantity, d.UnitPrice, d.Status)).ToList();

            return result;
        }

        // ---------------- Cảnh báo tồn kho ----------------
        public async Task<WarningResult> GetWarningsAsync(int threshold, int categoryId, string keyword, bool visibleOnly, CancellationToken ct = default)
        {
            await using var context = new CoffeeShopContext();
            var query = context.Products.AsNoTracking().AsQueryable();
            if (categoryId > 0) query = query.Where(p => p.CategoryId == categoryId);
            if (visibleOnly) query = query.Where(p => p.IsVisible);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string k = keyword.Trim();
                query = query.Where(p => p.Name.Contains(k) || p.Code.Contains(k));
            }

            var result = new WarningResult
            {
                OutOfStock = await query.CountAsync(p => p.StockQuantity == 0, ct),
                LowStock = await query.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= threshold, ct),
                OkStock = await query.CountAsync(p => p.StockQuantity > threshold, ct)
            };

            var items = await query
                .Where(p => p.StockQuantity <= threshold)
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.Name)
                .Select(p => new { p.Code, p.Name, CategoryName = p.Category.Name, p.StockQuantity, p.IsVisible })
                .ToListAsync(ct);
            result.Items = items.Select(p => new WarningRow(p.Code, p.Name, p.CategoryName, p.StockQuantity, p.IsVisible)).ToList();
            return result;
        }
    }
}
