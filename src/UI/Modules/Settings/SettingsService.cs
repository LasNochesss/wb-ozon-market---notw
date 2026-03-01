using System;
using System.IO;
using System.Text.Json;

namespace UI.Modules.Settings;

public class SettingsService
{
    private readonly string _path;
    public SettingsService()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProcurementDesktop");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "config.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_path)) return new AppSettings();
        return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
