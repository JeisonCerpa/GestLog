using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionMantenimientos.Views
{
    /// <summary>
    /// Lógica de interacción para CronogramaView.xaml
    /// </summary>
    public partial class CronogramaView : System.Windows.Controls.UserControl
    {
        public CronogramaView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaViewModel>();
            DataContext = viewModel;            
            // El ViewModel ya se inicializa automáticamente, no necesitamos disparar comandos extra
            // esto evita cargas múltiples
        }
    }
}


