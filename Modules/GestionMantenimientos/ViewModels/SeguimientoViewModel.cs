using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para el seguimiento de mantenimientos.
/// </summary>
public partial class SeguimientoViewModel : DatabaseAwareViewModel, IDisposable
{
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private CurrentUserInfo _currentUser;

    // Permisos reactivos para seguimiento
    [ObservableProperty]
    private bool canAddSeguimiento;
    [ObservableProperty]
    private bool canEditSeguimiento;
    [ObservableProperty]
    private bool canDeleteSeguimiento;
    [ObservableProperty]
    private bool canExportSeguimiento;

    [ObservableProperty]
    private ObservableCollection<SeguimientoMantenimientoDto> seguimientos = new();

    [ObservableProperty]
    private SeguimientoMantenimientoDto? selectedSeguimiento;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private string filtroSeguimiento = "";

    [ObservableProperty]
    private DateTime? fechaDesde;

    [ObservableProperty]
    private DateTime? fechaHasta;

    [ObservableProperty]
    private System.ComponentModel.ICollectionView? seguimientosView;

    // Optimización: Control de carga para evitar actualizaciones innecesarias
    private CancellationTokenSource? _loadCancellationToken;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private DateTime _lastObservacionesUpdateTime = DateTime.MinValue;
    private const int DEBOUNCE_DELAY_MS = 300; // 300ms de debounce para seguimientos
    private const int MIN_RELOAD_INTERVAL_MS = 1500; // Mínimo 1.5 segundos entre cargas
    private const int MIN_OBSERVACIONES_UPDATE_INTERVAL_MS = 5000; // Mínimo 5 segundos entre actualizaciones de observaciones

