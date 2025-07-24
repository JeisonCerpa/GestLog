using System.Windows;
using GestLog.Modules.Usuarios.ViewModels;
using System.Windows.Controls;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogos
{
    public partial class CatalogosManagementView : System.Windows.Controls.UserControl
    {
        public CatalogosManagementView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI para asegurar ModalService
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<CatalogosManagementViewModel>();
            DataContext = viewModel;
            this.Loaded += UserControl_Loaded;
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as System.Windows.FrameworkElement;
            if (fe?.DataContext is CatalogosManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}

