using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte un estado a Visibility.
    /// Si el estado coincide con TargetEstado, retorna Visible, sino Collapsed.
    /// Usado para mostrar badges o alertas basadas en estados específicos (ej. "EnReparacion")
    /// </summary>
    public class EstadoToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Estado que se está buscando. Por defecto "EnReparacion".
        /// </summary>
        public string TargetEstado { get; set; } = "EnReparacion";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) 
                return Visibility.Collapsed;

            var estadoStr = value.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            var targetStr = TargetEstado.ToLowerInvariant().Replace(" ", "");

            // Normalizar y comparar sin espacios
            estadoStr = estadoStr.Replace(" ", "");

            return estadoStr == targetStr ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
