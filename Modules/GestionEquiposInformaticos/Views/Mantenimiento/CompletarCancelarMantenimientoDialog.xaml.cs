using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    public partial class CompletarCancelarMantenimientoDialog : Window
    {
        public CompletarCancelarMantenimientoViewModel ViewModel { get; private set; }

        public CompletarCancelarMantenimientoDialog()
        {
            InitializeComponent();

            try
            {
                this.Owner = System.Windows.Application.Current?.MainWindow;
                this.ShowInTaskbar = false;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
            }

            this.Loaded += CompletarCancelarMantenimientoDialog_Loaded;

            var app = System.Windows.Application.Current as App;
            var viewModel = app?.ServiceProvider?.GetRequiredService<CompletarCancelarMantenimientoViewModel>();
            
            if (viewModel == null)
                throw new InvalidOperationException("No se pudo obtener CompletarCancelarMantenimientoViewModel del contenedor DI");

            ViewModel = viewModel;
            DataContext = ViewModel;

            ViewModel.OnMantenimientoProcesado += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            this.KeyDown += CompletarCancelarMantenimientoDialog_KeyDown;
        }

        private void CompletarCancelarMantenimientoDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;

            this.Loaded += (s, e) =>
            {
                if (this.Owner != null)
                {
                    this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                    this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
                }
            };
        }

        private void CompletarCancelarMantenimientoDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (this.WindowState != WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                }
                catch
                {
                    this.WindowState = WindowState.Maximized;
                }
            });
        }
    }
}
