using ClosedXML.Excel;
using Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Services;

public record ImportError(string Sheet, int Row, string Column, string Message);
public record ImportResult(bool Success, List<ImportError> Errors);

public class ExcelProjectStorageService
{
    private static readonly string[] Sheets = ["Товары", "Курсы валют", "Закупки", "Логистика", "Продажи", "Маркетинг", "Прочие расходы", "Склад", "Юнит-экономика", "P&L + KPI"];

    public async Task ExportAsync(AppDbContext db, string path, IReadOnlyCollection<InventoryRow> inventory, IReadOnlyCollection<UnitEconomicsRow> unitEconomics)
    {
        using var wb = new XLWorkbook();

        WriteProducts(wb.AddWorksheet("Товары"), await db.Products.AsNoTracking().ToListAsync());
        WriteFxRates(wb.AddWorksheet("Курсы валют"), await db.FxRates.AsNoTracking().ToListAsync());
        WritePurchases(wb.AddWorksheet("Закупки"), await db.Purchases.AsNoTracking().ToListAsync());
        WriteLogistics(wb.AddWorksheet("Логистика"), await db.Logistics.AsNoTracking().ToListAsync());
        WriteSales(wb.AddWorksheet("Продажи"), await db.Sales.AsNoTracking().ToListAsync());
        WriteMarketing(wb.AddWorksheet("Маркетинг"), await db.Marketing.AsNoTracking().ToListAsync());
        WriteOtherCosts(wb.AddWorksheet("Прочие расходы"), await db.OtherCosts.AsNoTracking().ToListAsync());
        WriteInventory(wb.AddWorksheet("Склад"), inventory);
        WriteUnitEconomics(wb.AddWorksheet("Юнит-экономика"), unitEconomics);
        WritePnlPlaceholder(wb.AddWorksheet("P&L + KPI"));

        wb.SaveAs(path);
    }

    public async Task<ImportResult> ImportAsync(AppDbContext db, string path)
    {
        var errors = new List<ImportError>();
        using var wb = new XLWorkbook(path);

        foreach (var sheet in Sheets)
            if (wb.Worksheets.All(s => s.Name != sheet))
                errors.Add(new ImportError(sheet, 0, "Sheet", "Лист отсутствует"));

        if (errors.Count > 0) return new(false, errors);

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            db.Products.RemoveRange(db.Products);
            db.FxRates.RemoveRange(db.FxRates);
            db.Purchases.RemoveRange(db.Purchases);
            db.Logistics.RemoveRange(db.Logistics);
            db.Sales.RemoveRange(db.Sales);
            db.Marketing.RemoveRange(db.Marketing);
            db.OtherCosts.RemoveRange(db.OtherCosts);
            await db.SaveChangesAsync();

            ImportProducts(wb.Worksheet("Товары"), db, errors);
            ImportFxRates(wb.Worksheet("Курсы валют"), db, errors);
            ImportPurchases(wb.Worksheet("Закупки"), db, errors);
            ImportLogistics(wb.Worksheet("Логистика"), db, errors);
            ImportSales(wb.Worksheet("Продажи"), db, errors);
            ImportMarketing(wb.Worksheet("Маркетинг"), db, errors);
            ImportOtherCosts(wb.Worksheet("Прочие расходы"), db, errors);

            if (errors.Count > 0)
            {
                await tx.RollbackAsync();
                return new(false, errors);
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new(true, errors);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            errors.Add(new ImportError("*", 0, "Exception", ex.Message));
            return new(false, errors);
        }
    }

    private static void InitSheet(IXLWorksheet ws)
    {
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents();
    }

