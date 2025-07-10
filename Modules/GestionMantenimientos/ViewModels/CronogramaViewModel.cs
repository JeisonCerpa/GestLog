using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private ObservableCollection<CronogramaMantenimientoDto> cronogramasFiltrados = new();

    public CronogramaViewModel(ICronogramaService cronogramaService, IGestLogLogger logger)
    {
        _cronogramaService = cronogramaService;
        _logger = logger;
        // Cargar datos automáticamente al crear el ViewModel
        Task.Run(async () => await LoadCronogramasAsync());
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
                Cronogramas.Add(nuevo);
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
                // Actualizar en la colección
                var idx = Cronogramas.IndexOf(SelectedCronograma);
                if (idx >= 0)
                    Cronogramas[idx] = editado;
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
            Cronogramas.Remove(SelectedCronograma);
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
    public void AgruparPorSemana()
    {
        Semanas.Clear();
        var añoActual = AnioSeleccionado;
        for (int i = 1; i <= 52; i++)
        {
            var fechaInicio = FirstDateOfWeekISO8601(añoActual, i);
            var fechaFin = fechaInicio.AddDays(6);
            var semanaVM = new SemanaViewModel(i, fechaInicio, fechaFin);
            if (CronogramasFiltrados != null)
            {
                foreach (var c in CronogramasFiltrados)
                {
                    if (c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        semanaVM.Mantenimientos.Add(c);
                }
            }
            Semanas.Add(semanaVM);
        }
    }

    // TODO: Agregar comandos para importar/exportar y backup de cronogramas

    [RelayCommand]
    public void AgruparSemanalmente()
    {
        AgruparPorSemana();
    }
}
