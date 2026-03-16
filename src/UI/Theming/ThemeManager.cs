using System.Windows;

namespace UI.Theming;

public static class ThemeManager
{
    private const string Light = "Styles/Themes/LightTheme.xaml";
    private const string Dark = "Styles/Themes/DarkTheme.xaml";

    public static void Apply(string theme)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d => d.Source is not null && (d.Source.OriginalString.EndsWith("LightTheme.xaml") || d.Source.OriginalString.EndsWith("DarkTheme.xaml")));
        if (existing is not null) merged.Remove(existing);

        var src = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? Dark : Light;
        merged.Add(new ResourceDictionary { Source = new Uri(src, UriKind.Relative) });
    }
}
