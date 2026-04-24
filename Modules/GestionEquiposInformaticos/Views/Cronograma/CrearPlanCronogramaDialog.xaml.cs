using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Equipos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Windows.Input;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma
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
            InitializeComponent();            // Obtener ViewModel del contenedor DI
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var planService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data.IPlanCronogramaService>();
            var equipoService = serviceProvider.GetRequiredService<GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data.IEquipoInformaticoService>();
            var logger = serviceProvider.GetRequiredService<GestLog.Services.Core.Logging.IGestLogLogger>();
            
            _viewModel = new CrearPlanCronogramaViewModel(planService, equipoService, logger);
            
            // Si se proporciona un código de equipo inicial, configurarlo
            if (!string.IsNullOrWhiteSpace(codigoEquipoInicial))
            {
                _viewModel.EstablecerEquipoInicial(codigoEquipoInicial);
            }

            // Suscribirse al evento de plan creado
            _viewModel.PlanCreado += OnPlanCreado;

            DataContext = _viewModel;
            
            // Configurar información de semana actual después de cargar
            Loaded += (s, e) => {
                if (FindName("InfoSemanaTextBlock") is System.Windows.Controls.TextBlock tb)
                {
                    tb.Text = _viewModel.ObtenerInfoSemanaActual();
                }
            };
        }

        private void CrearPlanCronogramaDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CloseDialog(false);
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == RootGrid)
            {
                CloseDialog(false);
            }
        }

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OnPlanCreado(PlanCronogramaEquipo plan)
        {
            PlanCreado = plan;
            CloseDialog(true);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(false);
        }

        private void CloseDialog(bool result)
        {
            DialogResult = result;
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

