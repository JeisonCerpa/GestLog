using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Forms;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma
{
    /// <summary>
    /// Ventana modal para mostrar el detalle de un plan de cronograma
    /// </summary>
    public partial class PlanDetalleModalWindow : Window
    {
        private Screen? _lastScreenOwner;

        public PlanDetalleModalWindow()
        {
            InitializeComponent();
            
            // Manejar tecla Escape
            KeyDown += PlanDetalleModalWindow_KeyDown;
        }

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

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

        private void PlanDetalleModalWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Cerrar al hacer clic en el fondo
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Evitar que el clic en el panel cierre la ventana
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
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
                    
                    // Detectar si el Owner cambió de pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    // Si cambió de pantalla, actualizar la referencia
                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
                    // En caso de error, asegurar que la ventana está maximizada
                    this.WindowState = WindowState.Maximized;
                }
            });
        }
    }
}

