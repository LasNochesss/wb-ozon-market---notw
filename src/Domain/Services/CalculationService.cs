using Domain.Models;

namespace Domain.Services;

public class CalculationService
{
    public decimal LookupFxRate(DateOnly paidDate, IReadOnlyCollection<FxRate> rates)
    {
        var rate = rates.Where(r => r.FxDate <= paidDate).OrderByDescending(r => r.FxDate).FirstOrDefault();
        return rate?.CnyToRub ?? throw new InvalidOperationException("Нет курса на дату оплаты");
    }

    public decimal CalculateAdsPerUnit(string sku, DateOnly from, DateOnly to, IReadOnlyCollection<MarketingCost> marketing, IReadOnlyCollection<Sale> sales)
    {
        var ads = marketing.Where(m => m.Sku == sku && m.Date >= from && m.Date <= to).Sum(x => x.AmountRub);
        var realUnits = sales.Where(s => s.Sku == sku && s.SaleDate >= from && s.SaleDate <= to && s.SaleType == SaleType.Real && s.Status == SaleStatus.Sold).Sum(x => x.Qty);
        return realUnits > 0 ? ads / realUnits : 0;
    }

    public SaleCalculationResult CalculateSale(Sale sale, decimal avgCostRubPerUnit, decimal packagingPerUnit, decimal adsPerUnit)
    {
        var revenue = sale.Status == SaleStatus.Sold && sale.SaleType == SaleType.Real ? sale.Qty * sale.PriceRub :
            sale.Status == SaleStatus.Returned && sale.SaleType == SaleType.Real ? -sale.Qty * sale.PriceRub : 0;
        var cogs = sale.Status == SaleStatus.Sold ? sale.Qty * avgCostRubPerUnit : sale.Status == SaleStatus.Returned ? -sale.Qty * avgCostRubPerUnit : 0;
        var packaging = sale.Status == SaleStatus.Sold ? sale.Qty * packagingPerUnit : 0;
        var selfBuyMarketing = sale.SaleType == SaleType.SelfBuy && sale.Status == SaleStatus.Sold ? sale.CommissionRub + sale.MpLogisticsRub : 0;
        var allocatedAds = sale.SaleType == SaleType.Real && sale.Status == SaleStatus.Sold ? sale.Qty * adsPerUnit : 0;
        var gross = revenue - cogs;
        var net = gross - sale.CommissionRub - sale.MpLogisticsRub - packaging - allocatedAds;
        decimal? margin = revenue != 0 ? (net / revenue) * 100 : null;
        return new(revenue, cogs, gross, packaging, selfBuyMarketing, allocatedAds, net, margin);
    }

    public PnlResult CalculatePnl(IReadOnlyCollection<SaleCalculationResult> saleRows, IReadOnlyCollection<Sale> salesRaw, decimal ads, decimal otherCosts)
    {
        var revenue = saleRows.Sum(x => x.RevenueRub);
        var cogs = saleRows.Sum(x => x.CogsRub);
        var commissions = salesRaw.Where(s => s.Status != SaleStatus.Cancelled).Sum(x => x.CommissionRub);
        var mpLogistics = salesRaw.Where(s => s.Status != SaleStatus.Cancelled).Sum(x => x.MpLogisticsRub);
        var packaging = saleRows.Sum(x => x.PackagingRub);
        var selfBuyMarketing = saleRows.Sum(x => x.SelfBuyMarketingRub);
        var gross = revenue - cogs;
        var net = gross - commissions - mpLogistics - packaging - ads - otherCosts - selfBuyMarketing;
        var margin = revenue > 0 ? (net / revenue) * 100 : 0;
        var denom = cogs + commissions + mpLogistics + packaging + ads + otherCosts + selfBuyMarketing;
        var roi = denom > 0 ? (net / denom) * 100 : 0;
        var orders = salesRaw.Count(s => s.SaleType == SaleType.Real && s.Status == SaleStatus.Sold);
        var aov = orders > 0 ? revenue / orders : 0;
        var returnedUnits = salesRaw.Where(s => s.Status == SaleStatus.Returned && s.SaleType == SaleType.Real).Sum(s => s.Qty);
        var soldUnits = salesRaw.Where(s => s.Status == SaleStatus.Sold && s.SaleType == SaleType.Real).Sum(s => s.Qty);
        var selfUnits = salesRaw.Where(s => s.Status == SaleStatus.Sold && s.SaleType == SaleType.SelfBuy).Sum(s => s.Qty);
        var returnPct = returnedUnits + soldUnits > 0 ? (decimal)returnedUnits / (returnedUnits + soldUnits) * 100 : 0;
        var selfPct = soldUnits + selfUnits > 0 ? (decimal)selfUnits / (soldUnits + selfUnits) * 100 : 0;
        return new(revenue, cogs, gross, commissions, mpLogistics, packaging, ads, otherCosts, selfBuyMarketing, net, margin, roi, aov, returnPct, selfPct);
    }
}
