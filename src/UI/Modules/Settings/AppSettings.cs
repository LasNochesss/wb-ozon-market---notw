namespace UI.Modules.Settings;

public class AppSettings
{
    public string Language { get; set; } = "ru-RU";
    public string Theme { get; set; } = "Light";
    public string? CustomBackgroundHex { get; set; }
    public bool AutoSaveEnabled { get; set; }
    public string? LastExcelPath { get; set; }
}
