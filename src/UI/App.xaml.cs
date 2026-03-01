using System.Globalization;
using System.Threading;
using System.Windows;

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
        base.OnStartup(e);
    }
}
