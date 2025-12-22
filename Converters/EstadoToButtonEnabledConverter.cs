using System;
using System.Globalization;
using System.Windows.Data;

namespace GestLog.Converters
{
    /// <summary>
    /// Convierte el estado de un equipo/periférico para determinar si un botón debe estar habilitado.
    /// Retorna false si el estado es "EnReparacion", true en caso contrario.
    /// Usado para deshabilitar acciones como "Dar de Baja" cuando un equipo está en reparación.
    /// </summary>
    public class EstadoToButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return true;

            var estadoStr = value.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            
            // Normalizar sin espacios
            estadoStr = estadoStr.Replace(" ", "");
            
            // Retorna false si está en reparación, true en caso contrario
            return estadoStr != "enreparacion";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
