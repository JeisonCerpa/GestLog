using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Converters
{    /// <summary>
    /// Devuelve un color diferenciado para planes semanales según su estado.
    /// Ejecutado: verde intenso. Pendiente: azul intenso. Atrasado: ámbar intenso. No Realizado: rojo intenso. No plan: gris medio.
    /// </summary>
    public class PlanEjecucionEstadoToBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush AzulPendiente = new(System.Windows.Media.Color.FromRgb(37, 99, 235));
        private static readonly SolidColorBrush AmberAtrasado = new(System.Windows.Media.Color.FromRgb(217, 119, 6));
        private static readonly SolidColorBrush RojoNoRealizado = new(System.Windows.Media.Color.FromRgb(220, 38, 38));
        private static readonly SolidColorBrush VerdeEjecutado = new(System.Windows.Media.Color.FromRgb(22, 163, 74));
        private static readonly SolidColorBrush GrisDefault = new(System.Windows.Media.Color.FromRgb(107, 114, 128));

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
