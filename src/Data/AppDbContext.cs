using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<FxRate> FxRates => Set<FxRate>();
    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<Logistics> Logistics => Set<Logistics>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<MarketingCost> Marketing => Set<MarketingCost>();
    public DbSet<OtherCost> OtherCosts => Set<OtherCost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().HasKey(x => x.Sku);
        modelBuilder.Entity<FxRate>().HasKey(x => x.FxDate);
        modelBuilder.Entity<Purchase>().HasKey(x => x.PurchaseId);
        modelBuilder.Entity<Logistics>().HasKey(x => x.LogId);
        modelBuilder.Entity<Sale>().HasKey(x => x.OrderId);
        modelBuilder.Entity<MarketingCost>().HasKey(x => x.MarketingId);
        modelBuilder.Entity<OtherCost>().HasKey(x => x.CostId);

        modelBuilder.Entity<Product>().Property(x => x.PackagingRubPerUnit).HasColumnType("NUMERIC");
        modelBuilder.Entity<Product>().Property(x => x.MinMarginPct).HasColumnType("NUMERIC");
        modelBuilder.Entity<FxRate>().Property(x => x.CnyToRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<Purchase>().Property(x => x.UnitPriceCny).HasColumnType("NUMERIC");
        modelBuilder.Entity<Purchase>().Property(x => x.FeesCny).HasColumnType("NUMERIC");
        modelBuilder.Entity<Logistics>().Property(x => x.DeliveryRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<Sale>().Property(x => x.PriceRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<Sale>().Property(x => x.CommissionRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<Sale>().Property(x => x.MpLogisticsRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<MarketingCost>().Property(x => x.AmountRub).HasColumnType("NUMERIC");
        modelBuilder.Entity<OtherCost>().Property(x => x.AmountRub).HasColumnType("NUMERIC");

        modelBuilder.Entity<Logistics>().HasIndex(x => x.PurchaseId).IsUnique();
    }
}
