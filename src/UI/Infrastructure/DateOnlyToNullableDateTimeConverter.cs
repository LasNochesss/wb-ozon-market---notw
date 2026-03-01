using System.Globalization;
using System.Windows.Data;

namespace UI.Infrastructure;

public class DateOnlyToNullableDateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            DateTime dateTime => dateTime,
            _ => null
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime.Date);
        }

        if (value is null)
        {
            return DateOnly.MinValue;
        }

        return Binding.DoNothing;
    }
}
