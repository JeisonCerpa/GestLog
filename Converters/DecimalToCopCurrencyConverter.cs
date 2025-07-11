using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class DecimalToCopCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
                return decimalValue.ToString("C0", new CultureInfo("es-CO"));
            if (value is double doubleValue)
                return doubleValue.ToString("C0", new CultureInfo("es-CO"));
            if (value is float floatValue)
                return floatValue.ToString("C0", new CultureInfo("es-CO"));
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && decimal.TryParse(str, NumberStyles.Currency, new CultureInfo("es-CO"), out var result))
                return result;
            return 0m;
        }
    }
}
