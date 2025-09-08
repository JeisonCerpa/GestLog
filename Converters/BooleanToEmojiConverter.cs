using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class BooleanToEmojiConverter : IValueConverter
    {
        // Devuelve ✅ cuando true, ❌ cuando false
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "✅" : "❌";
            return "❌";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
            {
                if (s.Contains("✅")) return true;
                if (s.Contains("❌")) return false;
            }
            return System.Windows.Data.Binding.DoNothing;
        }
    }
}
