using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;

namespace GestLog.ViewModels.Tools.GestionEquipos
{
    public class GestionEquiposHomeViewModel : ObservableObject
    {
        public GestionEquiposHomeViewModel(CronogramaDiarioViewModel cronogramaVm)
        {
            CronogramaVm = cronogramaVm;
        }

        public CronogramaDiarioViewModel CronogramaVm { get; }
    }
}
