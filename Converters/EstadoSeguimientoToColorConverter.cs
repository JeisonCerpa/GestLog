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
            return new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)); // Gris por defecto        // Intentar parsear como enum
        if (value is EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {
                // #B3E5FC - Celeste claro (Pendiente) - semántica: información / pendiente
                EstadoSeguimientoMantenimiento.Pendiente => new SolidColorBrush(MediaColor.FromRgb(179, 229, 252)),
                // #388E3C - Verde (Realizado en tiempo) - semántica: éxito/ok
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => new SolidColorBrush(MediaColor.FromRgb(56, 142, 60)),
                // #FFB300 - Ámbar / Amarillo (Realizado fuera de tiempo) - semántica: atención/advertencia
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => new SolidColorBrush(MediaColor.FromRgb(255, 179, 0)),
                // #C80000 - Rojo (No realizado) - semántica: error/urgente
                EstadoSeguimientoMantenimiento.NoRealizado => new SolidColorBrush(MediaColor.FromRgb(200, 0, 0)),
                // #A85B00 - Naranja / Ámbar oscuro (Atrasado) - semántica: intermedio entre advertencia y error
                EstadoSeguimientoMantenimiento.Atrasado => new SolidColorBrush(MediaColor.FromRgb(168, 91, 0)),
                // #7E57C2 - Morado (Correctivo) - semántica: mantenimiento no planificado, urgencia especial
                EstadoSeguimientoMantenimiento.Correctivo => new SolidColorBrush(MediaColor.FromRgb(126, 87, 194)),
                _ => new SolidColorBrush(MediaColor.FromRgb(157, 157, 156)) // Gris por defecto
            };
        }        // Si es string, intentar convertir
        if (value is string estadoStr)
        {
            if (Enum.TryParse<EstadoSeguimientoMantenimiento>(estadoStr, ignoreCase: true, out var parsedEstado))
            {
                return parsedEstado switch
                {
                    // #B3E5FC - Celeste claro (Pendiente)
                    EstadoSeguimientoMantenimiento.Pendiente => new SolidColorBrush(MediaColor.FromRgb(179, 229, 252)),
                    // #388E3C - Verde (Realizado en tiempo)
                    EstadoSeguimientoMantenimiento.RealizadoEnTiempo => new SolidColorBrush(MediaColor.FromRgb(56, 142, 60)),
                    // #FFB300 - Ámbar (Realizado fuera de tiempo)
                    EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => new SolidColorBrush(MediaColor.FromRgb(255, 179, 0)),
                    // #C80000 - Rojo (No realizado)
                    EstadoSeguimientoMantenimiento.NoRealizado => new SolidColorBrush(MediaColor.FromRgb(200, 0, 0)),
                    // #A85B00 - Naranja oscuro (Atrasado)
                    EstadoSeguimientoMantenimiento.Atrasado => new SolidColorBrush(MediaColor.FromRgb(168, 91, 0)),
                    // #7E57C2 - Morado (Correctivo)
                    EstadoSeguimientoMantenimiento.Correctivo => new SolidColorBrush(MediaColor.FromRgb(126, 87, 194)),
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
