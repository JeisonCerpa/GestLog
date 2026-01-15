using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using GestLog.Modules.GestionMantenimientos.ViewModels;

namespace GestLog.Modules.GestionMantenimientos.Views.Equipos
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
                dynamic? viewModel = this.DataContext;
                if (viewModel == null)
                    return;

                var editMethod = viewModel?.GetType()?.GetMethod("EditEquipoAsync", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (editMethod != null)
                {
                    var task = editMethod.Invoke(viewModel, null) as System.Threading.Tasks.Task;
                    if (task != null)
                    {
                        // Mantener la ventana de detalles abierta mientras se edita
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

            var resultado = System.Windows.MessageBox.Show(
                $"¿Está seguro que desea dar de baja el equipo \"{equipoSeleccionado.Nombre}\" (código: {equipoSeleccionado.Codigo})?",
                "Confirmar baja de equipo",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (resultado != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                dynamic? viewModel = this.DataContext;
                if (viewModel == null)
                    return;

                try
                {
#pragma warning disable CS8602
                    var deleteCommand = viewModel.DeleteEquipoAsyncCommand;
                    if (deleteCommand != null && deleteCommand.CanExecute(null))
                    {
                        await deleteCommand.ExecuteAsync(null);
                        this.DialogResult = true;
                        this.Close();
                    }
#pragma warning restore CS8602
                }
                catch
                {
                    var deleteMethod = viewModel?.GetType()?.GetMethod("DeleteEquipoAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (deleteMethod != null)
                    {
                        var result = deleteMethod.Invoke(viewModel, null);
                        if (result is System.Threading.Tasks.Task task)
                        {
                            await task;
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

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
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

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
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
                    this.WindowState = WindowState.Maximized;
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
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


