using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.DaaterProccesor.ViewModels;
using GestLog.Modules.DaaterProccesor.Views;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.DaaterProccesor.Views
{
    public partial class DaaterProccesorView : System.Windows.Controls.UserControl
    {
        private readonly IGestLogLogger _logger;

        public DaaterProccesorView()
        {
            this.InitializeComponent();

            _logger = LoggingService.GetLogger<DaaterProccesorView>();

            // Usar DI para obtener el MainViewModel con el usuario real
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var mainViewModel = serviceProvider.GetRequiredService<GestLog.Modules.DaaterProccesor.ViewModels.MainViewModel>();
            this.DataContext = mainViewModel;
        }

        private void OnOpenFilteredDataViewClick(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                var window = new FilteredDataView
                {
                    Owner = Window.GetWindow(this)
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al abrir FilteredDataView");
                System.Windows.MessageBox.Show(
                    $"No se pudo abrir la ventana de filtros: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

