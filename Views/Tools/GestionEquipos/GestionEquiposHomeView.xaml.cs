using System.Windows.Controls;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using UserControl = System.Windows.Controls.UserControl;

namespace GestLog.Views.Tools.GestionEquipos
{
    public partial class GestionEquiposHomeView : UserControl
    {
        public GestionEquiposHomeView()
        {
            try
            {
                System.Windows.Application.LoadComponent(this, new System.Uri("/GestLog;component/Views/Tools/GestionEquipos/GestionEquiposHomeView.xaml", System.UriKind.Relative));
            }
            catch { }
            // Resolver DataContext
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var vm = DataContext as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel ?? serviceProvider?.GetService(typeof(GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel)) as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel;
                if (vm != null)
                    DataContext = vm;
                try
                {
                    var cronogramaVm = vm?.CronogramaVm;
                    if (cronogramaVm != null && cronogramaVm.Planificados.Count == 0)
                        _ = cronogramaVm.LoadAsync(System.Threading.CancellationToken.None);
                }
                catch { }
            }
            catch { }
        }
    }
}
