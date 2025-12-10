using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma;
using System.Collections.ObjectModel;

namespace GestLog.Modules.GestionMantenimientos.Views.Cronograma.SemanaDetalle
{
    public partial class SemanaDetalleDialog : Window
    {
        public SemanaDetalleDialog(SemanaDetalleViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Si el Owner se mueve/redimensiona, mantener sincronizado
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
                    // Siempre maximizar para mantener el overlay cubriendo toda la pantalla
                    this.WindowState = WindowState.Maximized;
                }
                catch
                {
                    // En caso de error, asegurar que la ventana está maximizada
                    this.WindowState = WindowState.Maximized;
                }
            });
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}


