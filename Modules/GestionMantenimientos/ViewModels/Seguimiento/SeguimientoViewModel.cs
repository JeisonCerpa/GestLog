using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Win32;
using GestLog.Modules.GestionMantenimientos.Utilities;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Interfaces.Import;
using ClosedXML.Excel;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento;

/// <summary>
/// ViewModel para el seguimiento de mantenimientos.
/// </summary>
public partial class SeguimientoViewModel : DatabaseAwareViewModel, IDisposable
{
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISeguimientosExportService _seguimientosExportService;
    private readonly ISeguimientoImportService _seguimientoImportService;
    private CurrentUserInfo _currentUser;

    [ObservableProperty]
    private bool canAddSeguimiento;
    [ObservableProperty]
    private bool canEditSeguimiento;
    [ObservableProperty]
    private bool canDeleteSeguimiento;
    [ObservableProperty]
    private bool canExportSeguimiento;
    [ObservableProperty]
    private bool canImportSeguimiento;
    [ObservableProperty]
    private int seguimientosTotal;

    [ObservableProperty]
    private int seguimientosPendientes;

    [ObservableProperty]
    private int seguimientosEjecutados;

    [ObservableProperty]
    private int seguimientosRetrasados;

    [ObservableProperty]
    private int seguimientosRealizadosFueraDeTiempo;

    [ObservableProperty]
    private int seguimientosNoRealizados;

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

    [ObservableProperty]
    private int anioSeleccionado = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> aniosDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<SeguimientoMantenimientoDto> seguimientosFiltrados = new();

    [ObservableProperty]
    private int progressValue;

    private CancellationTokenSource? _importCancellationTokenSource;

    private CancellationTokenSource? _loadCancellationToken;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private DateTime _lastObservacionesUpdateTime = DateTime.MinValue;
    private const int DEBOUNCE_DELAY_MS = 300;
    private const int MIN_RELOAD_INTERVAL_MS = 1500;
    private const int MIN_OBSERVACIONES_UPDATE_INTERVAL_MS = 5000;

