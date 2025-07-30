using System.Windows.Controls;
using Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Permisos
{
    public partial class PermisosView : System.Windows.Controls.UserControl
    {
        public PermisosView()
        {
            InitializeComponent();
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<PermisoManagementViewModel>();
            DataContext = viewModel;
        }
    }
}

