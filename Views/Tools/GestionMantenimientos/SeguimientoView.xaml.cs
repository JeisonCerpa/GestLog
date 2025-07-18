using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Data;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Lógica de interacción para SeguimientoView.xaml
    /// </summary>
    public partial class SeguimientoView : System.Windows.Controls.UserControl
    {
        public SeguimientoView()
        {
            InitializeComponent();
            // Asignar el DataContext usando DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var viewModel = serviceProvider.GetRequiredService<GestLog.Modules.GestionMantenimientos.ViewModels.SeguimientoViewModel>();
            DataContext = viewModel;

            // Filtro para ocultar seguimientos con estado Pendiente
            var cvs = (CollectionViewSource)this.Resources["SeguimientosFiltrados"];
            cvs.Filter += OnSeguimientoFilter;
        }

        private void OnSeguimientoFilter(object sender, FilterEventArgs e)
        {
            if (e.Item is GestLog.Modules.GestionMantenimientos.Models.SeguimientoMantenimientoDto dto)
            {
                e.Accepted = dto.Estado != EstadoSeguimientoMantenimiento.Pendiente;
            }
            else
            {
                e.Accepted = false;
            }
        }
    }
}