    public SeguimientoViewModel(
        ISeguimientoService seguimientoService, 
        ICurrentUserService currentUserService,
        ISeguimientosExportService seguimientosExportService,
        ISeguimientoImportService seguimientoImportService,
        IDatabaseConnectionService databaseService,
        IGestLogLogger logger)
        : base(databaseService, logger)
    {
        try
        {
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _seguimientosExportService = seguimientosExportService ?? throw new ArgumentNullException(nameof(seguimientosExportService));
            _seguimientoImportService = seguimientoImportService ?? throw new ArgumentNullException(nameof(seguimientoImportService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
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

            Task.Run(async () => 
            {
                try
                {
                    await LoadSeguimientosAsync(forceReload: true);
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
            throw;
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
        CanImportSeguimiento = _currentUser.HasPermission("GestionMantenimientos.ImportarSeguimientos");
    }

    partial void OnAnioSeleccionadoChanged(int value)
    {
        FiltrarPorAnio();
    }

    private void FiltrarPorAnio()
    {
        if (Seguimientos == null) return;
        var filtrados = Seguimientos.Where(s => s.Anio == AnioSeleccionado).ToList();
        SeguimientosFiltrados = new ObservableCollection<SeguimientoMantenimientoDto>(filtrados);
        
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                SeguimientosView?.Refresh();
            }
            catch (System.InvalidOperationException)
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    new Action(() => { try { SeguimientosView?.Refresh(); } catch { } }),
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        });
        
        CalcularEstadisticasPorAnio();
    }

    private void CalcularEstadisticasPorAnio()
    {
        if (SeguimientosFiltrados == null)
        {
            SeguimientosTotal = 0;
            SeguimientosPendientes = 0;
            SeguimientosEjecutados = 0;
            SeguimientosRetrasados = 0;
            SeguimientosRealizadosFueraDeTiempo = 0;
            SeguimientosNoRealizados = 0;
            return;
        }

        SeguimientosTotal = SeguimientosFiltrados.Count;
        SeguimientosPendientes = SeguimientosFiltrados.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
        SeguimientosEjecutados = SeguimientosFiltrados.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
        SeguimientosRetrasados = SeguimientosFiltrados.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
        SeguimientosRealizadosFueraDeTiempo = SeguimientosFiltrados.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo);
        SeguimientosNoRealizados = SeguimientosFiltrados.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
    }

    [RelayCommand]
    public async Task LoadSeguimientos()
    {
        await LoadSeguimientosAsync(forceReload: true);
    }

    public async Task LoadSeguimientosAsync(bool forceReload = true)
    {
        if (!forceReload)
        {
            var timeSinceLastLoad = DateTime.Now - _lastLoadTime;
            if (timeSinceLastLoad.TotalMilliseconds < MIN_RELOAD_INTERVAL_MS && !IsLoading)
            {
                return;
            }
        }

        _loadCancellationToken?.Cancel();
        _loadCancellationToken = new CancellationTokenSource();
        var cancellationToken = _loadCancellationToken.Token;

        if (!forceReload)
        {
            try
            {
                await Task.Delay(DEBOUNCE_DELAY_MS, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        IsLoading = true;
        StatusMessage = "Cargando seguimientos...";
        try
        {
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
                s.RefrescarCacheFiltro();
            }

            Seguimientos = new ObservableCollection<SeguimientoMantenimientoDto>(lista);
            
            var anios = lista.Select(s => s.Anio).Distinct().OrderByDescending(a => a).ToList();
            AniosDisponibles = new ObservableCollection<int>(anios);
            if (!AniosDisponibles.Contains(AnioSeleccionado))
            {
                AnioSeleccionado = anios.FirstOrDefault() == 0 ? DateTime.Now.Year : anios.FirstOrDefault();
            }
            
            FiltrarPorAnio();
            StatusMessage = $"{Seguimientos.Count} seguimientos cargados.";
            _lastLoadTime = DateTime.Now;
        }
        catch (OperationCanceledException)
        {
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
            await LoadSeguimientosAsync(forceReload: true);
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
        var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
        if (dialog.ShowDialog() == true)
        {
            var nuevo = dialog.Seguimiento;
            try
            {
                await _seguimientoService.AddAsync(nuevo);
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

        var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog(SelectedSeguimiento);
        dialog.Owner = System.Windows.Application.Current.MainWindow;
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

    private bool TieneFiltrosActivos()
    {
        return !string.IsNullOrWhiteSpace(FiltroSeguimiento) || 
               FechaDesde.HasValue || 
               FechaHasta.HasValue;
    }

    [RelayCommand(CanExecute = nameof(CanExportSeguimiento))]
    public async Task ExportarAsync()
    {
        try
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                FileName = $"SEGUIMIENTOS_{AnioSeleccionado}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Exportar seguimientos a Excel"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;
            StatusMessage = "Exportando seguimientos...";

            var datosAExportar = SeguimientosFiltrados.ToList();

            await _seguimientosExportService.ExportAsync(datosAExportar, AnioSeleccionado, saveFileDialog.FileName, CancellationToken.None);

            StatusMessage = $"Exportación completada: {saveFileDialog.FileName} ({datosAExportar.Count} seguimientos)";
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

    [RelayCommand(CanExecute = nameof(CanImportSeguimiento))]
    public async Task ImportarSeguimientosAntiguosAsync(CancellationToken cancellationToken = default)
    {
        if (!CanImportSeguimiento)
        {
            StatusMessage = "No tiene permiso para importar seguimientos.";
            return;
        }

        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            Title = "Importar seguimientos antiguos"
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        var filePath = openFileDialog.FileName;

        try
        {
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException("El archivo seleccionado no existe.", filePath);

            if (!System.IO.Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new GestionMantenimientos.Models.Exceptions.GestionMantenimientosDomainException("El archivo debe ser un Excel (.xlsx)");

            IsLoading = true;
            StatusMessage = "Importando seguimientos...";
            ProgressValue = 0;

            _importCancellationTokenSource = new CancellationTokenSource();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_importCancellationTokenSource.Token, cancellationToken);
            var token = linkedCts.Token;

            var progress = new Progress<int>(p =>
            {
                ProgressValue = p;
            });

            var importResult = await _seguimientoImportService.ImportAsync(filePath, token, progress);

            await LoadSeguimientosAsync(forceReload: true);

            StatusMessage = $"Importación completada. Nuevos: {importResult.ImportedCount}, Actualizados: {importResult.UpdatedCount}, Ignorados: {importResult.IgnoredCount}";

            if (importResult.IgnoredRows.Any())
            {
                foreach (var ign in importResult.IgnoredRows)
                    _logger.LogWarning("[SeguimientoViewModel] Fila {Row} ignorada: {Reason}", ign.Row, ign.Reason);

                StatusMessage += " (Ver logs para detalles de filas ignoradas.)";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Importación cancelada.";
        }
        catch (GestionMantenimientos.Models.Exceptions.GestionMantenimientosDomainException ex)
        {
            _logger.LogWarning(ex, "Error de validación al importar seguimientos");
            StatusMessage = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al importar seguimientos antiguos");
            StatusMessage = "Error al importar seguimientos. Contacte soporte técnico.";
        }
        finally
        {
            IsLoading = false;
            _importCancellationTokenSource?.Dispose();
            _importCancellationTokenSource = null;
            ProgressValue = 0;
        }
    }

    [RelayCommand(CanExecute = nameof(CanImportSeguimiento))]
    public async Task DescargarPlantillaAsync()
    {
        var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            FileName = $"PLANTILLA_SEGUIMIENTOS_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Guardar plantilla de importación"
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        IsLoading = true;
        StatusMessage = "Generando plantilla...";

        try
        {
            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Plantilla");

                var headers = new[] { "Codigo", "Nombre", "TipoMtno", "Descripcion", "Responsable", "Costo", "Observaciones", "FechaRealizacion" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = headers[i];
                }

                // Ejemplo de fila
                ws.Cell(2, 1).Value = "EQP-0001";
                ws.Cell(2, 2).Value = "Equipo ejemplo";
                ws.Cell(2, 3).Value = "Preventivo";
                ws.Cell(2, 4).Value = "Descripción de ejemplo";
                ws.Cell(2, 5).Value = "Técnico Ejemplo";
                ws.Cell(2, 6).Value = 0;
                ws.Cell(2, 7).Value = "Sin observaciones";
                ws.Cell(2, 8).Value = DateTime.Now.ToString("dd/MM/yyyy");

                // Formato de encabezado
                ws.Row(1).Style.Font.Bold = true;
                ws.Columns().AdjustToContents();

                wb.SaveAs(saveFileDialog.FileName);
            });

            StatusMessage = $"Plantilla guardada: {saveFileDialog.FileName}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generación de plantilla cancelada.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar plantilla de importación");
            StatusMessage = "Error al generar la plantilla. Ver logs para detalles.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanImportSeguimiento))]
    public void CancelarImportacion()
    {
        if (_importCancellationTokenSource == null)
            return;

        try
        {
            _importCancellationTokenSource.Cancel();
            StatusMessage = "Cancelando importación...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar cancelar la importación");
            StatusMessage = "Error al cancelar el proceso.";
        }
    }

    [RelayCommand]
    public void Filtrar()
    {
        SeguimientosView?.Refresh();
    }

    [RelayCommand]
    public void LimpiarFiltros()
    {
        FiltroSeguimiento = "";
        FechaDesde = null;
        FechaHasta = null;
        SeguimientosView?.Refresh();
        StatusMessage = "Filtros limpiados.";
    }

    partial void OnFiltroSeguimientoChanged(string value)
    {
    }

    partial void OnFechaDesdeChanged(DateTime? value)
    {
    }

    partial void OnFechaHastaChanged(DateTime? value)
    {
    }

    partial void OnSeguimientosChanged(ObservableCollection<SeguimientoMantenimientoDto> value)
    {
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            if (value != null)
            {
                foreach (var s in value)
                    s.RefrescarCacheFiltro();
            }

            SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
            if (SeguimientosView != null)
                SeguimientosView.Filter = FiltrarSeguimiento;
            try { SeguimientosView?.Refresh(); } catch (System.InvalidOperationException) { }
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

                try
                {
                    SeguimientosView?.Refresh();
                }
                catch (System.InvalidOperationException)
                {
                    app.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try { SeguimientosView?.Refresh(); } catch { }
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

        if (s.Anio != AnioSeleccionado)
            return false;

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

        if (FechaDesde.HasValue && (s.FechaRegistro == null || s.FechaRegistro < FechaDesde.Value))
            return false;
        if (FechaHasta.HasValue && (s.FechaRegistro == null || s.FechaRegistro > FechaHasta.Value))
            return false;

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
        return System.Text.RegularExpressions.Regex.Replace(texto, "([a-z])([A-Z])", "$1 $2");
    }

    private void RecalcularEstadisticas()
    {
        SeguimientosTotal = Seguimientos.Count;
        SeguimientosPendientes = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
        SeguimientosEjecutados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
        SeguimientosRetrasados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
        SeguimientosRealizadosFueraDeTiempo = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo);
        SeguimientosNoRealizados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
    }

    protected override async Task RefreshDataAsync()
    {
        await LoadSeguimientosAsync(forceReload: true);
    }

    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexión - Módulo no disponible";
    }

    public new void Dispose()
    {
        _loadCancellationToken?.Cancel();
        _loadCancellationToken?.Dispose();
        
        WeakReferenceMessenger.Default.Unregister<SeguimientosActualizadosMessage>(this);
        
        if (_currentUserService != null)
            _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
        
        base.Dispose();
    }
}


