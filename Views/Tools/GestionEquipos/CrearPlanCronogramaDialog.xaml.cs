using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// Lógica de interacción para CrearPlanCronogramaDialog.xaml
    /// </summary>
    public partial class CrearPlanCronogramaDialog : Window
    {
        public PlanCronogramaEquipo? PlanCreado { get; private set; }
        
        private readonly CrearPlanCronogramaViewModel _viewModel;

        public CrearPlanCronogramaDialog(string? codigoEquipoInicial = null)
        {
            InitializeComponent();

            // Obtener ViewModel del contenedor DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var planService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.IPlanCronogramaService>();
            var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
            
            _viewModel = new CrearPlanCronogramaViewModel(planService, logger);
            
            // Si se proporciona un código de equipo inicial, configurarlo
            if (!string.IsNullOrWhiteSpace(codigoEquipoInicial))
            {
                _viewModel.CodigoEquipo = codigoEquipoInicial;
            }

            // Configurar información de semana actual
            InfoSemanaTextBlock.Text = _viewModel.ObtenerInfoSemanaActual();

            // Suscribirse al evento de plan creado
            _viewModel.PlanCreado += OnPlanCreado;

            DataContext = _viewModel;
        }

        private void OnPlanCreado(PlanCronogramaEquipo plan)
        {
            PlanCreado = plan;
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            // Desuscribirse del evento para evitar memory leaks
            if (_viewModel != null)
            {
                _viewModel.PlanCreado -= OnPlanCreado;
            }
            base.OnClosed(e);
        }
    }
}
