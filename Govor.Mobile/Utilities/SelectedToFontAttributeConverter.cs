using System.Globalization;

namespace Govor.Mobile.Utilities;

// Конвертер для изменения жирности текста
public class SelectedToFontAttributeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSelected = (bool)value;
        return isSelected ? FontAttributes.Bold : FontAttributes.None;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();

}