using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace GestLog.Views.Tools.GestionEquipos
{
    /// <summary>
    /// Interaction logic for GestionarPlanesDialog.xaml
    /// </summary>
    public partial class GestionarPlanesDialog : Window
    {
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IGestLogLogger _logger;
        
        public ObservableCollection<PlanCronogramaEquipo> Planes { get; set; } = new();
        
        private string _statusMessage = "Listo";
        public string StatusMessage 
        { 
            get => _statusMessage; 
            set 
            { 
                _statusMessage = value;
                // Actualizar el TextBlock en la UI
                if (StatusTextBlock != null)
                    StatusTextBlock.Text = value;
            } 
        }

        public GestionarPlanesDialog(IPlanCronogramaService planCronogramaService, IGestLogLogger logger)
        {
            InitializeComponent();
            _planCronogramaService = planCronogramaService;
            _logger = logger;
            DataContext = this;
            
            Loaded += GestionarPlanesDialog_Loaded;
        }

        private async void GestionarPlanesDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarPlanesAsync();
        }

        private async Task CargarPlanesAsync()
        {
            try
            {
                StatusMessage = "Cargando planes...";
                var planes = await _planCronogramaService.GetAllAsync();
                
                Planes.Clear();
                foreach (var plan in planes.OrderBy(p => p.CodigoEquipo).ThenBy(p => p.DiaProgramado))
                {
                    Planes.Add(plan);
                }
                
                StatusMessage = $"Se cargaron {Planes.Count} planes";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GestionarPlanesDialog] Error al cargar planes");
                StatusMessage = "Error al cargar los planes";
            }
        }

        private async void ToggleActivoButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlanesListView.SelectedItem is PlanCronogramaEquipo plan)
            {
                try
                {
                    plan.Activo = !plan.Activo;
                    await _planCronogramaService.UpdateAsync(plan);
                    StatusMessage = $"Plan {(plan.Activo ? "activado" : "desactivado")} correctamente";
                    _logger.LogInformation("[GestionarPlanesDialog] Plan {PlanId} {Estado}", plan.PlanId, plan.Activo ? "activado" : "desactivado");
                    
                    // Refrescar la vista
                    await CargarPlanesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GestionarPlanesDialog] Error al cambiar estado del plan");
                    StatusMessage = "Error al cambiar el estado del plan";
                }
            }
            else
            {
                StatusMessage = "Seleccione un plan primero";
            }
        }

        private async void EliminarButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlanesListView.SelectedItem is PlanCronogramaEquipo plan)
            {
                var resultado = System.Windows.MessageBox.Show(
                    $"¿Está seguro de eliminar el plan para el equipo {plan.CodigoEquipo}?",
                    "Confirmar eliminación",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (resultado == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        await _planCronogramaService.DeleteAsync(plan.PlanId);
                        StatusMessage = "Plan eliminado correctamente";
                        _logger.LogInformation("[GestionarPlanesDialog] Plan eliminado: {PlanId}", plan.PlanId);
                        
                        // Refrescar la vista
                        await CargarPlanesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[GestionarPlanesDialog] Error al eliminar plan");
                        StatusMessage = "Error al eliminar el plan";
                    }
                }
            }
            else
            {
                StatusMessage = "Seleccione un plan primero";
            }
        }

        private void CerrarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
