namespace UI.Modules.Localization;

public class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _map = new()
    {
        ["ru-RU"] = new()
        {
            ["Tab.Files"] = "Файлы",
            ["Btn.New"] = "Новый",
            ["Btn.Open"] = "Открыть (.xlsx)",
            ["Btn.Save"] = "Сохранить",
            ["Btn.SaveAs"] = "Сохранить как…",
            ["Btn.Export"] = "Экспорт",
            ["Btn.Import"] = "Импорт",
            ["Status.Saved"] = "сохранено",
            ["Status.Dirty"] = "есть изменения"
        },
        ["en-US"] = new()
        {
            ["Tab.Files"] = "Files",
            ["Btn.New"] = "New",
            ["Btn.Open"] = "Open (.xlsx)",
            ["Btn.Save"] = "Save",
            ["Btn.SaveAs"] = "Save as…",
            ["Btn.Export"] = "Export",
            ["Btn.Import"] = "Import",
            ["Status.Saved"] = "saved",
            ["Status.Dirty"] = "has changes"
        }
    };

    public string T(string lang, string key) => _map.TryGetValue(lang, out var d) && d.TryGetValue(key, out var v) ? v : key;
}
