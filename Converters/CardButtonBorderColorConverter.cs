using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Elige el color del borde del botón según el color de la tarjeta.
    /// </summary>
    public class CardButtonBorderColorConverter : IValueConverter
    {        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string hex)
                {
                    // Rojo: borde rojo
                    if (hex.Equals("#DF0000", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(223,0,0));
                    // Ámbar: borde ámbar
                    if (hex.Equals("#FFB300", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,179,0));
                    // Verde: borde verde
                    if (hex.Equals("#388E3C", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(56,142,60));
                    // Gris: borde gris
                    if (hex.Equals("#BDBDBD", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(189,189,189));
                    // Blanco: borde gris claro
                    if (hex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200,200,200));
                }
                return System.Windows.Media.Brushes.Gray;
            }
            catch
            {
                // Fallback seguro: nunca devolver UnsetValue o lanzar excepción
                return System.Windows.Media.Brushes.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
