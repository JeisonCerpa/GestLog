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
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

// Eliminado: Este converter ya no se utiliza en la aplicación. Archivo dejado vacío para futura limpieza.
