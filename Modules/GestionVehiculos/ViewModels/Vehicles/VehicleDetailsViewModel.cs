using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using GestLog.Services.Core.Logging;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using LiveChartsCore.Defaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    public partial class VehicleDetailsViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPlanMantenimientoVehiculoService _planMantenimientoVehiculoService;
        private readonly IConsumoCombustibleService _consumoCombustibleService;
        private readonly IEjecucionMantenimientoService _ejecucionMantenimientoService;
        private readonly IVehicleDocumentService _vehicleDocumentService;
        private readonly IAppDialogService _dialogService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private Guid id;

        [ObservableProperty]
        private string plate = string.Empty;

        [ObservableProperty]
        private string vin = string.Empty;

        [ObservableProperty]
        private string brand = string.Empty;

        [ObservableProperty]
        private string model = string.Empty;

        [ObservableProperty]
        private string? version;

        [ObservableProperty]
        private int year;

        [ObservableProperty]
        private string? color;

        [ObservableProperty]
        private long mileage;

        [ObservableProperty]
        private VehicleType type;

        [ObservableProperty]
        private VehicleState state;

        [ObservableProperty]
        private string? photoPath;

        [ObservableProperty]
        private string? photoThumbPath;

        [ObservableProperty]
        private string? fuelType;

        [ObservableProperty]
        private string nuevoKilometrajeInput = string.Empty;

        [ObservableProperty]
        private string mileageUpdateMessage = string.Empty;

        [ObservableProperty]
        private bool hasMileageUpdateError = false;

        [ObservableProperty]
        private string proximoMantenimientoDisplay = "Sin programar";

        [ObservableProperty]
        private int totalTanqueadas;

        [ObservableProperty]
        private decimal totalGalonesConsumidos;

        [ObservableProperty]
        private decimal totalCostoCombustible;

        [ObservableProperty]
        private int cantidadMantenimientos;

        [ObservableProperty]
        private decimal costoTotalMantenimiento;

        [ObservableProperty]
        private decimal costoTotalOperacion;

        [ObservableProperty]
        private string costoPorKmDisplay = "N/D";

        [ObservableProperty]
        private string ultimaTanqueadaDisplay = "Sin registros";

        [ObservableProperty]
        private string ultimoMantenimientoDisplay = "Sin registros";

        [ObservableProperty]
        private int totalDocumentos;

        [ObservableProperty]
        private int documentosVigentes;

        [ObservableProperty]
        private int documentosVencidos;

        [ObservableProperty]
        private int documentosPorVencer;

        // nuevos KPI
        [ObservableProperty]
        private string kmDesdeUltimoMantenimientoDisplay = "N/A";

        [ObservableProperty]
        private string proximoMantenimientoKmDisplay = "N/A";

        [ObservableProperty]
        private string consumoPromedioDisplay = "N/D";

        [ObservableProperty]
        private decimal gastoMensualCombustible;

        [ObservableProperty]
        private string estadoResumen = "OK";

        [ObservableProperty]
        private string kmActualDisplay = "N/D";

        // propiedades gráficas adicionales
        [ObservableProperty]
        private ISeries[] consumoPromedioSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] kilometrajeSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] mantenimientosTimelineSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] documentosPieSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private string versionDisplay = "-";

        [ObservableProperty]
        private string colorDisplay = "-";

        [ObservableProperty]
        private string fuelTypeDisplay = "No especificado";

        [ObservableProperty]
        private string typeDisplay = "-";

        [ObservableProperty]
        private string stateDisplay = "-";

        [ObservableProperty]
        private string createdAtDisplay = "-";

        [ObservableProperty]
        private string updatedAtDisplay = "-";

        [ObservableProperty]
        private string auditDisplay = "-";

        [ObservableProperty]
        private string generalNotes = "Sin datos operativos aún.";

        [ObservableProperty]
        private ISeries[] costoMensualSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] galonesMensualesSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private ISeries[] distribucionGastoSeries = Array.Empty<ISeries>();

        [ObservableProperty]
        private Axis[] chartMonthAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartCurrencyAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartGallonsAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartDateAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartConsumptionAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartKmAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartMaintenanceAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private Axis[] chartMaintenanceCountAxes = Array.Empty<Axis>();

        [ObservableProperty]
        private ObservableCollection<string> mantenimientosTitulos = new();

        // Propiedades calculadas para bindings en UI
        public string VehicleTitle => $"{Brand} {Model} {Year}".Trim();
        public string PlateDisplay => $"Placa: {Plate}";
        public string BrandModelDisplay => $"{Brand} {Model}".Trim();
        public string YearDisplay => $"Año: {Year}";

        public VehicleDetailsViewModel(
            IVehicleService vehicleService,
            IPlanMantenimientoVehiculoService planMantenimientoVehiculoService,
            IConsumoCombustibleService consumoCombustibleService,
            IEjecucionMantenimientoService ejecucionMantenimientoService,
            IVehicleDocumentService vehicleDocumentService,
            IAppDialogService dialogService,
            IGestLogLogger logger)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _planMantenimientoVehiculoService = planMantenimientoVehiculoService ?? throw new ArgumentNullException(nameof(planMantenimientoVehiculoService));
            _consumoCombustibleService = consumoCombustibleService ?? throw new ArgumentNullException(nameof(consumoCombustibleService));
            _ejecucionMantenimientoService = ejecucionMantenimientoService ?? throw new ArgumentNullException(nameof(ejecucionMantenimientoService));
            _vehicleDocumentService = vehicleDocumentService ?? throw new ArgumentNullException(nameof(vehicleDocumentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        {
            Id = vehicleId;

            try
            {
                var dto = await _vehicleService.GetByIdAsync(vehicleId, cancellationToken);
                if (dto == null)
                {
                    throw new InvalidOperationException("Vehículo no encontrado");
                }

                Plate = dto.Plate ?? string.Empty;
                Vin = dto.Vin ?? string.Empty;
                Brand = dto.Brand ?? string.Empty;
                Model = dto.Model ?? string.Empty;
                Version = dto.Version;
                Year = dto.Year;
                Color = dto.Color;
                Mileage = dto.Mileage;
                NuevoKilometrajeInput = dto.Mileage.ToString();
                Type = dto.Type;
                State = dto.State;
                PhotoPath = dto.PhotoPath;
                PhotoThumbPath = dto.PhotoThumbPath;
                FuelType = dto.FuelType;

                VersionDisplay = string.IsNullOrWhiteSpace(dto.Version) ? "-" : dto.Version;
                ColorDisplay = string.IsNullOrWhiteSpace(dto.Color) ? "-" : dto.Color;
                FuelTypeDisplay = string.IsNullOrWhiteSpace(dto.FuelType) ? "No especificado" : dto.FuelType;
                TypeDisplay = dto.Type.ToString();
                StateDisplay = dto.State.ToString();
                CreatedAtDisplay = dto.CreatedAt == default ? "-" : dto.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm");
                UpdatedAtDisplay = dto.UpdatedAt == default ? "-" : dto.UpdatedAt.LocalDateTime.ToString("dd/MM/yyyy HH:mm");
                AuditDisplay = $"Creado: {CreatedAtDisplay} · Actualizado: {UpdatedAtDisplay}";

                // Notificar que las propiedades calculadas han cambiado
                OnPropertyChanged(nameof(VehicleTitle));
                OnPropertyChanged(nameof(PlateDisplay));
                OnPropertyChanged(nameof(BrandModelDisplay));
                OnPropertyChanged(nameof(YearDisplay));

                await CargarProximoMantenimientoAsync(cancellationToken);
                await CargarKpisYResumenGeneralAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando detalles del vehículo");
                throw;
            }
        }

        private async Task CargarKpisYResumenGeneralAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                TotalTanqueadas = 0;
                TotalGalonesConsumidos = 0m;
                TotalCostoCombustible = 0m;
                CantidadMantenimientos = 0;
                CostoTotalMantenimiento = 0m;
                CostoTotalOperacion = 0m;
                CostoPorKmDisplay = "N/D";
                UltimaTanqueadaDisplay = "Sin registros";
                UltimoMantenimientoDisplay = "Sin registros";
                TotalDocumentos = 0;
                DocumentosVigentes = 0;
                DocumentosVencidos = 0;
                DocumentosPorVencer = 0;
                CostoMensualSeries = Array.Empty<ISeries>();
                GalonesMensualesSeries = Array.Empty<ISeries>();
                DistribucionGastoSeries = Array.Empty<ISeries>();
                ChartMonthAxes = Array.Empty<Axis>();
                ChartCurrencyAxes = Array.Empty<Axis>();
                ChartGallonsAxes = Array.Empty<Axis>();
                ChartDateAxes = Array.Empty<Axis>();
                ChartConsumptionAxes = Array.Empty<Axis>();
                ChartKmAxes = Array.Empty<Axis>();
                ChartMaintenanceAxes = Array.Empty<Axis>();
                ChartMaintenanceCountAxes = Array.Empty<Axis>();
                MantenimientosTitulos = new ObservableCollection<string>();

                if (string.IsNullOrWhiteSpace(Plate) || Id == Guid.Empty)
                {
                    GeneralNotes = "No se pudo calcular el resumen por falta de identificación del vehículo.";
                    return;
                }

                var resumenCombustible = await _consumoCombustibleService.GetResumenByPlacaAsync(Plate, cancellationToken);
                var tanqueadas = (await _consumoCombustibleService.GetByPlacaAsync(Plate, cancellationToken)).ToList();

                TotalTanqueadas = resumenCombustible.TotalRegistros;
                TotalGalonesConsumidos = resumenCombustible.TotalGalones;
                TotalCostoCombustible = resumenCombustible.TotalCosto;

                // gasto mensual en combustible (mes actual)
                var hoy = DateTime.Today;
                GastoMensualCombustible = tanqueadas
                    .Where(t => t.FechaTanqueada.Year == hoy.Year && t.FechaTanqueada.Month == hoy.Month)
                    .Sum(t => t.ValorTotal);

                KmActualDisplay = Mileage.ToString("N0");

                var ultimaTanqueada = tanqueadas
                    .OrderByDescending(x => x.FechaTanqueada)
                    .FirstOrDefault();

                if (ultimaTanqueada != null)
                {
                    UltimaTanqueadaDisplay = $"{ultimaTanqueada.FechaTanqueada:dd/MM/yyyy} · {ultimaTanqueada.Galones:N2} gal";
                }

                var mantenimientosPreventivos = await _ejecucionMantenimientoService.GetByPlacaAndTipoAsync(Plate, (int)TipoMantenimientoVehiculo.Preventivo, cancellationToken);
                var mantenimientosCorrectivos = await _ejecucionMantenimientoService.GetByPlacaAndTipoAsync(Plate, (int)TipoMantenimientoVehiculo.Correctivo, cancellationToken);
                var mantenimientos = mantenimientosPreventivos.Concat(mantenimientosCorrectivos).ToList();

                var maxKmTanqueadas = tanqueadas.Any() ? tanqueadas.Max(t => t.KMAlMomento) : 0;
                var maxKmMantenimientos = mantenimientos.Any() ? mantenimientos.Max(m => m.KMAlMomento) : 0;
                var kmOperativoMax = Math.Max(Mileage, Math.Max(maxKmTanqueadas, maxKmMantenimientos));

                KmActualDisplay = kmOperativoMax.ToString("N0");

                CantidadMantenimientos = mantenimientos.Count;
                CostoTotalMantenimiento = mantenimientos.Sum(x => x.Costo ?? 0m);

                var ultimoMantenimiento = mantenimientos
                    .OrderByDescending(x => x.FechaEjecucion)
                    .FirstOrDefault();

                if (ultimoMantenimiento != null)
                {
                    var tipo = ultimoMantenimiento.TipoMantenimiento == (int)TipoMantenimientoVehiculo.Correctivo
                        ? "Correctivo"
                        : "Preventivo";

                    UltimoMantenimientoDisplay = $"{ultimoMantenimiento.FechaEjecucion:dd/MM/yyyy} · {tipo}";
                    // km desde ultimo mantenimiento
                    if (ultimoMantenimiento.KMAlMomento > 0)
                    {
                        var kmDesde = kmOperativoMax - ultimoMantenimiento.KMAlMomento;
                        KmDesdeUltimoMantenimientoDisplay = kmDesde.ToString("N0") + " km";
                        ProximoMantenimientoKmDisplay = (ultimoMantenimiento.KMAlMomento + 5000).ToString("N0") + " km"; // placeholder
                    }
                }

                var documentStats = await _vehicleDocumentService.GetStatisticsAsync(Id);
                TotalDocumentos = documentStats.TotalDocuments;
                DocumentosVigentes = documentStats.ValidDocuments;
                DocumentosVencidos = documentStats.ExpiredDocuments;
                DocumentosPorVencer = documentStats.SoonToExpireDocuments;

                // estado resumen
                if (DocumentosPorVencer > 0 || (ProximoMantenimientoKmDisplay != "N/A" && long.TryParse(ProximoMantenimientoKmDisplay.Split(' ')[0], out var pk) && pk - kmOperativoMax < 1000))
                {
                    EstadoResumen = "ALERTA";
                }
                else
                {
                    EstadoResumen = "OK";
                }

                CostoTotalOperacion = TotalCostoCombustible + CostoTotalMantenimiento;

                var kmInicial = tanqueadas
                    .Where(x => x.KMAlMomento > 0)
                    .OrderBy(x => x.FechaTanqueada)
                    .Select(x => x.KMAlMomento)
                    .FirstOrDefault();

                var kmRecorridos = kmInicial > 0 && kmOperativoMax > kmInicial
                    ? kmOperativoMax - kmInicial
                    : 0;

                if (kmRecorridos > 0 && TotalGalonesConsumidos > 0)
                {
                    var prom = (double)kmRecorridos / (double)TotalGalonesConsumidos;
                    ConsumoPromedioDisplay = prom.ToString("N2") + " km/gal";
                }

                ConstruirGraficos(tanqueadas, mantenimientos);

                GeneralNotes = $"Resumen calculado con {TotalTanqueadas} tanqueadas, {CantidadMantenimientos} mantenimientos y {TotalDocumentos} documentos activos.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo calcular el resumen KPI del vehículo {Plate}", Plate);
                GeneralNotes = "No fue posible calcular todos los KPIs en este momento.";
            }
        }

        private void ConstruirGraficos(
            System.Collections.Generic.IReadOnlyCollection<ConsumoCombustibleVehiculoDto> tanqueadas,
            System.Collections.Generic.IReadOnlyCollection<EjecucionMantenimientoDto> mantenimientos)
        {
            // los meses con datos de combustible (sin mezclar mantenimientos)
            var mesesSet = new SortedSet<DateTime>();
            foreach (var t in tanqueadas)
                mesesSet.Add(new DateTime(t.FechaTanqueada.Year, t.FechaTanqueada.Month, 1));
            var meses = mesesSet.ToList();

            var labels = meses.Select(m => m.ToString("MM/yy")).ToArray();

            var costosCombustibleMensual = meses
                .Select(m => (double)tanqueadas
                    .Where(t => t.FechaTanqueada.Year == m.Year && t.FechaTanqueada.Month == m.Month)
                    .Sum(t => t.ValorTotal))
                .ToArray();

            var galonesMensual = meses
                .Select(m => (double)tanqueadas
                    .Where(t => t.FechaTanqueada.Year == m.Year && t.FechaTanqueada.Month == m.Month)
                    .Sum(t => t.Galones))
                .ToArray();

            // consumo promedio por tanqueada (km/gal) over time
            var ordered = tanqueadas.OrderBy(t => t.FechaTanqueada).ToList();
            var puntosConsumo = new List<DateTimePoint>();
            for (int i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var cur = ordered[i];
                var kms = cur.KMAlMomento - prev.KMAlMomento;
                if (kms <= 0 || cur.Galones <= 0) continue;
                var kmgal = (double)kms / (double)cur.Galones;
                puntosConsumo.Add(new DateTimePoint(cur.FechaTanqueada.Date, kmgal));
            }

            ConsumoPromedioSeries =
            [
                new LineSeries<DateTimePoint>
                {
                    Name = "Consumo km/gal",
                    Values = puntosConsumo,
                    Fill = null,
                    GeometrySize = 6,
                    Stroke = new SolidColorPaint(SKColors.Blue, 2)
                }
            ];

            // kilometraje crecimiento (tomando tanqueadas y mantenimientos)
            var kmPoints = tanqueadas
                .Select(t => new { Fecha = t.FechaTanqueada.Date, Km = t.KMAlMomento })
                .Concat(mantenimientos.Select(m => new { Fecha = m.FechaEjecucion.Date, Km = m.KMAlMomento }))
                .Where(x => x.Km > 0)
                .OrderBy(x => x.Fecha)
                .GroupBy(x => x.Fecha)
                .Select(g => new DateTimePoint(g.Key, g.Max(v => (double)v.Km)))
                .ToList();
            KilometrajeSeries =
            [
                new LineSeries<DateTimePoint>
                {
                    Name = "Kilometraje",
                    Values = kmPoints,
                    Fill = null,
                    GeometrySize = 6,
                    Stroke = new SolidColorPaint(SKColors.Green, 2)
                }
            ];

            // mantenimientos: cantidad por fecha + etiqueta con títulos
            var mantenimientosPorFecha = mantenimientos
                .Where(m => m.FechaEjecucion != default)
                .OrderBy(m => m.FechaEjecucion)
                .GroupBy(m => m.FechaEjecucion.Date)
                .ToList();

            var mantenimientoLabels = mantenimientosPorFecha.Select(g => g.Key.ToString("dd/MM/yyyy")).ToArray();
            var mantenimientoCounts = mantenimientosPorFecha.Select(g => (double)g.Count()).ToArray();
            var mantenimientoTitulos = mantenimientosPorFecha
                .Select(g => string.Join(", ", g.Select(x => string.IsNullOrWhiteSpace(x.TituloActividad)
                    ? (x.TipoMantenimiento == (int)TipoMantenimientoVehiculo.Correctivo ? "Correctivo" : "Preventivo")
                    : x.TituloActividad!).Distinct()))
                .ToList();

            MantenimientosTitulos = new ObservableCollection<string>(mantenimientoTitulos);

            MantenimientosTimelineSeries =
            [
                new ColumnSeries<double>
                {
                    Name = "Cantidad",
                    Values = mantenimientoCounts,
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11)),
                    DataLabelsSize = 10,
                    DataLabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    DataLabelsPosition = DataLabelsPosition.Top
                }
            ];

            // documentos pie
            DocumentosPieSeries =
            [
                new PieSeries<int>
                {
                    Name = "Vigentes",
                    Values = new[] { DocumentosVigentes },
                    Fill = new SolidColorPaint(new SKColor(17,137,56))
                },
                new PieSeries<int>
                {
                    Name = "Vencidos",
                    Values = new[] { DocumentosVencidos },
                    Fill = new SolidColorPaint(new SKColor(192,57,43))
                }
            ];

            CostoMensualSeries =
            [
                new LineSeries<double>
                {
                    Name = "Combustible",
                    Values = costosCombustibleMensual,
                    Fill = null,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(17, 137, 56), 3)
                }
            ];

            GalonesMensualesSeries =
            [
                new ColumnSeries<double>
                {
                    Name = "Galones",
                    Values = galonesMensual,
                    Fill = new SolidColorPaint(new SKColor(55, 171, 78))
                }
            ];

            DistribucionGastoSeries =
            [
                new PieSeries<double>
                {
                    Name = "Combustible",
                    Values = new[] { (double)TotalCostoCombustible },
                    Fill = new SolidColorPaint(new SKColor(17, 137, 56)),
                    DataLabelsSize = 12,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsFormatter = point => point.Coordinate.PrimaryValue.ToString("C0")
                },
                new PieSeries<double>
                {
                    Name = "Mantenimiento",
                    Values = new[] { (double)CostoTotalMantenimiento },
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11)),
                    DataLabelsSize = 12,
                    DataLabelsPosition = PolarLabelsPosition.Middle,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsFormatter = point => point.Coordinate.PrimaryValue.ToString("C0")
                }
            ];

            ChartMonthAxes =
            [
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 11
                }
            ];

            ChartCurrencyAxes =
            [
                new Axis
                {
                    Labeler = value => value.ToString("C0"),
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 11
                }
            ];

            ChartGallonsAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:N0}",
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 11
                }
            ];

            ChartDateAxes =
            [
                new Axis
                {
                    Labeler = FormatDateAxisLabel,
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 10,
                    UnitWidth = TimeSpan.FromDays(1).Ticks,
                    MinStep = TimeSpan.FromDays(1).Ticks
                }
            ];

            ChartConsumptionAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:N1}",
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 10
                }
            ];

            ChartKmAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:N0}",
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 10
                }
            ];

            ChartMaintenanceAxes =
            [
                new Axis
                {
                    Labels = mantenimientoLabels,
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 10,
                    LabelsRotation = 0
                }
            ];

            ChartMaintenanceCountAxes =
            [
                new Axis
                {
                    Labeler = value => $"{value:N0}",
                    MinLimit = 0,
                    LabelsPaint = new SolidColorPaint(new SKColor(80, 79, 78)),
                    TextSize = 10
                }
            ];
        }

        private static string FormatDateAxisLabel(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return string.Empty;
            }

            var rounded = Math.Round(value);
            if (rounded >= DateTime.MinValue.Ticks && rounded <= DateTime.MaxValue.Ticks)
            {
                return new DateTime((long)rounded).ToString("dd/MM/yy");
            }

            try
            {
                return DateTime.FromOADate(value).ToString("dd/MM/yy");
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task CargarProximoMantenimientoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ProximoMantenimientoDisplay = "Sin programar";

                if (string.IsNullOrWhiteSpace(Plate))
                {
                    return;
                }

                var planes = await _planMantenimientoVehiculoService.GetByPlacaListAsync(Plate, cancellationToken);
                var activos = planes?.Where(p => p.Activo).ToList();

                if (activos == null || activos.Count == 0)
                {
                    return;
                }

                var porFecha = activos
                    .Where(p => p.ProximaFechaEjecucion.HasValue)
                    .OrderBy(p => p.ProximaFechaEjecucion)
                    .FirstOrDefault();

                if (porFecha?.ProximaFechaEjecucion != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(porFecha.PlantillaNombre)
                        ? "Mantenimiento"
                        : porFecha.PlantillaNombre;

                    ProximoMantenimientoDisplay = $"{porFecha.ProximaFechaEjecucion.Value:dd/MM/yyyy} · {nombre}";
                    return;
                }

                var porKm = activos
                    .Where(p => p.ProximoKMEjecucion.HasValue)
                    .OrderBy(p => p.ProximoKMEjecucion)
                    .FirstOrDefault();

                if (porKm?.ProximoKMEjecucion != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(porKm.PlantillaNombre)
                        ? "Mantenimiento"
                        : porKm.PlantillaNombre;

                    ProximoMantenimientoDisplay = $"{porKm.ProximoKMEjecucion.Value:N0} km · {nombre}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo calcular el próximo mantenimiento para la placa {Plate}", Plate);
                ProximoMantenimientoDisplay = "Sin programar";
            }
        }

        [RelayCommand]
        private async Task EditarVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo para editar.";
                    return;
                }

                var current = await _vehicleService.GetByIdAsync(Id, cancellationToken);
                if (current == null)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "El vehículo ya no existe o no está disponible.";
                    return;
                }

                var dbContextFactory = ((App)System.Windows.Application.Current).ServiceProvider?
                    .GetService(typeof(IDbContextFactory<GestLogDbContext>)) as IDbContextFactory<GestLogDbContext>;

                if (dbContextFactory == null)
                {
                    throw new InvalidOperationException("IDbContextFactory no está registrado en DI");
                }

                bool? result = null;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var dialog = new VehicleFormDialog(current, dbContextFactory);
                    var owner = System.Windows.Application.Current?.MainWindow;
                    if (owner != null)
                    {
                        dialog.Owner = owner;
                    }

                    result = dialog.ShowDialog();
                });

                if (result == true)
                {
                    await LoadAsync(Id, cancellationToken);
                    HasMileageUpdateError = false;
                    MileageUpdateMessage = "Vehículo actualizado correctamente.";
                }
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al abrir el formulario de edición.";
                _logger.LogError(ex, "Error abriendo edición de vehículo desde detalle");
            }
        }

        [RelayCommand]
        private async Task EliminarVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo a eliminar.";
                    return;
                }

                var confirmacion1 = _dialogService.ConfirmWarning(
                    "Esta acción eliminará el vehículo (borrado lógico). ¿Deseas continuar?",
                    "Confirmar eliminación");

                if (!confirmacion1)
                {
                    return;
                }

                var confirmacion2 = _dialogService.Confirm(
                    $"Confirmación final: se eliminará el vehículo con placa '{Plate}'. ¿Eliminar ahora?",
                    "Confirmación final");

                if (!confirmacion2)
                {
                    return;
                }

                await _vehicleService.DeleteAsync(Id, cancellationToken);
                HasMileageUpdateError = false;
                MileageUpdateMessage = "Vehículo eliminado correctamente.";
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al eliminar el vehículo.";
                _logger.LogError(ex, "Error eliminando vehículo desde detalle");
            }
        }

        [RelayCommand]
        public async Task ActualizarKilometrajeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                MileageUpdateMessage = string.Empty;
                HasMileageUpdateError = false;

                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo para actualizar kilometraje";
                    return;
                }

                if (!long.TryParse(NuevoKilometrajeInput?.Trim(), out var nuevoKilometraje) || nuevoKilometraje < 0)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "Ingrese un kilometraje válido (0 o mayor)";
                    return;
                }

                if (nuevoKilometraje < Mileage)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = $"El nuevo kilometraje ({nuevoKilometraje:N0}) no puede ser menor al actual ({Mileage:N0})";
                    return;
                }

                if (nuevoKilometraje == Mileage)
                {
                    MileageUpdateMessage = "El kilometraje ya está actualizado";
                    return;
                }

                var vehicle = await _vehicleService.GetByIdAsync(Id, cancellationToken);
                if (vehicle == null)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "Vehículo no encontrado";
                    return;
                }

                vehicle.Mileage = nuevoKilometraje;
                await _vehicleService.UpdateAsync(Id, vehicle, cancellationToken);

                Mileage = nuevoKilometraje;
                NuevoKilometrajeInput = nuevoKilometraje.ToString();
                MileageUpdateMessage = "Kilometraje actualizado correctamente";
            }
            catch (OperationCanceledException)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Operación cancelada";
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al actualizar el kilometraje";
                _logger.LogError(ex, "Error actualizando kilometraje del vehículo en detalle");
            }
        }
    }
}
