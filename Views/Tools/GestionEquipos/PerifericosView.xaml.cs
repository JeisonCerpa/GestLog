using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionEquipos
{    /// <summary>
    /// Vista para la gestión de periféricos de equipos informáticos
    /// Maneja correctamente el lifecycle del ViewModel con auto-refresh
    /// </summary>
    public partial class PerifericosView : System.Windows.Controls.UserControl
    {
        private readonly IGestLogLogger? _logger;

        public PerifericosView()
        {
            // Llamar InitializeComponent() para cargar el XAML
            InitializeComponent();
            
            // Obtener logger para debugging
            var serviceProvider = LoggingService.GetServiceProvider();
            _logger = serviceProvider?.GetService<IGestLogLogger>();
            
            _logger?.LogInformation("[PerifericosView] Constructor iniciado - InitializeComponent() completado");
            
            // Obtener ViewModel usando el patrón estándar de GestLog
            var viewModel = serviceProvider?.GetRequiredService<PerifericosViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
                
                _logger?.LogInformation("[PerifericosView] ViewModel asignado al DataContext con auto-refresh");
                
                // INICIALIZACIÓN ULTRARRÁPIDA - Sin delays ni eventos
                // Inicializar inmediatamente en el constructor para experiencia fluida
                _ = InicializarUltraRapido(viewModel);
            }
            
            // Manejar cleanup cuando se cierre la vista
            Unloaded += OnViewUnloaded;
        }

        private async Task InicializarUltraRapido(PerifericosViewModel viewModel)
        {
            try
            {
                _logger?.LogInformation("[PerifericosView] Iniciando inicialización ultrarrápida");
                
                // Sin delay - inicializar inmediatamente
                await viewModel.InicializarAsync();
                
                _logger?.LogInformation("[PerifericosView] Inicialización ultrarrápida completada");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PerifericosView] Error en inicialización inmediata");
            }
        }        /// <summary>
        /// Método público para recargar los datos desde el exterior
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (DataContext is PerifericosViewModel vm)
            {
                await vm.CargarPerifericosAsync();
            }
        }

        /// <summary>
        /// Maneja la limpieza del ViewModel cuando se cierra la vista
        /// </summary>
        private void OnViewUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("[PerifericosView] Vista siendo descargada - limpiando ViewModel");
                
                // Limpiar suscripción al evento
                Unloaded -= OnViewUnloaded;
                
                // Dispose del ViewModel si implementa IDisposable
                if (DataContext is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                    _logger?.LogInformation("[PerifericosView] ViewModel disposed correctamente");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PerifericosView] Error al limpiar ViewModel");
            }
        }
    }
}
