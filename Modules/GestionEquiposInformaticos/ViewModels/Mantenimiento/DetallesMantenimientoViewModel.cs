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
    }

    /// <summary>
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
    /// Inicializa el ViewModel con los datos del mantenimiento
    /// </summary>
    public void InitializarMantenimiento(MantenimientoCorrectivoDto mantenimiento)
    {
        Mantenimiento = mantenimiento;
    }
    }
}
