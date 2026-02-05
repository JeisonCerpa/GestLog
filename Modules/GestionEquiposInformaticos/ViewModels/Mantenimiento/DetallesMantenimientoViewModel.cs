using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using System;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para la ventana de detalles de un mantenimiento correctivo
    /// Muestra información read-only del mantenimiento completado o cancelado
    /// </summary>
    public partial class DetallesMantenimientoViewModel : ObservableObject
    {
    /// <summary>
    /// Mantenimiento a mostrar en detalles
    /// </summary>
    [ObservableProperty]
    private MantenimientoCorrectivoDto? mantenimiento;

    /// <summary>
    /// Duración total del mantenimiento en días (calculada)
    /// </summary>
    public int? DuracionTotalDias
    {
        get
        {
            if (Mantenimiento?.FechaInicio == null || Mantenimiento?.FechaCompletado == null)
                return null;

            return (int)(Mantenimiento.FechaCompletado.Value - Mantenimiento.FechaInicio.Value).TotalDays;
        }
    }    /// <summary>
    /// Fecha de vencimiento de la garantía (calculada)
    /// </summary>
    public DateTime? FechaVencimientoGarantia
    {
        get
        {
            if (Mantenimiento?.FechaCompletado == null || !Mantenimiento.PeriodoGarantia.HasValue)
                return null;

            return Mantenimiento.FechaCompletado.Value.AddDays(Mantenimiento.PeriodoGarantia.Value);
        }
    }

    /// <summary>
    /// Estado de la garantía: "Vigente", "Vencida" o "Sin garantía" (calculada)
    /// </summary>
    public string EstadoGarantia
    {
        get
        {
            // Si no hay fecha de completado, no hay garantía
            if (Mantenimiento?.FechaCompletado == null)
                return "Sin garantía";

            // Si no hay período de garantía, no hay garantía
            if (!Mantenimiento.PeriodoGarantia.HasValue || Mantenimiento.PeriodoGarantia.Value <= 0)
                return "Sin garantía";

            // Calcular vencimiento
            DateTime fechaVencimiento = Mantenimiento.FechaCompletado.Value.AddDays(Mantenimiento.PeriodoGarantia.Value);
            DateTime hoy = DateTime.Today;

            if (hoy <= fechaVencimiento)
                return "Vigente";
            else
                return "Vencida";
        }
    }

    /// <summary>
    /// Color del indicador de estado de garantía (calculada)
    /// </summary>
    public string ColorGarantia
    {
        get
        {
            return EstadoGarantia switch
            {
                "Vigente" => "#059669",   // Verde
                "Vencida" => "#C0392B",   // Rojo
                _ => "#9D9D9C"            // Gris - Sin garantía
            };
        }
    }

    /// <summary>
    /// Emoji del estado de garantía (calculada)
    /// </summary>
    public string EmojiGarantia
    {
        get
        {
            return EstadoGarantia switch
            {
                "Vigente" => "🟢",
                "Vencida" => "🔴",
                _ => "⚪"
            };
        }
    }

    /// <summary>
    /// Inicializa el ViewModel con los datos del mantenimiento
    /// </summary>
    public void InitializarMantenimiento(MantenimientoCorrectivoDto mantenimiento)
    {
        Mantenimiento = mantenimiento;
    }
    }
}
