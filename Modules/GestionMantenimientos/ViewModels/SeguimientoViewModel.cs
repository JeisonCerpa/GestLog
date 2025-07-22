using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Threading;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para el seguimiento de mantenimientos.
/// </summary>
public partial class SeguimientoViewModel : ObservableObject
{
    private readonly ISeguimientoService _seguimientoService;
    private readonly IGestLogLogger _logger;

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

    private CancellationTokenSource? _debounceToken;

    public SeguimientoViewModel(ISeguimientoService seguimientoService, IGestLogLogger logger)
    {
        _seguimientoService = seguimientoService;
        _logger = logger;
        // Suscribirse a mensajes de actualización de seguimientos
        WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await LoadSeguimientosAsync());
        SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
        if (SeguimientosView != null)
            SeguimientosView.Filter = FiltrarSeguimiento;
        // Cargar datos automáticamente al crear el ViewModel
        Task.Run(async () => await LoadSeguimientosAsync());
    }

    [RelayCommand]
    public async Task LoadSeguimientosAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando seguimientos...";
        try
        {
            var lista = await _seguimientoService.GetSeguimientosAsync();
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    [RelayCommand]
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

    partial void OnFiltroSeguimientoChanged(string value)
    {
        _debounceToken?.Cancel();
        _debounceToken = new CancellationTokenSource();
        var token = _debounceToken.Token;
        Task.Run(async () =>
        {
            await Task.Delay(250, token); // 250ms debounce
            if (!token.IsCancellationRequested)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => SeguimientosView?.Refresh());
            }
        }, token);
    }
    partial void OnFechaDesdeChanged(DateTime? value)
    {
        SeguimientosView?.Refresh();
    }
    partial void OnFechaHastaChanged(DateTime? value)
    {
        SeguimientosView?.Refresh();
    }
    partial void OnSeguimientosChanged(ObservableCollection<SeguimientoMantenimientoDto> value)
    {
        if (value != null)
        {
            foreach (var s in value)
                s.RefrescarCacheFiltro();
        }
        SeguimientosView = System.Windows.Data.CollectionViewSource.GetDefaultView(Seguimientos);
        if (SeguimientosView != null)
            SeguimientosView.Filter = FiltrarSeguimiento;
        SeguimientosView?.Refresh();
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

    // TODO: Agregar comandos para importar/exportar y backup de seguimientos
}
