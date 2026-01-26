using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte texto a mayúsculas (invariable cultural) para mostrar/guardar en la UI.
    /// </summary>
    public class UppercaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Manejar nulls de forma segura
            var s = value?.ToString();
            return (s == null) ? string.Empty : s.ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ConvertBack debe devolver un string no nulo para evitar warnings y problemas de binding
            var s = value?.ToString();
            return (s == null) ? string.Empty : s.ToUpperInvariant();
        }
    }
}
