using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class BooleanToProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 100 : 0;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i)
                return i >= 100;
            return false;
        }
    }
}
