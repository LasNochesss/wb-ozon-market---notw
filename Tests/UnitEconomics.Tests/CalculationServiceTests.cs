using Domain.Models;
using Domain.Services;

namespace UnitEconomics.Tests;

public class CalculationServiceTests
{
    private readonly CalculationService _service = new();

    [Fact] public void LookupFxRate_PicksLastRate() => Assert.Equal(12m, _service.LookupFxRate(new DateOnly(2024,2,2), [new FxRate{FxDate=new(2024,1,1),CnyToRub=10}, new FxRate{FxDate=new(2024,2,1),CnyToRub=12}]));
    [Fact] public void LookupFxRate_ThrowsWhenMissing() => Assert.Throws<InvalidOperationException>(() => _service.LookupFxRate(new DateOnly(2024,1,1), [new FxRate{FxDate=new(2024,1,2),CnyToRub=10}]));
    [Fact] public void AdsPerUnit_ZeroWhenNoSales() => Assert.Equal(0, _service.CalculateAdsPerUnit("SKU", new(2024,1,1), new(2024,1,31), [new MarketingCost{Sku="SKU",Date=new(2024,1,2),AmountRub=100}], []));
    [Fact] public void AdsPerUnit_ComputesAverage() => Assert.Equal(50, _service.CalculateAdsPerUnit("SKU", new(2024,1,1), new(2024,1,31), [new MarketingCost{Sku="SKU",Date=new(2024,1,2),AmountRub=100}], [new Sale{Sku="SKU",SaleDate=new(2024,1,3),Qty=2,SaleType=SaleType.Real,Status=SaleStatus.Sold}]));
    [Fact] public void RealSale_RevenuePositive() => Assert.Equal(200, _service.CalculateSale(new Sale{Qty=2,PriceRub=100,SaleType=SaleType.Real,Status=SaleStatus.Sold}, 40, 5, 10).RevenueRub);
    [Fact] public void Return_RevenueNegativeAndNegativeCogs() { var r=_service.CalculateSale(new Sale{Qty=1,PriceRub=100,SaleType=SaleType.Real,Status=SaleStatus.Returned}, 60, 5, 1); Assert.Equal(-100,r.RevenueRub); Assert.Equal(-60,r.CogsRub);} 
    [Fact] public void SelfBuy_NoRevenueButMarketing() { var r=_service.CalculateSale(new Sale{Qty=1,PriceRub=100,SaleType=SaleType.SelfBuy,Status=SaleStatus.Sold,CommissionRub=10,MpLogisticsRub=4}, 60, 5, 1); Assert.Equal(0,r.RevenueRub); Assert.Equal(14,r.SelfBuyMarketingRub);} 
    [Fact] public void Cancelled_GivesZeroRevenueAndCogs() { var r=_service.CalculateSale(new Sale{Qty=1,PriceRub=100,SaleType=SaleType.Real,Status=SaleStatus.Cancelled}, 60, 5, 1); Assert.Equal(0,r.RevenueRub); Assert.Equal(0,r.CogsRub);} 
    [Fact] public void Pnl_MarginZeroWhenNoRevenue() { var r=_service.CalculatePnl([], [], 0, 0); Assert.Equal(0,r.MarginPct);} 
    [Fact] public void Pnl_ReturnAndSelfShareCalculated() { var sales = new[]{ new Sale{Qty=2,SaleType=SaleType.Real,Status=SaleStatus.Sold}, new Sale{Qty=1,SaleType=SaleType.Real,Status=SaleStatus.Returned}, new Sale{Qty=1,SaleType=SaleType.SelfBuy,Status=SaleStatus.Sold}}; var r=_service.CalculatePnl([], sales,0,0); Assert.Equal(33.333333333333333333333333333m, r.ReturnPct); Assert.Equal(33.333333333333333333333333333m,r.SelfBuySharePct);} 
}
