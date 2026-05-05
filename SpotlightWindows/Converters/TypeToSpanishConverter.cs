using System.Globalization;
using System.Windows.Data;
using SpotWhy.Models;

namespace SpotWhy.Converters;

public class TypeToSpanishConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SearchResultType type)
        {
            return type switch
            {
                SearchResultType.Application => "Aplicación",
                SearchResultType.File => "Archivo",
                SearchResultType.Folder => "Carpeta",
                _ => type.ToString()
            };
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
