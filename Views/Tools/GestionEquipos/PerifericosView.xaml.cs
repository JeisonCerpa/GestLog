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
            
            // Obtener ViewModel usando el patrón estándar de GestLog
            var viewModel = serviceProvider?.GetRequiredService<PerifericosViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
                
                // INICIALIZACIÓN CUANDO LA VISTA SE HACE VISIBLE
                // Usar Loaded en lugar de constructor para evitar problemas de timing
                Loaded += async (s, e) => await InicializarCuandoSeaVisible(viewModel);
            }
        }        
        private async Task InicializarCuandoSeaVisible(PerifericosViewModel viewModel)
        {
            try
            {
                // Solo inicializar si no se ha hecho antes o si no hay datos
                if (viewModel.Perifericos.Count == 0)
                {
                    await viewModel.InicializarAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[PerifericosView] Error en inicialización");
            }
        }        
        /// <summary>
        /// Método público para recargar los datos desde el exterior
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (DataContext is PerifericosViewModel vm)
            {
                await vm.CargarPerifericosAsync();
            }
        }
    }
}
