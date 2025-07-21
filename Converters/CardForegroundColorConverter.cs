using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte el color de fondo de la tarjeta a un color de texto (negro o blanco) según el contraste.
    /// </summary>
    public class CardForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
                    // Luminancia relativa (fórmula WCAG)
                    double luminancia = (0.299 * color.R + 0.587 * color.G + 0.114 * color.B) / 255;
                    // Si es fondo claro, texto negro; si es fondo oscuro, texto blanco
                    return luminancia > 0.6 ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
                }
                catch { }
            }
            return System.Windows.Media.Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
