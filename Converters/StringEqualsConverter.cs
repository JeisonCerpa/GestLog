using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte entre un string y un bool comprobando si el string coincide con el ConverterParameter.
    /// Útil para enlazar RadioButtons a propiedades string en MVVM.
    /// </summary>
    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            var p = parameter?.ToString();
            return string.Equals(s, p, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
            {
                return parameter?.ToString() ?? string.Empty;
            }

            // Cuando se desmarca no cambiamos el valor
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
