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
    /// ViewModel para gestionar ejecuciones de mantenimiento
    /// </summary>
    public partial class EjecucionesMantenimientoViewModel : ObservableObject
    {
        private readonly IEjecucionMantenimientoService _ejecucionService;
        private readonly IPlanMantenimientoVehiculoService _planService;
        private readonly IPlantillaMantenimientoService _plantillaService;
        private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<EjecucionMantenimientoDto> ejecuciones = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private EjecucionMantenimientoDto? selectedEjecucion;

        [ObservableProperty]
        private string filterPlaca = string.Empty;

        [ObservableProperty]
        private DateTime? filterFechaDesde;

        [ObservableProperty]
        private DateTime? filterFechaHasta;

        [ObservableProperty]
        private DateTime? registroFechaEjecucion = DateTime.Today;

        [ObservableProperty]
        private string registroKMAlMomentoInput = string.Empty;

        [ObservableProperty]
        private string registroResponsable = string.Empty;

        [ObservableProperty]
        private string registroObservaciones = string.Empty;

        [ObservableProperty]
        private string registroCostoInput = string.Empty;

        [ObservableProperty]
        private bool preventivoCambioAceite;

        [ObservableProperty]
        private bool preventivoFiltroAceite;

        [ObservableProperty]
        private bool preventivoFiltroAire;

        [ObservableProperty]
        private bool preventivoFiltroCombustible;

        [ObservableProperty]
        private bool preventivoRevisionGeneral;

        [ObservableProperty]
        private string planPreventivoInfo = "Sin plan activo para preventivo";

        [ObservableProperty]
        private string ventanaPreventivoInfo = "Cargue placa para validar ventana sugerida (7 días o 500 km).";

        [ObservableProperty]
        private bool ventanaPreventivoAdvertencia;
    [ObservableProperty]
    private bool hasPlanPreventivo;

        public EjecucionesMantenimientoViewModel(
            IEjecucionMantenimientoService ejecucionService,
            IPlanMantenimientoVehiculoService planService,
            IPlantillaMantenimientoService plantillaService,
            IVehicleService vehicleService,
            IGestLogLogger logger)
        {
            _ejecucionService = ejecucionService;
            _planService = planService;
            _plantillaService = plantillaService;
            _vehicleService = vehicleService;
            _logger = logger;
        }

        [RelayCommand]
        public async Task RegistrarEjecucionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe indicar la placa del vehículo";
                    return;
                }

                var placa = NormalizePlate(FilterPlaca);
                if (!long.TryParse(RegistroKMAlMomentoInput?.Trim(), out var kmAlMomento) || kmAlMomento <= 0)
                {
                    ErrorMessage = "Debe ingresar un KM al momento válido";
                    return;
                }

                var vehiculoActual = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculoActual != null && kmAlMomento < vehiculoActual.Mileage)
                {
                    ErrorMessage = $"El KM registrado ({kmAlMomento:N0}) no puede ser menor al KM actual del vehículo ({vehiculoActual.Mileage:N0})";
                    return;
                }

                decimal? costo = null;
                if (!string.IsNullOrWhiteSpace(RegistroCostoInput))
                {
                    if (!decimal.TryParse(RegistroCostoInput.Trim(), out var parsedCosto) || parsedCosto < 0)
                    {
                        ErrorMessage = "El costo debe ser un valor numérico válido";
                        return;
                    }

                    costo = parsedCosto;
                }

                IsLoading = true;

                var plan = await _planService.GetByPlacaAsync(placa, cancellationToken);
                if (plan == null)
                {
                    ErrorMessage = "Para registrar preventivo, el vehículo debe tener un plan activo asignado";
                    return;
                }

                var actividades = BuildPreventivoChecklist();
                var tieneDetalleLibre = !string.IsNullOrWhiteSpace(RegistroObservaciones);
                if (actividades.Count == 0 && !tieneDetalleLibre)
                {
                    ErrorMessage = "Debe marcar al menos una actividad del checklist o escribir detalle de lo realizado";
                    return;
                }

                var fechaEjecucion = RegistroFechaEjecucion.HasValue
                    ? new DateTimeOffset(RegistroFechaEjecucion.Value.Date)
                    : DateTimeOffset.Now;

                var observacionesPreventivo = BuildPreventivoObservaciones(actividades, RegistroObservaciones);

                var dto = new EjecucionMantenimientoDto
                {
                    PlacaVehiculo = placa,
                    PlanMantenimientoId = plan.Id,
                    FechaEjecucion = fechaEjecucion,
                    KMAlMomento = kmAlMomento,
                    ObservacionesTecnico = observacionesPreventivo,
                    Costo = costo,
                    ResponsableEjecucion = string.IsNullOrWhiteSpace(RegistroResponsable) ? null : RegistroResponsable.Trim(),
                    Estado = 2
                };

                await _ejecucionService.CreateAsync(dto, cancellationToken);

                var vehiculo = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculo != null && kmAlMomento > vehiculo.Mileage)
                {
                    vehiculo.Mileage = kmAlMomento;
                    await _vehicleService.UpdateAsync(vehiculo.Id, vehiculo, cancellationToken);
                }

                plan.UltimoKMRegistrado = kmAlMomento;
                plan.UltimaFechaMantenimiento = fechaEjecucion;
                await _planService.UpdateAsync(plan.Id, plan, cancellationToken);

                await LoadHistorialVehiculoAsync(cancellationToken);

                PreventivoCambioAceite = false;
                PreventivoFiltroAceite = false;
                PreventivoFiltroAire = false;
                PreventivoFiltroCombustible = false;
                PreventivoRevisionGeneral = false;
                RegistroObservaciones = string.Empty;
                RegistroResponsable = string.Empty;
                RegistroCostoInput = string.Empty;

                SuccessMessage = VentanaPreventivoAdvertencia
                    ? "Preventivo registrado. Advertencia: se registró fuera de la ventana sugerida (7 días / 500 km)."
                    : "Preventivo registrado correctamente";
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Registro de ejecución cancelado");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al registrar ejecución de mantenimiento";
                _logger.LogError(ex, "Error registrando ejecución de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CargarKilometrajeActualAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe indicar la placa del vehículo";
                    return;
                }

                var placa = NormalizePlate(FilterPlaca);
                var vehiculo = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculo == null)
                {
                    ErrorMessage = $"No se encontró el vehículo con placa {placa}";
                    return;
                }

                RegistroKMAlMomentoInput = vehiculo.Mileage.ToString();
                SuccessMessage = "Se cargó el kilometraje actual del vehículo";
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al consultar el kilometraje actual";
                _logger.LogError(ex, "Error cargando kilometraje actual del vehículo");
            }
        }

        [RelayCommand]
        public async Task ActualizarKilometrajeVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe indicar la placa del vehículo";
                    return;
                }

                if (!long.TryParse(RegistroKMAlMomentoInput?.Trim(), out var nuevoKm) || nuevoKm <= 0)
                {
                    ErrorMessage = "Debe ingresar un kilometraje válido";
                    return;
                }

                var placa = NormalizePlate(FilterPlaca);
                IsLoading = true;

                var vehiculo = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculo == null)
                {
                    ErrorMessage = $"No se encontró el vehículo con placa {placa}";
                    return;
                }

                if (nuevoKm < vehiculo.Mileage)
                {
                    ErrorMessage = $"El nuevo kilometraje ({nuevoKm:N0}) no puede ser menor al actual ({vehiculo.Mileage:N0})";
                    return;
                }

                if (nuevoKm == vehiculo.Mileage)
                {
                    SuccessMessage = "El kilometraje ya está actualizado";
                    return;
                }

                vehiculo.Mileage = nuevoKm;
                await _vehicleService.UpdateAsync(vehiculo.Id, vehiculo, cancellationToken);

                SuccessMessage = "Kilometraje del vehículo actualizado correctamente";
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Actualización de kilometraje cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar kilometraje del vehículo";
                _logger.LogError(ex, "Error actualizando kilometraje del vehículo");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga todas las ejecuciones de mantenimiento
        /// </summary>
        [RelayCommand]
        public async Task LoadEjecucionesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _ejecucionService.GetAllAsync(cancellationToken);
                
                Ejecuciones.Clear();
                foreach (var ejecucion in result)
                {
                    Ejecuciones.Add(ejecucion);
                }

                _logger.LogInformation("Ejecuciones de mantenimiento cargadas exitosamente");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de ejecuciones cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar las ejecuciones";
                _logger.LogError(ex, "Error cargando ejecuciones de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Carga el historial de ejecuciones para un vehículo específico
        /// </summary>
        [RelayCommand]
        public async Task LoadHistorialVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe ingresar una placa de vehículo";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                FilterPlaca = NormalizePlate(FilterPlaca);

                var result = await _ejecucionService.GetHistorialVehiculoAsync(FilterPlaca, cancellationToken);
                
                Ejecuciones.Clear();
                foreach (var ejecucion in result)
                {
                    Ejecuciones.Add(ejecucion);
                }

                var vehiculo = await _vehicleService.GetByPlateAsync(FilterPlaca, cancellationToken);
                if (vehiculo != null)
                {
                    RegistroKMAlMomentoInput = vehiculo.Mileage.ToString();
                }

                await RefreshPreventivoContextAsync(FilterPlaca, cancellationToken);

                _logger.LogInformation("Historial de ejecuciones cargado para vehículo: {Placa}", FilterPlaca);
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de historial cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al cargar historial para {FilterPlaca}";
                _logger.LogError(ex, "Error cargando historial de vehículo");
            }
            finally
            {
                IsLoading = false;
            }
        }        /// <summary>
        /// Obtiene la última ejecución de mantenimiento de un vehículo
        /// </summary>
        [RelayCommand]
        public async Task GetUltimaEjecucionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe ingresar una placa de vehículo";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                FilterPlaca = NormalizePlate(FilterPlaca);
                var historial = await _ejecucionService.GetHistorialVehiculoAsync(FilterPlaca, cancellationToken);
                var ejecucion = historial?.FirstOrDefault();
                
                if (ejecucion != null)
                {
                    SelectedEjecucion = ejecucion;
                    SuccessMessage = "Última ejecución encontrada";
                    _logger.LogInformation("Última ejecución obtenida para vehículo: {Placa}", FilterPlaca);
                }
                else
                {
                    ErrorMessage = $"No hay registros de ejecución para el vehículo {FilterPlaca}";
                }
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Búsqueda cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al obtener última ejecución de {FilterPlaca}";
                _logger.LogError(ex, "Error obteniendo última ejecución");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Selecciona una ejecución para ver/editar detalles
        /// </summary>
        [RelayCommand]
        public void SelectEjecucion(EjecucionMantenimientoDto ejecucion)
        {
            SelectedEjecucion = ejecucion;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Limpia la selección actual
        /// </summary>
        [RelayCommand]
        public void ClearSelection()
        {
            SelectedEjecucion = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            RegistroFechaEjecucion = DateTime.Today;
            RegistroKMAlMomentoInput = string.Empty;
            RegistroResponsable = string.Empty;
            RegistroObservaciones = string.Empty;
            RegistroCostoInput = string.Empty;
            PreventivoCambioAceite = false;
            PreventivoFiltroAceite = false;
            PreventivoFiltroAire = false;
            PreventivoFiltroCombustible = false;
            PreventivoRevisionGeneral = false;
            HasPlanPreventivo = false;
        }

        /// <summary>
        /// Filtra ejecuciones por rango de fechas
        /// </summary>
        [RelayCommand]
        public async Task FilterByFechasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!FilterFechaDesde.HasValue || !FilterFechaHasta.HasValue)
                {
                    ErrorMessage = "Debe especificar ambas fechas (desde y hasta)";
                    return;
                }

                if (FilterFechaDesde > FilterFechaHasta)
                {
                    ErrorMessage = "La fecha inicial no puede ser mayor que la fecha final";
                    return;
                }

                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _ejecucionService.GetAllAsync(cancellationToken);
                
                Ejecuciones.Clear();
                foreach (var ejecucion in result)
                {
                    var fechaEjecucion = ejecucion.FechaEjecucion.Date;
                    if (fechaEjecucion >= FilterFechaDesde.Value.Date && 
                        fechaEjecucion <= FilterFechaHasta.Value.Date)
                    {
                        Ejecuciones.Add(ejecucion);
                    }
                }

                _logger.LogInformation("Ejecuciones filtradas por fechas: {FechaDesde:d} a {FechaHasta:d}", FilterFechaDesde, FilterFechaHasta);
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Filtrado por fechas cancelado");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al filtrar ejecuciones por fechas";
                _logger.LogError(ex, "Error filtrando por fechas");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static string NormalizePlate(string? plate)
        {
            return string.IsNullOrWhiteSpace(plate)
                ? string.Empty
                : plate.Trim().ToUpperInvariant();
        }

        private async Task RefreshPreventivoContextAsync(string placa, CancellationToken cancellationToken)
        {
            var placaNormalizada = NormalizePlate(placa);
            if (string.IsNullOrWhiteSpace(placaNormalizada))
            {
                PlanPreventivoInfo = "Sin plan activo para preventivo";
                VentanaPreventivoInfo = "Cargue placa para validar ventana sugerida (7 días o 500 km).";
                VentanaPreventivoAdvertencia = false;
                HasPlanPreventivo = false;
                return;
            }

            var plan = await _planService.GetByPlacaAsync(placaNormalizada, cancellationToken);
            if (plan == null)
            {
                PlanPreventivoInfo = "No hay plan activo asignado para este vehículo.";
                VentanaPreventivoInfo = "Preventivo deshabilitado hasta asignar plan.";
                VentanaPreventivoAdvertencia = true;
                HasPlanPreventivo = false;
                return;
            }
            HasPlanPreventivo = true;

            var plantilla = await _plantillaService.GetByIdAsync(plan.PlantillaId, cancellationToken);
            var vehiculo = await _vehicleService.GetByPlateAsync(placaNormalizada, cancellationToken);

            var nombrePlantilla = plantilla?.Nombre ?? $"Plantilla #{plan.PlantillaId}";
            PlanPreventivoInfo = $"Plan activo: {nombrePlantilla} | Próximo: {plan.ProximaFechaEjecucion:dd/MM/yyyy} o {plan.ProximoKMEjecucion:N0} km";

            var diasRestantes = plan.ProximaFechaEjecucion.HasValue
                ? (int?)(plan.ProximaFechaEjecucion.Value.Date - DateTimeOffset.Now.Date).TotalDays
                : null;

            var kmsRestantes = (plan.ProximoKMEjecucion.HasValue && vehiculo != null)
                ? plan.ProximoKMEjecucion.Value - vehiculo.Mileage
                : (long?)null;

            var cercaPorFecha = diasRestantes.HasValue && diasRestantes.Value <= 7;
            var cercaPorKm = kmsRestantes.HasValue && kmsRestantes.Value <= 500;
            var enVentana = cercaPorFecha || cercaPorKm;

            VentanaPreventivoAdvertencia = !enVentana;

            var diasTxt = diasRestantes.HasValue ? $"{diasRestantes.Value} días" : "N/D";
            var kmTxt = kmsRestantes.HasValue ? $"{kmsRestantes.Value:N0} km" : "N/D";
            VentanaPreventivoInfo = enVentana
                ? $"Dentro de ventana sugerida. Restante aprox.: {diasTxt} o {kmTxt}."
                : $"Fuera de ventana sugerida (7 días/500 km). Restante aprox.: {diasTxt} o {kmTxt}.";
        }

        private System.Collections.Generic.List<string> BuildPreventivoChecklist()
        {
            var items = new System.Collections.Generic.List<string>();
            if (PreventivoCambioAceite) items.Add("Cambio de aceite");
            if (PreventivoFiltroAceite) items.Add("Cambio de filtro de aceite");
            if (PreventivoFiltroAire) items.Add("Cambio de filtro de aire");
            if (PreventivoFiltroCombustible) items.Add("Cambio de filtro de combustible");
            if (PreventivoRevisionGeneral) items.Add("Revisión general");
            return items;
        }

        private static string BuildPreventivoObservaciones(System.Collections.Generic.IEnumerable<string> actividades, string? detalleLibre)
        {
            var actividadesTexto = string.Join(", ", actividades);
            var detalle = string.IsNullOrWhiteSpace(detalleLibre) ? "" : detalleLibre.Trim();

            if (!string.IsNullOrWhiteSpace(actividadesTexto) && !string.IsNullOrWhiteSpace(detalle))
            {
                return $"Tipo: Preventivo | Actividades: {actividadesTexto} | Detalle: {detalle}";
            }

            if (!string.IsNullOrWhiteSpace(actividadesTexto))
            {
                return $"Tipo: Preventivo | Actividades: {actividadesTexto}";
            }

            return $"Tipo: Preventivo | Detalle: {detalle}";
        }
    }
}