    private static void WriteProducts(IXLWorksheet ws, IReadOnlyCollection<Product> rows)
    {
        var headers = new[] { "SKU", "Название", "Категория", "Целевая закупка (CNY)", "Вес (кг)", "Габариты", "Упаковка (руб/шт)", "Мин. маржа (%)", "Целевая цена (руб)", "Статус" };
        WriteHeader(ws, headers);
        var r = 2;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.Sku;
            ws.Cell(r, 2).Value = x.Name;
            ws.Cell(r, 3).Value = x.Category;
            ws.Cell(r, 4).Value = x.PurchaseTargetCny;
            ws.Cell(r, 5).Value = x.WeightKg;
            ws.Cell(r, 6).Value = x.DimensionsText;
            ws.Cell(r, 7).Value = x.PackagingRubPerUnit;
            ws.Cell(r, 8).Value = x.MinMarginPct;
            ws.Cell(r, 9).Value = x.TargetPriceRub;
            ws.Cell(r,10).Value = x.Status.ToString();
            r++;
        }
        InitSheet(ws);
    }

    private static void WriteFxRates(IXLWorksheet ws, IReadOnlyCollection<FxRate> rows)
    {
        WriteHeader(ws, ["Дата курса", "Курс CNY→RUB"]); int r=2;
        foreach (var x in rows){ ws.Cell(r,1).Value=x.FxDate.ToDateTime(TimeOnly.MinValue); ws.Cell(r,1).Style.DateFormat.Format="dd.MM.yyyy"; ws.Cell(r,2).Value=x.CnyToRub; r++; }
        InitSheet(ws);
    }

    private static void WritePurchases(IXLWorksheet ws, IReadOnlyCollection<Purchase> rows)
    {
        WriteHeader(ws,["ID закупки","Дата оплаты","SKU","Количество","Цена за единицу (CNY)","Комиссии (CNY)"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.PurchaseId; ws.Cell(r,2).Value=x.PaidDate.ToDateTime(TimeOnly.MinValue); ws.Cell(r,2).Style.DateFormat.Format="dd.MM.yyyy"; ws.Cell(r,3).Value=x.Sku; ws.Cell(r,4).Value=x.Qty; ws.Cell(r,5).Value=x.UnitPriceCny; ws.Cell(r,6).Value=x.FeesCny; r++; }
        InitSheet(ws);
    }

    private static void WriteLogistics(IXLWorksheet ws, IReadOnlyCollection<Logistics> rows)
    {
        WriteHeader(ws,["ID логистики","ID закупки","Доставка (руб)","Таможня (руб)","Прочее (руб)"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.LogId; ws.Cell(r,2).Value=x.PurchaseId; ws.Cell(r,3).Value=x.DeliveryRub; ws.Cell(r,4).Value=x.CustomsRub; ws.Cell(r,5).Value=x.OtherRub; r++; }
        InitSheet(ws);
    }

    private static void WriteSales(IXLWorksheet ws, IReadOnlyCollection<Sale> rows)
    {
        WriteHeader(ws,["ID заказа","Дата продажи","Маркетплейс","Тип продажи","SKU","Количество","Цена (руб)","Комиссия (руб)","Логистика МП (руб)","Статус"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.OrderId; ws.Cell(r,2).Value=x.SaleDate.ToDateTime(TimeOnly.MinValue); ws.Cell(r,2).Style.DateFormat.Format="dd.MM.yyyy"; ws.Cell(r,3).Value=x.Marketplace.ToString(); ws.Cell(r,4).Value=x.SaleType.ToString(); ws.Cell(r,5).Value=x.Sku; ws.Cell(r,6).Value=x.Qty; ws.Cell(r,7).Value=x.PriceRub; ws.Cell(r,8).Value=x.CommissionRub; ws.Cell(r,9).Value=x.MpLogisticsRub; ws.Cell(r,10).Value=x.Status.ToString(); r++; }
        InitSheet(ws);
    }

    private static void WriteMarketing(IXLWorksheet ws, IReadOnlyCollection<MarketingCost> rows)
    {
        WriteHeader(ws,["ID маркетинга","Дата","Маркетплейс","SKU","Сумма (руб)","Тип"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.MarketingId; ws.Cell(r,2).Value=x.Date.ToDateTime(TimeOnly.MinValue); ws.Cell(r,2).Style.DateFormat.Format="dd.MM.yyyy"; ws.Cell(r,3).Value=x.Marketplace.ToString(); ws.Cell(r,4).Value=x.Sku; ws.Cell(r,5).Value=x.AmountRub; ws.Cell(r,6).Value=x.Type; r++; }
        InitSheet(ws);
    }

    private static void WriteOtherCosts(IXLWorksheet ws, IReadOnlyCollection<OtherCost> rows)
    {
        WriteHeader(ws,["ID расхода","Дата","Тип расхода","Сумма (руб)","Комментарий"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.CostId; ws.Cell(r,2).Value=x.Date.ToDateTime(TimeOnly.MinValue); ws.Cell(r,2).Style.DateFormat.Format="dd.MM.yyyy"; ws.Cell(r,3).Value=x.CostType.ToString(); ws.Cell(r,4).Value=x.AmountRub; ws.Cell(r,5).Value=x.Comment; r++; }
        InitSheet(ws);
    }

    private static void WriteInventory(IXLWorksheet ws, IReadOnlyCollection<InventoryRow> rows)
    {
        WriteHeader(ws,["SKU","Поступило (шт)","Выбыло (шт)","Возвраты (шт)","Остаток (шт)","Ср. себестоимость (руб/шт)","Стоимость остатков (руб)"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.Sku; ws.Cell(r,2).Value=x.TotalInQty; ws.Cell(r,3).Value=x.TotalOutQty; ws.Cell(r,4).Value=x.TotalReturnQty; ws.Cell(r,5).Value=x.StockQty; ws.Cell(r,6).Value=x.AvgCostRubPerUnit; ws.Cell(r,7).Value=x.StockValueRub; r++; }
        InitSheet(ws);
    }

    private static void WriteUnitEconomics(IXLWorksheet ws, IReadOnlyCollection<UnitEconomicsRow> rows)
    {
        WriteHeader(ws,["SKU","Ср. цена продажи (руб)","Ср. комиссия (руб/шт)","Ср. логистика МП (руб/шт)","Реклама (руб/шт)","Ср. себестоимость (руб/шт)","Упаковка (руб/шт)","Полная себестоимость (руб/шт)","Валовая прибыль (руб/шт)","Чистая прибыль (руб/шт)","ROI (%)","Доля самовыкупа (%)"]); int r=2;
        foreach(var x in rows){ ws.Cell(r,1).Value=x.Sku; ws.Cell(r,2).Value=x.AvgSalePrice; ws.Cell(r,3).Value=x.AvgCommissionPerUnit; ws.Cell(r,4).Value=x.AvgMpLogisticsPerUnit; ws.Cell(r,5).Value=x.AdsPerUnit; ws.Cell(r,6).Value=x.AvgCostUnit; ws.Cell(r,7).Value=x.PackagingPerUnit; ws.Cell(r,8).Value=x.FullCostPerUnit; ws.Cell(r,9).Value=x.GrossProfitPerUnit; ws.Cell(r,10).Value=x.NetProfitPerUnit; ws.Cell(r,11).Value=x.RoiPct; ws.Cell(r,12).Value=x.SelfBuySharePct; r++; }
        InitSheet(ws);
    }

    private static void WritePnlPlaceholder(IXLWorksheet ws)
    {
        WriteHeader(ws,["Метрика","Значение"]); ws.Cell(2,1).Value="P&L + KPI"; ws.Cell(2,2).Value="Рассчитывается в приложении"; InitSheet(ws);
    }

    private static void WriteHeader(IXLWorksheet ws, IReadOnlyList<string> headers)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        }
    }

    private static void ImportProducts(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            var sku = row.Cell(1).GetString();
            if (string.IsNullOrWhiteSpace(sku)) continue;
            if (!TryDecimal(row.Cell(7), out var packaging)) errors.Add(new("Товары", r, "PackagingRubPerUnit", "не число"));
            if (!TryDecimal(row.Cell(8), out var minMargin)) errors.Add(new("Товары", r, "MinMarginPct", "не число"));
            db.Products.Add(new Product
            {
                Sku = sku,
                Name = row.Cell(2).GetString(),
                Category = row.Cell(3).GetString(),
                PurchaseTargetCny = ParseNullableDecimal(row.Cell(4)),
                WeightKg = ParseNullableDecimal(row.Cell(5)),
                DimensionsText = row.Cell(6).GetString(),
                PackagingRubPerUnit = packaging,
                MinMarginPct = minMargin,
                TargetPriceRub = ParseNullableDecimal(row.Cell(9)),
                Status = ParseProductStatus(row.Cell(10).GetString())
            });
        }
    }

    private static void ImportFxRates(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            if (row.Cell(1).IsEmpty()) continue;
            if (!DateOnly.TryParse(row.Cell(1).GetFormattedString(), out var date)) errors.Add(new("Курсы валют", r, "FxDate", "некорректная дата"));
            if (!TryDecimal(row.Cell(2), out var rate)) errors.Add(new("Курсы валют", r, "CnyToRub", "не число"));
            db.FxRates.Add(new FxRate { FxDate = date, CnyToRub = rate });
        }
    }

    private static void ImportPurchases(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            var id = row.Cell(1).GetString(); if (string.IsNullOrWhiteSpace(id)) continue;
            if (!DateOnly.TryParse(row.Cell(2).GetFormattedString(), out var date)) errors.Add(new("Закупки", r, "PaidDate", "некорректная дата"));
            if (!int.TryParse(row.Cell(4).GetString(), out var qty)) errors.Add(new("Закупки", r, "Qty", "не число"));
            if (!TryDecimal(row.Cell(5), out var price)) errors.Add(new("Закупки", r, "UnitPriceCny", "не число"));
            if (!TryDecimal(row.Cell(6), out var fees)) errors.Add(new("Закупки", r, "FeesCny", "не число"));
            db.Purchases.Add(new Purchase { PurchaseId = id, PaidDate = date, Sku = row.Cell(3).GetString(), Qty = qty, UnitPriceCny = price, FeesCny = fees });
        }
    }

    private static void ImportLogistics(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            var id = row.Cell(1).GetString(); if (string.IsNullOrWhiteSpace(id)) continue;
            if (!TryDecimal(row.Cell(3), out var delivery)) errors.Add(new("Логистика", r, "DeliveryRub", "не число"));
            if (!TryDecimal(row.Cell(4), out var customs)) errors.Add(new("Логистика", r, "CustomsRub", "не число"));
            if (!TryDecimal(row.Cell(5), out var other)) errors.Add(new("Логистика", r, "OtherRub", "не число"));
            db.Logistics.Add(new Logistics { LogId = id, PurchaseId = row.Cell(2).GetString(), DeliveryRub = delivery, CustomsRub = customs, OtherRub = other });
        }
    }

    private static void ImportSales(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            var id = row.Cell(1).GetString(); if (string.IsNullOrWhiteSpace(id)) continue;
            if (!DateOnly.TryParse(row.Cell(2).GetFormattedString(), out var date)) errors.Add(new("Продажи", r, "SaleDate", "некорректная дата"));
            if (!int.TryParse(row.Cell(6).GetString(), out var qty)) errors.Add(new("Продажи", r, "Qty", "не число"));
            if (!TryDecimal(row.Cell(7), out var price)) errors.Add(new("Продажи", r, "PriceRub", "не число"));
            if (!TryDecimal(row.Cell(8), out var comm)) errors.Add(new("Продажи", r, "CommissionRub", "не число"));
            if (!TryDecimal(row.Cell(9), out var mplog)) errors.Add(new("Продажи", r, "MpLogisticsRub", "не число"));
            db.Sales.Add(new Sale { OrderId = id, SaleDate = date, Marketplace = ParseMarketplace(row.Cell(3).GetString()), SaleType = ParseSaleType(row.Cell(4).GetString()), Sku = row.Cell(5).GetString(), Qty = qty, PriceRub = price, CommissionRub = comm, MpLogisticsRub = mplog, Status = ParseSaleStatus(row.Cell(10).GetString()) });
        }
    }

    private static void ImportMarketing(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            if (row.Cell(1).IsEmpty()) continue;
            if (!DateOnly.TryParse(row.Cell(2).GetFormattedString(), out var date)) errors.Add(new("Маркетинг", r, "Date", "некорректная дата"));
            if (!TryDecimal(row.Cell(5), out var amount)) errors.Add(new("Маркетинг", r, "AmountRub", "не число"));
            db.Marketing.Add(new MarketingCost { Date = date, Marketplace = ParseMarketplace(row.Cell(3).GetString()), Sku = row.Cell(4).GetString(), AmountRub = amount, Type = row.Cell(6).GetString() });
        }
    }

    private static void ImportOtherCosts(IXLWorksheet ws, AppDbContext db, List<ImportError> errors)
    {
        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var r = row.RowNumber();
            if (row.Cell(1).IsEmpty()) continue;
            if (!DateOnly.TryParse(row.Cell(2).GetFormattedString(), out var date)) errors.Add(new("Прочие расходы", r, "Date", "некорректная дата"));
            if (!TryDecimal(row.Cell(4), out var amount)) errors.Add(new("Прочие расходы", r, "AmountRub", "не число"));
            db.OtherCosts.Add(new OtherCost { Date = date, CostType = ParseCostType(row.Cell(3).GetString()), AmountRub = amount, Comment = row.Cell(5).GetString() });
        }
    }

    private static bool TryDecimal(IXLCell c, out decimal value) => decimal.TryParse(c.GetString(), out value) || decimal.TryParse(c.GetFormattedString(), out value);
    private static decimal? ParseNullableDecimal(IXLCell c) => TryDecimal(c, out var d) ? d : null;

    private static ProductStatus ParseProductStatus(string v) => v.Contains("Стоп", StringComparison.OrdinalIgnoreCase) ? ProductStatus.Stop : ProductStatus.Active;
    private static Marketplace ParseMarketplace(string v) => v switch { "WB" => Marketplace.WB, "Ozon" => Marketplace.Ozon, "YM" or "ЯМ" => Marketplace.YM, _ => Marketplace.Other };
    private static SaleType ParseSaleType(string v) => v.Contains("Сам", StringComparison.OrdinalIgnoreCase) ? SaleType.SelfBuy : SaleType.Real;
    private static SaleStatus ParseSaleStatus(string v) => v switch { var s when s.Contains("Возв", StringComparison.OrdinalIgnoreCase) => SaleStatus.Returned, var s when s.Contains("Отмен", StringComparison.OrdinalIgnoreCase) => SaleStatus.Cancelled, _ => SaleStatus.Sold };
    private static CostType ParseCostType(string v) => v switch { var s when s.Contains("Упак", StringComparison.OrdinalIgnoreCase) => CostType.Packaging, var s when s.Contains("Склад", StringComparison.OrdinalIgnoreCase) => CostType.Warehouse, var s when s.Contains("Фото", StringComparison.OrdinalIgnoreCase) => CostType.PhotoContent, var s when s.Contains("Рекл", StringComparison.OrdinalIgnoreCase) => CostType.Ads, _ => CostType.Other };
}
