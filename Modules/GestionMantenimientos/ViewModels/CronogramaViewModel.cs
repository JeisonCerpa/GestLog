using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para la gestión del cronograma de mantenimientos.
/// </summary>
public partial class CronogramaViewModel : ObservableObject
{
    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly IGestLogLogger _logger;

    [ObservableProperty]
    private ObservableCollection<CronogramaMantenimientoDto> cronogramas = new();

    [ObservableProperty]
    private CronogramaMantenimientoDto? selectedCronograma;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private ObservableCollection<SemanaViewModel> semanas = new();

    [ObservableProperty]
    private int anioSeleccionado = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> aniosDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<CronogramaMantenimientoDto> cronogramasFiltrados = new();    public CronogramaViewModel(ICronogramaService cronogramaService, ISeguimientoService seguimientoService, IGestLogLogger logger)
    {
        _cronogramaService = cronogramaService;
        _seguimientoService = seguimientoService;
        _logger = logger;
        // Suscribirse a mensajes de actualización de cronogramas y seguimientos
        WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => await LoadCronogramasAsync());
        WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await LoadCronogramasAsync());
        // Cargar datos automáticamente al crear el ViewModel
        Task.Run(async () => 
        {
            try
            {
                // Primero asegurar cronogramas completos
                await _cronogramaService.EnsureAllCronogramasUpToDateAsync();
                // Luego generar seguimientos faltantes
                await _cronogramaService.GenerarSeguimientosFaltantesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaViewModel] Error al actualizar cronogramas y seguimientos");
            }
            
            await LoadCronogramasAsync();
        });
    }

    partial void OnAnioSeleccionadoChanged(int value)
    {
        FiltrarPorAnio();
    }

    private void FiltrarPorAnio()
    {
        if (Cronogramas == null) return;
        var filtrados = Cronogramas.Where(c => c.Anio == AnioSeleccionado).ToList();
        CronogramasFiltrados = new ObservableCollection<CronogramaMantenimientoDto>(filtrados);
        AgruparPorSemana();
    }

    [RelayCommand]
    public async Task LoadCronogramasAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando cronogramas...";
        try
        {
            var lista = await _cronogramaService.GetCronogramasAsync();
            var anios = lista.Select(c => c.Anio).Distinct().OrderByDescending(a => a).ToList();
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Cronogramas = new ObservableCollection<CronogramaMantenimientoDto>(lista);
                AniosDisponibles = new ObservableCollection<int>(anios);
                if (!AniosDisponibles.Contains(AnioSeleccionado))
                    AnioSeleccionado = anios.FirstOrDefault(DateTime.Now.Year);
                FiltrarPorAnio();
                StatusMessage = $"{Cronogramas.Count} cronogramas cargados.";
            });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar cronogramas");
            StatusMessage = "Error al cargar cronogramas.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddCronogramaAsync()
    {
        var dialog = new GestLog.Views.Tools.GestionMantenimientos.CronogramaDialog();
        if (dialog.ShowDialog() == true)
        {
            var nuevo = dialog.Cronograma;
            try
            {
                await _cronogramaService.AddAsync(nuevo);
                WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
                StatusMessage = "Cronograma agregado correctamente.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al agregar cronograma");
                StatusMessage = "Error al agregar cronograma.";
            }
        }
    }

    [RelayCommand]
    public async Task EditCronogramaAsync()
    {
        if (SelectedCronograma == null)
            return;
        var dialog = new GestLog.Views.Tools.GestionMantenimientos.CronogramaDialog(SelectedCronograma);
        if (dialog.ShowDialog() == true)
        {
            var editado = dialog.Cronograma;
            try
            {
                await _cronogramaService.UpdateAsync(editado);
                WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
                StatusMessage = "Cronograma editado correctamente.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al editar cronograma");
                StatusMessage = "Error al editar cronograma.";
            }
        }
    }

    [RelayCommand]
    public async Task DeleteCronogramaAsync()
    {
        if (SelectedCronograma == null)
            return;
        try
        {
            await _cronogramaService.DeleteAsync(SelectedCronograma.Codigo!);
            WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
            StatusMessage = "Cronograma eliminado correctamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar cronograma");
            StatusMessage = "Error al eliminar cronograma.";
        }
    }

    // Utilidad para obtener el primer día de la semana ISO 8601
    private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
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

    // Agrupa los cronogramas por semana del año (ISO 8601)
    public async void AgruparPorSemana()
    {
        Semanas.Clear();
        var añoActual = AnioSeleccionado;
        // Obtener todos los seguimientos del año seleccionado
        var seguimientos = (await _seguimientoService.GetSeguimientosAsync())
            .Where(s => s.Anio == añoActual)
            .ToList();
        var tareas = new List<Task>();
        for (int i = 1; i <= 52; i++)
        {
            var fechaInicio = FirstDateOfWeekISO8601(añoActual, i);
            var fechaFin = fechaInicio.AddDays(6);
            var semanaVM = new SemanaViewModel(i, fechaInicio, fechaFin, _cronogramaService, AnioSeleccionado);
            if (CronogramasFiltrados != null)
            {
                // 1. Agregar programados
                foreach (var c in CronogramasFiltrados)
                {
                    if (c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                    {
                        semanaVM.Mantenimientos.Add(c);
                    }
                }
                // 2. Agregar no programados (manuales)
                var codigosProgramados = CronogramasFiltrados
                    .Where(c => c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                    .Select(c => c.Codigo)
                    .ToHashSet();
                var seguimientosSemana = seguimientos.Where(s => s.Semana == i && !codigosProgramados.Contains(s.Codigo)).ToList();
                foreach (var s in seguimientosSemana)
                {
                    // Crear un CronogramaMantenimientoDto "ficticio" solo para mostrar en la semana
                    var noProgramado = new CronogramaMantenimientoDto
                    {
                        Codigo = s.Codigo,
                        Nombre = s.Nombre,
                        Anio = s.Anio,
                        Semanas = new bool[52],
                        FrecuenciaMtto = s.Frecuencia,
                        IsCodigoReadOnly = true,
                        IsCodigoEnabled = false
                    };
                    // Marcar visualmente que es no programado (puedes usar una propiedad auxiliar si la UI lo soporta)
                    semanaVM.Mantenimientos.Add(noProgramado);
                }
            }
            // Inicializar estados de mantenimientos para la semana (carga asíncrona)
            tareas.Add(semanaVM.CargarEstadosMantenimientosAsync(añoActual, _cronogramaService));
            Semanas.Add(semanaVM);
        }
        await Task.WhenAll(tareas);
    }

    // TODO: Agregar comandos para importar/exportar y backup de cronogramas

    [RelayCommand]
    public void AgruparSemanalmente()
    {
        AgruparPorSemana();
    }
}
