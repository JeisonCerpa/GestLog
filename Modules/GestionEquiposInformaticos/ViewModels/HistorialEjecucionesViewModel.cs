using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
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

    public partial class HistorialEjecucionesViewModel : ObservableObject
    {
        private readonly IPlanCronogramaService _planService;
        private readonly IEquipoInformaticoService _equipoService; // Nuevo servicio para obtener nombres

        public HistorialEjecucionesViewModel(IPlanCronogramaService planService, IEquipoInformaticoService equipoService)
        {
            _planService = planService;
            _equipoService = equipoService; // asignación
            Years = new ObservableCollection<int>(Enumerable.Range(DateTime.Now.Year - 3, 4).OrderByDescending(x=>x));
            SelectedYear = DateTime.Now.Year;
        }

        [ObservableProperty] private ObservableCollection<EjecucionHistorialItem> items = new();
        [ObservableProperty] private ObservableCollection<int> years;
        [ObservableProperty] private int selectedYear;
        [ObservableProperty] private string? filtroCodigo;
        [ObservableProperty] private string? filtroDescripcion;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string statusMessage = string.Empty;
        [ObservableProperty] private int maxRows = 500;
        [ObservableProperty] private EjecucionHistorialItem? selectedEjecucion; // para detalle
        [ObservableProperty] private bool mostrarDetalle;

        [RelayCommand]
        private async Task CargarAsync()
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

        public async Task LoadAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Cargando...";
                Items.Clear();
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
                    Items.Add(itemVm);
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
        }
    }
}
