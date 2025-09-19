using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(CronogramaDiarioViewModel cronogramaVm, HistorialEjecucionesViewModel historialVm, PerifericosViewModel perifericosVm)
        {
            CronogramaVm = cronogramaVm;
            HistorialVm = historialVm;
            PerifericosVm = perifericosVm;
        }

        public CronogramaDiarioViewModel CronogramaVm { get; }
        public HistorialEjecucionesViewModel HistorialVm { get; }
        public PerifericosViewModel PerifericosVm { get; }
    }
}
