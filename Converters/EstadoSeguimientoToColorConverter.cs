using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using MediaColor = System.Windows.Media.Color;

namespace GestLog.Converters;

/// <summary>
/// Convertidor que asigna colores según el estado del seguimiento de mantenimiento.
/// </summary>
public class EstadoSeguimientoToColorConverter : IValueConverter
{    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
            return new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)); // Gris por defecto

        // Intentar parsear como enum
        if (value is EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {
                EstadoSeguimientoMantenimiento.Pendiente => new SolidColorBrush(MediaColor.FromRgb(243, 156, 18)), // Ámbar #F39C12
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => new SolidColorBrush(MediaColor.FromRgb(39, 174, 96)),  // Verde #27AE60
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => new SolidColorBrush(MediaColor.FromRgb(155, 89, 182)),  // Púrpura #9B59B6
                EstadoSeguimientoMantenimiento.NoRealizado => new SolidColorBrush(MediaColor.FromRgb(192, 57, 43)),  // Rojo #C0392B
                EstadoSeguimientoMantenimiento.Atrasado => new SolidColorBrush(MediaColor.FromRgb(230, 126, 34)),   // Naranja #E67E22
                _ => new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)) // Gris por defecto
            };
        }

        // Si es string, intentar convertir
        if (value is string estadoStr)
        {
            if (Enum.TryParse<EstadoSeguimientoMantenimiento>(estadoStr, ignoreCase: true, out var parsedEstado))
            {
                return parsedEstado switch
                {
                    EstadoSeguimientoMantenimiento.Pendiente => new SolidColorBrush(MediaColor.FromRgb(243, 156, 18)), // Ámbar
                    EstadoSeguimientoMantenimiento.RealizadoEnTiempo => new SolidColorBrush(MediaColor.FromRgb(39, 174, 96)),  // Verde
                    EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => new SolidColorBrush(MediaColor.FromRgb(155, 89, 182)),  // Púrpura
                    EstadoSeguimientoMantenimiento.NoRealizado => new SolidColorBrush(MediaColor.FromRgb(192, 57, 43)), // Rojo
                    EstadoSeguimientoMantenimiento.Atrasado => new SolidColorBrush(MediaColor.FromRgb(230, 126, 34)),   // Naranja
                    _ => new SolidColorBrush(MediaColor.FromRgb(157, 157, 156))
                };
            }
        }

        return new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)); // Gris por defecto
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
