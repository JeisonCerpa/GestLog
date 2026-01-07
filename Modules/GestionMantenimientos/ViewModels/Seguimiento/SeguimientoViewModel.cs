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
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Utilities;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Seguimiento;

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

            await Task.Run(() =>
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add($"Seguimientos {AnioSeleccionado}");

                // ===== CONFIGURAR ANCHO DE COLUMNAS =====
                ws.Column("A").Width = 12;
                ws.Column("B").Width = 16;
                ws.Column("C").Width = 12;
                ws.Column("D").Width = 15;
                ws.Column("E").Width = 35;
                ws.Column("F").Width = 20;
                ws.Column("G").Width = 15;
                ws.Column("H").Width = 18;
                ws.Column("I").Width = 18;
                ws.Column("J").Width = 15;
                ws.Column("K").Width = 35;

                ws.ShowGridLines = false;

                // ===== FILAS 1-2: LOGO + TÃTULO =====
                ws.Row(1).Height = 35;
                ws.Row(2).Height = 35;
                ws.Range(1, 1, 2, 2).Merge();

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

                var titleRange = ws.Range(1, 3, 2, 11);
                titleRange.Merge();
                var titleCell = titleRange.FirstCell();
                titleCell.Value = "SEGUIMIENTOS DE MANTENIMIENTOS";
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 18;
                titleCell.Style.Font.FontColor = XLColor.Black;
                titleCell.Style.Fill.BackgroundColor = XLColor.White;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Dibujar una lÃ­nea horizontal (border) justo debajo del tÃ­tulo para separar visualmente
                try
                {
                    ws.Range(2, 1, 2, 11).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                catch { }

                // Agregar borde derecho al tÃ­tulo
                titleRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                int currentRowSeg = 3;

                // ===== ENCABEZADOS DE TABLA =====
                var headersSeg = new[] { "Equipo", "Nombre", "Semana", "Tipo", "DescripciÃ³n", "Responsable", "Estado", "Fecha Registro", "Fecha RealizaciÃ³n", "Costo", "Observaciones" };
                for (int col = 1; col <= headersSeg.Length; col++)
                {
                    var headerCell = ws.Cell(currentRowSeg, col);
                    headerCell.Value = headersSeg[col - 1];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                }
                ws.Row(currentRowSeg).Height = 22;
                currentRowSeg++;

                // ===== FILAS DE DATOS =====
                int rowCountSeg = 0;
                foreach (var seg in datosAExportar.OrderBy(s => s.Semana).ThenBy(s => s.Codigo))
                {
                    ws.Cell(currentRowSeg, 1).Value = seg.Codigo ?? "";
                    ws.Cell(currentRowSeg, 2).Value = seg.Nombre ?? "";
                    ws.Cell(currentRowSeg, 3).Value = seg.Semana;
                    ws.Cell(currentRowSeg, 4).Value = seg.TipoMtno?.ToString() ?? "-";
                    
                    var descCell = ws.Cell(currentRowSeg, 5);
                    descCell.Value = seg.Descripcion ?? "";
                    descCell.Style.Alignment.WrapText = true;
                    
                    ws.Cell(currentRowSeg, 6).Value = seg.Responsable ?? "";
                    
                    var estadoCell = ws.Cell(currentRowSeg, 7);
                    estadoCell.Value = EstadoSeguimientoUtils.EstadoToTexto(seg.Estado);
                    estadoCell.Style.Fill.BackgroundColor = EstadoSeguimientoUtils.XLColorFromEstado(seg.Estado);
                    estadoCell.Style.Font.FontColor = XLColor.White;
                    estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    ws.Cell(currentRowSeg, 8).Value = seg.FechaRegistro?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                    ws.Cell(currentRowSeg, 9).Value = seg.FechaRealizacion?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                    
                    var costoCell = ws.Cell(currentRowSeg, 10);
                    costoCell.Value = seg.Costo ?? 0;
                    costoCell.Style.NumberFormat.Format = "$#,##0";
                    
                    var obsCell = ws.Cell(currentRowSeg, 11);
                    obsCell.Value = seg.Observaciones ?? "-";
                    obsCell.Style.Alignment.WrapText = true;
                    obsCell.Style.Alignment.Indent = 2;
                    
                    // Filas alternas con color gris claro
                    if (rowCountSeg % 2 == 0)
                    {
                        for (int col = 1; col <= 11; col++)
                        {
                            if (col != 7)
                                ws.Cell(currentRowSeg, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                        }
                    }
                    
                    ws.Cell(currentRowSeg, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRowSeg, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(currentRowSeg, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    
                    ws.Row(currentRowSeg).Height = 30;
                    currentRowSeg++;
                    rowCountSeg++;
                }

                // Agregar filtros automÃ¡ticos
                if (datosAExportar.Count > 0)
                {
                    int headerRow = currentRowSeg - datosAExportar.Count - 1;
                    ws.Range(headerRow, 1, currentRowSeg - 1, 11).SetAutoFilter();
                }

                // ===== PANEL DE KPIs (INDICADORES CLAVE) =====
                if (datosAExportar.Count > 0)
                {
                    currentRowSeg += 2;
                    
                    var preventivos = datosAExportar.Count(s => s.TipoMtno == TipoMantenimiento.Preventivo);
                    var correctivos = datosAExportar.Count(s => s.TipoMtno == TipoMantenimiento.Correctivo);
                    var realizadosEnTiempo = datosAExportar.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
                    var realizadosFueraTiempo = datosAExportar.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
                    var noRealizados = datosAExportar.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
                    var pendientes = datosAExportar.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
                    var totalCosto = datosAExportar.Sum(s => s.Costo ?? 0);
                    var costoPreventivoTotal = datosAExportar.Where(s => s.TipoMtno == TipoMantenimiento.Preventivo).Sum(s => s.Costo ?? 0);
                    var costoCorrectivo = totalCosto - costoPreventivoTotal;
                    
                    var totalMtto = datosAExportar.Count;
                    var pctCumplimiento = totalMtto > 0 ? (realizadosEnTiempo + realizadosFueraTiempo) / (decimal)totalMtto * 100 : 0;
                    var pctCorrectivos = totalMtto > 0 ? correctivos / (decimal)totalMtto * 100 : 0;
                    var pctPreventivos = totalMtto > 0 ? preventivos / (decimal)totalMtto * 100 : 0;
                    
                    // TÃ­tulo KPIs
                    var kpiTitle = ws.Cell(currentRowSeg, 1);
                    kpiTitle.Value = "INDICADORES DE DESEMPEÃ‘O - AÃ‘O " + AnioSeleccionado;
                    kpiTitle.Style.Font.Bold = true;
                    kpiTitle.Style.Font.FontSize = 14;
                    kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                    kpiTitle.Style.Font.FontColor = XLColor.White;
                    kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                    ws.Row(currentRowSeg).Height = 22;
                    currentRowSeg++;

                    // KPI Row
                    var kpiLabels = new[] { "Cumplimiento", "Total Mtos", "Correctivos", "Preventivos" };
                    var kpiValues = new object[] 
                    { 
                        $"{pctCumplimiento:F1}%", 
                        totalMtto, 
                        $"{correctivos} ({pctCorrectivos:F1}%)",
                        $"{preventivos} ({pctPreventivos:F1}%)"
                    };

                    for (int col = 0; col < kpiLabels.Length; col++)
                    {
                        var labelCell = ws.Cell(currentRowSeg, col + 1);
                        labelCell.Value = kpiLabels[col];
                        labelCell.Style.Font.Bold = true;
                        labelCell.Style.Font.FontSize = 10;
                        labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                        labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                        
                        var valueCell = ws.Cell(currentRowSeg + 1, col + 1);
                        if (kpiValues[col] is string strVal)
                            valueCell.Value = strVal;
                        else if (kpiValues[col] is int intVal)
                            valueCell.Value = intVal;
                        else
                            valueCell.Value = kpiValues[col]?.ToString() ?? "-";
                        
                        valueCell.Style.Font.Bold = true;
                        valueCell.Style.Font.FontSize = 12;
                        valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        valueCell.Style.Font.FontColor = XLColor.White;
                        valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }
                    currentRowSeg += 2;
                    
                    // ===== RESUMEN POR TIPO DE MANTENIMIENTO =====
                    currentRowSeg += 1;
                    var tipoTitle = ws.Cell(currentRowSeg, 1);
                    tipoTitle.Value = "RESUMEN POR TIPO DE MANTENIMIENTO";
                    tipoTitle.Style.Font.Bold = true;
                    tipoTitle.Style.Font.FontSize = 12;
                    tipoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                    tipoTitle.Style.Font.FontColor = XLColor.White;
                    tipoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(currentRowSeg, 1, currentRowSeg, 5).Merge();
                    ws.Row(currentRowSeg).Height = 20;
                    currentRowSeg++;
                    
                    var tipoHeaders = new[] { "Tipo", "Cantidad", "%", "Costo Total", "% Costo" };
                    for (int col = 0; col < tipoHeaders.Length; col++)
                    {
                        var headerCell = ws.Cell(currentRowSeg, col + 1);
                        headerCell.Value = tipoHeaders[col];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    currentRowSeg++;
                    
                    var tipoData = new (string tipo, int cantidad, decimal costo)[]
                    {
                        ("Preventivo", preventivos, costoPreventivoTotal),
                        ("Correctivo", correctivos, costoCorrectivo),
                        ("TOTAL", totalMtto, totalCosto)
                    };
                    
                    foreach (var data in tipoData)
                    {
                        int col = 1;
                        var tipoCell = ws.Cell(currentRowSeg, col++);
                        tipoCell.Value = data.tipo;
                        tipoCell.Style.Font.Bold = data.tipo == "TOTAL";
                        tipoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                        
                        var cantCell = ws.Cell(currentRowSeg, col++);
                        cantCell.Value = data.cantidad;
                        cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cantCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                          var pctCell = ws.Cell(currentRowSeg, col++);
                        if (data.tipo != "TOTAL" && totalMtto > 0)
                            pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                        pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                        pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        pctCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                        
                        var costoCell = ws.Cell(currentRowSeg, col++);
                        costoCell.Value = data.costo;
                        costoCell.Style.NumberFormat.Format = "$#,##0";
                        costoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        costoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                        
                        var pctCostoCell = ws.Cell(currentRowSeg, col++);
                        if (data.tipo != "TOTAL" && totalCosto > 0)
                            pctCostoCell.Value = (data.costo / totalCosto * 100);
                        pctCostoCell.Style.NumberFormat.Format = "0.0\"%\"";
                        pctCostoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        pctCostoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                        
                        currentRowSeg++;
                    }
                    
                    // ===== ANÃLISIS DE ESTADOS =====
                    currentRowSeg += 1;
                    var estadoTitle = ws.Cell(currentRowSeg, 1);
                    estadoTitle.Value = "ANÃLISIS DE CUMPLIMIENTO POR ESTADO";
                    estadoTitle.Style.Font.Bold = true;
                    estadoTitle.Style.Font.FontSize = 12;
                    estadoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                    estadoTitle.Style.Font.FontColor = XLColor.White;
                    estadoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(currentRowSeg, 1, currentRowSeg, 6).Merge();
                    ws.Row(currentRowSeg).Height = 20;
                    currentRowSeg++;

                    var estadoHeaders = new[] { "Estado", "Cantidad", "%", "Color" };
                    for (int col = 0; col < estadoHeaders.Length; col++)
                    {
                        var headerCell = ws.Cell(currentRowSeg, col + 1);
                        headerCell.Value = estadoHeaders[col];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    currentRowSeg++;
                    
                    var estadoData = new (string estado, int cantidad, decimal costo, string colorHex)[]
                    {
                        ("Realizado en Tiempo", realizadosEnTiempo, datosAExportar.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo).Sum(s => s.Costo ?? 0), "388E3C"),
                        ("Realizado Fuera de Tiempo", realizadosFueraTiempo, datosAExportar.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado).Sum(s => s.Costo ?? 0), "FFB300"),
                        ("No Realizado", noRealizados, datosAExportar.Where(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado).Sum(s => s.Costo ?? 0), "C80000"),
                        ("Pendiente", pendientes, datosAExportar.Where(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente).Sum(s => s.Costo ?? 0), "B3E5FC")
                    };
                    
                    foreach (var data in estadoData)
                    {
                        if (data.cantidad == 0) continue;
                        
                        int col = 1;
                        var estadoCell = ws.Cell(currentRowSeg, col++);
                        estadoCell.Value = data.estado;
                        
                        var cantCell = ws.Cell(currentRowSeg, col++);
                        cantCell.Value = data.cantidad;
                        cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                          var pctCell = ws.Cell(currentRowSeg, col++);
                        if (totalMtto > 0)
                            pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                        pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                        pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        
                        var colorCell = ws.Cell(currentRowSeg, col++);
                        colorCell.Value = "â– ";
                        colorCell.Style.Font.FontSize = 14;
                        var colorValue = XLColor.FromArgb(int.Parse(data.colorHex, System.Globalization.NumberStyles.HexNumber));
                        colorCell.Style.Fill.BackgroundColor = colorValue;
                        colorCell.Style.Font.FontColor = colorValue;
                        colorCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        colorCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        
                        currentRowSeg++;
                    }
                }

                // ===== PIE DE PÃGINA =====
                currentRowSeg += 2;
                var footerCell = ws.Cell(currentRowSeg, 1);
                footerCell.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} â€¢ Sistema GestLog Â© SIMICS Group SAS";
                footerCell.Style.Font.Italic = true;
                footerCell.Style.Font.FontSize = 9;
                footerCell.Style.Font.FontColor = XLColor.Gray;
                ws.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                
                // Agregar borde exterior grueso
                ws.Range(1, 1, currentRowSeg, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                
                ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                ws.PageSetup.AdjustTo(100);
                ws.PageSetup.FitToPages(1, 0);
                ws.PageSetup.Margins.Top = 0.5;
                ws.PageSetup.Margins.Bottom = 0.5;
                ws.PageSetup.Margins.Left = 0.5;
                ws.PageSetup.Margins.Right = 0.5;

                workbook.SaveAs(saveFileDialog.FileName);
            });

            StatusMessage = $"ExportaciÃ³n completada: {saveFileDialog.FileName} ({datosAExportar.Count} seguimientos)";
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


