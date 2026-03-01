using System.Globalization;
using System.Threading;
using System.Windows;
using UI.Modules.Settings;
using UI.Theming;

namespace UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        var ruCulture = new CultureInfo("ru-RU");
        Thread.CurrentThread.CurrentCulture = ruCulture;
        Thread.CurrentThread.CurrentUICulture = ruCulture;
        CultureInfo.DefaultThreadCurrentCulture = ruCulture;
        CultureInfo.DefaultThreadCurrentUICulture = ruCulture;

        var settings = new SettingsService().Load();
        ThemeManager.Apply(settings.Theme);

        base.OnStartup(e);
    }
}
