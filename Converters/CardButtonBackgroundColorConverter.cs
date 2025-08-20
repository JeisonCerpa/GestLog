using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Elige un color de fondo para el botón según el color de la tarjeta, buscando contraste y armonía.
    /// </summary>
    public class CardButtonBackgroundColorConverter : IValueConverter
    {        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string hex)
                {
                    // Si el fondo es rojo fuerte, usar un botón blanco con borde rojo
                    if (hex.Equals("#DF0000", StringComparison.OrdinalIgnoreCase))
                        return System.Windows.Media.Brushes.White;
                    // Si es ámbar, usar blanco y borde ámbar
                    if (hex.Equals("#FFB300", StringComparison.OrdinalIgnoreCase))
                        return System.Windows.Media.Brushes.White;
                    // Si es verde, usar blanco y borde verde
                    if (hex.Equals("#388E3C", StringComparison.OrdinalIgnoreCase))
                        return System.Windows.Media.Brushes.White;
                    // Si es gris, usar blanco
                    if (hex.Equals("#BDBDBD", StringComparison.OrdinalIgnoreCase))
                        return System.Windows.Media.Brushes.White;
                    // Si es blanco, usar gris claro
                    if (hex.Equals("#FFFFFF", StringComparison.OrdinalIgnoreCase))
                        return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240,240,240));
                }
                return System.Windows.Media.Brushes.White;
            }
            catch
            {
                // Fallback seguro: nunca devolver UnsetValue o lanzar excepción
                return System.Windows.Media.Brushes.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
