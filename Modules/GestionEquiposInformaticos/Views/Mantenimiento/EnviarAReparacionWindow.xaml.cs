using System;
using System.Windows;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{    public partial class EnviarAReparacionWindow : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        private EnviarAReparacionViewModel? _currentViewModel;

        public EnviarAReparacionWindow()
        {
            InitializeComponent();

            this.KeyDown += EnviarAReparacionWindow_KeyDown;
            this.Loaded += EnviarAReparacionWindow_Loaded;
            this.Closing += EnviarAReparacionWindow_Closing;
            
            // Suscribirse a cambios del DataContext
            this.DataContextChanged += EnviarAReparacionWindow_DataContextChanged;
        }

        private void EnviarAReparacionWindow_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Desuscribirse del anterior
            if (_currentViewModel != null)
            {
                _currentViewModel.OnExito -= Vm_OnExito;
            }

            // Suscribirse al nuevo
            _currentViewModel = this.DataContext as EnviarAReparacionViewModel;
            if (_currentViewModel != null)
            {
                _currentViewModel.OnExito += Vm_OnExito;
            }
        }        private void Vm_OnExito(object? sender, EventArgs e)
        {
            // Dar un pequeño delay para asegurar que el servicio haya terminado completamente
            System.Threading.Thread.Sleep(500);
            
            // Cerrar modal con éxito
            this.DialogResult = true;
            this.Close();
        }

        private void EnviarAReparacionWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void EnviarAReparacionWindow_Loaded(object? sender, RoutedEventArgs e)
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
                // Cuando la ventana propietaria se mueve o redimensiona, esta modal se recentraliza
                this.InvalidateArrange();
            });
        }        private void EnviarAReparacionWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.LocationChanged -= Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged -= Owner_SizeOrLocationChanged;
            }

            // Limpiar suscripción del evento
            if (_currentViewModel != null)
            {
                _currentViewModel.OnExito -= Vm_OnExito;
            }
        }
    }
}
