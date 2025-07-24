using System.Windows;
using Modules.Usuarios.ViewModels;
using Modules.Usuarios.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Usuarios
{
    public partial class CargoRegistroWindow : Window
    {
        public CargoRegistroWindow()
        {
            InitializeComponent();
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var cargoService = serviceProvider.GetService(typeof(ICargoService)) as ICargoService;
            if (cargoService == null)
            {
                System.Windows.MessageBox.Show("No se pudo obtener el servicio de cargos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            var vm = new CargoRegistroViewModel(cargoService);
            vm.SolicitarCerrar += () => this.DialogResult = true;
            DataContext = vm;
        }
    }
}
