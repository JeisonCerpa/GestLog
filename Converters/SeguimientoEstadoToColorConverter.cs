using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Converters
{
    /// <summary>
    /// Convertidor inteligente para mostrar colores en seguimientos.
    /// Si es Correctivo, muestra el color del tipo (morado).
    /// Si no es Correctivo, muestra el color del estado.
    /// </summary>
    public class SeguimientoEstadoToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Si el valor es un DTO de seguimiento, accedemos a su TipoMtno y Estado
            if (value is SeguimientoMantenimientoDto seguimiento)
            {
                return seguimiento.TipoMtno == TipoMantenimiento.Correctivo
                    ? BrushFromTipo(TipoMantenimiento.Correctivo)
                    : BrushFromEstado(seguimiento.Estado);
            }

            // Fallback: si recibimos un EstadoSeguimientoMantenimiento directamente
            if (value is EstadoSeguimientoMantenimiento estado)
            {
                return BrushFromEstado(estado);
            }

            return BrushFromRgb(157, 157, 156); // Gris por defecto
        }        /// <summary>
        /// Obtiene el color para un estado de mantenimiento.
        /// </summary>
        private static SolidColorBrush BrushFromEstado(EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => BrushFromRgb(56, 142, 60),
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => BrushFromRgb(255, 179, 0),
                EstadoSeguimientoMantenimiento.Atrasado => BrushFromRgb(168, 91, 0),
                EstadoSeguimientoMantenimiento.NoRealizado => BrushFromRgb(200, 0, 0),
                EstadoSeguimientoMantenimiento.Pendiente => BrushFromRgb(179, 229, 252),
                EstadoSeguimientoMantenimiento.Correctivo => BrushFromRgb(126, 87, 194),
                _ => BrushFromRgb(157, 157, 156)
            };
        }

        /// <summary>
        /// Obtiene el color para un tipo de mantenimiento.
        /// </summary>
        private static SolidColorBrush BrushFromTipo(TipoMantenimiento tipo)
        {
            return tipo switch
            {
                TipoMantenimiento.Correctivo => BrushFromRgb(126, 87, 194),
                TipoMantenimiento.Preventivo => BrushFromRgb(56, 142, 60),
                TipoMantenimiento.Predictivo => BrushFromRgb(33, 150, 243),
                _ => BrushFromRgb(157, 157, 156)
            };
        }

        private static SolidColorBrush BrushFromRgb(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
