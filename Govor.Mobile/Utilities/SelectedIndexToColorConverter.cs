using System.Globalization;

namespace Govor.Mobile.Utilities;

public sealed class SelectedIndexToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse(value?.ToString(), out var selectedIndex) ||
            !int.TryParse(parameter?.ToString(), out var tabIndex))
        {
            return Colors.Gray;
        }

        return selectedIndex == tabIndex ? Colors.White : Color.FromArgb("#A0A0A0");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
