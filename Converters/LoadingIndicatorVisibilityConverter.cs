using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte IsLoading (bool) y LoadingProgress (double) en Visibility.
    /// Visible cuando IsLoading == true y LoadingProgress > 0.0
    /// Evita mostrar indicador de carga cuando no hay progreso real.
    /// </summary>
    public class LoadingIndicatorVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return Visibility.Collapsed;

            bool isLoading = false;
            double progress = 0.0;

            if (values[0] is bool b) isLoading = b;
            else if (values[0] is bool?) isLoading = ((bool?)values[0]) ?? false;

            if (values[1] is double d) progress = d;
            else if (values[1] != null)
            {
                double.TryParse(values[1].ToString(), out progress);
            }

            if (isLoading && progress > 0.0)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
