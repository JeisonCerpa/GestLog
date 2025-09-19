using GestLog.Modules.GestionEquiposInformaticos.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// Vista para la gestión de periféricos de equipos informáticos
    /// </summary>
    public partial class PerifericosView : System.Windows.Controls.UserControl
    {
        public PerifericosView()
        {
            InitializeComponent();
            
            // Obtener ViewModel usando el patrón estándar de GestLog
            var serviceProvider = LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<PerifericosViewModel>();
            DataContext = viewModel;
        }

        /// <summary>
        /// Método público para recargar los datos desde el exterior
        /// </summary>
        public async System.Threading.Tasks.Task RefreshDataAsync()
        {
            if (DataContext is PerifericosViewModel vm)
            {
                await vm.CargarPerifericosAsync();
            }
        }
    }
}
