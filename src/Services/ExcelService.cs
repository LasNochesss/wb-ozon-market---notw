using ClosedXML.Excel;
using Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class ExcelService
{
    public async Task ExportMainSystemAsync(AppDbContext db, string path)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("MAIN_SYSTEM");
        var row = 1;
        row = await WriteBlock(ws, row, "Products", await db.Products.AsNoTracking().ToListAsync(), p => new object?[] { p.Sku, p.Name, p.Category, p.PackagingRubPerUnit, p.MinMarginPct, p.Status });
        row = await WriteBlock(ws, row, "FxRates", await db.FxRates.AsNoTracking().ToListAsync(), x => new object?[] { x.FxDate, x.CnyToRub });
        row = await WriteBlock(ws, row, "Purchases", await db.Purchases.AsNoTracking().ToListAsync(), x => new object?[] { x.PurchaseId, x.PaidDate, x.Sku, x.Qty, x.UnitPriceCny, x.FeesCny });
        row = await WriteBlock(ws, row, "Logistics", await db.Logistics.AsNoTracking().ToListAsync(), x => new object?[] { x.LogId, x.PurchaseId, x.DeliveryRub, x.CustomsRub, x.OtherRub });
        row = await WriteBlock(ws, row, "Sales", await db.Sales.AsNoTracking().ToListAsync(), x => new object?[] { x.OrderId, x.SaleDate, x.Marketplace, x.SaleType, x.Sku, x.Qty, x.PriceRub, x.CommissionRub, x.MpLogisticsRub, x.Status });
        wb.SaveAs(path);
    }

    private static Task<int> WriteBlock<T>(IXLWorksheet ws, int startRow, string name, IReadOnlyCollection<T> items, Func<T, object?[]> map)
    {
        ws.Cell(startRow, 1).Value = name;
        var r = startRow + 1;
        foreach (var item in items)
        {
            var data = map(item);
            for (var c = 0; c < data.Length; c++) ws.Cell(r, c + 1).Value = data[c]?.ToString() ?? string.Empty;
            r++;
        }
        return Task.FromResult(r + 1);
    }
}
