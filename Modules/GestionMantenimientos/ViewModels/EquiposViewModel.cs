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
using ClosedXML.Excel;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.ComponentModel;
using System.Windows.Data;
using System.Linq;
using System.Threading;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.GestionMantenimientos.ViewModels;

/// <summary>
/// ViewModel para la gestión de equipos.
/// </summary>
public partial class EquiposViewModel : ObservableObject
{
    private readonly IEquipoService _equipoService;
    private readonly IGestLogLogger _logger;
    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;

    private readonly ICurrentUserService _currentUserService;
    private CurrentUserInfo _currentUser;

    [ObservableProperty]
    private ObservableCollection<EquipoDto> equipos = new();

    [ObservableProperty]
    private EquipoDto? selectedEquipo;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool mostrarDadosDeBaja = false;

    [ObservableProperty]
    private string filtroEquipo = "";

    [ObservableProperty]
    private ICollectionView? equiposView;    [ObservableProperty]
    private bool canRegistrarEquipo;
    [ObservableProperty]
    private bool canEditarEquipo;
    [ObservableProperty]
    private bool canDarDeBajaEquipo;
    [ObservableProperty]
    private bool canRegistrarMantenimientoPermiso;
    private bool canImportEquipo;

    // Propiedades alias para compatibilidad con la vista XAML
    public bool CanAddEquipo => CanRegistrarEquipo;
    public bool CanDeleteEquipo => CanDarDeBajaEquipo;
    public bool CanImportEquipo => canImportEquipo;
    public bool CanExportEquipo => true; // Exportar no requiere permisos especiales

