using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte el estado de un equipo a un bool indicando si es "Dado de baja".
    /// Usado para aplicar TextDecorations (tachado) a equipos dados de baja.
    /// </summary>
    public class EstadoToBoolConverter : IValueConverter
    {
        /// <summary>
        /// Estado que se está buscando. Por defecto "DadoDeBaja".
        /// </summary>
        public string TargetEstado { get; set; } = "DadoDeBaja";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;

            var estadoStr = value.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            var targetStr = TargetEstado.ToLowerInvariant().Replace(" ", "");

            // Normalizar y comparar sin espacios
            estadoStr = estadoStr.Replace(" ", "");

            return estadoStr == targetStr;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
