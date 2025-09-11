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
            InitializeComponent();
            // Si no se asignó un DataContext al crear la vista, resolver desde el contenedor DI
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();

                // Intentar obtener el ViewModel desde DataContext o resolverlo desde el contenedor
                var vm = this.DataContext as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel;
                if (vm == null && serviceProvider != null)
                {
                    vm = serviceProvider.GetService(typeof(GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel)) as GestLog.ViewModels.Tools.GestionEquipos.GestionEquiposHomeViewModel;
                    if (vm != null)
                    {
                        this.DataContext = vm;
                    }
                }

                // Siempre intentar cargar el cronograma (el ViewModel del cronograma inicializa semana/año al actual)
                try
                {
                    vm?.CronogramaVm?.LoadCommand?.Execute(null);
                }
                catch { }
            }
            catch
            {
                // no bloquear la carga de la vista si DI no está disponible
            }
        }
    }
}
