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
using GestLog.Modules.GestionMantenimientos.Interfaces.Export; // <-- añadido

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento;

/// <summary>
/// ViewModel para el seguimiento de mantenimientos.
/// </summary>
public partial class SeguimientoViewModel : DatabaseAwareViewModel, IDisposable
{
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISeguimientosExportService _seguimientosExportService; // <-- añadido
    private CurrentUserInfo _currentUser;

    // Permisos reactivos para seguimiento
    [ObservableProperty]
    private bool canAddSeguimiento;
    [ObservableProperty]
    private bool canEditSeguimiento;
    [ObservableProperty]
    private bool canDeleteSeguimiento;
    [ObservableProperty]
    private bool canExportSeguimiento;    // Propiedades de estadÃ­sticas para el header
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

    // OptimizaciÃ³n: Control de carga para evitar actualizaciones innecesarias
    private CancellationTokenSource? _loadCancellationToken;
    private DateTime _lastLoadTime = DateTime.MinValue;
    private DateTime _lastObservacionesUpdateTime = DateTime.MinValue;
    private const int DEBOUNCE_DELAY_MS = 300; // 300ms de debounce para seguimientos
    private const int MIN_RELOAD_INTERVAL_MS = 1500; // MÃ­nimo 1.5 segundos entre cargas
    private const int MIN_OBSERVACIONES_UPDATE_INTERVAL_MS = 5000; // MÃ­nimo 5 segundos entre actualizaciones de observaciones

    public SeguimientoViewModel(
        ISeguimientoService seguimientoService, 
        ICurrentUserService currentUserService,
        ISeguimientosExportService seguimientosExportService, // <-- nuevo parámetro
        IDatabaseConnectionService databaseService,
        IGestLogLogger logger)
        : base(databaseService, logger)
    {
        try
        {
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _seguimientosExportService = seguimientosExportService ?? throw new ArgumentNullException(nameof(seguimientosExportService)); // asignación
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            // Suscribirse a mensajes de actualizaciÃ³n de seguimientos
            // OPTIMIZACIÃ“N: Solo recargar cuando sea realmente necesario
            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => 
            {
                try
                {
                    // Solo recargar si han pasado al menos 1.5 segundos desde la Ãºltima carga
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

            // Cargar datos automÃ¡ticamente al crear el ViewModel
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
            logger?.LogError(ex, "[SeguimientoViewModel] Error crÃ­tico en constructor");
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

    partial void OnAnioSeleccionadoChanged(int value)
    {
        FiltrarPorAnio();
    }    private void FiltrarPorAnio()
    {
        if (Seguimientos == null) return;
        var filtrados = Seguimientos.Where(s => s.Anio == AnioSeleccionado).ToList();
        SeguimientosFiltrados = new ObservableCollection<SeguimientoMantenimientoDto>(filtrados);
        
        // Refrescar la ICollectionView para que el DataGrid se actualice con el nuevo filtro de aÃ±o
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            try
            {
                SeguimientosView?.Refresh();
            }
            catch (System.InvalidOperationException)
            {
                // Si Refresh estÃ¡ aplazado, reintentar con prioridad mÃ¡s baja
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    new Action(() => { try { SeguimientosView?.Refresh(); } catch { } }),
                    System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
        });
        
        // Recalcular estadÃ­sticas para el aÃ±o seleccionado
        CalcularEstadisticasPorAnio();
    }private void CalcularEstadisticasPorAnio()
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

    // Wrapper sin parÃ¡metros para que MVVM Toolkit genere el comando
    [RelayCommand]
    public async Task LoadSeguimientos()
    {
        await LoadSeguimientosAsync(forceReload: true);
    }

    // MÃ©todo original (sin [RelayCommand])
    public async Task LoadSeguimientosAsync(bool forceReload = true)
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
        StatusMessage = "Cargando seguimientos...";
        try
        {
            // OPTIMIZACIÃ“N: Actualizar observaciones solo si han pasado mÃ¡s de 5 segundos
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

            // Continuar con la carga aunque falle la actualizaciÃ³n de observaciones
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
                    s.Estado = estadoCalculado;                if (string.IsNullOrWhiteSpace(s.Responsable))
                        s.Responsable = "Automático";
                    await _seguimientoService.UpdateAsync(s);
                }
                s.RefrescarCacheFiltro(); // Refresca la cachÃ© de campos normalizados
            }            Seguimientos = new ObservableCollection<SeguimientoMantenimientoDto>(lista);
            
            // Cargar aÃ±os disponibles y filtrar por aÃ±o seleccionado
            var anios = lista.Select(s => s.Anio).Distinct().OrderByDescending(a => a).ToList();            AniosDisponibles = new ObservableCollection<int>(anios);
            if (!AniosDisponibles.Contains(AnioSeleccionado))
            {
                // Si el año actual no existe en los datos, usar el primer año disponible
                AnioSeleccionado = anios.FirstOrDefault() == 0 ? DateTime.Now.Year : anios.FirstOrDefault();
            }
            
            FiltrarPorAnio();
            // Nota: FiltrarPorAnio() ya llama a CalcularEstadisticasPorAnio() que usa SeguimientosFiltrados
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
    }    [RelayCommand(CanExecute = nameof(CanAddSeguimiento))]    public async Task AddSeguimientoAsync()
    {
        var dialog = new GestLog.Modules.GestionMantenimientos.Views.Seguimiento.SeguimientoDialog();
        dialog.Owner = System.Windows.Application.Current.MainWindow;
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
    }    [RelayCommand(CanExecute = nameof(CanEditSeguimiento))]
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
    }    /// <summary>
    /// Determina si hay filtros activos (bÃºsqueda o fechas).
    /// </summary>
    private bool TieneFiltrosActivos()
    {
        return !string.IsNullOrWhiteSpace(FiltroSeguimiento) || 
               FechaDesde.HasValue || 
               FechaHasta.HasValue;
    }    [RelayCommand(CanExecute = nameof(CanExportSeguimiento))]
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

