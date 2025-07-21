using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Elige el color del texto del botón según el fondo del botón y el color de la tarjeta.
    /// </summary>
    public class CardButtonForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                // Si la tarjeta es roja, el botón es blanco, texto rojo
                if (hex.Equals("#C80000", StringComparison.OrdinalIgnoreCase))
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(200,0,0)); // Rojo personalizado #C80000
                // Si es ámbar, texto ámbar oscuro
                if (hex.Equals("#FFB300", StringComparison.OrdinalIgnoreCase))
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(178,111,0));
                // Si es verde, texto verde oscuro
                if (hex.Equals("#388E3C", StringComparison.OrdinalIgnoreCase))
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(27,94,32));
                // Si es gris, texto gris oscuro
                if (hex.Equals("#BDBDBD", StringComparison.OrdinalIgnoreCase))
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(66,66,66));
                // Si es blanco, texto gris
                if (hex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase))
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(51,51,51));
            }
            return System.Windows.Media.Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
