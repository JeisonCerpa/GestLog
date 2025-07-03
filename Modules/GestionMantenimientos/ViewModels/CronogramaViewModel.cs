using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

    public CronogramaViewModel(ICronogramaService cronogramaService, IGestLogLogger logger)
    {
        _cronogramaService = cronogramaService;
        _logger = logger;
        // Cargar datos automáticamente al crear el ViewModel
        Task.Run(async () => await LoadCronogramasAsync());
    }

    [RelayCommand]
    public async Task LoadCronogramasAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando cronogramas...";
        try
        {
            var lista = await _cronogramaService.GetCronogramasAsync();
            Cronogramas = new ObservableCollection<CronogramaMantenimientoDto>(lista);
            StatusMessage = $"{Cronogramas.Count} cronogramas cargados.";
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

    // TODO: Agregar comandos para importar/exportar y backup de cronogramas
}
