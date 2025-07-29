using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class EstadoToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is bool activo)
                return activo ? "✔️" : "❌";
            return "❓";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
