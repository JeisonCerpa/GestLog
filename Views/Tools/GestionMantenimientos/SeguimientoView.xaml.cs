using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Lógica de interacción para SeguimientoView.xaml
    /// </summary>
    public partial class SeguimientoView : System.Windows.Controls.UserControl
    {
        public SeguimientoView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.SeguimientoViewModel>();
            DataContext = viewModel;
        }
    }
}
