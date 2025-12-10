using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionMantenimientos.Views.Equipos
{
    /// <summary>
    /// Lógica de interacción para EquiposView.xaml
    /// </summary>
    public partial class EquiposView : System.Windows.Controls.UserControl
    {        public EquiposView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.Equipos.EquiposViewModel>();
            DataContext = viewModel;this.Loaded += async (s, e) =>
            {
                if (viewModel != null)
                    await viewModel.LoadEquiposAsync(forceReload: true); // Carga inicial siempre forzada
            };
        }
    }
}


