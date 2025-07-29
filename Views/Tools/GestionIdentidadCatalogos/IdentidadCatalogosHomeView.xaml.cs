using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos
{
    public partial class IdentidadCatalogosHomeView : System.Windows.Controls.UserControl
    {
        public IdentidadCatalogosHomeView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI, igual que en Herramientas y Home
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<IdentidadCatalogosHomeViewModel>();
            DataContext = viewModel;
        }
    }
}
