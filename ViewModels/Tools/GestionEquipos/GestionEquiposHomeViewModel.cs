using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(CronogramaDiarioViewModel cronogramaVm, HistorialEjecucionesViewModel historialVm)
        {
            CronogramaVm = cronogramaVm;
            HistorialVm = historialVm;
        }

        public CronogramaDiarioViewModel CronogramaVm { get; }
        public HistorialEjecucionesViewModel HistorialVm { get; }
    }
}
