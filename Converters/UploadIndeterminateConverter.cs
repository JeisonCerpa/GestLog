using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte IsUploading (bool) y UploadProgress (double) a un valor bool para establecer ProgressBar.IsIndeterminate.
    /// Devuelve true cuando IsUploading == true y UploadProgress <= 0.0
    /// </summary>
    public class UploadIndeterminateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            bool isUploading = false;
            double progress = 0.0;

            if (values[0] is bool b) isUploading = b;
            else if (values[0] is bool?) isUploading = ((bool?)values[0]) ?? false;

            if (values[1] is double d) progress = d;
            else if (values[1] != null)
            {
                double.TryParse(values[1].ToString(), out progress);
            }

            return isUploading && progress <= 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}