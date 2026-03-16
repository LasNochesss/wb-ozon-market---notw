using System.Globalization;
using System.Windows.Data;

namespace UI.Infrastructure;

public class EnumLocalizationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        var text = value.ToString() ?? string.Empty;
        return text switch
        {
            "WB" => "WB",
            "Ozon" => "Ozon",
            "YM" => "ЯМ",
            "Other" => "Прочее",
            "Real" => "Реальная",
            "SelfBuy" => "Самовыкуп",
            "Sold" => "Продано",
            "Returned" => "Возврат",
            "Cancelled" => "Отмена",
            "Active" => "Активен",
            "Stop" => "Стоп",
            "Ads" => "Реклама",
            "Packaging" => "Упаковка",
            "Warehouse" => "Склад",
            "PhotoContent" => "Фотоконтент",
            _ => text
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value ?? string.Empty;
}
