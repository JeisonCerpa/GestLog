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
        public ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.ViewModels.EquipoComboItem> EquiposActivos { get; set; } = new ObservableCollection<GestLog.Modules.GestionEquiposInformaticos.ViewModels.EquipoComboItem>();

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

        private PlanCronogramaEquipo? _planEnEdicion = null;

        private string? _selectedEquipoEditarCodigo;
        public string? SelectedEquipoEditarCodigo
        {
            get => _selectedEquipoEditarCodigo;
            set
            {
                _selectedEquipoEditarCodigo = value;
                // Actualizar selección en el ComboBox si existe
                try
                {
                    if (CmbEquipoEditar != null && !string.IsNullOrWhiteSpace(value))
                    {
                        CmbEquipoEditar.SelectedValue = value;
                    }
                }
                catch { /* ignore if control not yet initialized */ }
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

                // Intentar resolver servicio de equipos desde el ServiceProvider para poblar la navegación Equipo
                IEquipoInformaticoService? equipoService = null;
                try
                {
                    var app = (App)System.Windows.Application.Current!;
                    var sp = app.ServiceProvider;
                    // Usar GetService para evitar excepción si no está registrado
                    equipoService = sp.GetService(typeof(GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService)) as GestLog.Modules.GestionEquiposInformaticos.Interfaces.IEquipoInformaticoService;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GestionarPlanesDialog] No se pudo resolver IEquipoInformaticoService desde DI");
                }

                // Si tenemos servicio de equipos, obtener sólo los equipos activos para el selector
                EquiposActivos.Clear();
                if (equipoService != null)
                {
                    try
                    {
                        var todos = await equipoService.GetAllAsync();
                        var activos = todos.Where(e => string.Equals(e.Estado, "Activo", StringComparison.OrdinalIgnoreCase))
                                           .OrderBy(e => e.Codigo)
                                           .ToList();

                        // Mapear a EquipoComboItem (clase del VM CrearPlan)
                        foreach (var e in activos)
                        {
                            EquiposActivos.Add(new GestLog.Modules.GestionEquiposInformaticos.ViewModels.EquipoComboItem
                            {
                                Codigo = e.Codigo ?? string.Empty,
                                NombreEquipo = e.NombreEquipo ?? string.Empty,
                                UsuarioAsignado = e.UsuarioAsignado ?? string.Empty
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[GestionarPlanesDialog] Error al obtener equipos activos para el selector");
                    }
                }

                Planes.Clear();
                foreach (var plan in planes.OrderBy(p => p.CodigoEquipo).ThenBy(p => p.DiaProgramado))
                {
                    // Si el servicio está disponible y necesitamos completar datos del equipo, obtenerlos
                    if (equipoService != null && !string.IsNullOrWhiteSpace(plan.CodigoEquipo))
                    {
                        bool necesitaRefresh = plan.Equipo == null || string.IsNullOrWhiteSpace(plan.Equipo.NombreEquipo) || string.IsNullOrWhiteSpace(plan.Equipo.UsuarioAsignado);
                        if (necesitaRefresh)
                        {
                            try
                            {
                                var equipo = await equipoService.GetByCodigoAsync(plan.CodigoEquipo);
                                if (equipo != null)
                                {
                                    // Reemplazar o asignar la navegación para asegurar datos completos
                                    plan.Equipo = equipo;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "[GestionarPlanesDialog] Error al obtener equipo {Codigo} para plan {PlanId}", plan.CodigoEquipo, plan.PlanId);
                            }
                        }
                    }

                    // Si aún no hay usuario asignado en el equipo, usar Responsable del plan como fallback para mostrar en la columna
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
        }

        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlanesListView.SelectedItem is PlanCronogramaEquipo plan)
            {
                _planEnEdicion = plan;
                // Mostrar panel y poblar campos
                EditPanel.Visibility = Visibility.Visible;
                // Seleccionar equipo en el combo (solo equipos activos estarán listados)
                try
                {
                    if (CmbEquipoEditar != null)
                    {
                        CmbEquipoEditar.SelectedValue = plan.CodigoEquipo;
                    }
                }
                catch { }

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

        private async void GuardarEdicionButton_Click(object sender, RoutedEventArgs e)
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
                }

                // Actualizar entidad y persistir
                _planEnEdicion.DiaProgramado = nuevoDia;
                _planEnEdicion.Responsable = TxtResponsableEditar.Text?.Trim() ?? string.Empty;

                // Si el usuario seleccionó un equipo distinto en el combo, actualizar CodigoEquipo
                if (!string.IsNullOrWhiteSpace(SelectedEquipoEditarCodigo) && !string.Equals(SelectedEquipoEditarCodigo, _planEnEdicion.CodigoEquipo, StringComparison.OrdinalIgnoreCase))
                {
                    _planEnEdicion.CodigoEquipo = SelectedEquipoEditarCodigo;
                }

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
