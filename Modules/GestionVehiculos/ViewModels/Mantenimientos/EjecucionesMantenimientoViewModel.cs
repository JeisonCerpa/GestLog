using System;
using System.Collections.Generic;
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
        private string registroProveedor = string.Empty;

        [ObservableProperty]
        private string registroRutaFactura = string.Empty;

        [ObservableProperty]
        private bool registroEsExtraordinario;

        [ObservableProperty]
        private string registroMotivoExtraordinario = string.Empty;

        [ObservableProperty]
        private string kmValidationMessage = string.Empty;

        [ObservableProperty]
        private string fechaValidationMessage = string.Empty;

        [ObservableProperty]
        private string responsableValidationMessage = string.Empty;

        [ObservableProperty]
        private string costoValidationMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<PlanPreventivoSelectionItem> planesPreventivoSeleccionables = new();

        [ObservableProperty]
        private string planPreventivoInfo = "Sin plan activo para preventivo";

        [ObservableProperty]
        private string ventanaPreventivoInfo = "Cargue placa para validar ventana sugerida (7 días o 500 km).";

        [ObservableProperty]
        private bool ventanaPreventivoAdvertencia;

        [ObservableProperty]
        private string confirmacionRegistroMessage = string.Empty;

    [ObservableProperty]
    private bool hasPlanPreventivo;

        public bool HasPlanesSeleccionados => PlanesPreventivoSeleccionables.Any(p => p.IsSelected);

        public string PlanesSeleccionadosResumen
        {
            get
            {
                var count = PlanesPreventivoSeleccionables.Count(p => p.IsSelected);
                return count == 0
                    ? "Selecciona uno o varios planes a registrar"
                    : $"{count} plan(es) seleccionado(s)";
            }
        }

        public string ResumenPrevioGuardado
        {
            get
            {
                var planes = PlanesPreventivoSeleccionables.Where(p => p.IsSelected).ToList();
                if (planes.Count == 0)
                {
                    return "Selecciona al menos un plan para ver el resumen previo.";
                }

                var planesTxt = string.Join(", ", planes.Select(p => p.NombrePlantilla));
                var fechaTxt = RegistroFechaEjecucion?.ToString("dd/MM/yyyy") ?? "Sin fecha";
                var kmTxt = string.IsNullOrWhiteSpace(RegistroKMAlMomentoInput) ? "Sin KM" : RegistroKMAlMomentoInput.Trim();
                var responsableTxt = string.IsNullOrWhiteSpace(RegistroResponsable) ? "Sin responsable" : RegistroResponsable.Trim();

                decimal? costo = null;
                if (decimal.TryParse(RegistroCostoInput?.Trim(), out var parsedCosto) && parsedCosto >= 0)
                {
                    costo = parsedCosto;
                }

                if (!costo.HasValue)
                {
                    var extraTxt = RegistroEsExtraordinario
                        ? $" Extraordinario: Sí. Motivo: {(string.IsNullOrWhiteSpace(RegistroMotivoExtraordinario) ? "Sin motivo" : RegistroMotivoExtraordinario.Trim())}."
                        : string.Empty;
                    return $"Se crearán {planes.Count} ejecución(es) para: {planesTxt}. Fecha: {fechaTxt}. KM: {kmTxt}. Responsable: {responsableTxt}.{extraTxt}";
                }

                if (planes.Count == 1)
                {
                    var extraTxt = RegistroEsExtraordinario
                        ? $" Extraordinario: Sí. Motivo: {(string.IsNullOrWhiteSpace(RegistroMotivoExtraordinario) ? "Sin motivo" : RegistroMotivoExtraordinario.Trim())}."
                        : string.Empty;
                    return $"Se creará 1 ejecución para: {planesTxt}. Fecha: {fechaTxt}. KM: {kmTxt}. Responsable: {responsableTxt}. Costo: ${costo.Value:N0}.{extraTxt}";
                }

                var costoProrrateado = Math.Round(costo.Value / planes.Count, 2);
                var extraTxtFinal = RegistroEsExtraordinario
                    ? $" Extraordinario: Sí. Motivo: {(string.IsNullOrWhiteSpace(RegistroMotivoExtraordinario) ? "Sin motivo" : RegistroMotivoExtraordinario.Trim())}."
                    : string.Empty;
                return $"Se crearán {planes.Count} ejecución(es) para: {planesTxt}. Fecha: {fechaTxt}. KM: {kmTxt}. Responsable: {responsableTxt}. Costo total: ${costo.Value:N0} (prorrateado aprox. ${costoProrrateado:N2} por plan).{extraTxtFinal}";
            }
        }

        public string RegistrarPreventivoButtonText
        {
            get
            {
                var count = PlanesPreventivoSeleccionables.Count(p => p.IsSelected);
                return count <= 1
                    ? "Registrar 1 preventivo"
                    : $"Registrar {count} preventivos";
            }
        }

        private void ClearInlineValidationMessages()
        {
            KmValidationMessage = string.Empty;
            FechaValidationMessage = string.Empty;
            ResponsableValidationMessage = string.Empty;
            CostoValidationMessage = string.Empty;
        }

        partial void OnRegistroKMAlMomentoInputChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroFechaEjecucionChanged(DateTime? value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroResponsableChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroCostoInputChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroProveedorChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroRutaFacturaChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroEsExtraordinarioChanged(bool value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));
        partial void OnRegistroMotivoExtraordinarioChanged(string value) => OnPropertyChanged(nameof(ResumenPrevioGuardado));

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
                ConfirmacionRegistroMessage = string.Empty;
                ClearInlineValidationMessages();

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe indicar la placa del vehículo";
                    return;
                }

                var placa = NormalizePlate(FilterPlaca);
                if (!long.TryParse(RegistroKMAlMomentoInput?.Trim(), out var kmAlMomento) || kmAlMomento <= 0)
                {
                    KmValidationMessage = "Ingresa un kilometraje válido mayor que cero.";
                    ErrorMessage = "Debe ingresar un KM al momento válido";
                    return;
                }

                if (!RegistroFechaEjecucion.HasValue)
                {
                    FechaValidationMessage = "Selecciona la fecha de ejecución.";
                    ErrorMessage = "Debe seleccionar una fecha de ejecución";
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegistroResponsable))
                {
                    ResponsableValidationMessage = "Ingresa el responsable de la ejecución.";
                    ErrorMessage = "Debe indicar un responsable";
                    return;
                }

                if (RegistroEsExtraordinario && string.IsNullOrWhiteSpace(RegistroMotivoExtraordinario))
                {
                    ErrorMessage = "Debe indicar el motivo del preventivo extraordinario";
                    return;
                }

                var vehiculoActual = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculoActual != null && kmAlMomento < vehiculoActual.Mileage)
                {
                    KmValidationMessage = $"El KM no puede ser menor al actual ({vehiculoActual.Mileage:N0}).";
                    ErrorMessage = $"El KM registrado ({kmAlMomento:N0}) no puede ser menor al KM actual del vehículo ({vehiculoActual.Mileage:N0})";
                    return;
                }

                decimal? costo = null;
                if (!string.IsNullOrWhiteSpace(RegistroCostoInput))
                {
                    if (!decimal.TryParse(RegistroCostoInput.Trim(), out var parsedCosto) || parsedCosto < 0)
                    {
                        CostoValidationMessage = "Ingresa un costo válido mayor o igual a cero.";
                        ErrorMessage = "El costo debe ser un valor numérico válido";
                        return;
                    }

                    costo = parsedCosto;
                }

                IsLoading = true;

                var planesSeleccionados = PlanesPreventivoSeleccionables.Where(p => p.IsSelected).ToList();
                if (planesSeleccionados.Count == 0)
                {
                    ErrorMessage = "Debe seleccionar al menos un plan preventivo";
                    return;
                }

                var fechaEjecucion = RegistroFechaEjecucion.HasValue
                    ? new DateTimeOffset(RegistroFechaEjecucion.Value.Date)
                    : DateTimeOffset.Now;

                var costosPorPlan = BuildCostDistribution(costo, planesSeleccionados.Count);
                var nombresPlanesSeleccionados = planesSeleccionados.Select(p => p.NombrePlantilla).ToList();

                for (var index = 0; index < planesSeleccionados.Count; index++)
                {
                    var planSeleccionado = planesSeleccionados[index];
                    var proveedorAplicado = !string.IsNullOrWhiteSpace(planSeleccionado.ProveedorOpcional)
                        ? planSeleccionado.ProveedorOpcional.Trim()
                        : (string.IsNullOrWhiteSpace(RegistroProveedor) ? null : RegistroProveedor.Trim());

                    var rutaFacturaAplicada = !string.IsNullOrWhiteSpace(planSeleccionado.RutaFacturaOpcional)
                        ? planSeleccionado.RutaFacturaOpcional.Trim()
                        : (string.IsNullOrWhiteSpace(RegistroRutaFactura) ? null : RegistroRutaFactura.Trim());

                    var observacionesPreventivo = BuildPreventivoObservaciones(
                        RegistroObservaciones,
                        planSeleccionado.DetalleOpcional,
                        planSeleccionado.NombrePlantilla,
                        costo,
                        costosPorPlan[index],
                        nombresPlanesSeleccionados,
                        proveedorAplicado,
                        rutaFacturaAplicada,
                        RegistroEsExtraordinario,
                        RegistroMotivoExtraordinario);

                    var dto = new EjecucionMantenimientoDto
                    {
                        PlacaVehiculo = placa,
                        PlanMantenimientoId = planSeleccionado.PlanId,
                        FechaEjecucion = fechaEjecucion,
                        KMAlMomento = kmAlMomento,
                        ObservacionesTecnico = observacionesPreventivo,
                        Costo = costosPorPlan[index],
                        ResponsableEjecucion = string.IsNullOrWhiteSpace(RegistroResponsable) ? null : RegistroResponsable.Trim(),
                        Proveedor = proveedorAplicado,
                        RutaFactura = rutaFacturaAplicada,
                        TipoMantenimiento = (int)Models.Enums.TipoMantenimientoVehiculo.Preventivo,
                        EsExtraordinario = RegistroEsExtraordinario,
                        Estado = 2
                    };

                    await _ejecucionService.CreateAsync(dto, cancellationToken);

                    var planActualizado = new PlanMantenimientoVehiculoDto
                    {
                        Id = planSeleccionado.PlanId,
                        PlacaVehiculo = placa,
                        PlantillaId = planSeleccionado.PlantillaId,
                        IntervaloKMPersonalizado = planSeleccionado.IntervaloKMPersonalizado,
                        IntervaloDiasPersonalizado = planSeleccionado.IntervaloDiasPersonalizado,
                        FechaInicio = planSeleccionado.FechaInicio,
                        UltimoKMRegistrado = kmAlMomento,
                        UltimaFechaMantenimiento = fechaEjecucion,
                        FechaFin = planSeleccionado.FechaFin,
                        Activo = true
                    };

                    await _planService.UpdateAsync(planSeleccionado.PlanId, planActualizado, cancellationToken);
                }

                var vehiculo = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
                if (vehiculo != null && kmAlMomento > vehiculo.Mileage)
                {
                    vehiculo.Mileage = kmAlMomento;
                    await _vehicleService.UpdateAsync(vehiculo.Id, vehiculo, cancellationToken);
                }

                await LoadHistorialVehiculoAsync(cancellationToken);

                foreach (var planItem in PlanesPreventivoSeleccionables)
                {
                    planItem.IsSelected = false;
                    planItem.DetalleOpcional = string.Empty;
                    planItem.ProveedorOpcional = string.Empty;
                    planItem.RutaFacturaOpcional = string.Empty;
                }
                RegistroObservaciones = string.Empty;
                RegistroResponsable = string.Empty;
                RegistroCostoInput = string.Empty;
                RegistroProveedor = string.Empty;
                RegistroRutaFactura = string.Empty;
                RegistroEsExtraordinario = false;
                RegistroMotivoExtraordinario = string.Empty;

                var planesTxt = string.Join(", ", planesSeleccionados.Select(p => p.NombrePlantilla));
                SuccessMessage = VentanaPreventivoAdvertencia
                    ? $"Se registraron {planesSeleccionados.Count} preventivo(s): {planesTxt}. Advertencia: fuera de ventana sugerida (7 días / 500 km)."
                    : $"Se registraron {planesSeleccionados.Count} preventivo(s): {planesTxt}.";

                ConfirmacionRegistroMessage = BuildConfirmacionRegistroMessage(
                    planesSeleccionados.Select(p => p.NombrePlantilla),
                    costo,
                    costosPorPlan,
                    VentanaPreventivoAdvertencia,
                    RegistroEsExtraordinario,
                    RegistroMotivoExtraordinario);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = System.Windows.Application.Current.Windows
                        .OfType<System.Windows.Window>()
                        .FirstOrDefault(w => ReferenceEquals(w.DataContext, this));

                    if (dialog != null)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });
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
        public async Task LoadByPlanAsync(int planId, CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var result = await _ejecucionService.GetByPlanAsync(planId, cancellationToken);
                Ejecuciones.Clear();
                foreach (var ejec in result)
                {
                    Ejecuciones.Add(ejec);
                }

                SuccessMessage = $"Historial cargado para plan #{planId}";
                _logger.LogInformation("Ejecuciones cargadas por plan {PlanId}", planId);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar ejecuciones por plan";
                _logger.LogError(ex, "Error cargando ejecuciones por plan {PlanId}", planId);
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
        /// Llena los nombres de los planes asociados a un conjunto de ejecuciones.
        /// </summary>
        private async Task EnrichPlanNamesAsync(IEnumerable<EjecucionMantenimientoDto> ejecuciones, CancellationToken cancellationToken = default)
        {
            var ids = ejecuciones
                .Where(e => e.PlanMantenimientoId.HasValue)
                .Select(e => e.PlanMantenimientoId!.Value)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                return;

            var cache = new Dictionary<int, string?>();
            foreach (var id in ids)
            {
                try
                {
                    var plan = await _planService.GetByIdAsync(id, cancellationToken);
                    cache[id] = plan?.PlantillaNombre;
                }
                catch
                {
                    cache[id] = null;
                }
            }

            foreach (var ejec in ejecuciones.Where(e => e.PlanMantenimientoId.HasValue))
            {
                if (cache.TryGetValue(ejec.PlanMantenimientoId!.Value, out var nombre))
                {
                    ejec.PlanNombre = nombre;
                }
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
                
                // poblar nombre del plan en cada ejecución
                await EnrichPlanNamesAsync(result, cancellationToken);

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
                
                // poblar nombre del plan
                await EnrichPlanNamesAsync(result, cancellationToken);

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
            ConfirmacionRegistroMessage = string.Empty;
            RegistroFechaEjecucion = DateTime.Today;
            RegistroKMAlMomentoInput = string.Empty;
            RegistroResponsable = string.Empty;
            RegistroObservaciones = string.Empty;
            RegistroCostoInput = string.Empty;
            RegistroProveedor = string.Empty;
            RegistroRutaFactura = string.Empty;
            RegistroEsExtraordinario = false;
            RegistroMotivoExtraordinario = string.Empty;
            PlanesPreventivoSeleccionables.Clear();
            HasPlanPreventivo = false;
            ClearInlineValidationMessages();
        }

        /// <summary>
        /// Elimina una ejecución (soft delete) y la remueve de la colección
        /// </summary>
        [RelayCommand]
        public async Task DeleteEjecucionAsync(EjecucionMantenimientoDto ejecucion, CancellationToken cancellationToken = default)
        {
            if (ejecucion == null)
                return;
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                await _ejecucionService.DeleteAsync(ejecucion.Id, cancellationToken);
                Ejecuciones.Remove(ejecucion);
                SuccessMessage = "Ejecución eliminada correctamente.";
                _logger.LogInformation("Ejecución eliminada desde vista: {Id}", ejecucion.Id);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar la ejecución.";
                _logger.LogError(ex, "Error eliminando ejecución {Id}", ejecucion.Id);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Actualiza una ejecución ya existente en el backend.
        /// </summary>
        [RelayCommand]
        public async Task UpdateEjecucionAsync(EjecucionMantenimientoDto ejecucion, CancellationToken cancellationToken = default)
        {
            if (ejecucion == null)
                return;
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _ejecucionService.UpdateAsync(ejecucion.Id, ejecucion, cancellationToken);
                SuccessMessage = "Ejecución actualizada correctamente.";
                _logger.LogInformation("Ejecución actualizada desde vista: {Id}", ejecucion.Id);

                // reload the current list so that any bound DataGrid reflects the change
                if (!string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    await LoadHistorialVehiculoAsync(cancellationToken);
                }
                else
                {
                    await LoadEjecucionesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar la ejecución.";
                _logger.LogError(ex, "Error actualizando ejecución {Id}", ejecucion.Id);
            }
            finally
            {
                IsLoading = false;
            }
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

            var planes = (await _planService.GetByPlacaListAsync(placaNormalizada, cancellationToken)).ToList();
            if (planes.Count == 0)
            {
                PlanPreventivoInfo = "No hay plan activo asignado para este vehículo.";
                VentanaPreventivoInfo = "Preventivo deshabilitado hasta asignar plan.";
                VentanaPreventivoAdvertencia = true;
                HasPlanPreventivo = false;
                PlanesPreventivoSeleccionables.Clear();
                OnPropertyChanged(nameof(PlanesSeleccionadosResumen));
                OnPropertyChanged(nameof(HasPlanesSeleccionados));
                return;
            }
            HasPlanPreventivo = true;
            var vehiculo = await _vehicleService.GetByPlateAsync(placaNormalizada, cancellationToken);

            var planesOrdenados = planes
                .OrderBy(p => GetEstadoPrioridad(p.EstadoPlan))
                .ThenBy(p => p.ProximaFechaEjecucion ?? DateTimeOffset.MaxValue)
                .ThenBy(p => p.ProximoKMEjecucion ?? long.MaxValue)
                .ToList();

            PlanesPreventivoSeleccionables.Clear();
            foreach (var plan in planesOrdenados)
            {
                var nombrePlan = !string.IsNullOrWhiteSpace(plan.PlantillaNombre)
                    ? plan.PlantillaNombre!
                    : $"Plantilla #{plan.PlantillaId}";

                var item = new PlanPreventivoSelectionItem
                {
                    PlanId = plan.Id,
                    PlantillaId = plan.PlantillaId,
                    NombrePlantilla = nombrePlan,
                    ProximaFechaEjecucion = plan.ProximaFechaEjecucion,
                    ProximoKMEjecucion = plan.ProximoKMEjecucion,
                    EstadoPlan = plan.EstadoPlan ?? "N/D",
                    IntervaloKMPersonalizado = plan.IntervaloKMPersonalizado,
                    IntervaloDiasPersonalizado = plan.IntervaloDiasPersonalizado,
                    FechaInicio = plan.FechaInicio,
                    FechaFin = plan.FechaFin,
                    IsSelected = false
                };

                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PlanPreventivoSelectionItem.IsSelected)
                        || e.PropertyName == nameof(PlanPreventivoSelectionItem.DetalleOpcional))
                    {
                        OnPropertyChanged(nameof(HasPlanesSeleccionados));
                        OnPropertyChanged(nameof(PlanesSeleccionadosResumen));
                        OnPropertyChanged(nameof(RegistrarPreventivoButtonText));
                        OnPropertyChanged(nameof(ResumenPrevioGuardado));
                    }
                };

                PlanesPreventivoSeleccionables.Add(item);
            }

            var planResumen = string.Join(" | ", PlanesPreventivoSeleccionables
                .Take(3)
                .Select(p => p.NombrePlantilla));
            PlanPreventivoInfo = $"Planes disponibles: {PlanesPreventivoSeleccionables.Count}. {planResumen}";

            var now = DateTimeOffset.Now.Date;
            var diasMin = planes
                .Where(p => p.ProximaFechaEjecucion.HasValue)
                .Select(p => (int?)(p.ProximaFechaEjecucion!.Value.Date - now).TotalDays)
                .Where(v => v.HasValue)
                .DefaultIfEmpty(null)
                .Min();

            var kmMin = planes
                .Where(p => p.ProximoKMEjecucion.HasValue && vehiculo != null)
                .Select(p => (long?)(p.ProximoKMEjecucion!.Value - vehiculo!.Mileage))
                .Where(v => v.HasValue)
                .DefaultIfEmpty(null)
                .Min();

            var cercaPorFecha = diasMin.HasValue && diasMin.Value <= 7;
            var cercaPorKm = kmMin.HasValue && kmMin.Value <= 500;
            var enVentana = cercaPorFecha || cercaPorKm;

            VentanaPreventivoAdvertencia = !enVentana;

            var diasTxt = diasMin.HasValue ? $"{diasMin.Value} días" : "N/D";
            var kmTxt = kmMin.HasValue ? $"{kmMin.Value:N0} km" : "N/D";
            VentanaPreventivoInfo = enVentana
                ? $"Dentro de ventana sugerida. Restante mínimo aprox.: {diasTxt} o {kmTxt}."
                : $"Fuera de ventana sugerida (7 días/500 km). Restante mínimo aprox.: {diasTxt} o {kmTxt}.";

            OnPropertyChanged(nameof(HasPlanesSeleccionados));
            OnPropertyChanged(nameof(PlanesSeleccionadosResumen));
            OnPropertyChanged(nameof(RegistrarPreventivoButtonText));
            OnPropertyChanged(nameof(ResumenPrevioGuardado));
        }

        private static int GetEstadoPrioridad(string? estado)
        {
            return estado switch
            {
                "Vencido" => 0,
                "Próximo" => 1,
                "Vigente" => 2,
                _ => 3
            };
        }

        private static string BuildPreventivoObservaciones(string? detalleGeneral, string? detallePlan, string nombrePlan)
        {
            return BuildPreventivoObservaciones(detalleGeneral, detallePlan, nombrePlan, null, null, Array.Empty<string>(), null, null, false, null);
        }

        private static string BuildPreventivoObservaciones(
            string? detalleGeneral,
            string? detallePlan,
            string nombrePlan,
            decimal? costoTotalServicio,
            decimal? costoAsignadoRegistro,
            IReadOnlyCollection<string> planesDelServicio,
            string? proveedorAplicado,
            string? rutaFacturaAplicada,
            bool esExtraordinario,
            string? motivoExtraordinario)
        {
            var parts = new List<string>
            {
                "Tipo: Preventivo",
                $"Plan: {nombrePlan}"
            };

            var general = string.IsNullOrWhiteSpace(detalleGeneral) ? string.Empty : detalleGeneral.Trim();
            var detalle = string.IsNullOrWhiteSpace(detallePlan) ? string.Empty : detallePlan.Trim();

            if (!string.IsNullOrWhiteSpace(general))
            {
                parts.Add($"General: {general}");
            }

            if (!string.IsNullOrWhiteSpace(detalle))
            {
                parts.Add($"Detalle plan: {detalle}");
            }

            if (planesDelServicio.Count > 1)
            {
                parts.Add($"Servicio conjunto con planes: {string.Join(", ", planesDelServicio)}");
            }

            if (costoTotalServicio.HasValue)
            {
                parts.Add($"Costo total servicio: ${costoTotalServicio.Value:N0}");

                if (planesDelServicio.Count > 1 && costoAsignadoRegistro.HasValue)
                {
                    parts.Add($"Costo aplicado a este registro (prorrateado): ${costoAsignadoRegistro.Value:N2}");
                }
            }

            if (!string.IsNullOrWhiteSpace(proveedorAplicado))
            {
                parts.Add($"Proveedor: {proveedorAplicado.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(rutaFacturaAplicada))
            {
                parts.Add($"Factura: {rutaFacturaAplicada.Trim()}");
            }

            if (esExtraordinario)
            {
                parts.Add("Preventivo extraordinario: Sí");
                if (!string.IsNullOrWhiteSpace(motivoExtraordinario))
                {
                    parts.Add($"Motivo extraordinario: {motivoExtraordinario.Trim()}");
                }
            }

            return string.Join(" | ", parts);
        }

        private static string BuildConfirmacionRegistroMessage(
            IEnumerable<string> planes,
            decimal? costoTotalServicio,
            IReadOnlyList<decimal?> costosPorPlan,
            bool conAdvertenciaVentana,
            bool esExtraordinario,
            string? motivoExtraordinario)
        {
            var listaPlanes = planes.ToList();
            var planesTxt = string.Join(", ", listaPlanes);
            var total = listaPlanes.Count;

            var mensajeBase = $"Se registraron {total} preventivo(s): {planesTxt}.";
            if (esExtraordinario)
            {
                var motivo = string.IsNullOrWhiteSpace(motivoExtraordinario) ? "Sin motivo" : motivoExtraordinario.Trim();
                mensajeBase += $" Extraordinario: Sí ({motivo}).";
            }
            if (!costoTotalServicio.HasValue)
            {
                return conAdvertenciaVentana
                    ? $"{mensajeBase} Nota: fuera de ventana sugerida (7 días / 500 km)."
                    : mensajeBase;
            }

            if (total <= 1)
            {
                var simple = $"{mensajeBase} Costo total servicio: ${costoTotalServicio.Value:N0}.";
                return conAdvertenciaVentana
                    ? $"{simple} Nota: fuera de ventana sugerida (7 días / 500 km)."
                    : simple;
            }

            var costoPromedio = costosPorPlan.Where(c => c.HasValue).Select(c => c!.Value).DefaultIfEmpty(0m).Average();
            var prorrateado = $"{mensajeBase} Costo total servicio: ${costoTotalServicio.Value:N0} (prorrateado entre planes, aprox. ${costoPromedio:N2} por registro).";
            return conAdvertenciaVentana
                ? $"{prorrateado} Nota: fuera de ventana sugerida (7 días / 500 km)."
                : prorrateado;
        }

        private static List<decimal?> BuildCostDistribution(decimal? totalCost, int plansCount)
        {
            var result = new List<decimal?>();
            if (!totalCost.HasValue || plansCount <= 0)
            {
                for (var i = 0; i < plansCount; i++)
                {
                    result.Add(null);
                }
                return result;
            }

            if (plansCount == 1)
            {
                result.Add(totalCost.Value);
                return result;
            }

            var perPlan = Math.Round(totalCost.Value / plansCount, 2, MidpointRounding.AwayFromZero);
            var accumulated = 0m;

            for (var i = 0; i < plansCount; i++)
            {
                if (i < plansCount - 1)
                {
                    result.Add(perPlan);
                    accumulated += perPlan;
                }
                else
                {
                    result.Add(totalCost.Value - accumulated);
                }
            }

            return result;
        }

        public partial class PlanPreventivoSelectionItem : ObservableObject
        {
            public int PlanId { get; set; }
            public int PlantillaId { get; set; }
            public string NombrePlantilla { get; set; } = string.Empty;
            public DateTimeOffset? ProximaFechaEjecucion { get; set; }
            public long? ProximoKMEjecucion { get; set; }
            public string EstadoPlan { get; set; } = string.Empty;
            public int? IntervaloKMPersonalizado { get; set; }
            public int? IntervaloDiasPersonalizado { get; set; }
            public DateTimeOffset FechaInicio { get; set; }
            public DateTimeOffset? FechaFin { get; set; }
            public string EstadoVisual => EstadoPlan switch
            {
                "Vencido" => "🔴 Vencido",
                "Próximo" => "🟡 Próximo",
                "Vigente" => "🟢 Vigente",
                _ => "⚪ Sin estado"
            };

            [ObservableProperty]
            private bool isSelected;

            [ObservableProperty]
            private string detalleOpcional = string.Empty;

            [ObservableProperty]
            private string proveedorOpcional = string.Empty;

            [ObservableProperty]
            private string rutaFacturaOpcional = string.Empty;

            public bool HasDetalleOpcional => !string.IsNullOrWhiteSpace(DetalleOpcional);
            public bool HasProveedorOpcional => !string.IsNullOrWhiteSpace(ProveedorOpcional);
            public bool HasFacturaAdjunta => !string.IsNullOrWhiteSpace(RutaFacturaOpcional);

            partial void OnDetalleOpcionalChanged(string value)
            {
                OnPropertyChanged(nameof(HasDetalleOpcional));
            }

            partial void OnProveedorOpcionalChanged(string value)
            {
                OnPropertyChanged(nameof(HasProveedorOpcional));
            }

            partial void OnRutaFacturaOpcionalChanged(string value)
            {
                OnPropertyChanged(nameof(HasFacturaAdjunta));
            }
        }
    }
}
