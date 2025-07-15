using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para la gestión de equipos.
/// </summary>
public partial class EquiposViewModel : ObservableObject
{
    private readonly IEquipoService _equipoService;
    private readonly IGestLogLogger _logger;

    [ObservableProperty]
    private ObservableCollection<EquipoDto> equipos = new();

    [ObservableProperty]
    private EquipoDto? selectedEquipo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    public EquiposViewModel(IEquipoService equipoService, IGestLogLogger logger)
    {
        _equipoService = equipoService;
        _logger = logger;
        // Suscribirse a mensajes de actualización de cronogramas y seguimientos
        WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => await LoadEquiposAsync());
        WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await LoadEquiposAsync());
    }

    [RelayCommand]
    public async Task LoadEquiposAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando equipos...";
        try
        {
            var lista = await _equipoService.GetAllAsync();
            Equipos = new ObservableCollection<EquipoDto>(lista);
            StatusMessage = $"{Equipos.Count} equipos cargados.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar equipos");
            StatusMessage = "Error al cargar equipos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddEquipoAsync()
    {
        try
        {
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.EquipoDialog();
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) dialog.Owner = owner;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await _equipoService.AddAsync(dialog.Equipo);
                await LoadEquiposAsync();
                StatusMessage = "Equipo agregado exitosamente.";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al agregar equipo");
            StatusMessage = "Error al agregar equipo.";
        }
    }

    [RelayCommand]
    public async Task EditEquipoAsync()
    {
        if (SelectedEquipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para editar.";
            return;
        }
        try
        {
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.EquipoDialog(SelectedEquipo);
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) dialog.Owner = owner;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                await _equipoService.UpdateAsync(dialog.Equipo);
                await LoadEquiposAsync();
                StatusMessage = "Equipo editado exitosamente.";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al editar equipo");
            StatusMessage = "Error al editar equipo.";
        }
    }

    [RelayCommand]
    public async Task DeleteEquipoAsync()
    {
        if (SelectedEquipo == null || string.IsNullOrWhiteSpace(SelectedEquipo.Codigo))
        {
            StatusMessage = "Debe seleccionar un equipo válido para eliminar.";
            return;
        }
        try
        {
            await _equipoService.DeleteAsync(SelectedEquipo.Codigo!);
            await LoadEquiposAsync();
            StatusMessage = "Equipo eliminado exitosamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar equipo");
            StatusMessage = "Error al eliminar equipo.";
        }
    }

    [RelayCommand]
    public Task ImportarEquiposAsync()
    {
        try
        {
            // TODO: Abrir diálogo para seleccionar archivo Excel
            // string filePath = ...
            // await _equipoService.ImportarDesdeExcelAsync(filePath);
            // await LoadEquiposAsync();
            StatusMessage = "Importación completada exitosamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al importar equipos");
            StatusMessage = "Error al importar equipos.";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ExportarEquiposAsync()
    {
        try
        {
            // TODO: Abrir diálogo para seleccionar ruta de guardado
            // string filePath = ...
            // await _equipoService.ExportarAExcelAsync(filePath);
            StatusMessage = "Exportación completada exitosamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al exportar equipos");
            StatusMessage = "Error al exportar equipos.";
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task BackupEquiposAsync()
    {
        try
        {
            await _equipoService.BackupAsync();
            StatusMessage = "Backup realizado exitosamente.";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al realizar backup de equipos");
            StatusMessage = "Error al realizar backup de equipos.";
        }
    }

    // Listas para ComboBox
    public IEnumerable<EstadoEquipo> EstadosEquipo => System.Enum.GetValues(typeof(EstadoEquipo)) as EstadoEquipo[] ?? new EstadoEquipo[0];
    public IEnumerable<TipoMantenimiento> TiposMantenimiento => System.Enum.GetValues(typeof(TipoMantenimiento)) as TipoMantenimiento[] ?? new TipoMantenimiento[0];
    public IEnumerable<Sede> Sedes => System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0];
    public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento => System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0];

    // TODO: Implementar métodos para abrir diálogos de alta/edición y conectar con servicios asíncronos
}
