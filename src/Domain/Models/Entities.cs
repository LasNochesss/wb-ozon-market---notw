namespace Domain.Models;

public class Product
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Прочее";
    public decimal? PurchaseTargetCny { get; set; }
    public decimal? WeightKg { get; set; }
    public string? DimensionsText { get; set; }
    public decimal PackagingRubPerUnit { get; set; }
    public decimal MinMarginPct { get; set; }
    public decimal? TargetPriceRub { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
}

public class FxRate { public DateOnly FxDate { get; set; } public decimal CnyToRub { get; set; } }

public class Purchase
{
    public string PurchaseId { get; set; } = string.Empty;
    public DateOnly PaidDate { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal UnitPriceCny { get; set; }
    public decimal FeesCny { get; set; }
}

public class Logistics
{
    public string LogId { get; set; } = string.Empty;
    public string PurchaseId { get; set; } = string.Empty;
    public decimal DeliveryRub { get; set; }
    public decimal CustomsRub { get; set; }
    public decimal OtherRub { get; set; }
}

public class Sale
{
    public string OrderId { get; set; } = string.Empty;
    public DateOnly SaleDate { get; set; }
    public Marketplace Marketplace { get; set; }
    public SaleType SaleType { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal PriceRub { get; set; }
    public decimal CommissionRub { get; set; }
    public decimal MpLogisticsRub { get; set; }
    public SaleStatus Status { get; set; }
}

public class MarketingCost
{
    public int MarketingId { get; set; }
    public DateOnly Date { get; set; }
    public Marketplace Marketplace { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal AmountRub { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class OtherCost
{
    public int CostId { get; set; }
    public DateOnly Date { get; set; }
    public CostType CostType { get; set; }
    public decimal AmountRub { get; set; }
    public string? Comment { get; set; }
}
