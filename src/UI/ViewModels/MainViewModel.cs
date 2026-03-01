using System.Collections.ObjectModel;
using Domain.Models;
using Microsoft.Win32;
using Services;
using UI.Infrastructure;
using UI.Modules.Charts;
using UI.Modules.Localization;
using UI.Modules.Settings;
using UI.Views;

namespace UI.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ProjectService _projectService = new();
    private readonly ExcelProjectStorageService _excelStorage = new();
    private readonly SettingsService _settingsService = new();
    private readonly LocalizationService _localization = new();
    private readonly ChartsModuleService _charts = new();

    private Data.AppDbContext? _db;
    private ReportingService? _reporting;
    private AppSettings _settings;

    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<FxRate> FxRates { get; } = [];
    public ObservableCollection<Purchase> Purchases { get; } = [];
    public ObservableCollection<Logistics> Logistics { get; } = [];
    public ObservableCollection<Sale> Sales { get; } = [];
    public ObservableCollection<MarketingCost> Marketing { get; } = [];
    public ObservableCollection<OtherCost> OtherCosts { get; } = [];
    public ObservableCollection<InventoryRow> Inventory { get; } = [];
    public ObservableCollection<UnitEconomicsRow> UnitEconomics { get; } = [];
    public ObservableCollection<string> ValidationLog { get; } = [];

    private DateTime _from = DateTime.Today.AddMonths(-1);
    public DateTime From { get => _from; set => Set(ref _from, value); }
    private DateTime _to = DateTime.Today;
    public DateTime To { get => _to; set => Set(ref _to, value); }

    private string _currentExcelPath = "(не выбран)";
    public string CurrentExcelPath { get => _currentExcelPath; set => Set(ref _currentExcelPath, value); }

    private string _saveState = "сохранено";
    public string SaveState { get => _saveState; set => Set(ref _saveState, value); }

    private string _toastMessage = string.Empty;
    public string ToastMessage { get => _toastMessage; set => Set(ref _toastMessage, value); }

    public RelayCommand CreateProjectCommand { get; }
    public RelayCommand RecalculateCommand { get; }
    public RelayCommand NewFileCommand { get; }
    public RelayCommand OpenExcelCommand { get; }
    public RelayCommand SaveExcelCommand { get; }
    public RelayCommand SaveAsExcelCommand { get; }
    public RelayCommand ExportExcelCommand { get; }
    public RelayCommand ImportExcelCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        _settings = _settingsService.Load();
        CurrentExcelPath = _settings.LastExcelPath ?? "(не выбран)";
        SaveState = _localization.T(_settings.Language, "Status.Saved");

        CreateProjectCommand = new RelayCommand(CreateProject);
        RecalculateCommand = new RelayCommand(async () => await RecalculateAsync());
        NewFileCommand = new RelayCommand(NewFile);
        OpenExcelCommand = new RelayCommand(async () => await OpenExcelAsync());
        SaveExcelCommand = new RelayCommand(async () => await SaveExcelAsync());
        SaveAsExcelCommand = new RelayCommand(async () => await SaveAsExcelAsync());
        ExportExcelCommand = new RelayCommand(async () => await SaveAsExcelAsync());
        ImportExcelCommand = new RelayCommand(async () => await OpenExcelAsync());
        OpenSettingsCommand = new RelayCommand(OpenSettings);
    }

    private void CreateProject()
    {
        _db = _projectService.CreateContext("project.sqlite");
        _reporting = new ReportingService(_db);
        ToastMessage = "Проект создан";
    }

    private void NewFile()
    {
        CreateProject();
        CurrentExcelPath = "(новый файл)";
        SaveState = _localization.T(_settings.Language, "Status.Dirty");
    }

    private async Task OpenExcelAsync()
    {
        if (_db is null) CreateProject();
        var dlg = new OpenFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog() != true || _db is null) return;
        var result = await _excelStorage.ImportAsync(_db, dlg.FileName);
        ValidationLog.Clear();
        if (!result.Success)
        {
            foreach (var e in result.Errors) ValidationLog.Add($"{e.Sheet}: строка {e.Row}, колонка {e.Column} — {e.Message}");
            ToastMessage = "Ошибка валидации импорта";
            return;
        }

        CurrentExcelPath = dlg.FileName;
        _settings.LastExcelPath = dlg.FileName;
        _settingsService.Save(_settings);
        SaveState = _localization.T(_settings.Language, "Status.Saved");
        await RecalculateAsync();
        ToastMessage = "Импорт завершён";
    }

    private async Task SaveExcelAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentExcelPath) || CurrentExcelPath == "(не выбран)" || CurrentExcelPath == "(новый файл)")
        {
            await SaveAsExcelAsync();
            return;
        }
        if (_db is null) return;
        await _excelStorage.ExportAsync(_db, CurrentExcelPath, Inventory, UnitEconomics);
        SaveState = _localization.T(_settings.Language, "Status.Saved");
        ToastMessage = "Файл сохранён";
    }

    private async Task SaveAsExcelAsync()
    {
        if (_db is null) CreateProject();
        var dlg = new SaveFileDialog { Filter = "Excel (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog() != true || _db is null) return;
        CurrentExcelPath = dlg.FileName;
        _settings.LastExcelPath = dlg.FileName;
        _settingsService.Save(_settings);
        await _excelStorage.ExportAsync(_db, dlg.FileName, Inventory, UnitEconomics);
        SaveState = _localization.T(_settings.Language, "Status.Saved");
        ToastMessage = "Файл сохранён";
    }

    private async Task RecalculateAsync()
    {
        if (_reporting is null) return;

        Inventory.Clear();
        foreach (var row in await _reporting.BuildInventoryAsync()) Inventory.Add(row);
        UnitEconomics.Clear();
        foreach (var row in await _reporting.BuildUnitEconomicsAsync(DateOnly.FromDateTime(From), DateOnly.FromDateTime(To))) UnitEconomics.Add(row);

        _ = _charts.RevenueByMarketplace(Sales);

        SaveState = _localization.T(_settings.Language, "Status.Dirty");
        if (_settings.AutoSaveEnabled && CurrentExcelPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            await SaveExcelAsync();

        ToastMessage = "Пересчёт завершён";
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settings);
        if (window.ShowDialog() != true || window.Result is null) return;
        _settings.Language = window.Result.Language;
        _settings.Theme = window.Result.Theme;
        _settings.AutoSaveEnabled = window.Result.AutoSaveEnabled;
        _settingsService.Save(_settings);

        SaveState = _localization.T(_settings.Language, "Status.Saved");
        ToastMessage = "Настройки сохранены";
    }
}
