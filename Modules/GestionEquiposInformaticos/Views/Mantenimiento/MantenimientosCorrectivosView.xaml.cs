using System.Windows.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento
{
    /// <summary>
    /// Lógica de interacción para MantenimientosCorrectivosView.xaml
    /// </summary>
    public partial class MantenimientosCorrectivosView : System.Windows.Controls.UserControl
    {
        private CancellationTokenSource? _loadedCts;

        public MantenimientosCorrectivosView()
        {
            InitializeComponent();
            this.Loaded += MantenimientosCorrectivosView_Loaded;
            this.Unloaded += MantenimientosCorrectivosView_Unloaded;
            this.DataContextChanged += MantenimientosCorrectivosView_DataContextChanged;
        }

        private void MantenimientosCorrectivosView_DataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            // Si el DataContext ya es el ViewModel, intentar inicializar
            _ = TryInitializeFromDataContextAsync();
        }

        private async System.Threading.Tasks.Task TryInitializeFromDataContextAsync()
        {
            try
            {
                if (DataContext is MantenimientosCorrectivosViewModel vm)
                {
                    _loadedCts?.Cancel();
                    _loadedCts = new CancellationTokenSource();
                    await vm.InitializeAsync(_loadedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // El ViewModel maneja logging
            }
        }

        private async void MantenimientosCorrectivosView_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Intentar inicializar desde DataContext (si ya está)
                await TryInitializeFromDataContextAsync();
            }
            catch (Exception)
            {
                // ignorar
            }
        }

        private void MantenimientosCorrectivosView_Unloaded(object? sender, RoutedEventArgs e)
        {
            _loadedCts?.Cancel();
            _loadedCts?.Dispose();
            _loadedCts = null;

            if (DataContext is MantenimientosCorrectivosViewModel vm)
            {
                vm.Cleanup();
            }
        }
    }
}
