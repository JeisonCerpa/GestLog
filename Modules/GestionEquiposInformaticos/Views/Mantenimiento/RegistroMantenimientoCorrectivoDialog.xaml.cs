using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    /// <summary>
    /// Lógica de interacción para RegistroMantenimientoCorrectivoDialog.xaml
    /// Ventana modal para registrar mantenimientos correctivos - PATRÓN IDÉNTICO A DetallesEquipoInformaticoView
    /// </summary>
    public partial class RegistroMantenimientoCorrectivoDialog : Window
    {
        public RegistroMantenimientoCorrectivoViewModel ViewModel { get; private set; }

        /// <summary>
        /// Constructor para crear un nuevo mantenimiento
        /// </summary>
        public RegistroMantenimientoCorrectivoDialog()
        {
            InitializeComponent();

            // Asegurar que esta ventana se abra como modal sobre la ventana principal y no aparezca en la barra de tareas
            try
            {
                // Asignar el Owner para centrar respecto al padre y bloquear la interacción
                this.Owner = System.Windows.Application.Current?.MainWindow;
                this.ShowInTaskbar = false;
                this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                // ✅ CRÍTICO: Maximizar ANTES de mostrar para que el overlay cubra toda la pantalla
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // No crítico si no existe Application.Current o MainWindow en algunos escenarios de test
            }

            // Registrar Loaded para ajustar sincronización cuando se mueve el owner
            this.Loaded += RegistroMantenimientoCorrectivoDialog_Loaded;

            // Obtener ViewModel desde DI
            var app = System.Windows.Application.Current as App;
            var viewModel = app?.ServiceProvider?.GetRequiredService<RegistroMantenimientoCorrectivoViewModel>();
            
            if (viewModel == null)
                throw new InvalidOperationException("No se pudo obtener RegistroMantenimientoCorrectivoViewModel del contenedor DI");

            ViewModel = viewModel;
            DataContext = ViewModel;

            // Suscribirse al evento de guardado exitoso para cerrar el diálogo
            ViewModel.OnRegistroGuardado += (s, e) =>
            {
                DialogResult = true;
                Close();
            };

            // Manejar tecla Escape
            this.KeyDown += RegistroMantenimientoCorrectivoDialog_KeyDown;
        }        private void RegistroMantenimientoCorrectivoDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
            // Cerrar al hacer clic en el overlay oscuro (solo RootGrid)
            if (sender is Grid grid && grid.Name == "RootGrid")
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Marcar como manejado para evitar que el clic se propague al overlay
            e.Handled = true;
        }

        /// <summary>
        /// Configura la ventana como modal sobre una ventana padre
        /// Maximiza la ventana para cubrir toda la pantalla con el overlay semitransparente
        /// </summary>
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;
            this.WindowState = WindowState.Maximized;

            // Mantener sincronizado cuando la ventana padre se mueve
            this.Loaded += (s, e) =>
            {
                if (this.Owner != null)
                {
                    this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                    this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
                }
            };
        }

        private void RegistroMantenimientoCorrectivoDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            // Sincronizar tamaño cuando el owner cambia
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
                    // Mantener maximizado para que el overlay siempre cubra la pantalla
                    if (this.WindowState != WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Maximized;
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
