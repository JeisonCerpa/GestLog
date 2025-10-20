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
            // Obtener el equipo seleccionado desde el DataContext del modal
            var equipoSeleccionado = (this.DataContext as dynamic)?.SelectedEquipo;
            if (equipoSeleccionado == null)
                return;

            try
            {
                // Crear el diálogo de edición con el equipo actual
                var editDialog = new EquipoDialog(equipoSeleccionado)
                {
                    Owner = this.Owner ?? this
                };

                var result = editDialog.ShowDialog();
                
                if (result == true)
                {
                    // El diálogo actualizó el equipo, cerrar el detalle para refrescar datos
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir el editor: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
        /// Soporta multi-monitor: detecta en qué pantalla está el Owner y cubre esa pantalla completa.
        /// </summary>
        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Si la ventana padre está maximizada, maximizar esta también
                if (parentWindow.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
                else
                {
                    // Para ventanas no maximizadas, obtener los bounds de la pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                    var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                    
                    // Guardar referencia a la pantalla actual
                    _lastScreenOwner = screen;
                    
                    // Usar los bounds completos de la pantalla
                    var bounds = screen.Bounds;
                    
                    // Configurar para cubrir toda la pantalla
                    this.Left = bounds.Left;
                    this.Top = bounds.Top;
                    this.Width = bounds.Width;
                    this.Height = bounds.Height;
                    this.WindowState = WindowState.Normal;
                }
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
                    // Si la ventana padre está maximizada, maximizar esta también
                    if (this.Owner.WindowState == WindowState.Maximized)
                    {
                        this.WindowState = WindowState.Maximized;
                    }
                    else
                    {
                        // Detectar si el Owner cambió de pantalla
                        var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                        var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                        // Si cambió de pantalla, recalcular bounds
                        if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                        {
                            // Owner cambió de pantalla, recalcular
                            ConfigurarParaVentanaPadre(this.Owner);
                        }
                        else
                        {
                            // Mismo monitor, pero podría haber cambiado tamaño o posición
                            // Actualizar Left y Top manteniendo Width/Height que cubre la pantalla
                            var bounds = currentScreen.Bounds;
                            this.Left = bounds.Left;
                            this.Top = bounds.Top;
                            this.Width = bounds.Width;
                            this.Height = bounds.Height;
                        }
                    }
                }
                catch
                {
                    // Fallback: rellamar ConfigurarParaVentanaPadre
                    try
                    {
                        ConfigurarParaVentanaPadre(this.Owner);
                    }
                    catch { }
                }
            });
        }
    }
}
