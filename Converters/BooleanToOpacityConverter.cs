using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte un valor booleano en opacidad (1.0 si true, 0.5 si false)
    /// </summary>
    public class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 1.0 : 0.5;
            return 0.5;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}