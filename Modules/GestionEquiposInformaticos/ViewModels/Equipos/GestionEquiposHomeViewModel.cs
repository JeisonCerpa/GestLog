using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos
{    public class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(
            CronogramaDiarioViewModel cronogramaVm, 
            HistorialEjecucionesViewModel historialVm, 
            PerifericosViewModel perifericosVm,
            MantenimientosCorrectivosViewModel mantenimientosCorrectivosVm)
        {
            CronogramaVm = cronogramaVm;
            HistorialVm = historialVm;
            PerifericosVm = perifericosVm;
            MantenimientosCorrectivosVm = mantenimientosCorrectivosVm;
        }

        public CronogramaDiarioViewModel CronogramaVm { get; }
        public HistorialEjecucionesViewModel HistorialVm { get; }
        public PerifericosViewModel PerifericosVm { get; }
        public MantenimientosCorrectivosViewModel MantenimientosCorrectivosVm { get; }
    }
}
