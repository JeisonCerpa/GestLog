using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
                Seguimientos.Add(nuevo);
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
                // Actualizar en la colección
                var idx = Seguimientos.IndexOf(SelectedSeguimiento);
                if (idx >= 0)
                    Seguimientos[idx] = editado;
                StatusMessage = "Seguimiento editado correctamente.";
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error al editar seguimiento");
                StatusMessage = "Error al editar seguimiento.";
            }
        }
    }

    // TODO: Agregar comandos para eliminar, importar/exportar y backup de seguimientos
}
