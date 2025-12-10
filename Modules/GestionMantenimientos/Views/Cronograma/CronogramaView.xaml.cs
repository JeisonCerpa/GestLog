using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionMantenimientos.Views.Cronograma
{
    /// <summary>
    /// Lógica de interacción para CronogramaView.xaml
    /// </summary>
    public partial class CronogramaView : System.Windows.Controls.UserControl
    {
        public CronogramaView()
        {
            InitializeComponent();
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma.CronogramaViewModel>();
            DataContext = viewModel;
        }
    }
}


