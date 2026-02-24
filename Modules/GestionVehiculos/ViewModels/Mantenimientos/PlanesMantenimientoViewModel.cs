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
        private PlantillaMantenimientoDto? selectedPlantilla;

        [ObservableProperty]
        private string intervaloKMPersonalizadoInput = string.Empty;

        [ObservableProperty]
        private string intervaloDiasPersonalizadoInput = string.Empty;

        [ObservableProperty]
        private DateTime? fechaInicioPlan = DateTime.Today;

        [ObservableProperty]
        private string ultimoKMRegistradoInput = string.Empty;

        [ObservableProperty]
        private DateTime? ultimaFechaMantenimiento;

        public string PlantillaSeleccionadaResumen => SelectedPlantilla == null
            ? "Selecciona una plantilla para ver su configuración base."
            : $"Base plantilla → KM: {SelectedPlantilla.IntervaloKM:N0}, Días: {SelectedPlantilla.IntervaloDias:N0}";

        public bool HasPlanActual => SelectedPlan != null;

        public string PlanActualResumen => SelectedPlan == null
            ? "Este vehículo aún no tiene plan asignado."
            : $"Plan: {SelectedPlan.EstadoPlan ?? "N/D"} | Inicio: {SelectedPlan.FechaInicio:dd/MM/yyyy} | Último KM: {(SelectedPlan.UltimoKMRegistrado?.ToString("N0") ?? "N/D")} | Última fecha: {(SelectedPlan.UltimaFechaMantenimiento?.ToString("dd/MM/yyyy") ?? "N/D")}";

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
            foreach (var plantilla in plantillas.Where(p => p.Activo).OrderBy(p => p.Nombre))
            {
                PlantillasDisponibles.Add(plantilla);
            }
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

        /// <summary>
        /// Limpia la selección actual
        /// </summary>
        [RelayCommand]
        public void ClearSelection()
        {
            SelectedPlan = null;
            SelectedPlantilla = null;
            IntervaloKMPersonalizadoInput = string.Empty;
            IntervaloDiasPersonalizadoInput = string.Empty;
            FechaInicioPlan = DateTime.Today;
            UltimoKMRegistradoInput = "0";
            UltimaFechaMantenimiento = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
            OnPropertyChanged(nameof(HasPlanActual));
            OnPropertyChanged(nameof(PlanActualResumen));
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

                if (SelectedPlantilla == null)
                {
                    ErrorMessage = "Debe seleccionar una plantilla";
                    return;
                }

                var intervaloKmPersonalizado = ParseOptionalPositiveInt(IntervaloKMPersonalizadoInput, "intervalo KM personalizado");
                var intervaloDiasPersonalizado = ParseOptionalPositiveInt(IntervaloDiasPersonalizadoInput, "intervalo en días personalizado");
                var ultimoKmRegistrado = ParseOptionalNonNegativeLong(UltimoKMRegistradoInput, "último KM registrado") ?? 0;
                var vehiculo = await _vehicleService.GetByPlateAsync(placaNormalizada, cancellationToken);

                if (vehiculo == null)
                {
                    ErrorMessage = $"No se encontró el vehículo con placa {placaNormalizada}";
                    return;
                }

                var ultimaFecha = UltimaFechaMantenimiento.HasValue
                    ? new DateTimeOffset(UltimaFechaMantenimiento.Value.Date)
                    : (DateTimeOffset?)null;

                if (ultimoKmRegistrado > 0 && !ultimaFecha.HasValue)
                {
                    ErrorMessage = "Si registra un último KM mayor a 0, debe indicar también la fecha del último mantenimiento";
                    return;
                }

                if (ultimaFecha.HasValue && ultimaFecha.Value.Date > DateTimeOffset.Now.Date)
                {
                    ErrorMessage = "La fecha del último mantenimiento no puede ser futura";
                    return;
                }

                if (vehiculo.Mileage > 0 && ultimoKmRegistrado == 0)
                {
                    ErrorMessage = $"El vehículo tiene kilometraje actual ({vehiculo.Mileage:N0} km). Debe registrar el último KM de mantenimiento";
                    return;
                }

                if (ultimoKmRegistrado > vehiculo.Mileage)
                {
                    ErrorMessage = $"El último KM de mantenimiento ({ultimoKmRegistrado:N0}) no puede ser mayor al kilometraje actual del vehículo ({vehiculo.Mileage:N0})";
                    return;
                }

                IsLoading = true;

                var dto = new PlanMantenimientoVehiculoDto
                {
                    PlacaVehiculo = placaNormalizada,
                    PlantillaId = SelectedPlantilla.Id,
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

                if (SelectedPlan == null)
                {
                    await _planService.CreateAsync(dto, cancellationToken);
                }
                else
                {
                    await _planService.UpdateAsync(SelectedPlan.Id, dto, cancellationToken);
                }

                await FilterByPlacaAsync(cancellationToken);
                SuccessMessage = "Plan de mantenimiento guardado correctamente para este vehículo";
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

                var plan = await _planService.GetByPlacaAsync(FilterPlaca, cancellationToken);
                
                Planes.Clear();
                if (plan != null)
                {
                    Planes.Add(plan);
                    SelectedPlan = plan;
                    SelectedPlantilla = PlantillasDisponibles.FirstOrDefault(p => p.Id == plan.PlantillaId);
                    IntervaloKMPersonalizadoInput = plan.IntervaloKMPersonalizado?.ToString() ?? string.Empty;
                    IntervaloDiasPersonalizadoInput = plan.IntervaloDiasPersonalizado?.ToString() ?? string.Empty;
                    FechaInicioPlan = plan.FechaInicio.Date;
                    UltimoKMRegistradoInput = (plan.UltimoKMRegistrado ?? 0).ToString();
                    UltimaFechaMantenimiento = plan.UltimaFechaMantenimiento?.Date;
                }
                else
                {
                    SelectedPlan = null;
                    SelectedPlantilla = null;
                    IntervaloKMPersonalizadoInput = string.Empty;
                    IntervaloDiasPersonalizadoInput = string.Empty;
                    FechaInicioPlan = DateTime.Today;
                    UltimoKMRegistradoInput = "0";
                    UltimaFechaMantenimiento = null;
                    await CargarLineaBaseDesdeHistorialAsync(cancellationToken);
                }

                OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
                OnPropertyChanged(nameof(HasPlanActual));
                OnPropertyChanged(nameof(PlanActualResumen));

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
                UltimoKMRegistradoInput = ultimaEjecucion.KMAlMomento.ToString();
                UltimaFechaMantenimiento = ultimaEjecucion.FechaEjecucion.Date;
            }
        }

        partial void OnSelectedPlantillaChanged(PlantillaMantenimientoDto? value)
        {
            OnPropertyChanged(nameof(PlantillaSeleccionadaResumen));
        }

        private static string NormalizePlate(string? plate)
        {
            return string.IsNullOrWhiteSpace(plate)
                ? string.Empty
                : plate.Trim().ToUpperInvariant();
        }
    }
}
