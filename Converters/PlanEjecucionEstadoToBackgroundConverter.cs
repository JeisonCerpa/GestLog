using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Converters
{    /// <summary>
    /// Devuelve un color de fondo diferenciado para planes semanales según si ya fueron ejecutados en la semana actual.
    /// Ejecutado: verde. Pendiente: azul claro. Atrasado: ámbar. No Realizado: rojo claro. No plan: gris claro.
    /// </summary>
    public class PlanEjecucionEstadoToBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush AzulPendiente = new(System.Windows.Media.Color.FromRgb(227,242,253)); // Azul claro
        private static readonly SolidColorBrush AmberAtrasado = new(System.Windows.Media.Color.FromRgb(255,236,179)); // Ámbar claro
        private static readonly SolidColorBrush RojoNoRealizado = new(System.Windows.Media.Color.FromRgb(248,215,218)); // Rojo claro (#F8D7DA)
        private static readonly SolidColorBrush VerdeEjecutado = new(System.Windows.Media.Color.FromRgb(200,230,201));
        private static readonly SolidColorBrush GrisDefault = new(System.Windows.Media.Color.FromRgb(247,247,247));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CronogramaMantenimientoDto dto)
            {
                if (dto.EsPlanSemanal)
                {
                    if (dto.PlanEjecutadoSemana) return VerdeEjecutado;
                    if (dto.EsNoRealizadoSemana) return RojoNoRealizado; // Prioridad: No Realizado antes que Atrasado
                    if (dto.EsAtrasadoSemana) return AmberAtrasado;
                    return AzulPendiente;
                }
                return GrisDefault;
            }
            return GrisDefault;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
