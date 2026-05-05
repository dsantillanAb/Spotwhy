using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SpotWhy.Models;

namespace SpotWhy.Converters;

public class TypeToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SearchResultType type)
        {
            return type switch
            {
                SearchResultType.Application => new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 57, 82, 153)),
                SearchResultType.File => new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 45, 125, 80)),
                SearchResultType.Folder => new SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 180, 130, 30)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 200, 200, 210))
            };
        }
        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 200, 200, 210));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
