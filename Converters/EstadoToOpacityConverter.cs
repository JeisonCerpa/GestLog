using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte el estado de un equipo a un valor de opacidad.
    /// - "Dado de baja" -> 0.5
    /// - "Inactivo" -> 0.75
    /// - Otros estados -> 1.0
    /// </summary>
    public class EstadoToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 1.0;

            var estadoStr = value.ToString()?.Trim().ToLowerInvariant().Replace(" ", "") ?? string.Empty;

            // Dado de baja -> opacidad 0.5
            if (estadoStr == "dadodebaja" || estadoStr == "dadode baja")
                return 0.5;

            // Inactivo -> opacidad 0.75
            if (estadoStr == "inactivo")
                return 0.75;

            // Todos los demás -> opacidad completa
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
