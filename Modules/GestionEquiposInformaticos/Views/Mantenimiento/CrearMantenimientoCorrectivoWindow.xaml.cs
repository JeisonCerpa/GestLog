using System;
using System.Windows;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    public partial class CrearMantenimientoCorrectivoWindow : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        public CrearMantenimientoCorrectivoWindow()
        {
            InitializeComponent();

            // Resolver ViewModel desde el ServiceProvider si está disponible
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                if (serviceProvider != null)
                {
                    var vm = serviceProvider.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.CrearMantenimientoCorrectivoViewModel)) as GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.CrearMantenimientoCorrectivoViewModel;
                    if (vm != null)
                    {
                        this.DataContext = vm;
                        vm.OnExito += Vm_OnExito;
                    }
                }
            }
            catch { /* No fatal */ }

            this.KeyDown += CrearMantenimientoCorrectivoWindow_KeyDown;
            this.Loaded += CrearMantenimientoCorrectivoWindow_Loaded;
            this.Closing += CrearMantenimientoCorrectivoWindow_Closing;
        }        private void Vm_OnExito(object? sender, EventArgs e)
        {
            // Dar un pequeño delay para asegurar que el servicio haya terminado completamente
            System.Threading.Thread.Sleep(500);
            
            // Cerrar modal con éxito
            this.DialogResult = true;
            this.Close();
        }

        private void CrearMantenimientoCorrectivoWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CrearMantenimientoCorrectivoWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }
        }

        public void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null) return;

            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, EventArgs e)
        {
            if (this.Owner == null) return;
            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    this.WindowState = WindowState.Maximized;
                }
                catch { }
            });
        }

        private void CrearMantenimientoCorrectivoWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Si DataContext es ViewModel con flag de éxito, mapear DialogResult
            if (this.DataContext is GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento.CrearMantenimientoCorrectivoViewModel vm)
            {
                // No forzamos aquí; OnExito ya establece DialogResult true
            }
        }
    }
}
