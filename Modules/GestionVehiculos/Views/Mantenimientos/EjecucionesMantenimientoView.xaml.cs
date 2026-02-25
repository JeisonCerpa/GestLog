using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    /// <summary>
    /// Interacción lógica para EjecucionesMantenimientoView.xaml
    /// </summary>
    public partial class EjecucionesMantenimientoView : System.Windows.Controls.UserControl
    {
        public EjecucionesMantenimientoView()
        {
            InitializeComponent();
        }

        private async void BtnOpenPreventivoModal_Click(object sender, RoutedEventArgs e)
        {
            var sp = ((App)System.Windows.Application.Current).ServiceProvider;
            var vm = DataContext as EjecucionesMantenimientoViewModel;

            if (vm == null)
            {
                vm = sp?.GetService(typeof(EjecucionesMantenimientoViewModel)) as EjecucionesMantenimientoViewModel;
                if (vm != null)
                {
                    DataContext = vm;
                }
            }

            if (vm == null)
            {
                return;
            }

            var placa = vm.FilterPlaca;
            if (string.IsNullOrWhiteSpace(placa))
            {
                placa = TryResolvePlateFromParent();
                if (!string.IsNullOrWhiteSpace(placa))
                {
                    vm.FilterPlaca = placa;
                    await vm.LoadHistorialVehiculoAsync();
                }
            }

            var dialog = new RegistroPreventivoDialog(vm);
            var owner = System.Windows.Application.Current?.Windows.Count > 0
                ? System.Windows.Application.Current.Windows[0]
                : System.Windows.Application.Current?.MainWindow;
            dialog.Owner = owner;

            var result = dialog.ShowDialog();
            if (result == true)
            {
                if (!string.IsNullOrWhiteSpace(vm.FilterPlaca))
                {
                    await vm.LoadHistorialVehiculoAsync();
                }

                await RefreshPlanesViewAsync(vm.FilterPlaca);

                var message = string.IsNullOrWhiteSpace(vm.ConfirmacionRegistroMessage)
                    ? "Preventivo(s) registrado(s) correctamente."
                    : vm.ConfirmacionRegistroMessage;

                System.Windows.MessageBox.Show(
                    message,
                    "Confirmación de registro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private async Task RefreshPlanesViewAsync(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa))
            {
                return;
            }

            var vehicleDetailsView = FindParentVehicleDetailsView();
            if (vehicleDetailsView?.FindName("PlanesView") is not PlanesMantenimientoView planesView)
            {
                return;
            }

            var planesVm = planesView.DataContext as PlanesMantenimientoViewModel;
            if (planesVm == null)
            {
                var sp = ((App)System.Windows.Application.Current).ServiceProvider;
                planesVm = sp?.GetService(typeof(PlanesMantenimientoViewModel)) as PlanesMantenimientoViewModel;
                if (planesVm == null)
                {
                    return;
                }

                planesView.DataContext = planesVm;
            }

            planesVm.FilterPlaca = placa;
            await planesVm.FilterByPlacaAsync();
        }

        private VehicleDetailsView? FindParentVehicleDetailsView()
        {
            DependencyObject? parent = this;
            while (parent != null)
            {
                if (parent is VehicleDetailsView detailsView)
                {
                    return detailsView;
                }

                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private string TryResolvePlateFromParent()
        {
            DependencyObject? parent = this;
            while (parent != null)
            {
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe && fe.DataContext != null)
                {
                    var prop = fe.DataContext.GetType().GetProperty("Plate");
                    if (prop != null)
                    {
                        var value = prop.GetValue(fe.DataContext) as string;
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
            }

            return string.Empty;
        }
    }
}
