using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionMantenimientos
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
            this.Loaded += (s, e) => viewModel.AgruparSemanalmenteCommand.Execute(null);
        }
    }
}
