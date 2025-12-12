using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Cronograma;
using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using System.Threading.Tasks;
using System;
using System.Linq;
using Modules.Usuarios.Interfaces;

namespace GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma
{
    /// <summary>
    /// Interaction logic for GestionarPlanesDialog.xaml
    /// </summary>
    public partial class GestionarPlanesDialog : Window
    {
        private readonly IPlanCronogramaService _planCronogramaService;
        private readonly IGestLogLogger _logger;
        // Servicio de equipos y cache local para validaciones durante edición
        private GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService? _equipoService;
        private System.Collections.Generic.List<GestLog.Modules.GestionEquiposInformaticos.Models.Entities.EquipoInformaticoEntity> _equiposCache = new();        public ObservableCollection<PlanCronogramaEquipo> Planes { get; set; } = new();

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

        private PlanCronogramaEquipo? _planEnEdicion = null;        private string? _selectedEquipoEditarCodigo;
        public string? SelectedEquipoEditarCodigo
        {
            get => _selectedEquipoEditarCodigo;
            set => _selectedEquipoEditarCodigo = value;
        }public GestionarPlanesDialog(IPlanCronogramaService planCronogramaService, IGestLogLogger logger)
        {
            try
            {
                InitializeComponent();
                _planCronogramaService = planCronogramaService;
                _logger = logger;
                DataContext = this;
                
                Loaded += GestionarPlanesDialog_Loaded;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[GestionarPlanesDialog] Error en el constructor");
                System.Windows.MessageBox.Show($"Error al inicializar el diálogo: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private async void GestionarPlanesDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await CargarPlanesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GestionarPlanesDialog] Error fatal al cargar diálogo");
                StatusMessage = $"Error fatal: {ex.Message}";
                System.Windows.MessageBox.Show($"Error al cargar planes: {ex.Message}\n\n{ex.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }        private async Task CargarPlanesAsync()
        {
            try
            {
                StatusMessage = "Cargando planes...";
                var planes = (await _planCronogramaService.GetAllAsync()) ?? new List<PlanCronogramaEquipo>();

                // Intentar resolver servicio de equipos desde el ServiceProvider para poblar la navegación Equipo
                _equipoService = null;
                try
                {
                    var app = (App)System.Windows.Application.Current!;
                    var sp = app?.ServiceProvider;
                    if (sp != null)
                    {
                        _equipoService = sp.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService)) as GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GestionarPlanesDialog] Error al resolver IEquipoInformaticoService");
                }

                Planes.Clear();
                var planesOrdenados = planes.OrderBy(p => p.CodigoEquipo).ThenBy(p => p.DiaProgramado).ToList();
                
                foreach (var plan in planesOrdenados)
                {
                    // Si el servicio está disponible y necesitamos completar datos del equipo, obtenerlos
                    if (_equipoService != null && !string.IsNullOrWhiteSpace(plan.CodigoEquipo))
                    {
                        bool necesitaRefresh = plan.Equipo == null || string.IsNullOrWhiteSpace(plan.Equipo.NombreEquipo) || string.IsNullOrWhiteSpace(plan.Equipo.UsuarioAsignado);
                        
                        if (necesitaRefresh)
                        {
                            try
                            {
                                var equipo = await _equipoService.GetByCodigoAsync(plan.CodigoEquipo);
                                if (equipo != null)
                                {
                                    plan.Equipo = equipo;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "[GestionarPlanesDialog] Error al obtener equipo {Codigo}", plan.CodigoEquipo);
                            }
                        }
                    }

                    // Si aún no hay usuario asignado en el equipo, usar Responsable del plan como fallback
                    if (plan.Equipo != null && string.IsNullOrWhiteSpace(plan.Equipo.UsuarioAsignado) && !string.IsNullOrWhiteSpace(plan.Responsable))
                    {
                        plan.Equipo.UsuarioAsignado = plan.Responsable;
                    }

                    // Si no hay NombreEquipo, garantizar que muestre al menos el código
                    if (plan.Equipo != null && string.IsNullOrWhiteSpace(plan.Equipo.NombreEquipo))
                    {
                        plan.Equipo.NombreEquipo = plan.CodigoEquipo;
                    }

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
        }        private async void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PlanesListView.SelectedItem is PlanCronogramaEquipo plan)
                {
                    _planEnEdicion = plan;
                    EditPanel.Visibility = Visibility.Visible;
                    StatusMessage = "Editando plan";

                    // Dejar que el UI procese el render y bindings pendientes
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

                    // Asignar código de equipo al TextBox de solo lectura
                    if (TxtCodigoEquipoEditar != null) 
                    {
                        var displayText = plan.CodigoEquipo;
                        if (plan.Equipo != null && !string.IsNullOrWhiteSpace(plan.Equipo.NombreEquipo))
                        {
                            displayText = $"{plan.CodigoEquipo} - {plan.Equipo.NombreEquipo}";
                        }
                        TxtCodigoEquipoEditar.Text = displayText;
                    }

                    // Asignar responsable
                    if (TxtResponsableEditar != null) 
                        TxtResponsableEditar.Text = plan.Responsable;

                    // Seleccionar día
                    foreach (ComboBoxItem item in CmbDiaEditar.Items)
                    {
                        if (item.Tag != null && byte.TryParse(item.Tag.ToString(), out var val) && val == plan.DiaProgramado)
                        {
                            CmbDiaEditar.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    StatusMessage = "Seleccione un plan para editar";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GestionarPlanesDialog] Error al editar plan");
                StatusMessage = $"Error al editar: {ex.Message}";
            }
        }private async void GuardarEdicionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_planEnEdicion == null)
            {
                StatusMessage = "No hay un plan en edición";
                return;
            }

            try
            {
                // Validar día
                if (CmbDiaEditar.SelectedItem is not ComboBoxItem selectedItem || selectedItem.Tag == null)
                {
                    StatusMessage = "Seleccione un día válido";
                    return;
                }

                if (!byte.TryParse(selectedItem.Tag.ToString(), out var nuevoDia))
                {
                    StatusMessage = "Día inválido";
                    return;
                }                // Actualizar entidad y persistir
                _planEnEdicion.DiaProgramado = nuevoDia;
                _planEnEdicion.Responsable = TxtResponsableEditar.Text?.Trim() ?? string.Empty;

                // El equipo NO se puede cambiar al editar - el código de equipo permanece igual

                await _planCronogramaService.UpdateAsync(_planEnEdicion);
                StatusMessage = "Plan actualizado";
                _logger.LogInformation("[GestionarPlanesDialog] Plan {PlanId} actualizado", _planEnEdicion.PlanId);

                // Ocultar panel y refrescar lista
                EditPanel.Visibility = Visibility.Collapsed;
                _planEnEdicion = null;
                await CargarPlanesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[GestionarPlanesDialog] Error al guardar edición de plan");
                StatusMessage = "Error al guardar cambios";
            }
        }

        private void CancelarEdicionButton_Click(object sender, RoutedEventArgs e)
        {
            EditPanel.Visibility = Visibility.Collapsed;
            _planEnEdicion = null;
            StatusMessage = "Edición cancelada";
        }
    }
}

