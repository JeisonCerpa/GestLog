using CommunityToolkit.Mvvm.ComponentModel;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaDiarioViewModel cronogramaVm)
        {
            CronogramaVm = cronogramaVm;
        }

        public GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaDiarioViewModel CronogramaVm { get; }
    }
}
