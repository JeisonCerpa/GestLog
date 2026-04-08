using System.Windows;
using System.Windows.Input;

namespace GestLog.Modules.GestionVehiculos.Views.Mantenimientos
{
    public partial class CrearEditarPlantillaDialog : Window
    {
        public CrearEditarPlantillaDialog()
        {
            InitializeComponent();
            ConfigurarParaVentanaPadre(System.Windows.Application.Current?.MainWindow);

            this.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    this.Close();
                }
            };
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == RootGrid)
            {
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar campos y resetear selección al cancelar
            if (DataContext is GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos.PlantillasMantenimientoViewModel vm)
            {
                vm.NuevaPlantillaNombre = string.Empty;
                vm.NuevaPlantillaDescripcion = string.Empty;
                vm.NuevoIntervaloKm = 5000;
                vm.NuevoIntervaloDias = 180;
                vm.ErrorMessage = string.Empty;
                vm.SuccessMessage = string.Empty;
                vm.SelectedPlantilla = null;
            }
            Close();
        }

        private void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null)
            {
                return;
            }

            Owner = parentWindow;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;

            Loaded += (_, __) =>
            {
                if (Owner == null)
                {
                    return;
                }

                Owner.LocationChanged += (_, __) =>
                {
                    if (WindowState != WindowState.Maximized)
                    {
                        WindowState = WindowState.Maximized;
                    }
                };
                Owner.SizeChanged += (_, __) =>
                {
                    if (WindowState != WindowState.Maximized)
                    {
                        WindowState = WindowState.Maximized;
                    }
                };

                // Observar cambios en el SuccessMessage para cerrar automáticamente después de guardar
                if (DataContext is GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos.PlantillasMantenimientoViewModel vm)
                {
                    vm.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(vm.SuccessMessage) && 
                            !string.IsNullOrEmpty(vm.SuccessMessage) && 
                            string.IsNullOrEmpty(vm.ErrorMessage))
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                System.Threading.Thread.Sleep(500);
                                // Limpiar campos antes de cerrar
                                vm.NuevaPlantillaNombre = string.Empty;
                                vm.NuevaPlantillaDescripcion = string.Empty;
                                vm.NuevoIntervaloKm = 5000;
                                vm.NuevoIntervaloDias = 180;
                                vm.ErrorMessage = string.Empty;
                                vm.SuccessMessage = string.Empty;
                                Close();
                            });
                        }
                    };
                }
            };
        }
    }
}
