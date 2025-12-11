using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Services;
using GestLog.Modules.GestionMantenimientos.Services.Export;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
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

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Equipos;

/// <summary>
/// ViewModel para la gestiÃ³n de equipos.
/// </summary>
public partial class EquiposViewModel : DatabaseAwareViewModel, IDisposable
{    
    private readonly IEquipoService _equipoService;
    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private CurrentUserInfo _currentUser;    

    [ObservableProperty]
    private ObservableCollection<EquipoDto> equipos = new();

    // ColecciÃ³n completa sin filtrar - usada para calcular estadÃ­sticas correctas
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

    // Contadores de estadÃ­sticas para la vista (compatibles con la plantilla)
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

    [ObservableProperty]
    private int historialPaginaActual = 1;

    [ObservableProperty]
    private int historialRegistrosPorPagina = 10;

    [ObservableProperty]
    private int historialTotalPaginas = 0;

    [ObservableProperty]
    private ObservableCollection<SeguimientoMantenimientoDto> historialMantenimientosVisibles = new();

    [ObservableProperty]
    private bool historialPuedeIrSiguiente;

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
    }    

    private void RecalcularEstadisticas()
    {
        // Usar _allEquipos (colecciÃ³n completa sin filtros) para obtener estadÃ­sticas totales reales
        var list = _allEquipos ?? new ObservableCollection<EquipoDto>();
        EquiposActivos = list.Count(e => EsEstado(e.Estado, "activo") || EsEstado(e.Estado, "enuso"));
        EquiposEnMantenimiento = list.Count(e => EsEstado(e.Estado, "enmantenimiento"));
        EquiposEnReparacion = list.Count(e => EsEstado(e.Estado, "enreparacion") || EsEstado(e.Estado, "en reparacion"));
        EquiposInactivos = list.Count(e => EsEstado(e.Estado, "inactivo"));
        EquiposDadosBaja = list.Count(e => EsDadoDeBaja(e.Estado));
    }    /// <summary>
    /// Se ejecuta cuando cambia el equipo seleccionado. Carga el historial de mantenimientos realizados.
    /// </summary>
    partial void OnSelectedEquipoChanged(EquipoDto? value)
    {
        if (value == null)
            return;

        // Cargar los mantenimientos realizados de forma asÃ­ncrona
        _ = CargarMantenimientosRealizadosAsync(value);
        
        // Resetear paginaciÃ³n
        HistorialPaginaActual = 1;
        ActualizarHistorialVisible();
    }

    /// <summary>
    /// Carga los mantenimientos realizados (completados) del equipo seleccionado.
    /// Excluye los mantenimientos pendientes.
    /// </summary>
    private async Task CargarMantenimientosRealizadosAsync(EquipoDto equipo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(equipo.Codigo))
                return;

            // Obtener todos los seguimientos
            var todosSeguimientos = await _seguimientoService.GetSeguimientosAsync();

            // Filtrar solo los del equipo actual y que no estÃ©n pendientes
            var mantenimientosRealizados = todosSeguimientos
                .Where(s => s.Codigo == equipo.Codigo && 
                           s.Estado != EstadoSeguimientoMantenimiento.Pendiente &&
                           s.Estado != EstadoSeguimientoMantenimiento.Atrasado)
                .OrderByDescending(s => s.FechaRegistro) // Ordenar por fecha mÃ¡s reciente primero
                .ToList();

            // Limpiar y actualizar la colecciÃ³n de mantenimientos realizados
            equipo.MantenimientosRealizados.Clear();
            foreach (var m in mantenimientosRealizados)
            {
                equipo.MantenimientosRealizados.Add(m);
            }

            ActualizarHistorialVisible();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al cargar mantenimientos realizados para el equipo {equipo.Codigo}");
        }
    }

    /// <summary>
    /// Actualiza el historial visible segÃºn la pÃ¡gina actual.
    /// </summary>
    private void ActualizarHistorialVisible()
    {
        if (SelectedEquipo?.MantenimientosRealizados == null || SelectedEquipo.MantenimientosRealizados.Count == 0)
        {
            HistorialTotalPaginas = 0;
            HistorialMantenimientosVisibles.Clear();
            return;
        }

        // Ordenar de mÃ¡s recientes a mÃ¡s antiguos
        var mantenimientosOrdenados = SelectedEquipo.MantenimientosRealizados
            .OrderByDescending(m => m.FechaRegistro)
            .ToList();

        // Calcular total de pÃ¡ginas
        HistorialTotalPaginas = (int)Math.Ceiling((double)mantenimientosOrdenados.Count / HistorialRegistrosPorPagina);

        // Validar pÃ¡gina actual
        if (HistorialPaginaActual > HistorialTotalPaginas)
            HistorialPaginaActual = Math.Max(1, HistorialTotalPaginas);

        // Obtener registros de la pÃ¡gina actual
        var registrosPagina = mantenimientosOrdenados
            .Skip((HistorialPaginaActual - 1) * HistorialRegistrosPorPagina)
            .Take(HistorialRegistrosPorPagina)
            .ToList();

        HistorialMantenimientosVisibles.Clear();
        foreach (var registro in registrosPagina)
        {
            HistorialMantenimientosVisibles.Add(registro);
        }

        // Actualizar estado de los botones de paginaciÃ³n
        HistorialPuedeIrSiguiente = HistorialPaginaActual < HistorialTotalPaginas;
    }

    /// <summary>
    /// Ir a la pÃ¡gina anterior del historial.
    /// </summary>
    [RelayCommand]
    private void HistorialPaginaAnterior()
    {
        if (HistorialPaginaActual > 1)
        {
            HistorialPaginaActual--;
            ActualizarHistorialVisible();
        }
    }

    /// <summary>
    /// Ir a la pÃ¡gina siguiente del historial.
    /// </summary>
    [RelayCommand]
    private void HistorialPaginaSiguiente()
    {
        if (HistorialPaginaActual < HistorialTotalPaginas)
        {
            HistorialPaginaActual++;
            ActualizarHistorialVisible();
        }
    }

    /// <summary>
    /// Exporta la hoja de vida completa del equipo a Excel.
    /// </summary>
    [RelayCommand]
    private async Task ExportarHojaVidaEquipo()
    {
        try
        {
            if (SelectedEquipo == null)
            {
                StatusMessage = "Seleccione un equipo para exportar.";
                return;
            }

            IsLoading = true;
            StatusMessage = "Generando hoja de vida...";

            // Crear servicio de exportaciÃ³n
            var exportService = new HojaVidaExportService();
            
            // Obtener todos los mantenimientos realizados (no paginados)
            var todosSeguimientos = await _seguimientoService.GetSeguimientosAsync();
            var mantenimientosRealizados = todosSeguimientos
                .Where(s => s.Codigo == SelectedEquipo.Codigo && 
                           s.Estado != EstadoSeguimientoMantenimiento.Pendiente &&
                           s.Estado != EstadoSeguimientoMantenimiento.Atrasado)
                .OrderByDescending(s => s.FechaRegistro)
                .ToList();

            // Abrir diÃ¡logo para guardar archivo
            var dlg = new VistaSaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"HojaVida_{SelectedEquipo.Codigo}_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                await exportService.ExportarHojaVidaAsync(
                    SelectedEquipo,
                    mantenimientosRealizados,
                    dlg.FileName
                );

                StatusMessage = $"Hoja de vida exportada correctamente a {Path.GetFileName(dlg.FileName)}";
                System.Windows.MessageBox.Show("Â¡Hoja de vida exportada correctamente!", "Ã‰xito", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar hoja de vida");
            StatusMessage = "Error al exportar hoja de vida.";
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // OptimizaciÃ³n: Control de carga para evitar recargas innecesarias
    private CancellationTokenSource? _loadCancellationToken;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private const int DEBOUNCE_DELAY_MS = 500; // 500ms de debounce
    private const int MIN_RELOAD_INTERVAL_MS = 2000; // MÃ­nimo 2 segundos entre cargas

    [ObservableProperty]
    private bool canRegistrarEquipo;
    [ObservableProperty]
    private bool canEditarEquipo;
    [ObservableProperty]
    private bool canDarDeBajaEquipo;    
    [ObservableProperty]
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
    {        
        try
        {
            _equipoService = equipoService;
            _cronogramaService = cronogramaService;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService;
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
              // Suscribirse a mensajes de actualizaciÃ³n de cronogramas y seguimientos
            // OPTIMIZACIÃ“N: Solo recargar cuando sea realmente necesario
            WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Solo recargar si han pasado al menos 2 segundos desde la Ãºltima carga
                    await LoadEquiposAsync(forceReload: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquiposViewModel] Error en mensaje CronogramasActualizados");
                }
            });
            
            // Para seguimientos, ser mÃ¡s selectivo - solo recargar si afecta equipos directamente
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Los cambios en seguimientos normalmente no afectan la lista de equipos
                    // Solo recargar si han pasado mÃ¡s de 5 segundos desde la Ãºltima carga
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
        {            
            logger?.LogError(ex, "[EquiposViewModel] Error crÃ­tico en constructor");
            throw; // Re-lanzar para que se capture en el nivel superior
        }
    }    

    [RelayCommand]
    public async Task LoadEquipos()
    {
        await LoadEquiposAsync(forceReload: true);
    }

    public async Task LoadEquiposAsync(bool forceReload = true)
    {
        // OPTIMIZACIÃ“N: Evitar cargas duplicadas innecesarias
        if (!forceReload)
        {
            var timeSinceLastLoad = DateTime.Now - _lastLoadTime;
            if (timeSinceLastLoad.TotalMilliseconds < MIN_RELOAD_INTERVAL_MS && !IsLoading)
            {
                return; // Muy pronto desde la Ãºltima carga, omitir
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
                return; // Cancelado, otra carga estÃ¡ en progreso
            }
        }

        IsLoading = true;
        StatusMessage = "Cargando equipos...";
        
        try
        {        
            var lista = await _equipoService.GetAllAsync();
            
            if (cancellationToken.IsCancellationRequested)
                return;

            // Mantener copia completa para calcular estadÃ­sticas correctas
            _allEquipos = new ObservableCollection<EquipoDto>(lista);

            // Filtrar segÃºn MostrarDadosDeBaja para la vista
            var filtrados = MostrarDadosDeBaja ? lista : lista.Where(e => e.FechaBaja == null).ToList();
            Equipos = new ObservableCollection<EquipoDto>(filtrados);
            // Suscribirse a cambios para recalcular estadÃ­sticas cuando la colecciÃ³n cambie
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
    }    [RelayCommand]
    public async Task AddEquipoAsync()
    {
        try
        {
            var dialog = new GestLog.Modules.GestionMantenimientos.Views.Equipos.EquipoDialog();
            var owner = System.Windows.Application.Current?.MainWindow;
            if (owner != null) dialog.Owner = owner;
            dialog.ConfigurarParaVentanaPadre(owner);
            var result = dialog.ShowDialog();            
            if (result == true)
            {
                // ðŸ”§ Ejecutar la adiciÃ³n en un thread background para no bloquear la UI
                IsLoading = true;
                StatusMessage = "Guardando equipo...";
                
                await Task.Run(async () =>
                {
                    await _equipoService.AddAsync(dialog.Equipo);
                });
                
                // Recargar en thread background tambiÃ©n
                await Task.Run(async () =>
                {
                    await LoadEquiposAsync(forceReload: true);
                });
                
                StatusMessage = "Equipo agregado exitosamente.";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al agregar equipo");
            StatusMessage = "Error al agregar equipo.";
        }
        finally
        {
            IsLoading = false;
        }
    }[RelayCommand]
    public async Task EditEquipoAsync()
    {
        if (SelectedEquipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para editar.";
            return;
        }        try
        {
            var dialog = new GestLog.Modules.GestionMantenimientos.Views.Equipos.EquipoDialog(SelectedEquipo);
            var owner = System.Windows.Application.Current?.MainWindow;
            if (owner != null) dialog.Owner = owner;
            dialog.ConfigurarParaVentanaPadre(owner);
            var result = dialog.ShowDialog();
            if (result == true)
            {
                // Si el usuario cambiÃ³ el estado a Activo, limpiar la FechaBaja
                if (dialog.Equipo.Estado == EstadoEquipo.Activo)
                    dialog.Equipo.FechaBaja = null;
                
                // ðŸ”§ Ejecutar la actualizaciÃ³n en un thread background para no bloquear la UI
                IsLoading = true;
                StatusMessage = "Guardando cambios...";
                
                await Task.Run(async () =>
                {
                    await _equipoService.UpdateAsync(dialog.Equipo);
                });
                
                // Recargar en thread background tambiÃ©n
                await Task.Run(async () =>
                {
                    await LoadEquiposAsync(forceReload: true);
                });
                
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
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteEquipoAsync()
    {
        if (SelectedEquipo == null || string.IsNullOrWhiteSpace(SelectedEquipo.Codigo))
        {
            StatusMessage = "Debe seleccionar un equipo vÃ¡lido para dar de baja.";
            return;
        }

        // ConfirmaciÃ³n previa
        var confirm = System.Windows.MessageBox.Show(
            $"Â¿EstÃ¡ seguro que desea dar de baja el equipo '{SelectedEquipo.Codigo}'?",
            "Confirmar baja de equipo",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            StatusMessage = "OperaciÃ³n cancelada por el usuario.";
            return;
        }

        // Pedir observaciÃ³n obligatoria
        var obsDialog = new GestLog.Views.Shared.ObservacionDialog(SelectedEquipo.Observaciones);
        var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
        if (owner != null) obsDialog.Owner = owner;
        var dialogResult = obsDialog.ShowDialog();
        if (dialogResult != true)
        {
            StatusMessage = "OperaciÃ³n cancelada por el usuario.";
            return;
        }

        try
        {
            SelectedEquipo.Observaciones = obsDialog.Observacion;
            SelectedEquipo.FechaBaja = DateTime.Now;
            SelectedEquipo.Estado = EstadoEquipo.DadoDeBaja; // Actualiza el estado explÃ­citamente
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
    }    [RelayCommand]
    public async Task ExportarEquiposAsync()
    {
        try
        {
            var dialog = new VistaSaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos a Excel",
                FileName = $"EQUIPOS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Exportando equipos...";

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");

                    // ===== FILAS 1-2: LOGO (izquierda) + TÃTULO (derecha) =====
                    ws.Row(1).Height = 35;
                    ws.Row(2).Height = 35;
                    ws.ShowGridLines = false;

                    // Combinar celdas A1:B2 para el logo
                    ws.Range(1, 1, 2, 2).Merge();

                    // Agregar logo
                    var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
                    try
                    {
                        if (System.IO.File.Exists(logoPath))
                        {
                            var picture = ws.AddPicture(logoPath);
                            picture.MoveTo(ws.Cell(1, 1), 10, 10);
                            picture.Scale(0.15);
                        }
                    }
                    catch { }

                    // Agregar tÃ­tulo en C1:J2
                    var titleRange = ws.Range(1, 3, 2, 10);
                    titleRange.Merge();
                    var titleCell = titleRange.FirstCell();
                    titleCell.Value = "INVENTARIO DE EQUIPOS";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 18;
                    titleCell.Style.Font.FontColor = XLColor.Black;
                    titleCell.Style.Fill.BackgroundColor = XLColor.White;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // ===== ENCABEZADOS DE TABLA =====
                    int currentRow = 3;
                    var headers = new[] { "CÃ³digo", "Nombre", "Marca", "Estado", "Sede", "Frecuencia", "Precio", "Fecha Registro", "ClasificaciÃ³n", "Comprado a" };
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        var headerCell = ws.Cell(currentRow, col);
                        headerCell.Value = headers[col - 1];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938); // Verde oscuro
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 22;
                    currentRow++;

                    // ===== FILAS DE DATOS =====
                    int rowCount = 0;
                    var equiposExportar = Equipos.OrderBy(e => e.Codigo).ToList();
                    foreach (var eq in equiposExportar)
                    {
                        ws.Cell(currentRow, 1).Value = eq.Codigo ?? "";
                        ws.Cell(currentRow, 2).Value = eq.Nombre ?? "";
                        ws.Cell(currentRow, 3).Value = eq.Marca ?? "";
                        ws.Cell(currentRow, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(currentRow, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(currentRow, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";

                        // Precio formateado
                        var precioCell = ws.Cell(currentRow, 7);
                        precioCell.Value = eq.Precio ?? 0;
                        precioCell.Style.NumberFormat.Format = "$#,##0";

                        ws.Cell(currentRow, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(currentRow, 9).Value = eq.Clasificacion ?? "";
                        ws.Cell(currentRow, 10).Value = eq.CompradoA ?? "";

                        // Filas alternas con color gris claro
                        if (rowCount % 2 == 0)
                        {
                            for (int col = 1; col <= 10; col++)
                            {
                                ws.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                            }
                        }

                        ws.Row(currentRow).Height = 20;
                        currentRow++;
                        rowCount++;
                    }

                    // Agregar filtros automÃ¡ticos
                    if (equiposExportar.Count > 0)
                    {
                        int headerRow = currentRow - equiposExportar.Count - 1;
                        ws.Range(headerRow, 1, currentRow - 1, 10).SetAutoFilter();
                    }

                    // ===== PANEL DE KPIs (INDICADORES BÃSICOS) =====
                    if (equiposExportar.Count > 0)
                    {
                        currentRow += 2;

                        // Calcular estadÃ­sticas
                        var totalEquipos = equiposExportar.Count;
                        var equiposActPor = equiposExportar.Count(e => EsEstado(e.Estado, "activo") || EsEstado(e.Estado, "enuso"));                        var equiposInactivosPor = equiposExportar.Count(e => EsEstado(e.Estado, "inactivo"));
                        var equiposSinPrecio = equiposExportar.Count(e => (e.Precio ?? 0) == 0);
                        var precioTotal = equiposExportar.Sum(e => e.Precio ?? 0);

                        // TÃ­tulo KPIs
                        var kpiTitle = ws.Cell(currentRow, 1);
                        kpiTitle.Value = "INDICADORES DE INVENTARIO";
                        kpiTitle.Style.Font.Bold = true;
                        kpiTitle.Style.Font.FontSize = 12;
                        kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        kpiTitle.Style.Font.FontColor = XLColor.White;
                        kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(currentRow, 1, currentRow, 11).Merge();
                        ws.Row(currentRow).Height = 20;
                        currentRow++;

                        // KPI Row
                        var kpiLabels = new[] { "Total Equipos", "Activos", "Inactivos", "Sin Precio", "Valor Total Inventario" };
                        var kpiValues = new object[] 
                        { 
                            totalEquipos,
                            equiposActPor,
                            equiposInactivosPor,
                            equiposSinPrecio,
                            precioTotal                        };for (int col = 0; col < kpiLabels.Length; col++)
                        {
                            // Etiqueta
                            var labelCell = ws.Cell(currentRow, col + 1);
                            labelCell.Value = kpiLabels[col];
                            labelCell.Style.Font.Bold = true;
                            labelCell.Style.Font.FontSize = 10;
                            labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                            // Valor
                            var valueCell = ws.Cell(currentRow + 1, col + 1);
                            if (col == 4) // Valor Total Inventario
                            {
                                valueCell.Value = (decimal)kpiValues[col];
                                valueCell.Style.NumberFormat.Format = "$#,##0";
                            }
                            else
                            {
                                valueCell.Value = (int)kpiValues[col];
                            }

                            valueCell.Style.Font.Bold = true;
                            valueCell.Style.Font.FontSize = 12;
                            valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                            valueCell.Style.Font.FontColor = XLColor.White;
                            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        currentRow += 2;
                    }

                    // ===== AJUSTAR ANCHO DE COLUMNAS =====
                    ws.Column(1).Width = 12;
                    ws.Column(2).Width = 20;
                    ws.Column(3).Width = 15;
                    ws.Column(4).Width = 15;
                    ws.Column(5).Width = 12;
                    ws.Column(6).Width = 15;
                    ws.Column(7).Width = 14;
                    ws.Column(8).Width = 14;
                    ws.Column(9).Width = 16;
                    ws.Column(10).Width = 18;

                    // ===== PIE DE PÃGINA =====
                    currentRow += 1;
                    var footerCell = ws.Cell(currentRow, 1);
                    footerCell.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} â€¢ {equiposExportar.Count} equipos â€¢ Sistema GestLog Â© SIMICS Group SAS";
                    footerCell.Style.Font.Italic = true;
                    footerCell.Style.Font.FontSize = 9;
                    footerCell.Style.Font.FontColor = XLColor.Gray;
                    ws.Range(currentRow, 1, currentRow, 10).Merge();

                    // Configurar pÃ¡gina para exportaciÃ³n
                    ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    ws.PageSetup.Scale = 90;
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.5;
                    ws.PageSetup.Margins.Right = 0.5;

                    workbook.SaveAs(dialog.FileName);
                });

                StatusMessage = $"ExportaciÃ³n completada: {dialog.FileName} ({Equipos.Count} equipos)";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al exportar equipos");
            StatusMessage = "Error al exportar equipos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private TEnum? ParseEnumFlexible<TEnum>(string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace("Ã¡", "a").Replace("Ã©", "e").Replace("Ã­", "i").Replace("Ã³", "o").Replace("Ãº", "u").Replace("Ã¼", "u").Replace("Ã±", "n").Replace(" ", "").ToLowerInvariant();
        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            var normName = name.Trim().Replace("Ã¡", "a").Replace("Ã©", "e").Replace("Ã­", "i").Replace("Ã³", "o").Replace("Ãº", "u").Replace("Ã¼", "u").Replace("Ã±", "n").Replace(" ", "").ToLowerInvariant();
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
        .Where(f => f != FrecuenciaMantenimiento.Correctivo && f != FrecuenciaMantenimiento.Predictivo);    

    partial void OnMostrarDadosDeBajaChanged(bool value)
    {
        // Aplicar filtro a la colecciÃ³n existente sin recargar del servicio
        if (_allEquipos == null || _allEquipos.Count == 0)
        {
            // Si aÃºn no hay datos, recargar
            _ = LoadEquiposAsync(forceReload: true);
            return;
        }

        // Filtrar segÃºn MostrarDadosDeBaja y crear nueva ObservableCollection
        var filtrados = value 
            ? _allEquipos.ToList() 
            : _allEquipos.Where(e => e.FechaBaja == null).ToList();
        Equipos = new ObservableCollection<EquipoDto>(filtrados);
        
        // Actualizar vista y estadÃ­sticas
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
        // Permitir mÃºltiples tÃ©rminos separados por punto y coma
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
        // Todos los tÃ©rminos deben estar presentes en algÃºn campo
        return terminos.All(term => campos.Any(campo => campo.Contains(term)));
    }

    private string RemoverTildes(string texto)
    {
        return texto
            .Replace("Ã¡", "a").Replace("Ã©", "e").Replace("Ã­", "i")
            .Replace("Ã³", "o").Replace("Ãº", "u").Replace("Ã¼", "u")
            .Replace("Ã", "A").Replace("Ã‰", "E").Replace("Ã", "I")
            .Replace("Ã“", "O").Replace("Ãš", "U").Replace("Ãœ", "U")
            .Replace("Ã±", "n").Replace("Ã‘", "N");
    }    [RelayCommand]
    public async Task ExportarEquiposFiltradosAsync()
    {
        try
        {
            var dialog = new VistaSaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos filtrados a Excel",
                FileName = $"EQUIPOS_FILTRADOS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusMessage = "Exportando equipos filtrados...";

                await Task.Run(() =>
                {
                    var filtrados = EquiposView?.Cast<EquipoDto>().ToList() ?? new List<EquipoDto>();

                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");

                    // ===== FILAS 1-2: LOGO (izquierda) + TÃTULO (derecha) =====
                    ws.Row(1).Height = 35;
                    ws.Row(2).Height = 35;
                    ws.ShowGridLines = false;

                    // Combinar celdas A1:B2 para el logo
                    ws.Range(1, 1, 2, 2).Merge();

                    // Agregar logo
                    var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
                    try
                    {
                        if (System.IO.File.Exists(logoPath))
                        {
                            var picture = ws.AddPicture(logoPath);
                            picture.MoveTo(ws.Cell(1, 1), 10, 10);
                            picture.Scale(0.15);
                        }
                    }
                    catch { }

                    // Agregar tÃ­tulo en C1:J2
                    var titleRange = ws.Range(1, 3, 2, 10);
                    titleRange.Merge();
                    var titleCell = titleRange.FirstCell();
                    titleCell.Value = "INVENTARIO DE EQUIPOS (FILTRADOS)";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 18;
                    titleCell.Style.Font.FontColor = XLColor.Black;
                    titleCell.Style.Fill.BackgroundColor = XLColor.White;
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // ===== ENCABEZADOS DE TABLA =====
                    int currentRow = 3;
                    var headers = new[] { "CÃ³digo", "Nombre", "Marca", "Estado", "Sede", "Frecuencia", "Precio", "Fecha Registro", "ClasificaciÃ³n", "Comprado a" };
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        var headerCell = ws.Cell(currentRow, col);
                        headerCell.Value = headers[col - 1];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938); // Verde oscuro
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }
                    ws.Row(currentRow).Height = 22;
                    currentRow++;

                    // ===== FILAS DE DATOS =====
                    int rowCount = 0;
                    foreach (var eq in filtrados)
                    {
                        ws.Cell(currentRow, 1).Value = eq.Codigo ?? "";
                        ws.Cell(currentRow, 2).Value = eq.Nombre ?? "";
                        ws.Cell(currentRow, 3).Value = eq.Marca ?? "";
                        ws.Cell(currentRow, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(currentRow, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(currentRow, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";

                        // Precio formateado
                        var precioCell = ws.Cell(currentRow, 7);
                        precioCell.Value = eq.Precio ?? 0;
                        precioCell.Style.NumberFormat.Format = "$#,##0";

                        ws.Cell(currentRow, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(currentRow, 9).Value = eq.Clasificacion ?? "";
                        ws.Cell(currentRow, 10).Value = eq.CompradoA ?? "";

                        // Filas alternas con color gris claro
                        if (rowCount % 2 == 0)
                        {
                            for (int col = 1; col <= 10; col++)
                            {
                                ws.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                            }
                        }

                        ws.Row(currentRow).Height = 20;
                        currentRow++;
                        rowCount++;
                    }

                    // Agregar filtros automÃ¡ticos
                    if (filtrados.Count > 0)
                    {
                        int headerRow = currentRow - filtrados.Count - 1;
                        ws.Range(headerRow, 1, currentRow - 1, 10).SetAutoFilter();
                    }

                    // ===== PANEL DE KPIs (INDICADORES BÃSICOS) =====
                    if (filtrados.Count > 0)
                    {
                        currentRow += 2;                        // Calcular estadÃ­sticas
                        var totalEquipos = filtrados.Count;
                        var equiposActPor = filtrados.Count(e => EsEstado(e.Estado, "activo") || EsEstado(e.Estado, "enuso"));
                        var equiposInactivosPor = filtrados.Count(e => EsEstado(e.Estado, "inactivo"));
                        var equiposSinPrecio = filtrados.Count(e => (e.Precio ?? 0) == 0);
                        var precioTotal = filtrados.Sum(e => e.Precio ?? 0);

                        // TÃ­tulo KPIs
                        var kpiTitle = ws.Cell(currentRow, 1);
                        kpiTitle.Value = "INDICADORES DE INVENTARIO";
                        kpiTitle.Style.Font.Bold = true;
                        kpiTitle.Style.Font.FontSize = 12;
                        kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        kpiTitle.Style.Font.FontColor = XLColor.White;
                        kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Range(currentRow, 1, currentRow, 11).Merge();
                        ws.Row(currentRow).Height = 20;
                        currentRow++;                        // KPI Row
                        var kpiLabels = new[] { "Total Equipos", "Activos", "Inactivos", "Sin Precio", "Valor Total Inventario" };
                        var kpiValues = new object[] 
                        { 
                            totalEquipos,
                            equiposActPor,
                            equiposInactivosPor,
                            equiposSinPrecio,
                            precioTotal
                        };                        for (int col = 0; col < kpiLabels.Length; col++)
                        {
                            // Etiqueta
                            var labelCell = ws.Cell(currentRow, col + 1);
                            labelCell.Value = kpiLabels[col];
                            labelCell.Style.Font.Bold = true;
                            labelCell.Style.Font.FontSize = 10;
                            labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                            // Valor
                            var valueCell = ws.Cell(currentRow + 1, col + 1);
                            if (col == 4) // Valor Total Inventario
                            {
                                valueCell.Value = (decimal)kpiValues[col];
                                valueCell.Style.NumberFormat.Format = "$#,##0";
                            }
                            else
                            {
                                valueCell.Value = (int)kpiValues[col];
                            }

                            valueCell.Style.Font.Bold = true;
                            valueCell.Style.Font.FontSize = 12;
                            valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                            valueCell.Style.Font.FontColor = XLColor.White;
                            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        currentRow += 2;
                    }

                    // ===== AJUSTAR ANCHO DE COLUMNAS =====
                    ws.Column(1).Width = 12;
                    ws.Column(2).Width = 20;
                    ws.Column(3).Width = 15;
                    ws.Column(4).Width = 15;
                    ws.Column(5).Width = 12;
                    ws.Column(6).Width = 15;
                    ws.Column(7).Width = 14;
                    ws.Column(8).Width = 14;
                    ws.Column(9).Width = 16;
                    ws.Column(10).Width = 18;

                    // ===== PIE DE PÃGINA =====
                    currentRow += 1;
                    var footerCell = ws.Cell(currentRow, 1);
                    footerCell.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} â€¢ {filtrados.Count} equipos â€¢ Sistema GestLog Â© SIMICS Group SAS";
                    footerCell.Style.Font.Italic = true;
                    footerCell.Style.Font.FontSize = 9;
                    footerCell.Style.Font.FontColor = XLColor.Gray;
                    ws.Range(currentRow, 1, currentRow, 10).Merge();

                    // Configurar pÃ¡gina para exportaciÃ³n
                    ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    ws.PageSetup.Scale = 90;
                    ws.PageSetup.Margins.Top = 0.5;
                    ws.PageSetup.Margins.Bottom = 0.5;
                    ws.PageSetup.Margins.Left = 0.5;
                    ws.PageSetup.Margins.Right = 0.5;

                    workbook.SaveAs(dialog.FileName);
                });

                StatusMessage = $"ExportaciÃ³n completada: {dialog.FileName} ({(EquiposView?.Cast<EquipoDto>().Count() ?? 0)} equipos filtrados)";
            }
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

    [RelayCommand]
    public void VerDetallesEquipo(EquipoDto? equipo)
    {
        if (equipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para ver detalles.";
            return;
        }        try
        {
            SelectedEquipo = equipo;
            var detalleWindow = new GestLog.Modules.GestionMantenimientos.Views.Equipos.EquipoDetalleModalWindow
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
        // Solo permite registrar mantenimiento si el equipo estÃ¡ ACTIVO
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
                TipoMtno = TipoMantenimiento.Correctivo // Preseleccionado
                // Los campos TipoMtno y Frecuencia se llenan en el diÃ¡logo y al guardar
            };
            // Asignar la frecuencia por defecto para el flujo de Equipos (Correctivo) y abrir el diÃ¡logo en modo restringido
            seguimiento.Frecuencia = FrecuenciaMantenimiento.Correctivo;
            var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog(seguimiento, true);
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) dialog.Owner = owner;
            var result = dialog.ShowDialog();
            if (result == true)            
            {
                // Asignar la frecuencia automÃ¡ticamente segÃºn el tipo
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
    }    

    private void RecalcularPermisos()
    {
        CanRegistrarEquipo = _currentUser.HasPermission("GestionMantenimientos.NuevoEquipo");
        CanEditarEquipo = _currentUser.HasPermission("GestionMantenimientos.EditarEquipo"); // Si existe este permiso en la BD
        CanDarDeBajaEquipo = _currentUser.HasPermission("GestionMantenimientos.DarDeBajaEquipo");
        CanRegistrarMantenimientoPermiso = _currentUser.HasPermission("GestionMantenimientos.RegistrarMantenimiento");
        // Notificar cambios en las propiedades alias
        OnPropertyChanged(nameof(CanAddEquipo));
        OnPropertyChanged(nameof(CanDeleteEquipo));
        OnPropertyChanged(nameof(CanExportEquipo));    
    }

    // âœ… IMPLEMENTACIÃ“N REQUERIDA: DatabaseAwareViewModel
    protected override async Task RefreshDataAsync()
    {
        await LoadEquiposAsync(forceReload: true);
    }

    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexiÃ³n - MÃ³dulo no disponible";
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
    }    [RelayCommand]
    public async Task ExportarInteligenteAsync()
    {
        // Detectar si realmente hay un filtro activo
        try
        {
            // 1. Verificar si hay filtro de texto
            var hayFiltroTexto = !string.IsNullOrWhiteSpace(FiltroEquipo);
            if (hayFiltroTexto)
            {
                await ExportarEquiposFiltradosAsync();
                return;
            }

            // 2. Verificar si Equipos (visible) es diferente de _allEquipos (total)
            // Esto indica que hay un filtro activo como "MostrarDadosDeBaja"
            var hayFiltroEstado = Equipos.Count != _allEquipos.Count;
            if (hayFiltroEstado)
            {
                await ExportarEquiposFiltradosAsync();
                return;
            }

            // Si no hay filtro alguno, exportar todo (sin cambios temporales)
            await ExportarEquiposAsync();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error en ExportarInteligente");
            StatusMessage = "Error al exportar equipos.";
        }
    }
}