            // Delegar la creación y guardado del Excel al servicio de exportación
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

    [RelayCommand]
    public async Task ImportarSeguimientosAntiguosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                Title = "Importar seguimientos antiguos"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;
            StatusMessage = "Importando seguimientos antiguos...";

            await _seguimientoService.ImportarDesdeExcelAsync(openFileDialog.FileName);

            await LoadSeguimientosAsync(forceReload: true);
            StatusMessage = $"Seguimientos importados correctamente desde {System.IO.Path.GetFileName(openFileDialog.FileName)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al importar seguimientos antiguos");
            StatusMessage = $"Error al importar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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
        // No refrescar automÃ¡ticamente
    }

    partial void OnFechaDesdeChanged(DateTime? value)
    {
        // No refrescar automÃ¡ticamente
    }

    partial void OnFechaHastaChanged(DateTime? value)
    {
        // No refrescar automÃ¡ticamente
    }

    partial void OnSeguimientosChanged(ObservableCollection<SeguimientoMantenimientoDto> value)
    {
        // Ejecutar la actualizaciÃ³n de la vista en el Dispatcher para evitar InvalidOperationException
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            // Fallback si no hay aplicaciÃ³n (ej. pruebas unitarias)
            if (value != null)
            {
                foreach (var s in value)
                    s.RefrescarCacheFiltro();
            }

            SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
            if (SeguimientosView != null)
                SeguimientosView.Filter = FiltrarSeguimiento;
            try { SeguimientosView?.Refresh(); } catch (System.InvalidOperationException) { /* ignorar si estÃ¡ en DeferRefresh */ }
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

                // Intentar refrescar; si falla porque Refresh estÃ¡ aplazado, reintentar con prioridad mÃ¡s baja
                try
                {
                    SeguimientosView?.Refresh();
                }
                catch (System.InvalidOperationException)
                {
                    // Reagendar un reintento cuando la UI estÃ© ociosa
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
    }    private bool FiltrarSeguimiento(object obj)
    {
        if (obj is not SeguimientoMantenimientoDto s) return false;

        // âœ… FILTRO POR AÃ‘O (primero y mÃ¡s importante)
        if (s.Anio != AnioSeleccionado)
            return false;

        // Filtro mÃºltiple por texto
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
            .Replace("Ã¡", "a").Replace("Ã©", "e").Replace("Ã­", "i")
            .Replace("Ã³", "o").Replace("Ãº", "u").Replace("Ã¼", "u")
            .Replace("Ã", "A").Replace("Ã‰", "E").Replace("Ã", "I")
            .Replace("Ã“", "O").Replace("Ãš", "U").Replace("Ãœ", "U")
            .Replace("Ã±", "n").Replace("Ã‘", "N");
    }

    private string SepararCamelCase(string texto)
    {
        // Convierte "RealizadoEnTiempo" en "Realizado en tiempo"
        return System.Text.RegularExpressions.Regex.Replace(texto, "([a-z])([A-Z])", "$1 $2");
    }    private void RecalcularEstadisticas()
    {
        SeguimientosTotal = Seguimientos.Count;
        SeguimientosPendientes = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
        SeguimientosEjecutados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
        SeguimientosRetrasados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
        SeguimientosRealizadosFueraDeTiempo = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo);
        SeguimientosNoRealizados = Seguimientos.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
    }

    // âœ… IMPLEMENTACIÃ“N REQUERIDA: DatabaseAwareViewModel
    protected override async Task RefreshDataAsync()
    {
        await LoadSeguimientosAsync(forceReload: true);
    }

    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexiÃ³n - MÃ³dulo no disponible";
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


