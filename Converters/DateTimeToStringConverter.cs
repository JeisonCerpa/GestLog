using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        private const string Format = "dd/MM/yyyy";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString(Format);
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s)) return null!;
            if (DateTime.TryParseExact(s.Trim(), Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            // If parsing fails, return Binding.DoNothing to keep previous value
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
