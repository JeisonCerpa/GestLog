using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs; // añadido para CronogramaMantenimientoDto
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;     // ✅ NUEVO: IGestLogLogger
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Windows;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{    public class EjecucionHistorialItem : ObservableObject
    {
        public Guid EjecucionId { get; set; }
        public Guid? PlanId { get; set; }  // ✅ REFACTOR: Ahora puede ser null (desacoplado del plan)
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        [Obsolete("DescripcionPlan is obsolete for the current view.")]
        public string DescripcionPlan { get; set; } = string.Empty; // (Obsoleto para la vista actual)
        public int AnioISO { get; set; }
        public int SemanaISO { get; set; }
        public DateTime FechaObjetivo { get; set; }
        public DateTime? FechaEjecucion { get; set; }
        public byte Estado { get; set; }        public string UsuarioEjecuta { get; set; } = string.Empty;
        public string? Resumen { get; set; }
        public string UsuarioAsignadoEquipo { get; set; } = string.Empty; // Nuevo: usuario asignado al equipo
        public string Sede { get; set; } = string.Empty; // Nuevo: sede del equipo        // ✅ Estado derivado: solo es "Atrasado" si fue GENERADO automáticamente y pasó la fecha
        // Estado 3 = NoRealizada (generada automáticamente) → No es "Atrasado", es "No Realizado"
        public bool EsAtrasado => FechaEjecucion == null && FechaObjetivo.Date < DateTime.Today && Estado != 2 && Estado != 3;
        
        public string EstadoDescripcion => Estado switch // Mapeo de estados según el valor en BD
        {
            0 => "Pendiente",
            1 => "En Proceso",
            2 => "Ejecutado",
            3 => "No Realizado",  // ✅ Registros generados automáticamente
            _ => EsAtrasado ? "Atrasado" : "Desconocido"
        };
        public ObservableCollection<EjecucionDetalleItem> DetalleItems { get; set; } = new(); // Lista detallada
        public string ToolTipResumen { get; set; } = string.Empty; // Tooltip enriquecido
    }

    public class EjecucionDetalleItem : ObservableObject
    {
        public int? Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Completado { get; set; }
        public string? Observacion { get; set; }
        public string EstadoTexto => Completado ? "OK" : string.IsNullOrWhiteSpace(Observacion) ? "Pendiente" : "Observado";
    }    public partial class HistorialEjecucionesViewModel : DatabaseAwareViewModel, IDisposable
    {        
        private readonly IPlanCronogramaService _planService;
        private readonly IEquipoInformaticoService _equipoService;
        private readonly IHistorialEjecucionesExportService _exportService;        
        // Evitar ejecuciones concurrentes de LoadAsync que causen duplicados
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        // Flag para prevenir llamada doble a LoadAsync desde CargarAñosDisponiblesAsync
        private bool _isInitializing = true;
        public HistorialEjecucionesViewModel(
            IPlanCronogramaService planService, 
            IEquipoInformaticoService equipoService,
            IHistorialEjecucionesExportService exportService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {            _planService = planService;
            _equipoService = equipoService;
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            Years = new ObservableCollection<int>();
            SelectedYear = DateTime.Now.Year;
            // Cargar años disponibles de forma asíncrona (sin dispara OnSelectedYearChanged)
            _ = CargarAñosDisponiblesAsync().ContinueWith(_ => { _isInitializing = false; }, TaskScheduler.FromCurrentSynchronizationContext());
            // Suscribirse a mensajes de actualización para refresh automático
            WeakReferenceMessenger.Default.Register<EjecucionesPlanesActualizadasMessage>(this, async (r, m) => 
            {
                try
                {
                    _logger.LogDebug("[HistorialEjecucionesViewModel] Refrescando por mensaje");
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error al refrescar por mensaje");
                }
            });

            // También suscribirse a mensajes de seguimientos por compatibilidad
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    _logger.LogDebug("[HistorialEjecucionesViewModel] Refrescando por mensaje seguimientos");
                    await LoadAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error al refrescar por mensaje seguimientos");
                }
            });
        }

        [ObservableProperty] private ObservableCollection<EjecucionHistorialItem> items = new();
        // Vista filtrada para la UI
        private ICollectionView? _filteredView;
        public ICollectionView FilteredItems => _filteredView ??= CollectionViewSource.GetDefaultView(Items);

        [ObservableProperty] private ObservableCollection<int> years;
        [ObservableProperty] private int selectedYear;
        [ObservableProperty] private string? filtroCodigo;
        [ObservableProperty] private string? filtroDescripcion;
        [ObservableProperty] private string? filtroNombre;
        [ObservableProperty] private string? filtroUsuario;
        [ObservableProperty] private string? filtroSemana;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string statusMessage = string.Empty;
        [ObservableProperty] private int maxRows = 500;        
        [ObservableProperty] private EjecucionHistorialItem? selectedEjecucion; // para detalle
        [ObservableProperty] private bool mostrarDetalle;        
        // Estadísticas para la vista
        [ObservableProperty] private int totalEjecuciones;
        [ObservableProperty] private int ejecutadasCount;
        [ObservableProperty] private int pendientesCount;
        [ObservableProperty] private int atrasadasCount;
        [ObservableProperty] private int enProcesoCount;
        
        /// <summary>
        /// Propiedad calculada: suma de pendientes + atrasados
        /// </summary>
        public int NoEjecutadosCount => PendientesCount + AtrasadasCount;

        // Refresh automático de la vista filtrada cuando cambian filtros
        partial void OnFiltroCodigoChanged(string? value) => RefreshFilter();
        partial void OnFiltroDescripcionChanged(string? value) => RefreshFilter();
        partial void OnFiltroNombreChanged(string? value) => RefreshFilter();
        partial void OnFiltroUsuarioChanged(string? value) => RefreshFilter();
        partial void OnFiltroSemanaChanged(string? value) => RefreshFilter();        // Al cambiar año, recargar los items del año seleccionado (para que se consulten desde BD)
        partial void OnSelectedYearChanged(int value)
        {
            // ✅ CRÍTICO: Prevenir LoadAsync durante inicialización
            if (!_isInitializing)
                _ = LoadAsync();
        }

        // Propiedades compatibles con PlanDetalleModalWindow (para reusar la ventana de detalle)
        [ObservableProperty] private CronogramaMantenimientoDto? selectedPlanDetalle;
        [ObservableProperty] private ObservableCollection<PlanDetalleChecklistItem> detalleChecklist = new();
        [ObservableProperty] private string? detalleEstadoTexto;
        [ObservableProperty] private DateTime? detalleFechaObjetivo;
        [ObservableProperty] private DateTime? detalleFechaEjecucion;
        [ObservableProperty] private string? detalleResumen;

        // Clase auxiliar local para checklist (misma forma que la usada en CronogramaDiarioViewModel)
        public class PlanDetalleChecklistItem : ObservableObject
        {
            public int? Id { get; set; }
            public string Descripcion { get; set; } = string.Empty;
            public bool Completado { get; set; }
            public string? Observacion { get; set; }
            public string Estado => Completado ? "OK" : string.IsNullOrWhiteSpace(Observacion) ? "Pendiente" : "Observado";
        }

        [RelayCommand]
        private void VerDetallePlan(EjecucionHistorialItem? item)
        {
            if (item == null) return;

            // Construir un DTO mínimo para mostrar en la ventana de detalle
            SelectedPlanDetalle = new CronogramaMantenimientoDto
            {
                Codigo = item.CodigoEquipo,
                Nombre = string.IsNullOrWhiteSpace(item.NombreEquipo) ? item.CodigoEquipo : item.NombreEquipo,
                Sede = item.UsuarioAsignadoEquipo,
                Marca = "Historial",
                Anio = item.AnioISO,
                EsPlanSemanal = false
            };

            DetalleChecklist.Clear();
            if (item.DetalleItems != null)
            {
                foreach (var d in item.DetalleItems)
                {
                    DetalleChecklist.Add(new PlanDetalleChecklistItem { Id = d.Id, Descripcion = d.Descripcion, Completado = d.Completado, Observacion = d.Observacion });
                }
            }

            DetalleFechaObjetivo = item.FechaObjetivo;
            DetalleFechaEjecucion = item.FechaEjecucion;
            DetalleEstadoTexto = item.EstadoDescripcion;
            DetalleResumen = item.Resumen ?? string.Empty;            try
            {
                var modalWindow = new GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma.PlanDetalleModalWindow
                {
                    DataContext = this
                };

                var parentWindow = System.Windows.Application.Current.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;

                if (parentWindow != null)
                    modalWindow.ConfigurarParaVentanaPadre(parentWindow);

                modalWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error al abrir ventana de detalle");
                // Mostrar el panel lateral como fallback
                MostrarDetalle = true;
            }
        }        
        [RelayCommand]
        private async Task CargarAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private async Task CargarItemsAsync()
        {
            await LoadAsync();
        }

        [RelayCommand]
        private void VerDetalle(EjecucionHistorialItem? item)
        {
            if (item == null) return;
            SelectedEjecucion = item;
            MostrarDetalle = true;
        }

        [RelayCommand]
        private void CerrarDetalle()
        {
            MostrarDetalle = false;
        }

        private async Task CargarAñosDisponiblesAsync()
        {
            try
            {
                var anosDisponibles = await _planService.GetAvailableYearsAsync();

                _logger.LogDebug("[HistorialEjecucionesViewModel] Años disponibles desde DB: {anos}", string.Join(", ", anosDisponibles));

                // Si no hay años en la BD, usar años por defecto
                if (anosDisponibles.Count == 0)
                {
                    anosDisponibles = Enumerable.Range(DateTime.Now.Year - 3, 4).OrderByDescending(x => x).ToList();
                }
                else
                {
                    // Garantizar que el año actual esté presente en la lista para permitir seleccionar 2026 aunque no tenga ejecuciones
                    if (!anosDisponibles.Contains(DateTime.Now.Year))
                    {
                        anosDisponibles = anosDisponibles.Union(new[] { DateTime.Now.Year }).OrderByDescending(x => x).ToList();
                        _logger.LogDebug("[HistorialEjecucionesViewModel] Año actual añadido a años disponibles: {year}", DateTime.Now.Year);
                    }
                }

                Years.Clear();
                foreach (var ano in anosDisponibles)
                {
                    Years.Add(ano);
                }

                _logger.LogDebug("[HistorialEjecucionesViewModel] Años finales en la vista: {anos}", string.Join(", ", Years));                // Seleccionar el año actual si está disponible; si no, seleccionar el primero
                if (anosDisponibles.Contains(DateTime.Now.Year))
                {
                    SelectedYear = DateTime.Now.Year;
                    _logger.LogDebug("[CargarAñosDisponiblesAsync] SelectedYear asignado a {year}", DateTime.Now.Year);
                }
                else if (anosDisponibles.Count > 0)
                {
                    SelectedYear = anosDisponibles.First();
                    _logger.LogDebug("[CargarAñosDisponiblesAsync] SelectedYear asignado a {year}", anosDisponibles.First());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error al cargar años disponibles");
                // Fallback: usar años por defecto si hay error
                Years.Clear();
                var anosDefault = Enumerable.Range(DateTime.Now.Year - 3, 4).OrderByDescending(x => x);
                foreach (var ano in anosDefault)
                {
                    Years.Add(ano);
                }
                SelectedYear = DateTime.Now.Year;
                // Intentar cargar datos del año actual aunque haya ocurrido un error al obtener años desde la BD
                try { await LoadAsync(); } catch { /* silenciar */ }
            }
        }        public async Task LoadAsync()
        {
            // Serializar llamadas para evitar repetidas cargas concurrentes que provoquen duplicados
            _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] ⏱️ LoadAsync() esperando semáforo para año {year} - _isInitializing={init} - ThreadId: {threadId}", SelectedYear, _isInitializing, System.Threading.Thread.CurrentThread.ManagedThreadId);
            await _loadSemaphore.WaitAsync();
            try
            {
                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] ✓ Semáforo adquirido - Iniciando LoadAsync para año {year}", SelectedYear);
                try
                {                    IsBusy = true;
                    StatusMessage = "Cargando...";
                    Items.Clear();
                    // asegurarse de que la vista filtrada está inicializada antes de añadir
                    SetupCollectionView();                    // ✅ Usar método con trazabilidad completa: genera registros para semanas faltantes
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] Llamando GenerarYObtenerEjecucionesConTrazabilidadAsync para año {year}", SelectedYear);
                    var ejecuciones = await _planService.GenerarYObtenerEjecucionesConTrazabilidadAsync(SelectedYear);                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] GenerarYObtenerEjecucionesConTrazabilidadAsync retornó {count} ejecuciones", ejecuciones.Count);
                    var query = ejecuciones.AsQueryable();
                    if (!string.IsNullOrWhiteSpace(FiltroCodigo))
                        query = query.Where(e => e.Plan != null && e.Plan.CodigoEquipo.Contains(FiltroCodigo, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrWhiteSpace(FiltroDescripcion))
                        query = query.Where(e => e.Plan != null && e.Plan.Descripcion.Contains(FiltroDescripcion, StringComparison.OrdinalIgnoreCase));
                
                    var codigosPendientes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    // Ordenar: atrasados primero (derivado), luego semana/año desc
                    var materialized = query.ToList();
                    foreach (var e in materialized
                                 .OrderByDescending(x => x.FechaObjetivo.Date < DateTime.Today && x.FechaEjecucion == null && x.Estado != 2) // atrasados primero
                                 .ThenByDescending(x => x.AnioISO)
                                 .ThenByDescending(x => x.SemanaISO)
                                 .Take(MaxRows))
                    {
                        string? resumen = null;
                        var detalleItems = new ObservableCollection<EjecucionDetalleItem>();
                        int total = 0; int comp = 0; int observados = 0; int pendientes = 0;
                        string toolTip = string.Empty;
                        if (!string.IsNullOrWhiteSpace(e.ResultadoJson))
                        {
                            try
                            {
                                var doc = System.Text.Json.JsonDocument.Parse(e.ResultadoJson);
                                if (doc.RootElement.TryGetProperty("items", out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    total = arr.GetArrayLength();
                                    foreach (var it in arr.EnumerateArray())
                                    {
                                        bool completado = it.TryGetProperty("Completado", out var cEl) && cEl.ValueKind == System.Text.Json.JsonValueKind.True;
                                        string desc = it.TryGetProperty("Descripcion", out var dEl) ? (dEl.GetString() ?? string.Empty) :
                                                      it.TryGetProperty("descripcion", out var d2El) ? (d2El.GetString() ?? string.Empty) : string.Empty;
                                        string? obs = it.TryGetProperty("Observacion", out var oEl) ? oEl.GetString() :
                                                      it.TryGetProperty("observacion", out var o2El) ? o2El.GetString() : null;
                                        int? id = it.TryGetProperty("Id", out var idEl) ? idEl.GetInt32() :
                                                   it.TryGetProperty("id", out var id2El) ? id2El.GetInt32() : (int?)null;
                                        var det = new EjecucionDetalleItem { Id = id, Descripcion = desc, Completado = completado, Observacion = obs };
                                        detalleItems.Add(det);
                                        if (completado) comp++; else if (!string.IsNullOrWhiteSpace(obs)) observados++; else pendientes++;
                                    }
                                    resumen = $"{comp}/{total} ítems OK";
                                    var okList = detalleItems.Where(x=>x.Completado).Select(x=>x.Descripcion).Take(5).ToList();
                                    var obsList = detalleItems.Where(x=>!x.Completado && !string.IsNullOrWhiteSpace(x.Observacion)).Select(x=>x.Descripcion).Take(5).ToList();
                                    var penList = detalleItems.Where(x=>!x.Completado && string.IsNullOrWhiteSpace(x.Observacion)).Select(x=>x.Descripcion).Take(5).ToList();
                                    toolTip = $"OK ({comp}): {string.Join(", ", okList)}";
                                    if (obsList.Any()) toolTip += $"\nObservados ({observados}): {string.Join(", ", obsList)}";
                                    if (penList.Any()) toolTip += $"\nPendientes ({pendientes}): {string.Join(", ", penList)}";
                                    if (okList.Count < comp || obsList.Count < observados || penList.Count < pendientes)
                                        toolTip += "\n...";
                                }
                            }
                            catch { }
                        }                        var nombreEquipo = e.Plan?.Equipo?.NombreEquipo;
                        // ✅ REFACTOR: Usar CodigoEquipo directamente de la ejecución (desacoplado del plan)
                        var codigoEquipo = e.CodigoEquipo ?? e.Plan?.CodigoEquipo ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(nombreEquipo) && !string.IsNullOrWhiteSpace(codigoEquipo))
                        {
                            nombreEquipo = "(cargando...)"; // placeholder
                            codigosPendientes.Add(codigoEquipo);
                        }                        var itemVm = new EjecucionHistorialItem
                        {
                            EjecucionId = e.EjecucionId,
                            PlanId = e.PlanId,  // ✅ Ahora puede ser null
                            CodigoEquipo = codigoEquipo,
                            NombreEquipo = nombreEquipo ?? string.Empty,
                            AnioISO = e.AnioISO,
                            SemanaISO = e.SemanaISO,
                            FechaObjetivo = e.FechaObjetivo,
                            FechaEjecucion = e.FechaEjecucion,
                            Estado = e.Estado,
                            UsuarioEjecuta = e.UsuarioEjecuta ?? string.Empty,
                            Resumen = resumen,
                            UsuarioAsignadoEquipo = e.Plan?.Equipo?.UsuarioAsignado ?? string.Empty,
                            Sede = e.Plan?.Equipo?.Sede ?? string.Empty,
                            DetalleItems = detalleItems,
                            ToolTipResumen = toolTip
                        };
                        if (itemVm.EsAtrasado && !string.IsNullOrEmpty(itemVm.ToolTipResumen))
                            itemVm.ToolTipResumen = "(Atrasado) " + itemVm.ToolTipResumen;                        // Evitar duplicados por EjecucionId (por seguridad ante cargas múltiples o datos duplicados)
                        if (Items.Any(i => i.EjecucionId == itemVm.EjecucionId))
                        {
                            _logger.LogWarning("[HistorialEjecucionesViewModel] Duplicado detectado: EjecucionId {id} para año {year} - se omite", itemVm.EjecucionId, SelectedYear);
                            continue;
                        }

                        Items.Add(itemVm);
                    }                    // Dedupe final por si quedó algún duplicado (seguridad adicional)
                    try
                    {
                        var unique = Items.GroupBy(i => i.EjecucionId).Select(g => g.First()).ToList();
                        if (unique.Count != Items.Count)
                        {
                            _logger.LogWarning("[HistorialEjecucionesViewModel] Se detectaron y eliminaron {dup} duplicados en Items", Items.Count - unique.Count);
                            Items.Clear();
                            foreach (var u in unique) Items.Add(u);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error deduplicando Items");
                    }// Enriquecer nombres faltantes consultando servicio
                    if (codigosPendientes.Count > 0)
                    {
                        foreach (var codigo in codigosPendientes)
                        {
                            try
                            {
                                var equipo = await _equipoService.GetByCodigoAsync(codigo);
                                if (equipo != null && !string.IsNullOrWhiteSpace(equipo.NombreEquipo))
                                {
                                    foreach (var item in Items.Where(i => i.CodigoEquipo.Equals(codigo, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        item.NombreEquipo = equipo.NombreEquipo!; // notifica cambio
                                        if (string.IsNullOrWhiteSpace(item.UsuarioAsignadoEquipo) && !string.IsNullOrWhiteSpace(equipo.UsuarioAsignado))
                                            item.UsuarioAsignadoEquipo = equipo.UsuarioAsignado!;
                                        if (string.IsNullOrWhiteSpace(item.Sede) && !string.IsNullOrWhiteSpace(equipo.Sede))
                                            item.Sede = equipo.Sede!;
                                    }
                                }
                            }
                            catch { /* silenciar errores individuales */ }
                        }                    }
                    
                    StatusMessage = $"{Items.Count} ejecuciones";
                    RecalcularEstadisticas();
                    // después de cargar recalcar filtro en la vista
                    RefreshFilter();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error al cargar";
                    System.Diagnostics.Debug.WriteLine(ex);
                }
                finally
                {
                    IsBusy = false;
                }
                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] ✓ LoadAsync finalizado para año {year} - total {count} items en la vista", SelectedYear, Items.Count);
            }
            finally
            {
                _loadSemaphore.Release();
                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS_VM] ↩️ Semáforo liberado - ThreadId: {threadId}", System.Threading.Thread.CurrentThread.ManagedThreadId);
            }
        }        
        /// <summary>
        /// Recalcula las estadísticas basadas en los items cargados
        /// </summary>
        private void RecalcularEstadisticas()
        {
            TotalEjecuciones = Items.Count;
            EjecutadasCount = Items.Count(x => x.Estado == 2); // Estado = 2 (Ejecutado)
            PendientesCount = Items.Count(x => x.Estado == 0 && !x.EsAtrasado); // Estado = 0 (Pendiente) y no atrasado
            AtrasadasCount = Items.Count(x => x.EsAtrasado); // Atrasados
            EnProcesoCount = Items.Count(x => x.Estado == 1); // Estado = 1 (En Proceso)
            
            // Notificar cambio en propiedad calculada
            OnPropertyChanged(nameof(NoEjecutadosCount));
        }

        private void SetupCollectionView()
        {
            if (_filteredView == null)
            {
                _filteredView = CollectionViewSource.GetDefaultView(Items);
                _filteredView.Filter = new Predicate<object?>(FilterPredicate);
            }
            else
            {
                // si ya existe, actualizar para que use la colección actual
                _filteredView = CollectionViewSource.GetDefaultView(Items);
                _filteredView.Filter = new Predicate<object?>(FilterPredicate);
            }
            OnPropertyChanged(nameof(FilteredItems));
        }

        private bool FilterPredicate(object? obj)
        {
            if (obj is not EjecucionHistorialItem it) return false;

            // Código
            if (!string.IsNullOrWhiteSpace(FiltroCodigo))
            {
                if (string.IsNullOrWhiteSpace(it.CodigoEquipo) || !it.CodigoEquipo.Contains(FiltroCodigo, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Nombre equipo
            if (!string.IsNullOrWhiteSpace(FiltroNombre))
            {
                if (string.IsNullOrWhiteSpace(it.NombreEquipo) || !it.NombreEquipo.Contains(FiltroNombre, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Usuario / Asignación
            if (!string.IsNullOrWhiteSpace(FiltroUsuario))
            {
                if (string.IsNullOrWhiteSpace(it.UsuarioAsignadoEquipo) || !it.UsuarioAsignadoEquipo.Contains(FiltroUsuario, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Semana
            if (!string.IsNullOrWhiteSpace(FiltroSemana))
            {
                if (int.TryParse(FiltroSemana, out var weekFilter))
                {
                    if (it.SemanaISO != weekFilter) return false;
                }
                else
                {
                    // si no es número, comparar como texto
                    if (!it.SemanaISO.ToString().Contains(FiltroSemana, StringComparison.OrdinalIgnoreCase)) return false;
                }
            }

            // Año
            if (SelectedYear != 0)
            {
                if (it.AnioISO != SelectedYear) return false;
            }

            return true;
        }

        private void RefreshFilter()
        {
            try
            {
                _filteredView ??= CollectionViewSource.GetDefaultView(Items);
                _filteredView.Refresh();
            }
            catch { }
        }

        // ✅ IMPLEMENTACIÓN REQUERIDA: DatabaseAwareViewModel
        protected override async Task RefreshDataAsync()
        {
            await LoadAsync();
        }        
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Datos no disponibles";
        }

        /// <summary>
        /// Limpieza de recursos y desuscripción de mensajes
        /// </summary>
        public new void Dispose()
        {
            // Desuscribirse de mensajes
            WeakReferenceMessenger.Default.Unregister<EjecucionesPlanesActualizadasMessage>(this);
            WeakReferenceMessenger.Default.Unregister<SeguimientosActualizadosMessage>(this);
            
            try { _loadSemaphore?.Dispose(); } catch { }
            base.Dispose();
        }        /// <summary>
        /// Exporta los items actuales a un archivo Excel (.xlsx) seleccionado por el usuario.
        /// </summary>
        [RelayCommand]
        private async Task ExportarItemsAsync()
        {
            try
            {
                // Tomar la vista actual filtrada y materializarla en una lista segura para el hilo de trabajo
                var itemsToExport = FilteredItems?.Cast<EjecucionHistorialItem>().ToList() ?? new List<EjecucionHistorialItem>();

                if (itemsToExport == null || itemsToExport.Count == 0)
                {
                    StatusMessage = "Nada que exportar";
                    return;
                }

                // Abrir diálogo de guardado
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = $"HistorialEjecuciones_{DateTime.Now:yyyyMMdd_HHmm}.xlsx",
                    DefaultExt = ".xlsx",
                    OverwritePrompt = true
                };

                var shown = dlg.ShowDialog();
                if (shown != true)
                {
                    StatusMessage = "Exportación cancelada";
                    return;
                }

                // Delegar exportación al servicio (separación de responsabilidades)
                await _exportService.ExportarHistorialAExcelAsync(dlg.FileName, itemsToExport);

                StatusMessage = $"Exportado {itemsToExport.Count} ejecuciones a {dlg.FileName}";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Exportación cancelada";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error exportando historial");
                StatusMessage = "Error al exportar";
            }        }
    }
}




