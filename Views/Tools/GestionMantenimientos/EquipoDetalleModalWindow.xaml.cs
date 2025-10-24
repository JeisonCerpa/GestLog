using System.Windows;
using System.Windows.Input;
using System.Windows.Forms; // Para Screen.FromHandle() en ConfigurarParaVentanaPadre
using GestLog.Modules.GestionMantenimientos.ViewModels;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Ventana modal para mostrar el detalle de un equipo de mantenimiento
    /// </summary>
    public partial class EquipoDetalleModalWindow : Window
    {
        private Screen? _lastScreenOwner;

        public EquipoDetalleModalWindow()
        {
            InitializeComponent();

            this.ShowInTaskbar = false;
            this.KeyDown += EquipoDetalleModalWindow_KeyDown;
        }

        private void EquipoDetalleModalWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Obtener el ViewModel del DataContext del modal
                dynamic? viewModel = this.DataContext;
                if (viewModel == null)
                    return;

                // ✅ Llamar directamente al método EditEquipoAsync del ViewModel
                // usando reflexión como fallback si el comando no existe
                var editMethod = viewModel?.GetType()?.GetMethod("EditEquipoAsync", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (editMethod != null)
                {
                    var task = editMethod.Invoke(viewModel, null) as System.Threading.Tasks.Task;
                    if (task != null)
                    {
                        // NO esperar de forma sincrónica (esto bloquearía la UI)
                        // Solo cerrar el modal y dejar que el ViewModel maneje la recarga
                        this.DialogResult = true;
                        this.Close();
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("No se pudo encontrar el método EditEquipoAsync", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al editar equipo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var equipoSeleccionado = (this.DataContext as dynamic)?.SelectedEquipo;
            if (equipoSeleccionado == null)
                return;

            try
            {
                // Obtener el ViewModel del DataContext
                dynamic? viewModel = this.DataContext;
                if (viewModel == null)
                    return;

                // Llamar directamente al método DeleteEquipoAsync del ViewModel
                // El decorador [RelayCommand] debería haber generado el comando, pero
                // si no está disponible, podemos llamar el método directamente
                try
                {
#pragma warning disable CS8602
                    var deleteCommand = viewModel.DeleteEquipoAsyncCommand;
                    if (deleteCommand != null && deleteCommand.CanExecute(null))
                    {
                        await deleteCommand.ExecuteAsync(null);
                        // Cerrar modal después de ejecutar el comando
                        this.DialogResult = true;
                        this.Close();
                    }
#pragma warning restore CS8602
                }
                catch
                {
                    // Si el comando no existe, intentar llamar el método directamente
                    var deleteMethod = viewModel?.GetType()?.GetMethod("DeleteEquipoAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (deleteMethod != null)
                    {
                        var result = deleteMethod.Invoke(viewModel, null);
                        if (result is System.Threading.Tasks.Task task)
                        {
                            await task;
                            // Cerrar modal después de ejecutar el comando
                            this.DialogResult = true;
                            this.Close();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al dar de baja el equipo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Helper para configurar la ventana padre con overlay que cubra toda la pantalla.
        /// Usa WindowState.Maximized para soporte multi-monitor robusto sin problemas de DPI.
        /// </summary>
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
