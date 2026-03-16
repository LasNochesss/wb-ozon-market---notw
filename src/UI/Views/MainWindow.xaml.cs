using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Domain.Models;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        AddHandler(DataGrid.PreviewKeyDownEvent, new KeyEventHandler(OnDataGridPreviewKeyDown));
        AddHandler(DataGrid.MouseDoubleClickEvent, new MouseButtonEventHandler(OnDataGridMouseDoubleClick));
    }

    private static void OnDataGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d)
        {
            var cell = FindParent<DataGridCell>(d);
            if (cell is not null && !cell.IsEditing)
            {
                cell.Focus();
                var grid = FindParent<DataGrid>(cell);
                grid?.BeginEdit();
            }
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var grid = GetActiveDataGrid();
        if (grid is null || grid.ItemsSource is not IList list)
        {
            vm.ToastMessage = "На этой вкладке добавление строк недоступно";
            return;
        }

        var row = CreateRowForGrid(grid);
        if (row is null)
        {
            vm.ToastMessage = "На этой вкладке добавление строк недоступно";
            return;
        }

        list.Add(row);
        vm.HasUnsavedChanges = true;
        vm.ToastMessage = "Строка добавлена";

        FocusNewRow(grid, row);
    }

    private void DeleteRow_Click(object sender, RoutedEventArgs e)
    {
        DeleteSelectedRow();
    }

    private void OnDataGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject d) return;
        var grid = FindParent<DataGrid>(d);
        if (grid is null) return;

        if (e.Key == Key.Enter && !grid.IsReadOnly)
        {
            grid.BeginEdit();
            return;
        }

        if (e.Key == Key.Delete)
        {
            DeleteSelectedRow(grid);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            ApplicationCommands.Copy.Execute(null, grid);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V && !grid.IsReadOnly)
        {
            ApplicationCommands.Paste.Execute(null, grid);
            if (DataContext is MainViewModel vm)
            {
                vm.HasUnsavedChanges = true;
                vm.ToastMessage = "Вставка выполнена";
            }
            e.Handled = true;
        }
    }

    private void DeleteSelectedRow(DataGrid? targetGrid = null)
    {
        if (DataContext is not MainViewModel vm) return;

        var grid = targetGrid ?? GetActiveDataGrid();
        if (grid is null || grid.ItemsSource is not IList list)
        {
            vm.ToastMessage = "На этой вкладке удаление строк недоступно";
            return;
        }

        var selected = grid.SelectedItem;
        if (selected is null)
        {
            vm.ToastMessage = "Выберите строку для удаления";
            return;
        }

        var selectedIndex = grid.SelectedIndex;
        var confirm = MessageBox.Show("Удалить выбранную строку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        list.Remove(selected);
        vm.HasUnsavedChanges = true;
        vm.ToastMessage = "Строка удалена";

        if (list.Count == 0) return;

        var nextIndex = Math.Min(selectedIndex, list.Count - 1);
        grid.SelectedIndex = nextIndex;
        grid.CurrentCell = new DataGridCellInfo(grid.Items[nextIndex], grid.Columns.FirstOrDefault());
        grid.ScrollIntoView(grid.Items[nextIndex]);
        grid.Focus();
    }

    private DataGrid? GetActiveDataGrid()
    {
        return MainTabControl.SelectedItem switch
        {
            TabItem { Header: "Товары" } => ProductsGrid,
            TabItem { Header: "Курсы валют" } => FxRatesGrid,
            TabItem { Header: "Закупки" } => PurchasesGrid,
            TabItem { Header: "Логистика" } => LogisticsGrid,
            TabItem { Header: "Продажи" } => SalesGrid,
            TabItem { Header: "Маркетинг" } => MarketingGrid,
            TabItem { Header: "Прочие расходы" } => OtherCostsGrid,
            TabItem { Header: "Склад" } => InventoryGrid,
            TabItem { Header: "Юнит-экономика" } => UnitEconomicsGrid,
            TabItem { Header: "Файлы" } => ValidationLogGrid,
            _ => null
        };
    }

    private static object? CreateRowForGrid(DataGrid grid)
    {
        return grid.Name switch
        {
            "ProductsGrid" => new Product(),
            "FxRatesGrid" => new FxRate { FxDate = DateOnly.FromDateTime(DateTime.Today), CnyToRub = 0m },
            "PurchasesGrid" => new Purchase { PaidDate = DateOnly.FromDateTime(DateTime.Today) },
            "LogisticsGrid" => new Logistics(),
            "SalesGrid" => new Sale { SaleDate = DateOnly.FromDateTime(DateTime.Today) },
            "MarketingGrid" => new MarketingCost { Date = DateOnly.FromDateTime(DateTime.Today) },
            "OtherCostsGrid" => new OtherCost { Date = DateOnly.FromDateTime(DateTime.Today) },
            "InventoryGrid" => new InventoryRow(string.Empty, 0, 0, 0, 0, 0m, 0m),
            "UnitEconomicsGrid" => new UnitEconomicsRow(string.Empty, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m),
            _ => null
        };
    }

    private static void FocusNewRow(DataGrid grid, object row)
    {
        grid.UpdateLayout();
        grid.SelectedItem = row;
        grid.ScrollIntoView(row);
        if (grid.Columns.Count == 0) return;
        grid.CurrentCell = new DataGridCellInfo(row, grid.Columns[0]);
        grid.Focus();
        grid.BeginEdit();
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T typed) return typed;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
