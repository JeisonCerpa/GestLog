using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.DaaterProccesor.ViewModels;
using GestLog.Modules.DaaterProccesor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.DaaterProccesor.Views
{
    public partial class DaaterProccesorView : System.Windows.Controls.UserControl
    {
        public DaaterProccesorView()
        {
            this.InitializeComponent();
            // Usar DI para obtener el MainViewModel con el usuario real
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var mainViewModel = serviceProvider.GetRequiredService<GestLog.Modules.DaaterProccesor.ViewModels.MainViewModel>();
            this.DataContext = mainViewModel;
        }

        private void OnOpenFilteredDataViewClick(object sender, System.Windows.RoutedEventArgs e)
        {
            var window = new FilteredDataView();
            window.Show();
        }
    }
}

