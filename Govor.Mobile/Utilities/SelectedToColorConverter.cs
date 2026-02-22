using System.Globalization;

namespace Govor.Mobile.Utilities;

public class SelectedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isSelected = (bool)value;
        // белый для активного, серый для неактивного
        return isSelected ? Colors.White : Color.FromArgb("#A0A0A0");
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => 
        throw new NotImplementedException();
}