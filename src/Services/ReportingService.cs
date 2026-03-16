using Data;
using Domain.Models;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class ReportingService
{
    private readonly AppDbContext _db;
    private readonly CalculationService _calc = new();

    public ReportingService(AppDbContext db) => _db = db;

    public async Task<List<InventoryRow>> BuildInventoryAsync()
    {
        var rates = await _db.FxRates.ToListAsync();
        var purchases = await _db.Purchases.ToListAsync();
        var logs = await _db.Logistics.ToListAsync();
        var sales = await _db.Sales.ToListAsync();

        return purchases.GroupBy(p => p.Sku).Select(g =>
        {
            var totalIn = g.Sum(x => x.Qty);
            var totalOut = sales.Where(s => s.Sku == g.Key && s.Status == SaleStatus.Sold).Sum(s => s.Qty);
            var totalReturn = sales.Where(s => s.Sku == g.Key && s.Status == SaleStatus.Returned).Sum(s => s.Qty);
            var stock = totalIn - totalOut + totalReturn;
            var totalPurchaseRub = g.Sum(x => (x.Qty * x.UnitPriceCny + x.FeesCny) * _calc.LookupFxRate(x.PaidDate, rates));
            var totalLogRub = (from p in g join l in logs on p.PurchaseId equals l.PurchaseId select l.DeliveryRub + l.CustomsRub + l.OtherRub).Sum();
            var avg = totalIn > 0 ? (totalPurchaseRub + totalLogRub) / totalIn : 0;
            return new InventoryRow(g.Key, totalIn, totalOut, totalReturn, stock, avg, stock * avg);
        }).ToList();
    }

    public async Task<List<UnitEconomicsRow>> BuildUnitEconomicsAsync(DateOnly from, DateOnly to)
    {
        var inventory = await BuildInventoryAsync();
        var products = await _db.Products.ToListAsync();
        var sales = await _db.Sales.Where(s => s.SaleDate >= from && s.SaleDate <= to).ToListAsync();
        var marketing = await _db.Marketing.Where(m => m.Date >= from && m.Date <= to).ToListAsync();

        return products.Select(p =>
        {
            var realSales = sales.Where(s => s.Sku == p.Sku && s.SaleType == SaleType.Real && s.Status == SaleStatus.Sold).ToList();
            var units = realSales.Sum(x => x.Qty);
            var avgSale = units > 0 ? realSales.Sum(x => x.PriceRub * x.Qty) / units : 0;
            var avgComm = units > 0 ? realSales.Sum(x => x.CommissionRub) / units : 0;
            var avgMp = units > 0 ? realSales.Sum(x => x.MpLogisticsRub) / units : 0;
            var ads = _calc.CalculateAdsPerUnit(p.Sku, from, to, marketing, sales);
            var avgCost = inventory.FirstOrDefault(i => i.Sku == p.Sku)?.AvgCostRubPerUnit ?? 0;
            var full = avgCost + p.PackagingRubPerUnit + avgMp;
            var gross = avgSale - full;
            var net = gross - avgComm - ads;
            var denom = full + avgComm + ads;
            var roi = denom > 0 ? net / denom * 100 : 0;
            var selfUnits = sales.Where(s => s.Sku == p.Sku && s.SaleType == SaleType.SelfBuy && s.Status == SaleStatus.Sold).Sum(x => x.Qty);
            var selfShare = (selfUnits + units) > 0 ? (decimal)selfUnits / (selfUnits + units) * 100 : 0;
            return new UnitEconomicsRow(p.Sku, avgSale, avgComm, avgMp, ads, avgCost, p.PackagingRubPerUnit, full, gross, net, roi, selfShare);
        }).ToList();
    }
}
