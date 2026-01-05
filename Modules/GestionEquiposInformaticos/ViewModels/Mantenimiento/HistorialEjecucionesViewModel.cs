using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
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
using ClosedXML.Excel;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    public class EjecucionHistorialItem : ObservableObject
    {
        public Guid EjecucionId { get; set; }
        public Guid PlanId { get; set; }
        public string CodigoEquipo { get; set; } = string.Empty;
        public string NombreEquipo { get; set; } = string.Empty;
        [Obsolete("DescripcionPlan is obsolete for the current view.")]
        public string DescripcionPlan { get; set; } = string.Empty; // (Obsoleto para la vista actual)
        public int AnioISO { get; set; }
        public int SemanaISO { get; set; }
        public DateTime FechaObjetivo { get; set; }
        public DateTime? FechaEjecucion { get; set; }
        public byte Estado { get; set; }
        public string UsuarioEjecuta { get; set; } = string.Empty;
        public string? Resumen { get; set; }
        public string UsuarioAsignadoEquipo { get; set; } = string.Empty; // Nuevo: usuario asignado al equipo
        // NUEVO: estado derivado atrasado (no ejecutado y fecha objetivo pasada)
        public bool EsAtrasado => FechaEjecucion == null && FechaObjetivo.Date < DateTime.Today && Estado != 2; // 2=Ejecutado
        public string EstadoDescripcion => EsAtrasado ? "Atrasado" : Estado switch // Mapeo simple de estados
        {
            0 => "Pendiente",
            1 => "En Proceso",
            2 => "Ejecutado",
            3 => "Observado",
            _ => "Desconocido"
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
    }

    public partial class HistorialEjecucionesViewModel : DatabaseAwareViewModel, IDisposable
    {        
        private readonly IPlanCronogramaService _planService;
        private readonly IEquipoInformaticoService _equipoService;        
        // Evitar ejecuciones concurrentes de LoadAsync que causen duplicados
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        public HistorialEjecucionesViewModel(
            IPlanCronogramaService planService, 
            IEquipoInformaticoService equipoService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _planService = planService;
            _equipoService = equipoService;
            Years = new ObservableCollection<int>();
            SelectedYear = DateTime.Now.Year;
            // Cargar años disponibles de forma asíncrona
            _ = CargarAñosDisponiblesAsync();
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
        partial void OnFiltroSemanaChanged(string? value) => RefreshFilter();
        // Al cambiar año, recargar los items del año seleccionado (para que se consulten desde BD)
        partial void OnSelectedYearChanged(int value) => _ = LoadAsync();

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

                _logger.LogDebug("[HistorialEjecucionesViewModel] Años finales en la vista: {anos}", string.Join(", ", Years));

                // Seleccionar el año actual si está disponible; si no, seleccionar el primero
                if (anosDisponibles.Contains(DateTime.Now.Year))
                {
                    SelectedYear = DateTime.Now.Year;
                }
                else if (anosDisponibles.Count > 0)
                {
                    SelectedYear = anosDisponibles.First();
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
        }

        public async Task LoadAsync()
        {
            // Serializar llamadas para evitar repetidas cargas concurrentes que provoquen duplicados
            await _loadSemaphore.WaitAsync();
            try
            {
                _logger.LogDebug("[HistorialEjecucionesViewModel] Iniciando LoadAsync para año {year}", SelectedYear);
                try
                {
                    IsBusy = true;
                    StatusMessage = "Cargando...";
                    Items.Clear();
                    // asegurarse de que la vista filtrada está inicializada antes de añadir
                    SetupCollectionView();
                    var ejecuciones = await _planService.GetEjecucionesByAnioAsync(SelectedYear);
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
                        }
                        var nombreEquipo = e.Plan?.Equipo?.NombreEquipo;
                        var codigoEquipo = e.Plan?.CodigoEquipo ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(nombreEquipo) && !string.IsNullOrWhiteSpace(codigoEquipo))
                        {
                            nombreEquipo = "(cargando...)"; // placeholder
                            codigosPendientes.Add(codigoEquipo);
                        }
                        var itemVm = new EjecucionHistorialItem
                        {
                            EjecucionId = e.EjecucionId,
                            PlanId = e.PlanId,
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
                            DetalleItems = detalleItems,
                            ToolTipResumen = toolTip
                        };
                        if (itemVm.EsAtrasado && !string.IsNullOrEmpty(itemVm.ToolTipResumen))
                            itemVm.ToolTipResumen = "(Atrasado) " + itemVm.ToolTipResumen;

                        // Evitar duplicados por EjecucionId (por seguridad ante cargas múltiples o datos duplicados)
                        if (Items.Any(i => i.EjecucionId == itemVm.EjecucionId))
                        {
                            _logger.LogWarning("[HistorialEjecucionesViewModel] Duplicado detectado: EjecucionId {id} para año {year} - se omite", itemVm.EjecucionId, SelectedYear);
                            continue;
                        }

                        Items.Add(itemVm);
                    }

                    // Dedupe final por si quedó algún duplicado (seguridad adicional)
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
                    }

                    // Enriquecer nombres faltantes consultando servicio
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
                                    }
                                }
                            }
                            catch { /* silenciar errores individuales */ }
                        }                
                    }
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
                _logger.LogDebug("[HistorialEjecucionesViewModel] LoadAsync finalizado para año {year} - total {count}", SelectedYear, Items.Count);
            }
            finally
            {
                _loadSemaphore.Release();
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
        }

        /// <summary>
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

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();

                    // Hoja principal con resumen por ejecución (ahora con métricas claras)
                    var ws = workbook.Worksheets.Add("Historial");
                    var headers = new[] { "Código", "Nombre", "Año", "Semana", "Fecha Objetivo", "Fecha Ejecución", "Estado", "Usuario Asignado", "Total Ítems", "Ítems OK", "% Completado", "Resumen" };
                    for (int i = 0; i < headers.Length; i++)
                        ws.Cell(1, i + 1).Value = headers[i];

                    int row = 2;
                    int totalItemsAll = 0;
                    int totalOkAll = 0;

                    foreach (var it in itemsToExport)
                    {
                        int totalItems = it.DetalleItems?.Count ?? 0;
                        int okCount = it.DetalleItems?.Count(d => d.Completado) ?? 0;
                        double pct = totalItems > 0 ? (double)okCount / totalItems : 0.0;

                        ws.Cell(row, 1).Value = it.CodigoEquipo;
                        ws.Cell(row, 2).Value = it.NombreEquipo;
                        ws.Cell(row, 3).Value = it.AnioISO;
                        ws.Cell(row, 4).Value = it.SemanaISO;

                        ws.Cell(row, 5).Value = it.FechaObjetivo;
                        ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                        if (it.FechaEjecucion.HasValue)
                        {
                            ws.Cell(row, 6).Value = it.FechaEjecucion.Value;
                            ws.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                        }
                        else
                        {
                            ws.Cell(row, 6).Value = string.Empty;
                        }

                        ws.Cell(row, 7).Value = it.EstadoDescripcion;
                        ws.Cell(row, 8).Value = it.UsuarioAsignadoEquipo;

                        ws.Cell(row, 9).Value = totalItems;
                        ws.Cell(row, 10).Value = okCount;
                        ws.Cell(row, 11).Value = pct; // valor entre 0 y 1
                        ws.Cell(row, 11).Style.NumberFormat.Format = "0.00%";

                        ws.Cell(row, 12).Value = it.Resumen;

                        totalItemsAll += totalItems;
                        totalOkAll += okCount;
                        row++;
                    }

                    // Formato encabezado y usabilidad
                    var headerRange = ws.Range(1, 1, 1, headers.Length);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
                    ws.SheetView.FreezeRows(1);
                    ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
                    ws.Columns().AdjustToContents();

                    // Hoja detalle: checklist por ejecución (con estado legible)
                    var wsChk = workbook.Worksheets.Add("Checklist");
                    var chkHeaders = new[] { "EjecucionId", "PlanId", "CódigoEquipo", "NombreEquipo", "ItemId", "Descripción", "Completado", "Estado Item", "Observación" };
                    for (int i = 0; i < chkHeaders.Length; i++)
                        wsChk.Cell(1, i + 1).Value = chkHeaders[i];

                    int chkRow = 2;
                    foreach (var ejec in itemsToExport)
                    {
                        if (ejec.DetalleItems == null || ejec.DetalleItems.Count == 0)
                            continue;

                        foreach (var det in ejec.DetalleItems)
                        {
                            wsChk.Cell(chkRow, 1).Value = ejec.EjecucionId.ToString();
                            wsChk.Cell(chkRow, 2).Value = ejec.PlanId.ToString();
                            wsChk.Cell(chkRow, 3).Value = ejec.CodigoEquipo;
                            wsChk.Cell(chkRow, 4).Value = ejec.NombreEquipo;
                            wsChk.Cell(chkRow, 5).Value = det.Id?.ToString() ?? string.Empty;
                            wsChk.Cell(chkRow, 6).Value = det.Descripcion;
                            wsChk.Cell(chkRow, 7).Value = det.Completado ? "Sí" : "No";

                            // Estado legible: OK / Observado / Pendiente
                            string estadoItem = det.Completado ? "OK" : (!string.IsNullOrWhiteSpace(det.Observacion) ? "Observado" : "Pendiente");
                            wsChk.Cell(chkRow, 8).Value = estadoItem;
                            wsChk.Cell(chkRow, 9).Value = det.Observacion ?? string.Empty;
                            chkRow++;
                        }
                    }

                    var chkHeaderRange = wsChk.Range(1, 1, 1, chkHeaders.Length);
                    chkHeaderRange.Style.Font.Bold = true;
                    chkHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
                    wsChk.SheetView.FreezeRows(1);
                    wsChk.Range(1, 1, chkRow - 1, chkHeaders.Length).SetAutoFilter();
                    wsChk.Columns().AdjustToContents();

                    // Hoja resumen explicativa para usuarios no técnicos
                    var wsSummary = workbook.Worksheets.Add("Resumen");
                    wsSummary.Cell(1, 1).Value = "Resumen de exportación";
                    wsSummary.Cell(1, 1).Style.Font.Bold = true;
                    wsSummary.Cell(3, 1).Value = "Fecha de generación:";
                    wsSummary.Cell(3, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                    wsSummary.Cell(4, 1).Value = "Hojas incluidas:";
                    wsSummary.Cell(4, 2).Value = "Historial (resumen por ejecución)";
                    wsSummary.Cell(5, 2).Value = "Checklist (cada ítem de checklist por fila)";

                    wsSummary.Cell(7, 1).Value = "Qué significa cada columna (breve):";
                    wsSummary.Cell(8, 1).Value = "Código:"; wsSummary.Cell(8, 2).Value = "Código identificador del equipo.";
                    wsSummary.Cell(9, 1).Value = "Nombre:"; wsSummary.Cell(9, 2).Value = "Nombre legible del equipo.";
                    wsSummary.Cell(10, 1).Value = "Total Ítems:"; wsSummary.Cell(10, 2).Value = "Número total de ítems en el checklist para esa ejecución.";
                    wsSummary.Cell(11, 1).Value = "Ítems OK:"; wsSummary.Cell(11, 2).Value = "Número de ítems marcados como completados.";
                    wsSummary.Cell(12, 1).Value = "% Completado:"; wsSummary.Cell(12, 2).Value = "Porcentaje de ítems completados (formato porcentaje).";
                    wsSummary.Cell(14, 1).Value = "Checklist - Estado Item:"; wsSummary.Cell(14, 2).Value = "OK = completado, Observado = tiene observación, Pendiente = no completado y sin observación.";

                    wsSummary.Columns().AdjustToContents();

                    // Guardar workbook
                    workbook.SaveAs(dlg.FileName);
                });

                StatusMessage = $"Exportado {itemsToExport.Count} ejecuciones a {dlg.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HistorialEjecucionesViewModel] Error exportando historial a Excel (mejorado)");
                StatusMessage = "Error al exportar";
            }
        }

        // Método auxiliar para escapado CSV (conservado para compatibilidad, no usado en exportación Excel)
        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var escaped = value.Replace("\"", "\"\"");
            // Encerrar en comillas si contiene comas, comillas o saltos de línea
            if (escaped.Contains(',') || escaped.Contains('"') || escaped.Contains('\n') || escaped.Contains('\r'))
                return "\"" + escaped + "\"";
            return escaped;
        }
    }
}




