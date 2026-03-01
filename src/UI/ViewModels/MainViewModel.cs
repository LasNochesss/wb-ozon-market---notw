using System.Collections.ObjectModel;
using Domain.Models;
using Services;
using UI.Infrastructure;

namespace UI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ProjectService _projectService = new();
    private ReportingService? _reporting;
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<FxRate> FxRates { get; } = [];
    public ObservableCollection<Purchase> Purchases { get; } = [];
    public ObservableCollection<Logistics> Logistics { get; } = [];
    public ObservableCollection<Sale> Sales { get; } = [];
    public ObservableCollection<MarketingCost> Marketing { get; } = [];
    public ObservableCollection<OtherCost> OtherCosts { get; } = [];
    public ObservableCollection<InventoryRow> Inventory { get; } = [];
    public ObservableCollection<UnitEconomicsRow> UnitEconomics { get; } = [];

    private DateTime _from = DateTime.Today.AddMonths(-1);
    public DateTime From { get => _from; set => Set(ref _from, value); }
    private DateTime _to = DateTime.Today;
    public DateTime To { get => _to; set => Set(ref _to, value); }

    public RelayCommand CreateProjectCommand { get; }
    public RelayCommand RecalculateCommand { get; }

    public MainViewModel()
    {
        CreateProjectCommand = new RelayCommand(CreateProject);
        RecalculateCommand = new RelayCommand(async () => await RecalculateAsync());
    }

    private void CreateProject()
    {
        var db = _projectService.CreateContext("project.sqlite");
        _reporting = new ReportingService(db);
    }

    private async Task RecalculateAsync()
    {
        if (_reporting is null) return;
        Inventory.Clear();
        foreach (var row in await _reporting.BuildInventoryAsync()) Inventory.Add(row);
        UnitEconomics.Clear();
        foreach (var row in await _reporting.BuildUnitEconomicsAsync(DateOnly.FromDateTime(From), DateOnly.FromDateTime(To))) UnitEconomics.Add(row);
    }
}
