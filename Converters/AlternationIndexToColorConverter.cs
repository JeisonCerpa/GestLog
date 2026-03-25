using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que alterna colores basado en el índice de alternancia en ItemsControl.
    /// Retorna blanco para índices pares y gris claro para índices impares.
    /// </summary>
    public class AlternationIndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                // Alterna entre blanco y gris claro
                return index % 2 == 0
                    ? Colors.White
                    : (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9FAFB");
            }

            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
