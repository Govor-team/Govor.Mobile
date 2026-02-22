using System.Globalization;

namespace Govor.Mobile.Utilities;

public class IndexToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int selectedIndex && parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out int tabIndex))
                return selectedIndex == tabIndex;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}