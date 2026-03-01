using System.Windows;
using System.Windows.Controls;
using UI.Modules.Settings;

namespace UI.Views;

public partial class SettingsWindow : Window
{
    public AppSettings? Result { get; private set; }

    public SettingsWindow(AppSettings current)
    {
        InitializeComponent();
        SelectByTag(LanguageBox, current.Language);
        SelectByTag(ThemeBox, current.Theme);
        AutoSaveBox.IsChecked = current.AutoSaveEnabled;
    }

    private static void SelectByTag(ComboBox box, string? tag)
    {
        foreach (var i in box.Items)
            if (i is ComboBoxItem c && (c.Tag?.ToString() ?? string.Empty) == tag)
                box.SelectedItem = c;
        if (box.SelectedItem is null && box.Items.Count > 0) box.SelectedIndex = 0;
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        Result = new AppSettings
        {
            Language = ((ComboBoxItem)LanguageBox.SelectedItem).Tag?.ToString() ?? "ru-RU",
            Theme = ((ComboBoxItem)ThemeBox.SelectedItem).Tag?.ToString() ?? "Light",
            AutoSaveEnabled = AutoSaveBox.IsChecked == true
        };
        DialogResult = true;
    }
}
