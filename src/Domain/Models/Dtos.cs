namespace Domain.Models;

public record InventoryRow(string Sku, int TotalInQty, int TotalOutQty, int TotalReturnQty, int StockQty, decimal AvgCostRubPerUnit, decimal StockValueRub);
public record UnitEconomicsRow(string Sku, decimal AvgSalePrice, decimal AvgCommissionPerUnit, decimal AvgMpLogisticsPerUnit, decimal AdsPerUnit, decimal AvgCostUnit, decimal PackagingPerUnit, decimal FullCostPerUnit, decimal GrossProfitPerUnit, decimal NetProfitPerUnit, decimal RoiPct, decimal SelfBuySharePct);

public record SaleCalculationResult(decimal RevenueRub, decimal CogsRub, decimal GrossProfitRub, decimal PackagingRub, decimal SelfBuyMarketingRub, decimal AllocatedAdsRub, decimal NetProfitRub, decimal? MarginPct);

public record PnlResult(decimal Revenue, decimal Cogs, decimal GrossProfit, decimal Commissions, decimal MpLogistics, decimal Packaging, decimal Ads, decimal OtherCosts, decimal SelfBuyMarketing, decimal NetProfit, decimal MarginPct, decimal Roi, decimal Aov, decimal ReturnPct, decimal SelfBuySharePct);
