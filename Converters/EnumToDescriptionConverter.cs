using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace GestLog.Converters
{
    public class EnumToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var type = value.GetType();
            if (!type.IsEnum) return value?.ToString() ?? string.Empty;
            var name = Enum.GetName(type, value);
            if (name == null) return value?.ToString() ?? string.Empty;
            var field = type.GetField(name);
            if (field == null) return name;
            var attr = field.GetCustomAttribute<DescriptionAttribute>();
            return attr != null ? attr.Description : name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
