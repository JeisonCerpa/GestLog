using System;
using System.Windows;
using System.Windows.Input;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    public partial class CompletarMantenimientoWindow : Window
    {
        private System.Windows.Forms.Screen? _lastScreenOwner;
        private CompletarMantenimientoViewModel? _currentViewModel;

        public CompletarMantenimientoWindow()
        {
            InitializeComponent();

            this.KeyDown += CompletarMantenimientoWindow_KeyDown;
            this.Loaded += CompletarMantenimientoWindow_Loaded;
            this.Closing += CompletarMantenimientoWindow_Closing;
            
            // Suscribirse a cambios del DataContext
            this.DataContextChanged += CompletarMantenimientoWindow_DataContextChanged;
        }

        private void CompletarMantenimientoWindow_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Desuscribirse del anterior
            if (_currentViewModel != null)
            {
                _currentViewModel.OnExito -= Vm_OnExito;
            }

            // Suscribirse al nuevo
            _currentViewModel = this.DataContext as CompletarMantenimientoViewModel;
            if (_currentViewModel != null)
            {
                _currentViewModel.OnExito += Vm_OnExito;
            }
        }

        private void Vm_OnExito(object? sender, EventArgs e)
        {
            // Cerrar modal con éxito
            this.DialogResult = true;
            this.Close();
        }        private void CompletarMantenimientoWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CostoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir números, punto y coma (separador decimal)
            e.Handled = !IsNumericInput(e.Text);
        }

        private void GarantiaTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir números (números enteros para días)
            e.Handled = !IsIntegerInput(e.Text);
        }

        private static bool IsNumericInput(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                    return false;
            }
            return true;
        }

        private static bool IsIntegerInput(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c))
                    return false;
            }
            return true;
        }

        private void CompletarMantenimientoWindow_Loaded(object? sender, RoutedEventArgs e)
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
        }

        private void CompletarMantenimientoWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
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