    public EquiposViewModel(
        IEquipoService equipoService,
        IGestLogLogger logger,
        ICronogramaService cronogramaService,
        ISeguimientoService seguimientoService,
        ICurrentUserService currentUserService)
    {
        _equipoService = equipoService;
        _logger = logger;
        _cronogramaService = cronogramaService;
        _seguimientoService = seguimientoService;
        _currentUserService = currentUserService;
        _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
        _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        // Suscribirse a mensajes de actualización de cronogramas y seguimientos
        WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => await LoadEquiposAsync());
        WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) => await LoadEquiposAsync());
        EquiposView = CollectionViewSource.GetDefaultView(Equipos);
        if (EquiposView != null)
            EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
    }

    [RelayCommand]
    public async Task LoadEquiposAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando equipos...";
        try
        {
            var lista = await _equipoService.GetAllAsync();
            // Filtrar según MostrarDadosDeBaja
            var filtrados = MostrarDadosDeBaja ? lista : lista.Where(e => e.FechaBaja == null).ToList();
            Equipos = new ObservableCollection<EquipoDto>(filtrados);
            EquiposView = CollectionViewSource.GetDefaultView(Equipos);
            if (EquiposView != null)
                EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
            StatusMessage = $"{Equipos.Count} equipos {(MostrarDadosDeBaja ? "(incluye dados de baja)" : "activos")} cargados.";
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
                // Si el usuario cambió el estado a Activo, limpiar la FechaBaja
                if (dialog.Equipo.Estado == EstadoEquipo.Activo)
                    dialog.Equipo.FechaBaja = null;
                await _equipoService.UpdateAsync(dialog.Equipo);
                await LoadEquiposAsync();
                StatusMessage = "Equipo editado exitosamente.";
                WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage());
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
            StatusMessage = "Debe seleccionar un equipo válido para dar de baja.";
            return;
        }
        // Confirmación antes de dar de baja
        var result = System.Windows.MessageBox.Show(
            $"¿Está seguro que desea dar de baja el equipo '{SelectedEquipo.Nombre}' (código: {SelectedEquipo.Codigo})?\nEsta acción es irreversible y eliminará cronogramas y seguimientos pendientes asociados.",
            "Confirmar baja de equipo",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning
        );
        if (result != System.Windows.MessageBoxResult.Yes)
        {
            StatusMessage = "Operación cancelada por el usuario.";
            return;
        }
        try
        {
            SelectedEquipo.FechaBaja = DateTime.Now;
            SelectedEquipo.Estado = EstadoEquipo.DadoDeBaja; // Actualiza el estado explícitamente
            await _equipoService.UpdateAsync(SelectedEquipo);
            // Eliminar cronogramas y seguimientos pendientes
            await _cronogramaService.DeleteByEquipoCodigoAsync(SelectedEquipo.Codigo!);
            WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
            await _seguimientoService.DeletePendientesByEquipoCodigoAsync(SelectedEquipo.Codigo!);
            await LoadEquiposAsync();
            StatusMessage = "Equipo dado de baja exitosamente. Se eliminaron cronogramas y seguimientos pendientes.";
            WeakReferenceMessenger.Default.Send(new EquiposActualizadosMessage());
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al dar de baja equipo");
            StatusMessage = "Error al dar de baja equipo.";
        }
    }

    [RelayCommand]
    public async Task ExportarEquiposAsync()
    {
        try
        {
            var dialog = new VistaSaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Exportar equipos a Excel",
                FileName = "Equipos.xlsx"
            };
            if (dialog.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");
                    // Encabezados
                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Nombre";
                    ws.Cell(1, 3).Value = "Marca";
                    ws.Cell(1, 4).Value = "Estado";
                    ws.Cell(1, 5).Value = "Sede";
                    ws.Cell(1, 6).Value = "Frecuencia Mtto";
                    ws.Cell(1, 7).Value = "Precio";
                    ws.Cell(1, 8).Value = "Fecha Registro";
                    ws.Cell(1, 9).Value = "Fecha Compra";
                    int row = 2;
                    foreach (var eq in Equipos)
                    {
                        ws.Cell(row, 1).Value = eq.Codigo ?? "";
                        ws.Cell(row, 2).Value = eq.Nombre ?? "";
                        ws.Cell(row, 3).Value = eq.Marca ?? "";
                        ws.Cell(row, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(row, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(row, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";
                        ws.Cell(row, 7).Value = eq.Precio ?? 0;
                        ws.Cell(row, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 9).Value = eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);
                });
                StatusMessage = $"Exportación completada: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al exportar equipos");
            StatusMessage = "Error al exportar equipos.";
        }
    }

    private TEnum? ParseEnumFlexible<TEnum>(string? value) where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u").Replace("ñ", "n").Replace(" ", "").ToLowerInvariant();
        foreach (var name in Enum.GetNames(typeof(TEnum)))
        {
            var normName = name.Trim().Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u").Replace("ü", "u").Replace("ñ", "n").Replace(" ", "").ToLowerInvariant();
            if (normalized == normName)
            {
                if (Enum.TryParse<TEnum>(name, out var result))
                    return result;
            }
        }
        return null;
    }

    [RelayCommand]
    public async Task ImportarEquiposAsync()
    {
        try
        {
            var dialog = new VistaOpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                Title = "Importar equipos desde Excel"
            };
            if (dialog.ShowDialog() == true)
            {
                using var workbook = new XLWorkbook(dialog.FileName);
                var ws = workbook.Worksheet(1);
                var equiposImportados = new List<EquipoDto>();
                foreach (var row in ws.RowsUsed().Skip(1)) // Saltar encabezado
                {
                    var eq = new EquipoDto
                    {
                        Codigo = row.Cell(1).GetString(),
                        Nombre = row.Cell(2).GetString(),                        Marca = row.Cell(3).GetString(),
                        Estado = ParseEnumFlexible<EstadoEquipo>(row.Cell(4).GetString()),
                        Sede = ParseEnumFlexible<Sede>(row.Cell(5).GetString()),
                        FrecuenciaMtto = ParseEnumFlexible<FrecuenciaMantenimiento>(row.Cell(6).GetString()),
                        Precio = row.Cell(7).GetValue<decimal>(),
                        FechaRegistro = DateTime.TryParse(row.Cell(8).GetString(), out var fecha) ? fecha : null,
                        FechaCompra = DateTime.TryParse(row.Cell(9).GetString(), out var fechaCompra) ? fechaCompra : null
                    };
                    equiposImportados.Add(eq);
                }
                foreach (var eq in equiposImportados)
                {
                    var existente = Equipos.FirstOrDefault(e => e.Codigo == eq.Codigo);
                    if (existente != null)
                        await _equipoService.UpdateAsync(eq);
                    else
                        await _equipoService.AddAsync(eq);
                }
                await LoadEquiposAsync();
                StatusMessage = $"Importación completada: {equiposImportados.Count} equipos importados.";
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al importar equipos");
            StatusMessage = "Error al importar equipos.";
        }
    }

    // Listas para ComboBox
    public IEnumerable<EstadoEquipo> EstadosEquipo => System.Enum.GetValues(typeof(EstadoEquipo)) as EstadoEquipo[] ?? new EstadoEquipo[0];
    public IEnumerable<TipoMantenimiento> TiposMantenimiento => System.Enum.GetValues(typeof(TipoMantenimiento)) as TipoMantenimiento[] ?? new TipoMantenimiento[0];
    public IEnumerable<Sede> Sedes => System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0];
    public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento => System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0];

    partial void OnMostrarDadosDeBajaChanged(bool value)
    {
        // Recargar la lista de equipos al cambiar el filtro
        _ = LoadEquiposAsync();
    }

    private CancellationTokenSource? _debounceToken;

    partial void OnFiltroEquipoChanged(string value)
    {
        _debounceToken?.Cancel();
        _debounceToken = new CancellationTokenSource();
        var token = _debounceToken.Token;
        Task.Run(async () =>
        {
            await Task.Delay(250, token); // 250ms debounce
            if (!token.IsCancellationRequested)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => EquiposView?.Refresh());
            }
        }, token);
    }

    partial void OnEquiposChanged(ObservableCollection<EquipoDto> value)
    {
        EquiposView = CollectionViewSource.GetDefaultView(Equipos);
        if (EquiposView != null)
            EquiposView.Filter = new Predicate<object>(FiltrarEquipo);
        EquiposView?.Refresh();
    }

    private bool FiltrarEquipo(object obj)
    {
        if (obj is not EquipoDto eq) return false;
        if (string.IsNullOrWhiteSpace(FiltroEquipo)) return true;
        // Permitir múltiples términos separados por punto y coma
        var terminos = FiltroEquipo.Split(';')
            .Select(t => RemoverTildes(t.Trim()).ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray();
        // Campos relevantes para filtrar
        var campos = new[]
        {
            RemoverTildes(eq.Codigo ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Nombre ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Marca ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Sede?.ToString() ?? "").ToLowerInvariant(),
            RemoverTildes(eq.Estado?.ToString() ?? "").ToLowerInvariant(),
            RemoverTildes(eq.FrecuenciaMtto?.ToString() ?? "").ToLowerInvariant(),
            eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? ""
        };
        // Todos los términos deben estar presentes en algún campo
        return terminos.All(term => campos.Any(campo => campo.Contains(term)));
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

    [RelayCommand]
    public async Task ExportarEquiposFiltradosAsync()
    {
        var dialog = new VistaSaveFileDialog
        {
            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            Title = "Exportar equipos filtrados a Excel",
            FileName = "EquiposFiltrados.xlsx"
        };
        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            StatusMessage = "Exportando a Excel...";
            try
            {
                var filtrados = EquiposView?.Cast<EquipoDto>().ToList() ?? new List<EquipoDto>();
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();
                    var ws = workbook.Worksheets.Add("Equipos");
                    ws.Cell(1, 1).Value = "Código";
                    ws.Cell(1, 2).Value = "Nombre";
                    ws.Cell(1, 3).Value = "Marca";
                    ws.Cell(1, 4).Value = "Estado";
                    ws.Cell(1, 5).Value = "Sede";
                    ws.Cell(1, 6).Value = "Frecuencia Mtto";
                    ws.Cell(1, 7).Value = "Precio";
                    ws.Cell(1, 8).Value = "Fecha Registro";
                    ws.Cell(1, 9).Value = "Fecha Compra";
                    int row = 2;
                    foreach (var eq in filtrados)
                    {
                        ws.Cell(row, 1).Value = eq.Codigo ?? "";
                        ws.Cell(row, 2).Value = eq.Nombre ?? "";
                        ws.Cell(row, 3).Value = eq.Marca ?? "";
                        ws.Cell(row, 4).Value = eq.Estado?.ToString() ?? "";
                        ws.Cell(row, 5).Value = eq.Sede?.ToString() ?? "";
                        ws.Cell(row, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";
                        ws.Cell(row, 7).Value = eq.Precio ?? 0;
                        ws.Cell(row, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
                        ws.Cell(row, 9).Value = eq.FechaCompra?.ToString("dd/MM/yyyy") ?? "";
                        row++;
                    }
                    ws.Columns().AdjustToContents();
                    workbook.SaveAs(dialog.FileName);
                });
                StatusMessage = $"Exportación completada: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar equipos filtrados");
                StatusMessage = $"Error al exportar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public bool CanRegistrarMantenimiento(EquipoDto? equipo)
    {
        // Solo permite registrar mantenimiento si el equipo está ACTIVO
        return CanRegistrarMantenimientoPermiso && equipo != null && (equipo.Estado == EstadoEquipo.Activo);
    }

    [RelayCommand(CanExecute = nameof(CanRegistrarMantenimiento))]
    public async Task RegistrarMantenimientoAsync(EquipoDto? equipo)
    {
        if (equipo == null)
        {
            StatusMessage = "Debe seleccionar un equipo para registrar mantenimiento.";
            return;
        }
        try
        {
            var now = DateTime.Now;
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int semanaActual = cal.GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int anioActual = now.Year;
            var seguimiento = new SeguimientoMantenimientoDto
            {
                Codigo = equipo.Codigo,
                Nombre = equipo.Nombre,
                Semana = semanaActual,
                Anio = anioActual,
                FechaRegistro = now,
                TipoMtno = TipoMantenimiento.Correctivo // Preseleccionado
                // Los campos TipoMtno y Frecuencia se llenan en el diálogo y al guardar
            };            // Abrir el diálogo con opciones correctivo/predictivo (modo NO restringido)
            var dialog = new GestLog.Views.Tools.GestionMantenimientos.SeguimientoDialog(seguimiento, false);
            var owner = System.Windows.Application.Current?.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null;
            if (owner != null) dialog.Owner = owner;
            var result = dialog.ShowDialog();
            if (result == true)
            {
                // Asignar la frecuencia automáticamente según el tipo
                if (dialog.Seguimiento.TipoMtno == TipoMantenimiento.Correctivo)
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Correctivo;
                else if (dialog.Seguimiento.TipoMtno == TipoMantenimiento.Predictivo)
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Predictivo;
                else
                    dialog.Seguimiento.Frecuencia = FrecuenciaMantenimiento.Otro;
                await _seguimientoService.AddAsync(dialog.Seguimiento);
                StatusMessage = "Mantenimiento registrado exitosamente.";
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al registrar mantenimiento");
            StatusMessage = "Error al registrar mantenimiento.";
        }
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
    }    private void RecalcularPermisos()
    {
        CanRegistrarEquipo = _currentUser.HasPermission("GestionMantenimientos.NuevoEquipo");
        CanEditarEquipo = _currentUser.HasPermission("GestionMantenimientos.EditarEquipo"); // Si existe este permiso en la BD
        CanDarDeBajaEquipo = _currentUser.HasPermission("GestionMantenimientos.DarDeBajaEquipo");
        canImportEquipo = _currentUser.HasPermission("GestionMantenimientos.ImportarEquipo");
        CanRegistrarMantenimientoPermiso = _currentUser.HasPermission("GestionMantenimientos.RegistrarMantenimiento");
        // Notificar cambios en las propiedades alias
        OnPropertyChanged(nameof(CanAddEquipo));
        OnPropertyChanged(nameof(CanDeleteEquipo));
        OnPropertyChanged(nameof(CanImportEquipo));
        OnPropertyChanged(nameof(CanExportEquipo));
    }
}