    public SeguimientoViewModel(
        ISeguimientoService seguimientoService, 
        ICurrentUserService currentUserService,
        IDatabaseConnectionService databaseService,
        IGestLogLogger logger)
        : base(databaseService, logger)
    {
        try
        {
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            // Suscribirse a mensajes de actualización de seguimientos
            // OPTIMIZACIÓN: Solo recargar cuando sea realmente necesario
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Solo recargar si han pasado al menos 1.5 segundos desde la última carga
                    await LoadSeguimientosAsync(forceReload: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SeguimientoViewModel] Error en mensaje SeguimientosActualizados");
                }
            });
            
            SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
            if (SeguimientosView != null)
                SeguimientosView.Filter = FiltrarSeguimiento;

            // Cargar datos automáticamente al crear el ViewModel
            Task.Run(async () => 
            {
                try
                {
                    await LoadSeguimientosAsync(forceReload: true); // Carga inicial siempre forzada
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SeguimientoViewModel] Error en carga inicial de seguimientos");
                }
            });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[SeguimientoViewModel] Error crítico en constructor");
            throw; // Re-lanzar para que se capture en el nivel superior
        }
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
    }

    private void RecalcularPermisos()
    {
        CanAddSeguimiento = _currentUser.HasPermission("GestionMantenimientos.AgregarSeguimiento");
        CanEditSeguimiento = _currentUser.HasPermission("GestionMantenimientos.EditarSeguimiento");
        CanDeleteSeguimiento = _currentUser.HasPermission("GestionMantenimientos.EliminarSeguimiento");
        CanExportSeguimiento = _currentUser.HasPermission("GestionMantenimientos.ExportarExcel");
    }

    [RelayCommand]
    public async Task LoadSeguimientosAsync(bool forceReload = true)
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
        StatusMessage = "Cargando seguimientos...";
        try
        {
            // OPTIMIZACIÓN: Actualizar observaciones solo si han pasado más de 5 segundos
            var timeSinceLastObservacionesUpdate = DateTime.Now - _lastObservacionesUpdateTime;
            if (forceReload || timeSinceLastObservacionesUpdate.TotalMilliseconds > MIN_OBSERVACIONES_UPDATE_INTERVAL_MS)
            {
                try
                {
                    await _seguimientoService.ActualizarObservacionesPendientesAsync();
                    _lastObservacionesUpdateTime = DateTime.Now;
                }
                catch (System.Exception exObs)
                {
                    _logger.LogError(exObs, "Error al actualizar observaciones de seguimientos");
                    StatusMessage = "Error al actualizar observaciones.";
                }
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            // Continuar con la carga aunque falle la actualización de observaciones
            var lista = await _seguimientoService.GetSeguimientosAsync();
            
            if (cancellationToken.IsCancellationRequested)
                return;

            var hoy = DateTime.Now;
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int semanaActual = cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int anioActual = hoy.Year;

            foreach (var s in lista)
            {
                var estadoCalculado = CalcularEstadoSeguimiento(s, semanaActual, anioActual, hoy);
                if (s.Estado != estadoCalculado)
                {
                    s.Estado = estadoCalculado;
                    if (string.IsNullOrWhiteSpace(s.Responsable))
                        s.Responsable = "Automático";
                    await _seguimientoService.UpdateAsync(s);
                }
                s.RefrescarCacheFiltro(); // Refresca la caché de campos normalizados
            }

            Seguimientos = new ObservableCollection<SeguimientoMantenimientoDto>(lista);
            StatusMessage = $"{Seguimientos.Count} seguimientos cargados.";
            _lastLoadTime = DateTime.Now;
        }
        catch (OperationCanceledException)
        {
            // Carga cancelada, no hacer nada
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar seguimientos");
            StatusMessage = "Error al cargar seguimientos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private EstadoSeguimientoMantenimiento CalcularEstadoSeguimiento(SeguimientoMantenimientoDto s, int semanaActual, int anioActual, DateTime hoy)
    {
        int diff = s.Semana - semanaActual;
        var fechaInicioSemana = FirstDateOfWeekISO8601(s.Anio, s.Semana);
        var fechaFinSemana = fechaInicioSemana.AddDays(6);

        if (s.Anio < anioActual || (s.Anio == anioActual && diff < -1))
        {
            if (s.FechaRegistro == null)
                return EstadoSeguimientoMantenimiento.NoRealizado;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value >= fechaInicioSemana && s.FechaRealizacion.Value <= fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value > fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
            return EstadoSeguimientoMantenimiento.NoRealizado;
        }

        if (s.Anio == anioActual && diff == -1)
        {
            if (s.FechaRegistro == null)
                return EstadoSeguimientoMantenimiento.Atrasado;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value >= fechaInicioSemana && s.FechaRealizacion.Value <= fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value > fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
            return EstadoSeguimientoMantenimiento.Atrasado;
        }

        if (s.Anio == anioActual && diff == 0)
        {
            if (s.FechaRegistro == null)
                return EstadoSeguimientoMantenimiento.Pendiente;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value >= fechaInicioSemana && s.FechaRealizacion.Value <= fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
            if (s.FechaRealizacion.HasValue && s.FechaRealizacion.Value > fechaFinSemana)
                return EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
            return EstadoSeguimientoMantenimiento.Pendiente;
        }

        if (s.Anio == anioActual && diff > 0)
        {
            return EstadoSeguimientoMantenimiento.Pendiente;
        }

        return EstadoSeguimientoMantenimiento.Pendiente;
    }

    private DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
        var firstThursday = jan1.AddDays(daysOffset);
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekNum = weekOfYear;
        if (firstWeek <= 1)
            weekNum -= 1;
        var result = firstThursday.AddDays(weekNum * 7);
        return result.AddDays(-3);
    }

    [RelayCommand]
    public async Task ActualizarObservacionesPendientesAsync()
    {
        IsLoading = true;
        StatusMessage = "Actualizando observaciones de seguimientos...";
        try
        {
            await _seguimientoService.ActualizarObservacionesPendientesAsync();
            StatusMessage = "Observaciones actualizadas correctamente.";
            await LoadSeguimientosAsync(forceReload: true); // Forzar recarga tras actualizar observaciones
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar observaciones");
            StatusMessage = "Error al actualizar observaciones.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddSeguimiento))]
    public async Task AddSeguimientoAsync()
    {
        var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog();
        if (dialog.ShowDialog() == true)
        {
            var nuevo = dialog.Seguimiento;
            try
            {
                await _seguimientoService.AddAsync(nuevo);
                // Notificar a otros ViewModels
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                StatusMessage = "Seguimiento agregado correctamente.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al agregar seguimiento");
                StatusMessage = "Error al agregar seguimiento.";
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanEditSeguimiento))]
    public async Task EditSeguimientoAsync()
    {
        if (SelectedSeguimiento == null)
            return;

        var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(SelectedSeguimiento);
        if (dialog.ShowDialog() == true)
        {
            var editado = dialog.Seguimiento;
            try
            {
                await _seguimientoService.UpdateAsync(editado);
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                StatusMessage = "Seguimiento editado correctamente.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al editar seguimiento");
                StatusMessage = "Error al editar seguimiento.";
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSeguimiento))]
    public async Task DeleteSeguimientoAsync()
    {
        if (SelectedSeguimiento == null)
            return;

        try
        {
            await _seguimientoService.DeleteAsync(SelectedSeguimiento.Codigo!);
            WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
            StatusMessage = "Seguimiento eliminado correctamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar seguimiento");
            StatusMessage = "Error al eliminar seguimiento.";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportSeguimiento))]
    public async Task ExportarSeguimientosAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            Title = "Exportar seguimientos a Excel",
            FileName = $"Seguimientos_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Exportando a Excel...";
            try
            {
                await _seguimientoService.ExportarAExcelAsync(dialog.FileName);
                StatusMessage = $"Exportación completada: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar seguimientos");
                StatusMessage = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportSeguimiento))]
    public async Task ExportarSeguimientosFiltradosAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            Title = "Exportar seguimientos filtrados a Excel",
            FileName = $"SeguimientosFiltrados_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Exportando a Excel...";
            try
            {
                var filtrados = SeguimientosView?.Cast<SeguimientoMantenimientoDto>().ToList() ?? new List<SeguimientoMantenimientoDto>();
                await Task.Run(() =>
                {
                    using var workbook = new ClosedXML.Excel.XLWorkbook();
                    var ws = workbook.Worksheets.Add("Seguimientos");

                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Nombre";
                    ws.Cell(1, 3).Value = "Tipo Mtno";
                    ws.Cell(1, 4).Value = "Responsable";
                    ws.Cell(1, 5).Value = "Fecha Registro";
                    ws.Cell(1, 6).Value = "Semana";
                    ws.Cell(1, 7).Value = "Año";
                    ws.Cell(1, 8).Value = "Estado";

                    int row = 2;
                    foreach (var s in filtrados)
                    {
                        ws.Cell(row, 1).Value = s.Codigo ?? "";
                        ws.Cell(row, 2).Value = s.Nombre ?? "";
                        ws.Cell(row, 3).Value = s.TipoMtno?.ToString() ?? "";
                        ws.Cell(row, 4).Value = s.Responsable ?? "";
                        ws.Cell(row, 5).Value = s.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 6).Value = s.Semana;
                        ws.Cell(row, 7).Value = s.Anio;
                        ws.Cell(row, 8).Value = s.Estado.ToString();
                        row++;
                    }

                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);
                });

                StatusMessage = $"Exportación completada: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar seguimientos filtrados");
                StatusMessage = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    public void Filtrar()
    {
        SeguimientosView?.Refresh();
    }

    partial void OnFiltroSeguimientoChanged(string value)
    {
        // No refrescar automáticamente
    }

    partial void OnFechaDesdeChanged(DateTime? value)
    {
        // No refrescar automáticamente
    }

    partial void OnFechaHastaChanged(DateTime? value)
    {
        // No refrescar automáticamente
    }

    partial void OnSeguimientosChanged(ObservableCollection<SeguimientoMantenimientoDto> value)
    {
        // Ejecutar la actualización de la vista en el Dispatcher para evitar InvalidOperationException
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            // Fallback si no hay aplicación (ej. pruebas unitarias)
            if (value != null)
            {
                foreach (var s in value)
                    s.RefrescarCacheFiltro();
            }

            SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
            if (SeguimientosView != null)
                SeguimientosView.Filter = FiltrarSeguimiento;
            try { SeguimientosView?.Refresh(); } catch (System.InvalidOperationException) { /* ignorar si está en DeferRefresh */ }
            return;
        }

        app.Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                if (value != null)
                {
                    foreach (var s in value)
                        s.RefrescarCacheFiltro();
                }

                SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
                if (SeguimientosView != null)
                    SeguimientosView.Filter = FiltrarSeguimiento;

                // Intentar refrescar; si falla porque Refresh está aplazado, reintentar con prioridad más baja
                try
                {
                    SeguimientosView?.Refresh();
                }
                catch (System.InvalidOperationException)
                {
                    // Reagendar un reintento cuando la UI esté ociosa
                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try { SeguimientosView?.Refresh(); } catch { /* swallow */ }
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "Error en OnSeguimientosChanged al actualizar la vista de seguimientos");
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private bool FiltrarSeguimiento(object obj)
    {
        if (obj is not SeguimientoMantenimientoDto s) return false;

        // Filtro múltiple por texto
        if (!string.IsNullOrWhiteSpace(FiltroSeguimiento))
        {
            var terminos = FiltroSeguimiento.Split(';')
                .Select(t => RemoverTildes(t.Trim()).ToLowerInvariant().Replace(" ", ""))
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var campos = new[]
            {
                s.CodigoNorm,
                s.NombreNorm,
                s.TipoMtnoNorm,
                s.ResponsableNorm,
                s.FechaRegistroNorm,
                s.SemanaNorm,
                s.AnioNorm,
                s.EstadoNorm
            };

            if (!terminos.All(termino => campos.Any(campo => campo.Contains(termino))))
                return false;
        }

        // Filtro por fechas
        if (FechaDesde.HasValue && (s.FechaRegistro == null || s.FechaRegistro < FechaDesde.Value))
            return false;
        if (FechaHasta.HasValue && (s.FechaRegistro == null || s.FechaRegistro > FechaHasta.Value))
            return false;

        // Filtro por estado: no mostrar "Pendiente"
        if (s.Estado == EstadoSeguimientoMantenimiento.Pendiente)
            return false;

        return true;
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

    private string SepararCamelCase(string texto)
    {
        // Convierte "RealizadoEnTiempo" en "Realizado en tiempo"
        return System.Text.RegularExpressions.Regex.Replace(texto, "([a-z])([A-Z])", "$1 $2");
    }

    // ✅ IMPLEMENTACIÓN REQUERIDA: DatabaseAwareViewModel
    protected override async Task RefreshDataAsync()
    {
        await LoadSeguimientosAsync(forceReload: true);
    }

    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexión - Módulo no disponible";
    }    // Implementar IDisposable para limpieza de recursos
    public new void Dispose()
    {
        _loadCancellationToken?.Cancel();
        _loadCancellationToken?.Dispose();
        
        // Desuscribirse de mensajes
        WeakReferenceMessenger.Default.Unregister<SeguimientosActualizadosMessage>(this);
        
        if (_currentUserService != null)
            _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
        
        base.Dispose();
    }
}
