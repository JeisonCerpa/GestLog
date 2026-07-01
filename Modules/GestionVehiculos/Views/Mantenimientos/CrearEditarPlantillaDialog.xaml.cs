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

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos.PlantillasMantenimientoViewModel vm)
            {
                await vm.CrearPlantillaCommand.ExecuteAsync(null);
                if (string.IsNullOrEmpty(vm.ErrorMessage))
                {
                    Close();
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Limpiar campos y resetear modo de edición al cancelar
            if (DataContext is GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos.PlantillasMantenimientoViewModel vm)
            {
                vm.OpenCrearPlantillaCommand.Execute(null);
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
                // El cierre tras guardar lo decide BtnGuardar_Click (sin Thread.Sleep ni hack de PropertyChanged).
            };
        }
    }
}
