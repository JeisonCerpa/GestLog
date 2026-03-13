using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Services.Utilities;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos
{
    /// <summary>
    /// ViewModel para listar mantenimientos correctivos de un vehículo.
    /// </summary>
    public partial class CorrectivosMantenimientoViewModel : ObservableObject
    {
        public class PlanPreventivoCostoInput
        {
            public int PlanId { get; set; }
            public decimal CostoAsignado { get; set; }
            public bool EsCostoPersonalizado { get; set; }
            // ruta del archivo de factura asociado cuando el costo es personalizado
            public string FacturaRuta { get; set; } = string.Empty;
            // información adicional de nota
            public string DetalleOpcional { get; set; } = string.Empty;
            public string ProveedorOpcional { get; set; } = string.Empty;
            public string RutaFacturaOpcional { get; set; } = string.Empty;
            public string CostoOpcionalInput { get; set; } = string.Empty;
        }

        private readonly IEjecucionMantenimientoService _ejecucionService;
        private readonly IPlanMantenimientoVehiculoService _planService;
        private readonly IVehicleService _vehicleService;
        private readonly IVehicleDocumentService _vehicleDocumentService;
        private readonly IGestLogLogger _logger;

        public event Action? RegistroCorrectivoExitoso;

        [ObservableProperty]
        private ObservableCollection<EjecucionMantenimientoDto> correctivos = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string filterPlaca = string.Empty;

        [ObservableProperty]
        private EjecucionMantenimientoDto? selectedCorrectivo;

        [ObservableProperty]
        private string registroTituloActividad = string.Empty;

        [ObservableProperty]
        private DateTime? registroFechaEjecucion = DateTime.Today;

        [ObservableProperty]
        private string registroKMAlMomentoInput = string.Empty;

        [ObservableProperty]
        private string registroResponsable = string.Empty;

        [ObservableProperty]
        private string registroProveedor = string.Empty;

        [ObservableProperty]
        private string registroCostoInput = string.Empty;

        [ObservableProperty]
        private string registroObservaciones = string.Empty;

        [ObservableProperty]
        private string registroRutaFactura = string.Empty;

        [ObservableProperty]
        private EstadoMantenimientoCorrectivoVehiculo registroEstadoCorrectivo = EstadoMantenimientoCorrectivoVehiculo.FallaReportada;

        [ObservableProperty]
        private string registroDescripcionFalla = string.Empty;

        [ObservableProperty]
        private List<EjecucionMantenimientoItemGastoDto> registroItemsGasto = new();

        public CorrectivosMantenimientoViewModel(
            IEjecucionMantenimientoService ejecucionService,
            IPlanMantenimientoVehiculoService planService,
            IVehicleService vehicleService,
            IVehicleDocumentService vehicleDocumentService,
            IGestLogLogger logger)
        {
            _ejecucionService = ejecucionService;
            _planService = planService;
            _vehicleService = vehicleService;
            _vehicleDocumentService = vehicleDocumentService;
            _logger = logger;
        }

        public async Task<IReadOnlyCollection<PlanMantenimientoVehiculoDto>> GetPlanesPreventivosParaCompletarAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(placaVehiculo))
            {
                return Array.Empty<PlanMantenimientoVehiculoDto>();
            }

            var planes = await _planService.GetByPlacaListAsync(placaVehiculo.Trim().ToUpperInvariant(), cancellationToken);
            // mostrar todos los planes del vehículo (no sólo los activos) para permitir selección completa
            return planes
                .OrderBy(p => p.PlantillaNombre)
                .ToList();
        }

        [RelayCommand]
        public void PrepararNuevoCorrectivo()
        {
            RegistroTituloActividad = string.Empty;
            RegistroFechaEjecucion = DateTime.Today;
            RegistroKMAlMomentoInput = string.Empty;
            RegistroResponsable = string.Empty;
            RegistroProveedor = string.Empty;
            RegistroCostoInput = string.Empty;
            RegistroDescripcionFalla = string.Empty;
            RegistroObservaciones = string.Empty;
            RegistroRutaFactura = string.Empty;
            RegistroEstadoCorrectivo = EstadoMantenimientoCorrectivoVehiculo.FallaReportada;
            RegistroItemsGasto = new List<EjecucionMantenimientoItemGastoDto>();
            ErrorMessage = string.Empty;
        }

        [RelayCommand]
        public async Task LoadCorrectivosVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe ingresar la placa del vehículo";
                    return;
                }

                IsLoading = true;

                var result = await _ejecucionService.GetByPlacaAndTipoAsync(
                    FilterPlaca.Trim().ToUpperInvariant(),
                    (int)TipoMantenimientoVehiculo.Correctivo,
                    cancellationToken);

                Correctivos.Clear();
                foreach (var item in result)
                {
                    Correctivos.Add(item);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar mantenimientos correctivos";
                _logger.LogError(ex, "Error cargando correctivos para placa: {Placa}", FilterPlaca);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RegistrarCorrectivoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(FilterPlaca))
                {
                    ErrorMessage = "Debe indicar la placa del vehículo";
                    return;
                }

                if (string.IsNullOrWhiteSpace(RegistroDescripcionFalla))
                {
                    ErrorMessage = "Debe ingresar la descripción de la falla";
                    return;
                }

                if (!RegistroFechaEjecucion.HasValue)
                {
                    ErrorMessage = "Debe seleccionar la fecha de la falla";
                    return;
                }

                if (!long.TryParse(RegistroKMAlMomentoInput?.Trim(), out var kmAlMomento) || kmAlMomento <= 0)
                {
                    ErrorMessage = "Debe ingresar un kilometraje válido";
                    return;
                }

                IsLoading = true;

                var descripcionFalla = RegistroDescripcionFalla.Trim();
                var tituloAuto = descripcionFalla.Length > 80
                    ? $"Falla reportada: {descripcionFalla[..80]}..."
                    : $"Falla reportada: {descripcionFalla}";

                var itemsGasto = RegistroItemsGasto
                    .Select(item => new EjecucionMantenimientoItemGastoDto
                    {
                        TipoGasto = item.TipoGasto,
                        Descripcion = item.Descripcion,
                        Proveedor = item.Proveedor,
                        Valor = item.Valor,
                        NumeroFactura = item.NumeroFactura,
                        RutaFactura = item.RutaFactura,
                        FechaDocumento = item.FechaDocumento ?? new DateTimeOffset(RegistroFechaEjecucion.Value.Date)
                    })
                    .ToList();

                var dto = new EjecucionMantenimientoDto
                {
                    PlacaVehiculo = FilterPlaca.Trim().ToUpperInvariant(),
                    PlanMantenimientoId = null,
                    TipoMantenimiento = (int)TipoMantenimientoVehiculo.Correctivo,
                    TituloActividad = tituloAuto,
                    FechaEjecucion = new DateTimeOffset(RegistroFechaEjecucion.Value.Date),
                    KMAlMomento = kmAlMomento,
                    ObservacionesTecnico = ObservacionesCorrectivoTimelineService.RegistrarFalla(
                        null,
                        descripcionFalla,
                        RegistroFechaEjecucion),
                    Costo = itemsGasto.Count > 0 ? decimal.Round(itemsGasto.Sum(x => x.Valor), 2) : null,
                    RutaFactura = string.IsNullOrWhiteSpace(RegistroRutaFactura) ? null : RegistroRutaFactura.Trim(),
                    ResponsableEjecucion = null,
                    Proveedor = null,
                    EstadoCorrectivo = (int)EstadoMantenimientoCorrectivoVehiculo.FallaReportada,
                    Estado = (int)MapEstadoGeneralFromCorrectivo(EstadoMantenimientoCorrectivoVehiculo.FallaReportada),
                    ItemsGasto = itemsGasto
                };

                await _ejecucionService.CreateAsync(dto, cancellationToken);
                await UpdateVehicleMileageIfHigherAsync(dto.PlacaVehiculo, kmAlMomento, cancellationToken);
                await EnsureFacturaDocumentAsync(dto.PlacaVehiculo, dto.RutaFactura, dto.FechaEjecucion, cancellationToken);
                await LoadCorrectivosVehiculoAsync(cancellationToken);
                RegistroCorrectivoExitoso?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al registrar mantenimiento correctivo";
                _logger.LogError(ex, "Error registrando correctivo para placa: {Placa}", FilterPlaca);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AvanzarEstadoCorrectivoAsync(EjecucionMantenimientoDto? correctivo, CancellationToken cancellationToken = default)
        {
            if (correctivo == null)
            {
                return;
            }

            try
            {
                var estadoActual = correctivo.EstadoCorrectivoEnum ?? EstadoMantenimientoCorrectivoVehiculo.FallaReportada;
                var siguiente = estadoActual switch
                {
                    EstadoMantenimientoCorrectivoVehiculo.FallaReportada => EstadoMantenimientoCorrectivoVehiculo.EnTaller,
                    EstadoMantenimientoCorrectivoVehiculo.EnTaller => EstadoMantenimientoCorrectivoVehiculo.Completado,
                    EstadoMantenimientoCorrectivoVehiculo.Completado => EstadoMantenimientoCorrectivoVehiculo.Completado,
                    EstadoMantenimientoCorrectivoVehiculo.Cancelado => EstadoMantenimientoCorrectivoVehiculo.Cancelado,
                    _ => EstadoMantenimientoCorrectivoVehiculo.EnTaller
                };

                if (siguiente == estadoActual)
                {
                    return;
                }

                correctivo.EstadoCorrectivoEnum = siguiente;
                correctivo.Estado = (int)MapEstadoGeneralFromCorrectivo(siguiente);

                await _ejecucionService.UpdateAsync(correctivo.Id, correctivo, cancellationToken);
                await LoadCorrectivosVehiculoAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar estado del correctivo";
                _logger.LogError(ex, "Error avanzando estado de correctivo: {Id}", correctivo.Id);
            }
        }

        public async Task EnviarAReparacionAsync(EjecucionMantenimientoDto correctivo, string proveedor, string? observaciones, CancellationToken cancellationToken = default)
        {
            if (correctivo == null || string.IsNullOrWhiteSpace(proveedor))
            {
                ErrorMessage = "Debe indicar proveedor para enviar a reparación";
                return;
            }

            try
            {
                correctivo.Proveedor = proveedor.Trim();
                correctivo.EstadoCorrectivoEnum = EstadoMantenimientoCorrectivoVehiculo.EnTaller;
                correctivo.Estado = (int)MapEstadoGeneralFromCorrectivo(EstadoMantenimientoCorrectivoVehiculo.EnTaller);
                correctivo.ObservacionesTecnico = ObservacionesCorrectivoTimelineService.EnviarAReparacion(
                    correctivo.ObservacionesTecnico,
                    proveedor,
                    observaciones);

                await _ejecucionService.UpdateAsync(correctivo.Id, correctivo, cancellationToken);
                await LoadCorrectivosVehiculoAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al enviar a reparación";
                _logger.LogError(ex, "Error enviando correctivo a reparación: {Id}", correctivo.Id);
            }
        }

        public async Task CompletarCorrectivoAsync(
            EjecucionMantenimientoDto correctivo,
            long kilometrajeAlCompletar,
            string? responsable,
            string? proveedor,
            decimal? costo,
            string? rutaFactura,
            string? observaciones,
            string? tituloActividad,
            IReadOnlyCollection<EjecucionMantenimientoItemGastoDto>? itemsGasto,
            IReadOnlyCollection<PlanMantenimientoVehiculoDto>? planesPreventivosEjecutados,
            IReadOnlyCollection<PlanPreventivoCostoInput>? planesPreventivosConCosto,
            CancellationToken cancellationToken = default)
        {
            if (correctivo == null)
            {
                return;
            }

            try
            {
                correctivo.KMAlMomento = kilometrajeAlCompletar;

                if (!string.IsNullOrWhiteSpace(responsable))
                {
                    correctivo.ResponsableEjecucion = responsable.Trim();
                }

                if (!string.IsNullOrWhiteSpace(proveedor))
                {
                    correctivo.Proveedor = proveedor.Trim();
                }

                correctivo.Costo = costo;
                correctivo.RutaFactura = string.IsNullOrWhiteSpace(rutaFactura) ? null : rutaFactura.Trim();
                correctivo.ItemsGasto = itemsGasto?.ToList() ?? new List<EjecucionMantenimientoItemGastoDto>();
                if (!string.IsNullOrWhiteSpace(tituloActividad))
                {
                    correctivo.TituloActividad = tituloActividad.Trim();
                }
                correctivo.EstadoCorrectivoEnum = EstadoMantenimientoCorrectivoVehiculo.Completado;
                correctivo.Estado = (int)MapEstadoGeneralFromCorrectivo(EstadoMantenimientoCorrectivoVehiculo.Completado);

                var planesTexto = planesPreventivosEjecutados != null && planesPreventivosEjecutados.Count > 0
                    ? string.Join(", ", planesPreventivosEjecutados.Select(p => p.PlantillaNombre ?? $"Plan #{p.Id}"))
                    : null;

                var observacionesCompletado = observaciones;
                if (!string.IsNullOrWhiteSpace(planesTexto))
                {
                    observacionesCompletado = string.IsNullOrWhiteSpace(observacionesCompletado)
                        ? $"Planes preventivos ejecutados: {planesTexto}"
                        : $"{observacionesCompletado}. Planes preventivos ejecutados: {planesTexto}";
                }

                correctivo.ObservacionesTecnico = ObservacionesCorrectivoTimelineService.Completar(
                    correctivo.ObservacionesTecnico,
                    correctivo.ResponsableEjecucion,
                    correctivo.Proveedor,
                    costo,
                    rutaFactura,
                    observacionesCompletado);

                await _ejecucionService.UpdateAsync(correctivo.Id, correctivo, cancellationToken);
                await UpdateVehicleMileageIfHigherAsync(correctivo.PlacaVehiculo, kilometrajeAlCompletar, cancellationToken);
                await EnsureFacturaDocumentAsync(correctivo.PlacaVehiculo, correctivo.RutaFactura, correctivo.FechaEjecucion, cancellationToken);

                if (planesPreventivosEjecutados != null)
                {
                    var fechaTexto = DateTime.Now.ToString("dd/MM/yyyy");

                    foreach (var plan in planesPreventivosEjecutados)
                    {
                        var costoPlan = planesPreventivosConCosto?
                            .FirstOrDefault(c => c.PlanId == plan.Id);

                        // build observation using the helper that handles optional notes/proveedor/factura
                        var nombresPlanes = planesPreventivosEjecutados.Select(p => p.PlantillaNombre ?? $"Plan #{p.Id}").ToList();
                        var detalleGeneral = observaciones ?? string.Empty;
                        var nombrePlan = plan.PlantillaNombre ?? $"Plan #{plan.Id}";
                        var proveedorAplicado = costoPlan != null && !string.IsNullOrWhiteSpace(costoPlan.ProveedorOpcional)
                            ? costoPlan.ProveedorOpcional
                            : proveedor;
                        var rutaFacturaAplicada = costoPlan != null && !string.IsNullOrWhiteSpace(costoPlan.RutaFacturaOpcional)
                            ? costoPlan.RutaFacturaOpcional
                            : null;

                        var observacionPreventiva = EjecucionesMantenimientoViewModel.BuildPreventivoObservaciones(
                            detalleGeneral,
                            costoPlan?.DetalleOpcional,
                            nombrePlan,
                            costo,
                            costoPlan?.CostoAsignado,
                            nombresPlanes,
                            proveedorAplicado,
                            rutaFacturaAplicada,
                            false,
                            null);

                        var preventivaDto = new EjecucionMantenimientoDto
                        {
                            PlacaVehiculo = correctivo.PlacaVehiculo,
                            PlanMantenimientoId = plan.Id,
                            TipoMantenimiento = (int)TipoMantenimientoVehiculo.Preventivo,
                            TituloActividad = plan.PlantillaNombre,
                            FechaEjecucion = DateTimeOffset.Now,
                            KMAlMomento = kilometrajeAlCompletar,
                            ObservacionesTecnico = observacionPreventiva,
                            ResponsableEjecucion = correctivo.ResponsableEjecucion,
                            Proveedor = correctivo.Proveedor,
                            EsExtraordinario = true,
                            Estado = (int)EstadoEjecucion.Completado,
                            EstadoCorrectivo = null,
                            Costo = (costoPlan != null && costoPlan.EsCostoPersonalizado)
                                ? costoPlan.CostoAsignado
                                : null,
                            RutaFactura = (costoPlan != null && costoPlan.EsCostoPersonalizado)
                                ? costoPlan.FacturaRuta
                                : null
                        };

                        if ((preventivaDto.Costo ?? 0) > 0 || !string.IsNullOrWhiteSpace(preventivaDto.Proveedor) || !string.IsNullOrWhiteSpace(preventivaDto.RutaFactura))
                        {
                            preventivaDto.ItemsGasto.Add(new EjecucionMantenimientoItemGastoDto
                            {
                                TipoGasto = (int)TipoGastoMantenimientoVehiculo.Servicio,
                                Descripcion = string.IsNullOrWhiteSpace(preventivaDto.TituloActividad)
                                    ? "Preventivo ejecutado desde correctivo"
                                    : $"Preventivo: {preventivaDto.TituloActividad}",
                                Proveedor = preventivaDto.Proveedor,
                                Valor = decimal.Round(preventivaDto.Costo ?? 0m, 2),
                                RutaFactura = preventivaDto.RutaFactura,
                                FechaDocumento = preventivaDto.FechaEjecucion
                            });
                        }

                        await _ejecucionService.CreateAsync(preventivaDto, cancellationToken);
                    }
                }

                await LoadCorrectivosVehiculoAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al completar correctivo";
                _logger.LogError(ex, "Error completando correctivo: {Id}", correctivo.Id);
            }
        }

        public async Task UpdateCorrectivoAsync(EjecucionMantenimientoDto correctivo, CancellationToken cancellationToken = default)
        {
            if (correctivo == null)
            {
                return;
            }

            try
            {
                await _ejecucionService.UpdateAsync(correctivo.Id, correctivo, cancellationToken);
                await UpdateVehicleMileageIfHigherAsync(correctivo.PlacaVehiculo, correctivo.KMAlMomento, cancellationToken);
                await EnsureFacturaDocumentAsync(correctivo.PlacaVehiculo, correctivo.RutaFactura, correctivo.FechaEjecucion, cancellationToken);
                await LoadCorrectivosVehiculoAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar correctivo";
                _logger.LogError(ex, "Error actualizando correctivo: {Id}", correctivo.Id);
            }
        }

        private static EstadoEjecucion MapEstadoGeneralFromCorrectivo(EstadoMantenimientoCorrectivoVehiculo estadoCorrectivo)
        {
            return estadoCorrectivo switch
            {
                EstadoMantenimientoCorrectivoVehiculo.Completado => EstadoEjecucion.Completado,
                EstadoMantenimientoCorrectivoVehiculo.Cancelado => EstadoEjecucion.Cancelado,
                _ => EstadoEjecucion.Pendiente
            };
        }

        private async Task UpdateVehicleMileageIfHigherAsync(string placaVehiculo, long kmRegistrado, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(placaVehiculo) || kmRegistrado <= 0)
            {
                return;
            }

            var placa = placaVehiculo.Trim().ToUpperInvariant();
            var vehiculo = await _vehicleService.GetByPlateAsync(placa, cancellationToken);
            if (vehiculo == null)
            {
                return;
            }

            if (kmRegistrado > vehiculo.Mileage)
            {
                vehiculo.Mileage = kmRegistrado;
                await _vehicleService.UpdateAsync(vehiculo.Id, vehiculo, cancellationToken);
            }
        }

        private async Task EnsureFacturaDocumentAsync(
            string placaVehiculo,
            string? rutaFactura,
            DateTimeOffset fechaEmision,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(placaVehiculo) || string.IsNullOrWhiteSpace(rutaFactura))
            {
                return;
            }

            try
            {
                var rutaNormalizada = rutaFactura.Trim();
                var vehiculo = await _vehicleService.GetByPlateAsync(placaVehiculo.Trim().ToUpperInvariant(), cancellationToken);
                if (vehiculo == null)
                {
                    return;
                }

                var documentos = await _vehicleDocumentService.GetByVehicleIdAsync(vehiculo.Id);
                var yaExiste = documentos.Any(d =>
                    d.DocumentType.Equals("Factura", StringComparison.OrdinalIgnoreCase) &&
                    d.IsActive &&
                    string.Equals(d.FilePath ?? string.Empty, rutaNormalizada, StringComparison.OrdinalIgnoreCase));

                if (yaExiste)
                {
                    return;
                }

                var facturaDto = new VehicleDocumentDto
                {
                    VehicleId = vehiculo.Id,
                    DocumentType = "Factura",
                    DocumentNumber = null,
                    IssuedDate = fechaEmision == default ? DateTimeOffset.UtcNow : fechaEmision,
                    ExpirationDate = fechaEmision == default ? DateTimeOffset.UtcNow : fechaEmision,
                    FileName = Path.GetFileName(rutaNormalizada),
                    FilePath = rutaNormalizada,
                    Notes = $"Factura de correctivo ({placaVehiculo})",
                    IsActive = true
                };

                await _vehicleDocumentService.AddAsync(facturaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sincronizando factura de correctivo en documentos para placa {Placa}", placaVehiculo);
            }
        }
    }
}
