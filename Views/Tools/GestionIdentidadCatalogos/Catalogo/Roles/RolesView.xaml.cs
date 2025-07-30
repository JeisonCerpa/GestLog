using System.Windows.Controls;
using Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Roles
{
    public partial class RolesView : System.Windows.Controls.UserControl
    {
        public RolesView()
        {
            InitializeComponent();
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<RolManagementViewModel>();
            DataContext = viewModel;
        }
    }
}

