using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

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

    public SeguimientoViewModel(ISeguimientoService seguimientoService, IGestLogLogger logger)
    {
        _seguimientoService = seguimientoService;
        _logger = logger;
        // Suscribirse a mensajes de actualización de seguimientos
        WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await LoadSeguimientosAsync());
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

    // TODO: Agregar comandos para importar/exportar y backup de seguimientos
}
