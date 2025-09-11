using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class BooleanToStringConverter : IValueConverter
    {
        public static readonly BooleanToStringConverter Instance = new BooleanToStringConverter();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string parameterString)
            {
                var parts = parameterString.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
