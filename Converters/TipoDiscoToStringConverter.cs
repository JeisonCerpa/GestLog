using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter para mostrar TipoDisco como string legible
    /// </summary>
    public class TipoDiscoToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tipo)
            {
                return tipo;
            }
            return "Otro";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() ?? "Otro";
        }
    }
}
