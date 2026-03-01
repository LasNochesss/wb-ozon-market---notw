using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
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
    public DateTime From
    {
        get => _from;
        set
        {
            Set(ref _from, value);
            ValidatePeriodAndMarkDirty();
        }
    }

    private DateTime _to = DateTime.Today;
    public DateTime To
    {
        get => _to;
        set
        {
            Set(ref _to, value);
            ValidatePeriodAndMarkDirty();
        }
    }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set { Set(ref _isBusy, value); RaiseCommandStates(); } }

    private bool _hasUnsavedChanges;
    public bool HasUnsavedChanges { get => _hasUnsavedChanges; set { Set(ref _hasUnsavedChanges, value); SaveState = value ? _localization.T(_settings.Language, "Status.Dirty") : _localization.T(_settings.Language, "Status.Saved"); } }

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

        CreateProjectCommand = new RelayCommand(CreateProject, () => !IsBusy);
        RecalculateCommand = new RelayCommand(RecalculateAsync, () => !IsBusy && _reporting is not null);
        NewFileCommand = new RelayCommand(NewFile, () => !IsBusy);
        OpenExcelCommand = new RelayCommand(OpenExcelAsync, () => !IsBusy);
        SaveExcelCommand = new RelayCommand(SaveExcelAsync, () => !IsBusy);
        SaveAsExcelCommand = new RelayCommand(SaveAsExcelAsync, () => !IsBusy);
        ExportExcelCommand = new RelayCommand(SaveAsExcelAsync, () => !IsBusy);
        ImportExcelCommand = new RelayCommand(OpenExcelAsync, () => !IsBusy);
        OpenSettingsCommand = new RelayCommand(OpenSettings, () => !IsBusy);
    }

    private void RaiseCommandStates()
    {
        CreateProjectCommand.RaiseCanExecuteChanged();
        RecalculateCommand.RaiseCanExecuteChanged();
        NewFileCommand.RaiseCanExecuteChanged();
        OpenExcelCommand.RaiseCanExecuteChanged();
        SaveExcelCommand.RaiseCanExecuteChanged();
        SaveAsExcelCommand.RaiseCanExecuteChanged();
        ExportExcelCommand.RaiseCanExecuteChanged();
        ImportExcelCommand.RaiseCanExecuteChanged();
        OpenSettingsCommand.RaiseCanExecuteChanged();
    }

    private void CreateProject()
    {
        if (HasUnsavedChanges)
        {
            var confirm = MessageBox.Show("Есть несохранённые изменения. Создать новый проект и очистить текущие данные?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        _db = _projectService.CreateContext("project.sqlite");
        _reporting = new ReportingService(_db);

        foreach (var col in new IEnumerable[] { Products, FxRates, Purchases, Logistics, Sales, Marketing, OtherCosts, Inventory, UnitEconomics, ValidationLog })
            if (col is IList list) list.Clear();

        HasUnsavedChanges = true;
        ToastMessage = "Проект создан";
        RaiseCommandStates();
    }

    private void NewFile()
    {
        CreateProject();
        CurrentExcelPath = "(новый файл)";
    }

    private async Task OpenExcelAsync()
    {
        if (_db is null) CreateProject();
        var dlg = new OpenFileDialog { Filter = "Файл Excel (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog() != true || _db is null) return;

        IsBusy = true;
        try
        {
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
            HasUnsavedChanges = false;
            await RecalculateAsync();
            ToastMessage = "Импорт завершён";
        }
        finally { IsBusy = false; }
    }

    private async Task SaveExcelAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentExcelPath) || CurrentExcelPath is "(не выбран)" or "(новый файл)")
        {
            await SaveAsExcelAsync();
            return;
        }
        if (_db is null) return;

        IsBusy = true;
        try
        {
            await _excelStorage.ExportAsync(_db, CurrentExcelPath, Inventory, UnitEconomics);
            HasUnsavedChanges = false;
            ToastMessage = "Файл сохранён";
        }
        finally { IsBusy = false; }
    }

    private async Task SaveAsExcelAsync()
    {
        if (_db is null) CreateProject();
        var dlg = new SaveFileDialog { Filter = "Файл Excel (*.xlsx)|*.xlsx" };
        if (dlg.ShowDialog() != true || _db is null) return;

        IsBusy = true;
        try
        {
            CurrentExcelPath = dlg.FileName;
            _settings.LastExcelPath = dlg.FileName;
            _settingsService.Save(_settings);
            await _excelStorage.ExportAsync(_db, dlg.FileName, Inventory, UnitEconomics);
            HasUnsavedChanges = false;
            ToastMessage = "Файл сохранён";
        }
        finally { IsBusy = false; }
    }

    private async Task RecalculateAsync()
    {
        if (_reporting is null) return;
        if (From > To)
        {
            ToastMessage = "Период задан неверно: дата " + "с" + " больше даты " + "по";
            return;
        }

        IsBusy = true;
        ToastMessage = "Идёт пересчёт...";
        try
        {
            Inventory.Clear();
            foreach (var row in await _reporting.BuildInventoryAsync()) Inventory.Add(row);
            UnitEconomics.Clear();
            foreach (var row in await _reporting.BuildUnitEconomicsAsync(DateOnly.FromDateTime(From), DateOnly.FromDateTime(To))) UnitEconomics.Add(row);

            _ = _charts.RevenueByMarketplace(Sales);
            HasUnsavedChanges = true;

            if (_settings.AutoSaveEnabled && CurrentExcelPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                await SaveExcelAsync();

            ToastMessage = "Пересчёт завершён";
        }
        finally { IsBusy = false; }
    }

    private void ValidatePeriodAndMarkDirty()
    {
        if (From > To) ToastMessage = "Период задан неверно: " + "с" + " больше " + "по";
        else if (_reporting is not null) HasUnsavedChanges = true;
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settings);
        if (window.ShowDialog() != true || window.Result is null) return;
        _settings.Language = window.Result.Language;
        _settings.Theme = window.Result.Theme;
        _settings.AutoSaveEnabled = window.Result.AutoSaveEnabled;
        _settingsService.Save(_settings);

        ToastMessage = "Настройки сохранены";
    }
}
