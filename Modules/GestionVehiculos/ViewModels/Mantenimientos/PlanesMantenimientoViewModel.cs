using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos
{
    /// <summary>
    /// ViewModel para gestionar planes de mantenimiento de vehículos
    /// </summary>
    public partial class PlanesMantenimientoViewModel : ObservableObject
    {
        private readonly IPlanMantenimientoVehiculoService _planService;
        private readonly IPlantillaMantenimientoService _plantillaService;
        private readonly IVehicleService _vehicleService;
        private readonly IEjecucionMantenimientoService _ejecucionService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<PlanMantenimientoVehiculoDto> planes = new();

        [ObservableProperty]
        private ObservableCollection<PlanMantenimientoVehiculoDto> planesVigentes = new();

        [ObservableProperty]
        private ObservableCollection<PlanMantenimientoVehiculoDto> planesVencidos = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool isEditingSinglePlan = false;

        [ObservableProperty]
        private int? editingPlanId;

        // event para notificar al view que debe cerrarse
        public event Action? RequestClose;

        [ObservableProperty]
        private PlantillaPlanSelectionItem? selectedTemplate;

        public bool IsDetailVisible => SelectedTemplate != null;

        [RelayCommand]
        private void SelectTemplate(PlantillaPlanSelectionItem template)
        {
            // when user opens a template for editing we also mark it as selected
            template.IsSelected = true;
            SelectedTemplate = template;
        }

        [RelayCommand]
        private void DeselectTemplate(PlantillaPlanSelectionItem template)
        {
            if (template != null)
            {
                template.IsSelected = false;
            }
            SelectedTemplate = null;
        }

        partial void OnSelectedTemplateChanged(PlantillaPlanSelectionItem? value)
        {
            // update visibility when template selection changes
            OnPropertyChanged(nameof(IsDetailVisible));
        }


        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private PlanMantenimientoVehiculoDto? selectedPlan;

        [ObservableProperty]
        private string filterPlaca = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PlantillaMantenimientoDto> plantillasDisponibles = new();

        [ObservableProperty]
        private ObservableCollection<PlantillaPlanSelectionItem> plantillasSeleccionables = new();

        [ObservableProperty]
        private DateTime? fechaInicioPlan = DateTime.Today;

        [ObservableProperty]
        private ObservableCollection<PlanMantenimientoVehiculoDto> planesOrdenados = new();

        public string PlantillaSeleccionadaResumen => PlantillasSeleccionables.Count == 0
            ? "No hay plantillas disponibles."
            : $"Seleccionadas: {PlantillasSeleccionables.Count(p => p.IsSelected)} de {PlantillasSeleccionables.Count}";

        public bool HasPlanActual => Planes.Count > 0;

        public string PlanesConfiguradosResumen => Planes.Count == 0
            ? "Sin planes asignados para este vehículo."
            : $"{Planes.Count} plan(es) asignado(s): {string.Join(", ", Planes.Select(p => p.PlantillaNombre ?? $"Plantilla #{p.PlantillaId}"))}";

        public string PlanActualResumen => SelectedPlan == null
            ? "Este vehículo aún no tiene plan asignado."
            : $"{SelectedPlan.PlantillaNombre ?? $"Plantilla #{SelectedPlan.PlantillaId}"} | Estado: {SelectedPlan.EstadoPlan ?? "N/D"} | Inicio: {SelectedPlan.FechaInicio:dd/MM/yyyy} | Último KM: {(SelectedPlan.UltimoKMRegistrado?.ToString("N0") ?? "N/D")} | Última fecha: {(SelectedPlan.UltimaFechaMantenimiento?.ToString("dd/MM/yyyy") ?? "N/D")}";

        public PlanesMantenimientoViewModel(
            IPlanMantenimientoVehiculoService planService,
            IPlantillaMantenimientoService plantillaService,
            IVehicleService vehicleService,
            IEjecucionMantenimientoService ejecucionService,
            IGestLogLogger logger)        {
            _planService = planService;
            _plantillaService = plantillaService;
            _vehicleService = vehicleService;
            _ejecucionService = ejecucionService;
            _logger = logger;

            Planes.CollectionChanged += Planes_CollectionChanged;
        }

        public async Task InitializeForVehicleAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            FilterPlaca = NormalizePlate(placaVehiculo);
            await LoadPlantillasDisponiblesAsync(cancellationToken);
            await FilterByPlacaAsync(cancellationToken);

            if (SelectedPlan == null)
            {
                await CargarLineaBaseDesdeHistorialAsync(cancellationToken);
            }
        }

        private async Task LoadPlantillasDisponiblesAsync(CancellationToken cancellationToken = default)
        {
            var plantillas = await _plantillaService.GetAllAsync(cancellationToken);

            PlantillasDisponibles.Clear();
            PlantillasSeleccionables.Clear();
            foreach (var plantilla in plantillas.Where(p => p.Activo).OrderBy(p => p.Nombre))
            {
                PlantillasDisponibles.Add(plantilla);
                PlantillasSeleccionables.Add(new PlantillaPlanSelectionItem
                {
                    PlantillaId = plantilla.Id,
                    NombrePlantilla = plantilla.Nombre,
                    IntervaloKmBase = plantilla.IntervaloKM,
                    IntervaloDiasBase = plantilla.IntervaloDias,
                    IntervaloKMAplicado = plantilla.IntervaloKM,
                    IsVisibleWhenEditing = true
                });
            }

            OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
        }

        /// <summary>
        /// Carga todos los planes de mantenimiento
        /// </summary>
        [RelayCommand]
        public async Task LoadPlanesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var result = await _planService.GetAllAsync(cancellationToken);
                
                Planes.Clear();
                foreach (var plan in result)
                {
                    Planes.Add(plan);
                }

                ActualizarPlanesOrdenados();

                _logger.LogInformation("Planes de mantenimiento cargados exitosamente");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de planes cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar los planes";
                _logger.LogError(ex, "Error cargando planes de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga planes vigentes (activos)
        /// </summary>
        [RelayCommand]
        public async Task LoadPlanesVigentesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var result = await _planService.GetVigentesAsync(cancellationToken);
                
                PlanesVigentes.Clear();
                foreach (var plan in result)
                {
                    PlanesVigentes.Add(plan);
                }

                _logger.LogInformation("Planes vigentes cargados exitosamente");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de planes vigentes cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar planes vigentes";
                _logger.LogError(ex, "Error cargando planes vigentes");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga planes vencidos o finalizados
        /// </summary>
        [RelayCommand]
        public async Task LoadPlanesVencidosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var result = await _planService.GetVencidosAsync(cancellationToken);
                
                PlanesVencidos.Clear();
                foreach (var plan in result)
                {
                    PlanesVencidos.Add(plan);
                }

                _logger.LogInformation("Planes vencidos cargados exitosamente");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de planes vencidos cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar planes vencidos";
                _logger.LogError(ex, "Error cargando planes vencidos");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Selecciona un plan para ver/editar detalles
        /// </summary>
        [RelayCommand]
        public void SelectPlan(PlanMantenimientoVehiculoDto plan)
        {
            SelectedPlan = plan;
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        private void EditPlan(PlanMantenimientoVehiculoDto plan)
        {
            if (plan == null) return;
            // prepare for editing (same as opening from main view)
            PrepareEditPlan(plan);
            SuccessMessage = "Editar plan seleccionado";
            OnPropertyChanged(nameof(PlanActualResumen));
        }

        [RelayCommand]
        private void OpenHistory(PlanMantenimientoVehiculoDto plan)
        {
            if (plan == null) return;
            SelectedPlan = plan;
            SuccessMessage = "Abriendo historial";
        }

        /// <summary>
        /// Limpia la selección actual
        /// </summary>
        [RelayCommand]
        public void ClearSelection()
        {
            SelectedPlan = null;
            FechaInicioPlan = DateTime.Today;
            foreach (var item in PlantillasSeleccionables)
            {
                item.IsSelected = false;
                item.IsExpanded = false;
                item.IntervaloKMPersonalizadoInput = string.Empty;
                item.IntervaloDiasPersonalizadoInput = string.Empty;
                item.UltimoKMRegistradoInput = string.Empty;
                item.UltimaFechaMantenimiento = null;
                item.PlanIdExistente = null;
            }
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
            OnPropertyChanged(nameof(HasPlanActual));
            OnPropertyChanged(nameof(PlanActualResumen));
            OnPropertyChanged(nameof(PlanesConfiguradosResumen));
        }

        [RelayCommand]
        public void PrepararNuevoPlan()
        {
            SelectedPlan = null;
            FechaInicioPlan = DateTime.Today;
            ErrorMessage = string.Empty;
            SuccessMessage = "Selecciona una o varias plantillas y personaliza KM/Días por cada una.";
            OnPropertyChanged(nameof(PlanActualResumen));
            ResetEditContext();
            // en modo "nuevo" ocultamos las plantillas que ya tienen un plan asignado
            foreach (var item in PlantillasSeleccionables)
            {
                item.IsVisibleWhenEditing = item.PlanIdExistente == null;
                item.IsSelected = false;
                item.IsExpanded = false;
            }
        }

        [RelayCommand]
        public async Task GuardarPlanVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "No hay placa de vehículo para configurar el plan";
                    return;
                }

                var placaNormalizada = NormalizePlate(FilterPlaca);
                if (string.IsNullOrWhiteSpace(placaNormalizada))
                {
                    ErrorMessage = "No hay placa válida para configurar el plan";
                    return;
                }

                var configuracionesSeleccionadas = PlantillasSeleccionables
                    .Where(p => p.IsSelected)
                    .ToList();

                if (configuracionesSeleccionadas.Count == 0)
                {
                    ErrorMessage = "Debe seleccionar al menos una plantilla";
                    return;
                }

                var vehiculo = await _vehicleService.GetByPlateAsync(placaNormalizada, cancellationToken);

                if (vehiculo == null)
                {
                    ErrorMessage = $"No se encontró el vehículo con placa {placaNormalizada}";
                    return;
                }

                IsLoading = true;

                foreach (var configuracion in configuracionesSeleccionadas)
                {
                    var ultimoKmRegistrado = ParseOptionalNonNegativeLong(
                        configuracion.UltimoKMRegistradoInput,
                        $"último KM de {configuracion.NombrePlantilla}") ?? 0;

                    var ultimaFecha = configuracion.UltimaFechaMantenimiento.HasValue
                        ? new DateTimeOffset(configuracion.UltimaFechaMantenimiento.Value.Date)
                        : (DateTimeOffset?)null;

                    if (ultimoKmRegistrado > 0 && !ultimaFecha.HasValue)
                    {
                        ErrorMessage = $"En '{configuracion.NombrePlantilla}', si registra último KM mayor a 0 debe indicar la fecha del último mantenimiento";
                        return;
                    }

                    if (ultimaFecha.HasValue && ultimaFecha.Value.Date > DateTimeOffset.Now.Date)
                    {
                        ErrorMessage = $"En '{configuracion.NombrePlantilla}', la fecha del último mantenimiento no puede ser futura";
                        return;
                    }

                    if (vehiculo.Mileage > 0 && ultimoKmRegistrado == 0)
                    {
                        ErrorMessage = $"En '{configuracion.NombrePlantilla}', debe registrar último KM (el vehículo está en {vehiculo.Mileage:N0} km)";
                        return;
                    }

                    if (ultimoKmRegistrado > vehiculo.Mileage)
                    {
                        ErrorMessage = $"En '{configuracion.NombrePlantilla}', el último KM ({ultimoKmRegistrado:N0}) no puede ser mayor al kilometraje actual ({vehiculo.Mileage:N0})";
                        return;
                    }

                    var intervaloKmPersonalizado = ParseOptionalPositiveInt(
                        configuracion.IntervaloKMPersonalizadoInput,
                        $"intervalo KM de {configuracion.NombrePlantilla}");
                    var intervaloDiasPersonalizado = ParseOptionalPositiveInt(
                        configuracion.IntervaloDiasPersonalizadoInput,
                        $"intervalo en días de {configuracion.NombrePlantilla}");

                    var dto = new PlanMantenimientoVehiculoDto
                    {
                        PlacaVehiculo = placaNormalizada,
                        PlantillaId = configuracion.PlantillaId,
                        IntervaloKMPersonalizado = intervaloKmPersonalizado,
                        IntervaloDiasPersonalizado = intervaloDiasPersonalizado,
                        FechaInicio = FechaInicioPlan.HasValue
                            ? new DateTimeOffset(FechaInicioPlan.Value.Date)
                            : DateTimeOffset.UtcNow,
                        UltimoKMRegistrado = ultimoKmRegistrado,
                        UltimaFechaMantenimiento = ultimaFecha,
                        FechaFin = null,
                        Activo = true
                    };

                    if (configuracion.PlanIdExistente.HasValue)
                    {
                        await _planService.UpdateAsync(configuracion.PlanIdExistente.Value, dto, cancellationToken);
                    }
                    else
                    {
                        await _planService.CreateAsync(dto, cancellationToken);
                    }

                    // recalculo inmediato local en edit único
                    if (IsEditingSinglePlan && EditingPlanId.HasValue &&
                        configuracion.PlanIdExistente == EditingPlanId.Value)
                    {
                        var planObj = Planes.FirstOrDefault(p => p.Id == EditingPlanId.Value);
                        if (planObj != null)
                        {
                            var baseKm = ultimoKmRegistrado;
                            var baseFecha = ultimaFecha ?? planObj.FechaInicio;
                            var intervaloKm = intervaloKmPersonalizado ?? planObj.IntervaloKMPersonalizado ?? 0;
                            var intervaloDias = intervaloDiasPersonalizado ?? planObj.IntervaloDiasPersonalizado ?? 0;
                            planObj.ProximoKMEjecucion = baseKm + intervaloKm;
                            planObj.ProximaFechaEjecucion = baseFecha.AddDays(intervaloDias);
                        }
                    }
                }

                // después de guardar salimos del modo edición individual
                ResetEditContext();
                await FilterByPlacaAsync(cancellationToken);
                SuccessMessage = $"Se guardaron {configuracionesSeleccionadas.Count} plan(es) de mantenimiento para el vehículo";
                RequestClose?.Invoke();
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Guardado de plan de mantenimiento cancelado");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al guardar el plan del vehículo";
                _logger.LogError(ex, "Error guardando plan de mantenimiento por vehículo");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Filtra planes por placa de vehículo
        /// </summary>
        [RelayCommand]
        public async Task FilterByPlacaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    await LoadPlanesAsync(cancellationToken);
                    return;
                }

                FilterPlaca = NormalizePlate(FilterPlaca);

                IsLoading = true;
                ErrorMessage = string.Empty;

                if (PlantillasDisponibles.Count == 0)
                {
                    await LoadPlantillasDisponiblesAsync(cancellationToken);
                }

                var planes = (await _planService.GetByPlacaListAsync(FilterPlaca, cancellationToken)).ToList();
                
                Planes.Clear();
                foreach (var plan in planes)
                {
                    Planes.Add(plan);
                }

                ActualizarPlanesOrdenados();

                if (planes.Count > 0)
                {
                    SelectedPlan = planes[0];
                    ApplySelectedPlanToForm(SelectedPlan);
                }
                else
                {
                    SelectedPlan = null;
                    FechaInicioPlan = DateTime.Today;
                    await CargarLineaBaseDesdeHistorialAsync(cancellationToken);
                }

                SyncPlantillasSeleccionablesConPlanes();

                OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
                OnPropertyChanged(nameof(HasPlanActual));
                OnPropertyChanged(nameof(PlanActualResumen));
                OnPropertyChanged(nameof(PlanesConfiguradosResumen));

                _logger.LogInformation("Plan filtrado por placa: {Placa}", FilterPlaca);
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Filtrado cancelado");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al filtrar planes por placa {FilterPlaca}";
                _logger.LogError(ex, "Error filtrando planes por placa");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static int? ParseOptionalPositiveInt(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (!int.TryParse(input.Trim(), out var value) || value <= 0)
            {
                throw new InvalidOperationException($"El valor de {fieldName} debe ser un número entero mayor que cero");
            }

            return value;
        }

        private static long? ParseOptionalNonNegativeLong(string input, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (!long.TryParse(input.Trim(), out var value) || value < 0)
            {
                throw new InvalidOperationException($"El valor de {fieldName} debe ser un número entero mayor o igual a cero");
            }

            return value;
        }

        [RelayCommand]
        public async Task DeletePlanAsync(PlanMantenimientoVehiculoDto plan, CancellationToken cancellationToken = default)
        {
            if (plan == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                await _planService.DeleteAsync(plan.Id, cancellationToken);
                await FilterByPlacaAsync(cancellationToken);
                SuccessMessage = "Plan eliminado";
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar el plan";
                _logger.LogError(ex, "Error eliminando plan {PlanId}", plan.Id);
            }
            finally
            {
                IsLoading = false;
                ResetEditContext();
            }
        }

        private async Task CargarLineaBaseDesdeHistorialAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(FilterPlaca))
            {
                return;
            }

            var historial = await _ejecucionService.GetHistorialVehiculoAsync(FilterPlaca, cancellationToken);
            var ultimaEjecucion = historial.FirstOrDefault();

            if (ultimaEjecucion != null)
            {
                var km = ultimaEjecucion.KMAlMomento.ToString();
                var fecha = ultimaEjecucion.FechaEjecucion.Date;

                foreach (var item in PlantillasSeleccionables)
                {
                    if (string.IsNullOrWhiteSpace(item.UltimoKMRegistradoInput))
                    {
                        item.UltimoKMRegistradoInput = km;
                    }

                    if (!item.UltimaFechaMantenimiento.HasValue)
                    {
                        item.UltimaFechaMantenimiento = fecha;
                    }
                }
            }
        }

        partial void OnSelectedPlanChanged(PlanMantenimientoVehiculoDto? value)
        {
            ApplySelectedPlanToForm(value);
            OnPropertyChanged(nameof(PlanActualResumen));
        }

        /// <summary>
        /// Prepara el ViewModel para editar un solo plan existente.
        /// La interfaz mostrará sólo la plantilla asociada.
        /// </summary>
        public void PrepareEditPlan(PlanMantenimientoVehiculoDto plan)
        {
            if (plan == null) return;
            SelectedPlan = plan;
            IsEditingSinglePlan = true;
            EditingPlanId = plan.Id;
            FilterPlaca = NormalizePlate(plan.PlacaVehiculo);
            SyncPlantillasSeleccionablesConPlanes();
            // seleccionar únicamente la plantilla que corresponde al plan y abrir su detalle
            foreach (var item in PlantillasSeleccionables)
            {
                bool matches = item.PlanIdExistente.HasValue && item.PlanIdExistente.Value == plan.Id;
                item.IsSelected = matches;
                item.IsExpanded = matches;
                item.IsVisibleWhenEditing = matches;
                if (matches)
                {
                    SelectTemplate(item);
                }
            }
        }


        public void ResetEditContext()
        {
            IsEditingSinglePlan = false;
            EditingPlanId = null;
            SelectedPlan = null;
            SelectedTemplate = null;
            foreach (var item in PlantillasSeleccionables)
            {
                item.IsVisibleWhenEditing = true;
            }
        }

        partial void OnPlanesChanging(ObservableCollection<PlanMantenimientoVehiculoDto> value)
        {
            // Desuscribirse del evento previo si existe
            if (Planes != null)
            {
                Planes.CollectionChanged -= Planes_CollectionChanged;
            }
        }

        partial void OnPlanesChanged(ObservableCollection<PlanMantenimientoVehiculoDto> value)
        {
            // Suscribirse al nuevo evento
            if (Planes != null)
            {
                Planes.CollectionChanged += Planes_CollectionChanged;
            }
            // Actualizar la colección ordenada
            ActualizarPlanesOrdenados();
        }

        private void Planes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ActualizarPlanesOrdenados();
        }

        private void ActualizarPlanesOrdenados()
        {
            var ordenados = new ObservableCollection<PlanMantenimientoVehiculoDto>();
            
            // Vencidos primero (más lejanos en pasado primero)
            foreach (var plan in Planes.Where(p => p.EstadoPlan == "Vencido").OrderByDescending(p => p.ProximaFechaEjecucion))
            {
                ordenados.Add(plan);
            }
            
            // Próximos (más cercanos PRIMERO - los más urgentes)
            foreach (var plan in Planes.Where(p => p.EstadoPlan == "Próximo").OrderBy(p => p.ProximaFechaEjecucion))
            {
                ordenados.Add(plan);
            }
            
            // Vigentes al final (más cercanos primero)
            foreach (var plan in Planes.Where(p => p.EstadoPlan == "Vigente").OrderBy(p => p.ProximaFechaEjecucion))
            {
                ordenados.Add(plan);
            }
            
            PlanesOrdenados = ordenados;
        }

        private void ApplySelectedPlanToForm(PlanMantenimientoVehiculoDto? plan)
        {
            if (plan == null)
            {
                return;
            }
            FechaInicioPlan = plan.FechaInicio.Date;
        }

        private void SyncPlantillasSeleccionablesConPlanes()
        {
            foreach (var item in PlantillasSeleccionables)
            {
                var plan = Planes.FirstOrDefault(p => p.PlantillaId == item.PlantillaId);
                item.PlanIdExistente = plan?.Id;
                item.IsSelected = plan != null;
                item.IsExpanded = plan != null;
                item.IntervaloKMPersonalizadoInput = plan?.IntervaloKMPersonalizado?.ToString() ?? string.Empty;
                item.IntervaloDiasPersonalizadoInput = plan?.IntervaloDiasPersonalizado?.ToString() ?? string.Empty;
                item.UltimoKMRegistradoInput = plan?.UltimoKMRegistrado?.ToString() ?? string.Empty;
                item.UltimaFechaMantenimiento = plan?.UltimaFechaMantenimiento?.Date;
                // aplicar intervalo existente o base
                item.IntervaloKMAplicado = plan?.IntervaloKMPersonalizado ?? item.IntervaloKmBase;
                item.IntervaloDiasAplicado = plan?.IntervaloDiasPersonalizado ?? item.IntervaloDiasBase;
            }
            OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
        }

        private static string NormalizePlate(string? plate)
        {
            return string.IsNullOrWhiteSpace(plate)
                ? string.Empty
                : plate.Trim().ToUpperInvariant();
        }

        public partial class PlantillaPlanSelectionItem : ObservableObject
        {
            public int PlantillaId { get; set; }
            public string NombrePlantilla { get; set; } = string.Empty;
            public int IntervaloKmBase { get; set; }
            public int IntervaloDiasBase { get; set; }

            // intervalo realmente usado (herramienta mostrará esto)
            [ObservableProperty]
            private int intervaloKMAplicado;

            [ObservableProperty]
            private int intervaloDiasAplicado;

            [ObservableProperty]
            private bool isSelected;

            [ObservableProperty]
            private bool isExpanded;

            [ObservableProperty]
            private string intervaloKMPersonalizadoInput = string.Empty;

            [ObservableProperty]
            private string intervaloDiasPersonalizadoInput = string.Empty;

            [ObservableProperty]
            private string ultimoKMRegistradoInput = string.Empty;

            [ObservableProperty]
            private DateTime? ultimaFechaMantenimiento;

            [ObservableProperty]
            private int? planIdExistente;

            [ObservableProperty]
            private bool isVisibleWhenEditing = true;

            // vistas de previsualización
            public long? NextKmPreview
            {
                get
                {
                    if (long.TryParse(UltimoKMRegistradoInput, out var km))
                    {
                        return km + IntervaloKMAplicado;
                    }
                    return IntervaloKMAplicado > 0 ? (long)IntervaloKMAplicado : (long?)null;
                }
            }

            public string NextKmFormula
            {
                get
                {
                    if (long.TryParse(UltimoKMRegistradoInput, out var km))
                    {
                        return $"{km:N0} + {IntervaloKMAplicado:N0} = {NextKmPreview:N0}";
                    }
                    return string.Empty;
                }
            }

            // update applied intervals when user changes custom inputs
            partial void OnIntervaloKMPersonalizadoInputChanged(string value)
            {
                if (int.TryParse(value, out var v) && v > 0)
                {
                    IntervaloKMAplicado = v;
                }
                else
                {
                    IntervaloKMAplicado = IntervaloKmBase;
                }
            }

            partial void OnIntervaloDiasPersonalizadoInputChanged(string value)
            {
                if (int.TryParse(value, out var v) && v > 0)
                {
                    IntervaloDiasAplicado = v;
                }
                else
                {
                    IntervaloDiasAplicado = IntervaloDiasBase;
                }
            }
        }
    }
}
