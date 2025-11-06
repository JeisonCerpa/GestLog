using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Linq;
using System.Threading;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para la gestión de equipos.
/// </summary>
public partial class EquiposViewModel : DatabaseAwareViewModel, IDisposable
{    private readonly IEquipoService _equipoService;
    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private CurrentUserInfo _currentUser;    [ObservableProperty]
    private ObservableCollection<EquipoDto> equipos = new();

    // Colección completa sin filtrar - usada para calcular estadísticas correctas
    private ObservableCollection<EquipoDto> _allEquipos = new();

    [ObservableProperty]
    private EquipoDto? selectedEquipo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool mostrarDadosDeBaja = false;

    [ObservableProperty]
    private string filtroEquipo = "";    

    [ObservableProperty]
    private ICollectionView? equiposView;

    // Contadores de estadísticas para la vista (compatibles con la plantilla)
    [ObservableProperty]
    private int equiposActivos;

    [ObservableProperty]
    private int equiposEnMantenimiento;

    [ObservableProperty]
    private int equiposEnReparacion;

    [ObservableProperty]
    private int equiposInactivos;

    [ObservableProperty]
    private int equiposDadosBaja;

    // Helper: normalizar estado y comparar
    private static string NormalizeEstado(object? estado)
    {
        // Acepta strings y enums (o cualquier objeto), usando ToString() para normalizar
        var str = estado?.ToString() ?? string.Empty;
        return str.Trim().ToLowerInvariant().Replace(" ", "");
    }

    private static bool EsEstado(object? estado, string target)
    {
        var s = NormalizeEstado(estado);
        return s.Contains(target.Trim().ToLowerInvariant().Replace(" ", ""));
    }

    private static bool EsDadoDeBaja(object? estado)
    {
        var s = NormalizeEstado(estado);
        return s == "dadodebaja" || s.Contains("dadodebaja") || s.Contains("baja");
    }    private void RecalcularEstadisticas()
    {
        // Usar _allEquipos (colección completa sin filtros) para obtener estadísticas totales reales
        var list = _allEquipos ?? new ObservableCollection<EquipoDto>();
        EquiposActivos = list.Count(e => EsEstado(e.Estado, "activo") || EsEstado(e.Estado, "enuso"));
        EquiposEnMantenimiento = list.Count(e => EsEstado(e.Estado, "enmantenimiento"));
        EquiposEnReparacion = list.Count(e => EsEstado(e.Estado, "enreparacion") || EsEstado(e.Estado, "en reparacion"));
        EquiposInactivos = list.Count(e => EsEstado(e.Estado, "inactivo"));
        EquiposDadosBaja = list.Count(e => EsDadoDeBaja(e.Estado));
    }

    // Optimización: Control de carga para evitar recargas innecesarias
    private CancellationTokenSource? _loadCancellationToken;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private const int DEBOUNCE_DELAY_MS = 500; // 500ms de debounce
    private const int MIN_RELOAD_INTERVAL_MS = 2000; // Mínimo 2 segundos entre cargas

    [ObservableProperty]
    private bool canRegistrarEquipo;
    [ObservableProperty]
    private bool canEditarEquipo;
    [ObservableProperty]
    private bool canDarDeBajaEquipo;    [ObservableProperty]
    private bool canRegistrarMantenimientoPermiso;
    // Propiedades alias para compatibilidad con la vista XAML
    public bool CanAddEquipo => CanRegistrarEquipo;
    public bool CanDeleteEquipo => CanDarDeBajaEquipo;
    public bool CanExportEquipo => true; // Exportar no requiere permisos especiales

