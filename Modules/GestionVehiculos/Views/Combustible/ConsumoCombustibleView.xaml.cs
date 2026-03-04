using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.ViewModels.Combustible;
using System.Windows;
using System.Windows.Controls;

namespace GestLog.Modules.GestionVehiculos.Views.Combustible
{
    public partial class ConsumoCombustibleView : System.Windows.Controls.UserControl
    {
        public ConsumoCombustibleView()
        {
            InitializeComponent();
        }

        public async System.Threading.Tasks.Task OpenRegistroTanqueadaAsync()
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                BtnRegistrarTanqueada_Click(this, new RoutedEventArgs());
            });
        }

        private async void BtnRegistrarTanqueada_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ConsumoCombustibleViewModel vm)
            {
                return;
            }

            var dialog = new RegistroTanqueadaDialog(null)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                await vm.RegistrarAsync(dialog.Resultado!);
            }
        }

        private async void BtnEditarTanqueada_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ConsumoCombustibleViewModel vm)
            {
                return;
            }

            if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not ConsumoCombustibleVehiculoDto item)
            {
                return;
            }

            var editable = new ConsumoCombustibleVehiculoDto
            {
                Id = item.Id,
                PlacaVehiculo = item.PlacaVehiculo,
                FechaTanqueada = item.FechaTanqueada,
                KMAlMomento = item.KMAlMomento,
                Galones = item.Galones,
                ValorTotal = item.ValorTotal,
                Proveedor = item.Proveedor,
                RutaFactura = item.RutaFactura,
                Observaciones = item.Observaciones
            };

            var dialog = new RegistroTanqueadaDialog(editable)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                await vm.EditarAsync(dialog.Resultado!);
            }
        }

        private async void BtnEliminarTanqueada_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ConsumoCombustibleViewModel vm)
            {
                return;
            }

            if (sender is not System.Windows.Controls.Button btn || btn.DataContext is not ConsumoCombustibleVehiculoDto item)
            {
                return;
            }

            var confirm = System.Windows.MessageBox.Show("¿Deseas eliminar este registro de tanqueada?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                await vm.EliminarAsync(item.Id);
            }
        }
    }
}
