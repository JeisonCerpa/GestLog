using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GestLog.Converters
{
    /// <summary>
    /// Converter que alterna colores basado en el índice de alternancia en ItemsControl.
    /// Retorna tonos oscuros para mantener consistencia con el tema.
    /// </summary>
    public class AlternationIndexToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                // Alterna entre dos brushes del tema para mantener consistencia visual
                return index % 2 == 0
                    ? GetColorFromBrushResource("BackgroundSurfaceBrush")
                    : GetColorFromBrushResource("BackgroundElevatedBrush");
            }

            return GetColorFromBrushResource("BackgroundSurfaceBrush");
        }

        private static System.Windows.Media.Color GetColorFromBrushResource(string resourceKey)
        {
            var brush = System.Windows.Application.Current?.TryFindResource(resourceKey) as SolidColorBrush;
            return brush?.Color ?? Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
