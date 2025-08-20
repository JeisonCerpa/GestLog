using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters;

/// <summary>
/// Convertidor que transforma un valor booleano en un SolidColorBrush
/// </summary>
public class BooleanToColorConverter : IValueConverter
{
    /// <summary>
    /// Color para el valor true (Verde)
    /// </summary>
    public System.Windows.Media.Color TrueColor { get; set; } = Colors.Green;    /// <summary>
    /// Color para el valor false (Rojo)
    /// </summary>
    public System.Windows.Media.Color FalseColor { get; set; } = System.Windows.Media.Color.FromRgb(200, 200, 200); // Gris claro

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        try
        {
            if (value is bool boolValue)
            {
                return new SolidColorBrush(boolValue ? TrueColor : FalseColor);
            }

            return new SolidColorBrush(FalseColor);
        }
        catch
        {
            // Fallback seguro: nunca devolver UnsetValue o lanzar excepción
            return new SolidColorBrush(Colors.Transparent);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("BooleanToColorConverter no admite conversión bidireccional.");
    }
}