    public EquiposViewModel(
        IEquipoService equipoService,
        IGestLogLogger logger,
        ICronogramaService cronogramaService,
        ISeguimientoService seguimientoService,
        ICurrentUserService currentUserService,
        IDatabaseConnectionService databaseService)
        : base(databaseService, logger)
    {        try
        {
            _equipoService = equipoService;
            _cronogramaService = cronogramaService;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
              // Suscribirse a mensajes de actualización de cronogramas y seguimientos
            // OPTIMIZACIÓN: Solo recargar cuando sea realmente necesario
            WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Solo recargar si han pasado al menos 2 segundos desde la última carga
                    await LoadEquiposAsync(forceReload: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposViewModel] Error en mensaje CronogramasActualizados");
                }
            });
            
            // Para seguimientos, ser más selectivo - solo recargar si afecta equipos directamente
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Los cambios en seguimientos normalmente no afectan la lista de equipos
                    // Solo recargar si han pasado más de 5 segundos desde la última carga
                    if ((DateTime.Now - _lastLoadTime).TotalSeconds > 5)
                    {
                        await LoadEquiposAsync(forceReload: false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposViewModel] Error en mensaje SeguimientosActualizados");
                }
            });

            // Suscribirse a actualizaciones globales de equipos para recargar la lista cuando sea necesario
            WeakReferenceMessenger.Default.Register<EquiposActualizadosMessage>(this, async (r, m) =>
            {
                try
                {
                    // Evitar recargas demasiado frecuentes: delegar a LoadEquiposAsync que aplica debounce
                    await LoadEquiposAsync(forceReload: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposViewModel] Error en mensaje EquiposActualizados");
                }
            });

            EquiposView = CollectionViewSource.GetDefaultView(Equipos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
        }
        catch (Exception ex)
        {            logger?.LogError(ex, "[EquiposViewModel] Error crítico en constructor");
            throw; // Re-lanzar para que se capture en el nivel superior
        }
    }    [RelayCommand]
    public async Task LoadEquipos()
    {
        await LoadEquiposAsync(forceReload: true);
    }

    public async Task LoadEquiposAsync(bool forceReload = true)
    {
        // OPTIMIZACIÓN: Evitar cargas duplicadas innecesarias
        if (!forceReload)
        {
            var timeSinceLastLoad = DateTime.Now - _lastLoadTime;
            if (timeSinceLastLoad.TotalMilliseconds < MIN_RELOAD_INTERVAL_MS && !IsLoading)
            {
                return; // Muy pronto desde la última carga, omitir
            }
        }

        // Cancelar cualquier carga en progreso
        _loadCancellationToken?.Cancel();
        _loadCancellationToken = new CancellationTokenSource();
        var cancellationToken = _loadCancellationToken.Token;

        // Debounce: esperar un poco antes de cargar
        if (!forceReload)
        {
            try
            {
                await Task.Delay(DEBOUNCE_DELAY_MS, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; // Cancelado, otra carga está en progreso
            }
        }

        IsLoading = true;
        StatusMessage = "Cargando equipos...";
        
        try
        {        var lista = await _equipoService.GetAllAsync();
            
            if (cancellationToken.IsCancellationRequested)
                return;

            // Mantener copia completa para calcular estadísticas correctas
            _allEquipos = new ObservableCollection<EquipoDto>(lista);

            // Filtrar según MostrarDadosDeBaja para la vista
            var filtrados = MostrarDadosDeBaja ? lista : lista.Where(e => e.FechaBaja == null).ToList();
            Equipos = new ObservableCollection<EquipoDto>(filtrados);
            // Suscribirse a cambios para recalcular estadísticas cuando la colección cambie
            Equipos.CollectionChanged += Equipos_CollectionChanged;
            EquiposView = CollectionViewSource.GetDefaultView(Equipos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);

            // Actualizar contadores (ahora usa _allEquipos para totales correctos)
            RecalcularEstadisticas();

            StatusMessage = $"{Equipos.Count} equipos {(MostrarDadosDeBaja ? "(incluye dados de baja)" : "activos")} cargados.";
            _lastLoadTime = DateTime.Now;
        }
        catch (OperationCanceledException)
        {
            // Carga cancelada, no hacer nada
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar equipos");
            StatusMessage = "Error al cargar equipos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Equipos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RecalcularEstadisticas();
    }

    [RelayCommand]
    public async Task AddEquipoAsync()
    {
        try
        {
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.EquipoDialog();
            var owner = System.Windows.Application.Current?.MainWindow;
            if (owner != null) dialog.Owner = owner;
            dialog.ConfigurarParaVentanaPadre(owner);
            var result = dialog.ShowDialog();            if (result == true)
            {
                await _equipoService.AddAsync(dialog.Equipo);
                await LoadEquiposAsync(forceReload: true); // Forzar recarga tras agregar
                StatusMessage = "Equipo agregado exitosamente.";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al agregar equipo");
            StatusMessage = "Error al agregar equipo.";
        }
    }

    [RelayCommand]
    public async Task EditEquipoAsync()
    {
        if (SelectedEquipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para editar.";
            return;
        }
        try
        {
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.EquipoDialog(SelectedEquipo);
            var owner = System.Windows.Application.Current?.MainWindow;
            if (owner != null) dialog.Owner = owner;
            dialog.ConfigurarParaVentanaPadre(owner);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                // Si el usuario cambió el estado a Activo, limpiar la FechaBaja
                if (dialog.Equipo.Estado == EstadoEquipo.Activo)
                    dialog.Equipo.FechaBaja = null;                
                await _equipoService.UpdateAsync(dialog.Equipo);
                await LoadEquiposAsync(forceReload: true); // Forzar recarga tras editar
                // Reasignar SelectedEquipo para refrescar la vista de detalles
                SelectedEquipo = Equipos.FirstOrDefault(e => e.Codigo == dialog.Equipo.Codigo);
                StatusMessage = "Equipo editado exitosamente.";
                WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage());
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al editar equipo");
            StatusMessage = "Error al editar equipo.";
        }
    }

    [RelayCommand]
    public async Task DeleteEquipoAsync()
    {
        if (SelectedEquipo == null || string.IsNullOrWhiteSpace(SelectedEquipo.Codigo))
        {
            StatusMessage = "Debe seleccionar un equipo válido para dar de baja.";
            return;
        }

        // Confirmación previa
        var confirm = System.Windows.MessageBox.Show(
            $"¿Está seguro que desea dar de baja el equipo '{SelectedEquipo.Codigo}'?",
            "Confirmar baja de equipo",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            StatusMessage = "Operación cancelada por el usuario.";
            return;
        }

        // Pedir observación obligatoria
        var obsDialog = new GestLog.Views.Shared.ObservacionDialog(SelectedEquipo.Observaciones);
        var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
        if (owner != null) obsDialog.Owner = owner;
        var dialogResult = obsDialog.ShowDialog();
        if (dialogResult != true)
        {
            StatusMessage = "Operación cancelada por el usuario.";
            return;
        }

        try
        {
            SelectedEquipo.Observaciones = obsDialog.Observacion;
            SelectedEquipo.FechaBaja = DateTime.Now;
            SelectedEquipo.Estado = EstadoEquipo.DadoDeBaja; // Actualiza el estado explícitamente
            await _equipoService.UpdateAsync(SelectedEquipo);

            // Eliminar cronogramas y seguimientos pendientes
            await _cronogramaService.DeleteByEquipoCodigoAsync(SelectedEquipo.Codigo!);
            await _seguimientoService.DeletePendientesByEquipoCodigoAsync(SelectedEquipo.Codigo!);

            await LoadEquiposAsync(forceReload: true); // Forzar recarga tras dar de baja
            StatusMessage = "Equipo dado de baja exitosamente. Se eliminaron cronogramas y seguimientos pendientes.";
            WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage());
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al dar de baja equipo");
            StatusMessage = "Error al dar de baja equipo.";
        }
    }

    [RelayCommand]
    public async Task ExportarEquiposAsync()
    {
        try
        {
            var dialog = new VistaSaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos a Excel",
                FileName = "Equipos.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");
                    // Encabezados
                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Nombre";
                    ws.Cell(1, 3).Value = "Marca";
                    ws.Cell(1, 4).Value = "Estado";
                    ws.Cell(1, 5).Value = "Sede";
                    ws.Cell(1, 6).Value = "Frecuencia Mtto";
                    ws.Cell(1, 7).Value = "Precio";
                    ws.Cell(1, 8).Value = "Fecha Registro";
                    ws.Cell(1, 9).Value = "Fecha Compra";
                    ws.Cell(1, 10).Value = "Clasificacion";
                    ws.Cell(1, 11).Value = "Comprado a";
                    int row = 2;
                    foreach (var eq in Equipos)
                    {
                        ws.Cell(row, 1).Value = eq.Codigo ?? "";
                        ws.Cell(row, 2).Value = eq.Nombre ?? "";
                        ws.Cell(row, 3).Value = eq.Marca ?? "";
                        ws.Cell(row, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(row, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(row, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";
                        ws.Cell(row, 7).Value = eq.Precio ?? 0;
                        ws.Cell(row, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 9).Value = eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 10).Value = eq.Clasificacion ?? "";
                        ws.Cell(row, 11).Value = eq.CompradoA ?? "";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);
                });
                StatusMessage = $"Exportación completada: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al exportar equipos");
            StatusMessage = "Error al exportar equipos.";
        }
    }

    private TEnum? ParseEnumFlexible<TEnum>(string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u").Replace("ñ", "n").Replace(" ", "").ToLowerInvariant();
        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            var normName = name.Trim().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u").Replace("ñ", "n").Replace(" ", "").ToLowerInvariant();
            if (normalized == normName)
            {
                if (Enum.TryParse<TEnum>(name, out var result))
                    return result;
            }
        }
        return null;
    }

    // Listas para ComboBox
    public IEnumerable<EstadoEquipo> EstadosEquipo => System.Enum.GetValues(typeof(EstadoEquipo)) as EstadoEquipo[] ?? new EstadoEquipo[0];
    public IEnumerable<TipoMantenimiento> TiposMantenimiento => System.Enum.GetValues(typeof(TipoMantenimiento)) as TipoMantenimiento[] ?? new TipoMantenimiento[0];
    public IEnumerable<Sede> Sedes => System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0];
    public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento => (System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0])
        .Where(f => f != FrecuenciaMantenimiento.Correctivo && f != FrecuenciaMantenimiento.Predictivo);    partial void OnMostrarDadosDeBajaChanged(bool value)
    {
        // Aplicar filtro a la colección existente sin recargar del servicio
        if (_allEquipos == null || _allEquipos.Count == 0)
        {
            // Si aún no hay datos, recargar
            _ = LoadEquiposAsync(forceReload: true);
            return;
        }

        // Filtrar según MostrarDadosDeBaja y crear nueva ObservableCollection
        var filtrados = value 
            ? _allEquipos.ToList() 
            : _allEquipos.Where(e => e.FechaBaja == null).ToList();
        Equipos = new ObservableCollection<EquipoDto>(filtrados);
        
        // Actualizar vista y estadísticas
        EquiposView = CollectionViewSource.GetDefaultView(Equipos);
        if (EquiposView != null)
        {
            EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
            EquiposView.Refresh();
        }

        StatusMessage = $"{Equipos.Count} equipos {(value ? "(incluye dados de baja)" : "activos")} mostrados.";
    }

    private CancellationTokenSource? _debounceToken;

    partial void OnFiltroEquipoChanged(string value)
    {
        _debounceToken?.Cancel();
        _debounceToken = new CancellationTokenSource();
        var token = _debounceToken.Token;
        Task.Run(async () =>
        {
            await Task.Delay(250, token); // 250ms debounce
            if (!token.IsCancellationRequested)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => EquiposView?.Refresh());
            }
        }, token);
    }

    partial void OnEquiposChanged(ObservableCollection<EquipoDto> value)
    {
        EquiposView = CollectionViewSource.GetDefaultView(Equipos);
        if (EquiposView != null)
            EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
        EquiposView?.Refresh();
    }

    private bool FiltrarEquipo(object obj)
    {
        if (obj is not EquipoDto eq) return false;
        if (string.IsNullOrWhiteSpace(FiltroEquipo)) return true;
        // Permitir múltiples términos separados por punto y coma
        var terminos = FiltroEquipo.Split(';')
            .Select(t => RemoverTildes(t.Trim()).ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();
        // Campos relevantes para filtrar
        var campos = new[]
        {
            RemoverTildes(eq.Codigo ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Nombre ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Marca ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Sede?.ToString() ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Estado?.ToString() ?? "").ToLowerInvariant(),
            RemoverTildes(eq.FrecuenciaMtto?.ToString() ?? "").ToLowerInvariant(),
            eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? ""
        };
        // Todos los términos deben estar presentes en algún campo
        return terminos.All(term => campos.Any(campo => campo.Contains(term)));
    }

    private string RemoverTildes(string texto)
    {
        return texto
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ü", "u")
            .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
            .Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U")
            .Replace("ñ", "n").Replace("Ñ", "N");
    }

    [RelayCommand]
    public async Task ExportarEquiposFiltradosAsync()
    {
        var dialog = new VistaSaveFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            Title = "Exportar equipos filtrados a Excel",
            FileName = "EquiposFiltrados.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Exportando a Excel...";
            try
            {
                var filtrados = EquiposView?.Cast<EquipoDto>().ToList() ?? new List<EquipoDto>();
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");
                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Nombre";
                    ws.Cell(1, 3).Value = "Marca";
                    ws.Cell(1, 4).Value = "Estado";
                    ws.Cell(1, 5).Value = "Sede";
                    ws.Cell(1, 6).Value = "Frecuencia Mtto";
                    ws.Cell(1, 7).Value = "Precio";
                    ws.Cell(1, 8).Value = "Fecha Registro";
                    ws.Cell(1, 9).Value = "Fecha Compra";
                    ws.Cell(1, 10).Value = "Clasificacion";
                    ws.Cell(1, 11).Value = "Comprado a";
                    int row = 2;
                    foreach (var eq in filtrados)
                    {
                        ws.Cell(row, 1).Value = eq.Codigo ?? "";
                        ws.Cell(row, 2).Value = eq.Nombre ?? "";
                        ws.Cell(row, 3).Value = eq.Marca ?? "";
                        ws.Cell(row, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(row, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(row, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";
                        ws.Cell(row, 7).Value = eq.Precio ?? 0;
                        ws.Cell(row, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 9).Value = eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 10).Value = eq.Clasificacion ?? "";
                        ws.Cell(row, 11).Value = eq.CompradoA ?? "";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);
                });
                StatusMessage = $"Exportación completada: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar equipos filtrados");
                StatusMessage = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }    [RelayCommand]
    public void VerDetallesEquipo(EquipoDto? equipo)
    {
        if (equipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para ver detalles.";
            return;
        }

        try
        {
            SelectedEquipo = equipo;
            var detalleWindow = new GestLog.Views.Tools.GestionMantenimientos.EquipoDetalleModalWindow
            {
                DataContext = this
            };

            var ownerWindow = System.Windows.Application.Current?.MainWindow;
            // Asegurar overlay correcto en multi-monitor/DPI
            try
            {
                detalleWindow.ConfigurarParaVentanaPadre(ownerWindow);
            }
            catch (System.Exception exCfg)
            {
                _logger.LogWarning(exCfg, "[EquiposViewModel] No se pudo configurar bounds del detalle - continuando con CenterOwner");
            }

            detalleWindow.ShowDialog();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al abrir detalles del equipo");
            StatusMessage = "Error al abrir detalles del equipo.";
        }
    }

    public bool CanRegistrarMantenimiento(EquipoDto? equipo)
    {
        // Solo permite registrar mantenimiento si el equipo está ACTIVO
        return CanRegistrarMantenimientoPermiso && equipo != null && (equipo.Estado == EstadoEquipo.Activo);
    }

    [RelayCommand(CanExecute = nameof(CanRegistrarMantenimiento))]
    public async Task RegistrarMantenimientoAsync(EquipoDto? equipo)
    {
        if (equipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para registrar mantenimiento.";
            return;
        }
        try
        {
            var now = DateTime.Now;
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int semanaActual = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int anioActual = now.Year;
            var seguimiento = new SeguimientoMantenimientoDto
            {
                Codigo = equipo.Codigo,
                Nombre = equipo.Nombre,
                Semana = semanaActual,
                Anio = anioActual,
                FechaRegistro = now,
                TipoMtno = TipoMantenimiento.Correctivo, // Preseleccionado
                Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo // Los correctivos se registran cuando ya se han realizado
                // Los campos TipoMtno y Frecuencia se llenan en el diálogo y al guardar
            };
            // Asignar la frecuencia por defecto para el flujo de Equipos (Correctivo) y abrir el diálogo en modo restringido
            seguimiento.Frecuencia = FrecuenciaMantenimiento.Correctivo;
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(seguimiento, true);
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) dialog.Owner = owner;
            var result = dialog.ShowDialog();
            if (result == true)            {
                // Asignar la frecuencia automáticamente según el tipo
                if (dialog.Seguimiento.TipoMtno == TipoMantenimiento.Correctivo)
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Correctivo;
                else if (dialog.Seguimiento.TipoMtno == TipoMantenimiento.Predictivo)
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Predictivo;
                else
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Anual;
                await _seguimientoService.AddAsync(dialog.Seguimiento);
                StatusMessage = "Mantenimiento registrado exitosamente.";
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al registrar mantenimiento");
            StatusMessage = "Error al registrar mantenimiento.";
        }
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
    }    private void RecalcularPermisos()
    {
        CanRegistrarEquipo = _currentUser.HasPermission("GestionMantenimientos.NuevoEquipo");
        CanEditarEquipo = _currentUser.HasPermission("GestionMantenimientos.EditarEquipo"); // Si existe este permiso en la BD
        CanDarDeBajaEquipo = _currentUser.HasPermission("GestionMantenimientos.DarDeBajaEquipo");
        CanRegistrarMantenimientoPermiso = _currentUser.HasPermission("GestionMantenimientos.RegistrarMantenimiento");
        // Notificar cambios en las propiedades alias
        OnPropertyChanged(nameof(CanAddEquipo));
        OnPropertyChanged(nameof(CanDeleteEquipo));
        OnPropertyChanged(nameof(CanExportEquipo));    }

    // ✅ IMPLEMENTACIÓN REQUERIDA: DatabaseAwareViewModel
    protected override async Task RefreshDataAsync()
    {
        await LoadEquiposAsync(forceReload: true);
    }

    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexión - Módulo no disponible";
    }

    // Implementar IDisposable para limpieza de recursos
    public new void Dispose()
    {
        _loadCancellationToken?.Cancel();
        _loadCancellationToken?.Dispose();
        _debounceToken?.Cancel();
        _debounceToken?.Dispose();
        
        // Desuscribirse de mensajes
        WeakReferenceMessenger.Default.Unregister<CronogramasActualizadosMessage>(this);
        WeakReferenceMessenger.Default.Unregister<SeguimientosActualizadosMessage>(this);
        WeakReferenceMessenger.Default.Unregister<EquiposActualizadosMessage>(this);
        
        if (_currentUserService != null)
            _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
        
        base.Dispose();
    }

    [RelayCommand]
    public async Task ExportarInteligenteAsync()
    {
        // Si hay filtro aplicado (texto no vacío) o la vista está filtrada, exportar filtrados
        try
        {
            var hayFiltroActivo = !string.IsNullOrWhiteSpace(FiltroEquipo);
            // Si hay filtro textual, preferir ExportarEquiposFiltradosAsync
            if (hayFiltroActivo)
            {
                await ExportarEquiposFiltradosAsync();
                return;
            }

            // Si no hay filtro textual, verificar si la vista tiene un filtro activo distinto de null
            var viewHasFilter = EquiposView != null && EquiposView.Filter != null;
            if (viewHasFilter)
            {
                // Si el filtro es activo pero no hay texto (por ejemplo MostrarDadosDeBaja cambia), exportar lo que muestra la vista
                await ExportarEquiposFiltradosAsync();
                return;
            }

            // Si no hay filtro, exportar todo (la colección completa _allEquipos si está disponible, sino Equipos)
            // Temporalmente cambiar la propiedad Equipos para asegurarnos de exportar todo sin filtrar
            var originalEquipos = Equipos;
            try
            {
                if (_allEquipos != null && _allEquipos.Count > 0)
                {
                    Equipos = new ObservableCollection<EquipoDto>(_allEquipos);
                    EquiposView = CollectionViewSource.GetDefaultView(Equipos);
                    if (EquiposView != null)
                        EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
                }

                await ExportarEquiposAsync();
            }
            finally
            {
                // Restaurar colección original y vista
                Equipos = originalEquipos;
                EquiposView = CollectionViewSource.GetDefaultView(Equipos);
                if (EquiposView != null)
                    EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
                EquiposView?.Refresh();
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error en ExportarInteligente");
            StatusMessage = "Error al exportar equipos.";
        }
    }
}
