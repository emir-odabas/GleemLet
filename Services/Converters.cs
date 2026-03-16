using System.Globalization;
using System.Windows.Data;

namespace GleemLet.Services;

public class WidthPercentageConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double totalWidth || values[1] is not double percentage)
            return 0.0;
        return Math.Max(0, totalWidth * percentage);
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
