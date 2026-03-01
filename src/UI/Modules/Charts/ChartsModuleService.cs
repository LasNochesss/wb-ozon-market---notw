using Domain.Models;

namespace UI.Modules.Charts;

public class ChartsModuleService
{
    public IEnumerable<(string Label, decimal Value)> RevenueBySku(IEnumerable<Sale> sales)
        => sales.GroupBy(s => s.Sku).Select(g => (g.Key, g.Sum(x => x.PriceRub * x.Qty)));

    public IEnumerable<(string Label, decimal Value)> RevenueByMarketplace(IEnumerable<Sale> sales)
        => sales.GroupBy(s => s.Marketplace.ToString()).Select(g => (g.Key, g.Sum(x => x.PriceRub * x.Qty)));
}
