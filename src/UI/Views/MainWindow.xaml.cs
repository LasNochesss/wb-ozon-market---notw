using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        if (e.Key == Key.Delete && grid.SelectedItem is not null && !grid.IsReadOnly)
        {
            var confirm = MessageBox.Show("Удалить выбранную строку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm == MessageBoxResult.Yes && grid.ItemsSource is IList list)
            {
                list.Remove(grid.SelectedItem);
                if (DataContext is MainViewModel vm) vm.ToastMessage = "Строка удалена";
            }
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
            if (DataContext is MainViewModel vm) vm.ToastMessage = "Вставка выполнена";
            e.Handled = true;
        }
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
