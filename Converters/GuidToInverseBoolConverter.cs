using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class GuidToInverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Guid guid)
            {
                return guid == Guid.Empty;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
