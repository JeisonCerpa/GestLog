using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Win32;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.ViewModels.Cronograma
{    /// <summary>
    /// ViewModel para la gestin del cronograma de mantenimientos.
    /// </summary>
    public partial class CronogramaViewModel : DatabaseAwareViewModel
{    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICronogramaExportService _exportService;
    private CurrentUserInfo _currentUser;

    // Permisos reactivos para cronograma
    [ObservableProperty]
    private bool canAddCronograma;
    [ObservableProperty]
    private bool canEditCronograma;
    [ObservableProperty]
    private bool canDeleteCronograma;
    [ObservableProperty]
    private bool canExportCronograma;

    [ObservableProperty]
    private ObservableCollection<CronogramaMantenimientoDto> cronogramas = new();    [ObservableProperty]
    private CronogramaMantenimientoDto? selectedCronograma;
    
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;
    
    private bool _isInitialized;    private readonly object _initializationLock = new object();
    private bool _isRefreshing = false;
    private bool _isLoadingData = false;
    private int _isGroupingFlag = 0;

    [ObservableProperty]
    private ObservableCollection<SemanaViewModel> semanas = new();

    [ObservableProperty]
    private ObservableCollection<int> placeholderSemanas = new();

    [ObservableProperty]
    private int anioSeleccionado = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> aniosDisponibles = new();    [ObservableProperty]
    private ObservableCollection<CronogramaMantenimientoDto> cronogramasFiltrados = new();

    public CronogramaViewModel(
        ICronogramaService cronogramaService, 
        ISeguimientoService seguimientoService, 
        ICurrentUserService currentUserService,
        IDatabaseConnectionService databaseService,
        IGestLogLogger logger,
        ICronogramaExportService exportService)
        : base(databaseService, logger)
    {
        try
        {
            _cronogramaService = cronogramaService;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => 
            {
                lock (_initializationLock)
                {
                    if (!_isInitialized) return;
                }
                await LoadCronogramasAsync();
            });

            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) =>
            {
                lock (_initializationLock)
                {
                    if (!_isInitialized) return;
                }
                
                try
                {
                    await LoadCronogramasAsync();
                    await AgruparPorSemanaAsync();
                }
                catch (Exception ex)
                {                    _logger.LogError(ex, "[CronogramaViewModel] Error en mensaje SeguimientosActualizados");
                }
            });

            WeakReferenceMessenger.Default.Register<EquiposActualizadosMessage>(this, async (r, m) =>
            {
                lock (_initializationLock)
                {
                    if (!_isInitialized) return;
                }
                
                try
                {
                    await _cronogramaService.EnsureAllCronogramasUpToDateAsync();
                    await _cronogramaService.GenerarSeguimientosFaltantesAsync();
                    await LoadCronogramasAsync();
                    await AgruparPorSemanaAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaViewModel] Error en mensaje EquiposActualizados");
                }
            });            IsLoading = true;
            StatusMessage = "Cargando cronogramas...";

            Task.Run(async () => 
            {
                try
                {
                    await LoadCronogramasAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaViewModel] Error en carga inicial de cronogramas");
                    System.Windows.Application.Current.Dispatcher.Invoke(() => IsLoading = false);
                }
            });
        }        catch (Exception ex)
        {
            logger?.LogError(ex, "[CronogramaViewModel] Error crítico en constructor");
            throw;
        }
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
    }

    private void RecalcularPermisos()
    {
        CanAddCronograma = _currentUser.HasPermission("GestionMantenimientos.AgregarCronograma");
        CanEditCronograma = _currentUser.HasPermission("GestionMantenimientos.EditarCronograma");
        CanDeleteCronograma = _currentUser.HasPermission("GestionMantenimientos.EliminarCronograma");
        CanExportCronograma = _currentUser.HasPermission("GestionMantenimientos.ExportarExcel");
    }    partial void OnAnioSeleccionadoChanged(int value)
    {
        FiltrarPorAnio();
    }

    private void FiltrarPorAnio()
    {
        if (Cronogramas == null) return;

        var filtrados = Cronogramas.Where(c => c.Anio == AnioSeleccionado).ToList();
        CronogramasFiltrados = new ObservableCollection<CronogramaMantenimientoDto>(filtrados);

        lock (_initializationLock)
        {
            if (_isInitialized && !_isRefreshing && !_isLoadingData)
            {
                _ = AgruparPorSemanaAsync();
            }
        }
    }

    [RelayCommand]    public async Task LoadCronogramasAsync()
    {
        lock (_initializationLock)
        {
            if (_isInitialized && IsLoading) return;
        }

        StatusMessage = "Cargando cronogramas...";

        try
        {
            _isLoadingData = true;
            
            var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(AnioSeleccionado);            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PlaceholderSemanas.Clear();
                foreach (var i in Enumerable.Range(1, weeksInYear)) PlaceholderSemanas.Add(i);
            });            var lista = await _cronogramaService.GetCronogramasAsync();
            var anios = lista.Select(c => c.Anio).Distinct().OrderByDescending(a => a).ToList();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Cronogramas = new ObservableCollection<CronogramaMantenimientoDto>(lista);
                AniosDisponibles = new ObservableCollection<int>(anios);
                if (!AniosDisponibles.Contains(AnioSeleccionado))
                    AnioSeleccionado = anios.FirstOrDefault(DateTime.Now.Year);
                FiltrarPorAnio();
                StatusMessage = $"{Cronogramas.Count} cronogramas cargados.";

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }

                PlaceholderSemanas.Clear();
                IsLoading = false;
                _isLoadingData = false;
            });

            _ = AgruparPorSemanaAsync();
        }
        catch (System.Exception ex)        {
            _logger.LogError(ex, "Error al cargar cronogramas");
            StatusMessage = "Error al cargar cronogramas.";
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            _isLoadingData = false;
        }
        finally
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlaceholderSemanas.Clear());
            _isLoadingData = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddCronograma))]
    public async Task AddCronogramaAsync()
    {
        var dialog = new GestLog.Modules.GestionMantenimientos.Views.Cronograma.CronogramaDialog();
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
            }        }
    }

    [RelayCommand(CanExecute = nameof(CanEditCronograma))]
    public async Task EditCronogramaAsync()
    {
        if (SelectedCronograma == null)
            return;
        var dialog = new GestLog.Modules.GestionMantenimientos.Views.Cronograma.CronogramaDialog(SelectedCronograma);
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
            }        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCronograma))]
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
        }    }

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
        var result = firstThursday.AddDays(weekNum * 7);        return result.AddDays(-3);
    }

    public async Task AgruparPorSemanaAsync()
    {
        if (System.Threading.Interlocked.CompareExchange(ref _isGroupingFlag, 1, 0) == 1)
        {
            _logger.LogDebug("[CronogramaViewModel] Agrupacin ya en progreso, evitando reentrada");
            return;
        }

        try
        {
            // Verificar que tengamos datos para procesar
            if (CronogramasFiltrados == null || !CronogramasFiltrados.Any())
            {
                _logger.LogDebug("[CronogramaViewModel]   No hay cronogramas filtrados para agrupar");
                return;
            }

            _logger.LogDebug("[CronogramaViewModel]  Iniciando agrupacin por semana para {Count} cronogramas", CronogramasFiltrados.Count);
            
            // Limpiar en el hilo de UI
            System.Windows.Application.Current.Dispatcher.Invoke(() => Semanas.Clear());
              var aoActual = AnioSeleccionado;
            var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(aoActual);
            var seguimientos = (await _seguimientoService.GetSeguimientosAsync())
                .Where(s => s.Anio == aoActual)
                .ToList();

            var semaphore = new System.Threading.SemaphoreSlim(3);
            var semanasTemp = new List<SemanaViewModel>();
            
            for (int i = 1; i <= weeksInYear; i++)
            {
                var fechaInicio = FirstDateOfWeekISO8601(aoActual, i);
                var fechaFin = fechaInicio.AddDays(6);
                var semanaVM = new SemanaViewModel(i, fechaInicio, fechaFin, _cronogramaService, AnioSeleccionado);
                if (CronogramasFiltrados != null)
                {
                    foreach (var c in CronogramasFiltrados)
                    {
                        if (c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        {
                            semanaVM.Mantenimientos.Add(c);                        }
                    }

                    var codigosProgramados = CronogramasFiltrados
                        .Where(c => c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        .Select(c => c.Codigo)
                        .ToHashSet();

                    var seguimientosSemana = seguimientos.Where(s =>
                        s.Semana == i && 
                        (!codigosProgramados.Contains(s.Codigo) || 
                         s.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Correctivo)
                    ).ToList();
                    
                    foreach (var s in seguimientosSemana)
                    {
                        var noProgramado = new CronogramaMantenimientoDto
                        {
                            Codigo = s.Codigo,
                            Nombre = s.Nombre,
                            Anio = s.Anio,
                            Semanas = new bool[weeksInYear],
                            FrecuenciaMtto = s.Frecuencia,
                            IsCodigoReadOnly = true,
                            IsCodigoEnabled = false
                        };                        semanaVM.Mantenimientos.Add(noProgramado);
                    }
                }

                _ = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await semanaVM.CargarEstadosMantenimientosAsync(aoActual, _cronogramaService);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Timeout"))
                    {
                        _logger.LogWarning(ex, $"  Timeout al cargar estados de la semana {i} - Pool agotado");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, $" Error al cargar estados de la semana {i}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                semanasTemp.Add(semanaVM);
            }
              // Agregar todas las semanas a la colección observable en el hilo de UI INMEDIATAMENTE
            // SIN esperar a que se carguen los estados
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var semana in semanasTemp)
                {
                    Semanas.Add(semana);
                }
            });
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _isGroupingFlag, 0);
        }
    }    [RelayCommand]    public async Task AgruparSemanalmente()
    {
        lock (_initializationLock)
        {
            if (!_isInitialized) return;
        }

        await AgruparPorSemanaAsync();
    }

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Evitar mltiples refrescos simultneos
        if (IsLoading) return;

        //  Ejecutar en background thread para NO bloquear la UI
        await Task.Run(async () =>
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = true;
                    _isRefreshing = true;  //  Indicar que estamos refrescando
                    StatusMessage = " Refrescando cronograma...";

                    // Inicializar placeholders mientras se realiza el refresco
                    try
                    {
                        var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(AnioSeleccionado);
                        PlaceholderSemanas.Clear();
                        foreach (var i in Enumerable.Range(1, weeksInYear)) PlaceholderSemanas.Add(i);
                    }
                    catch { /* swallo contenido si falla */ }
                });

                _logger.LogInformation("[CronogramaViewModel]  Iniciando refresco completo del cronograma desde 0");

                // PASO 1: Limpiar toda la UI
                _logger.LogInformation("[CronogramaViewModel]  PASO 1: Limpiando datos previos...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 1/5: Limpiando datos previos...";
                    Cronogramas.Clear();
                    CronogramasFiltrados.Clear();
                    Semanas.Clear();
                    AniosDisponibles.Clear();
                });

                // PASO 2: Actualizar cronogramas (asegurar que est©n completos)
                _logger.LogInformation("[CronogramaViewModel]  PASO 2: Actualizando cronogramas en BD...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 2/5: Actualizando cronogramas en BD...";
                });
                await _cronogramaService.EnsureAllCronogramasUpToDateAsync().ConfigureAwait(false);

                // PASO 3: Generar seguimientos faltantes
                _logger.LogInformation("[CronogramaViewModel]  PASO 3: Generando seguimientos faltantes...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 3/5: Generando seguimientos faltantes...";
                });
                await _cronogramaService.GenerarSeguimientosFaltantesAsync().ConfigureAwait(false);

                // PASO 4: Cargar cronogramas desde BD
                _logger.LogInformation("[CronogramaViewModel]  PASO 4: Cargando cronogramas desde BD...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 4/5: Cargando cronogramas desde BD...";
                });
                var lista = await _cronogramaService.GetCronogramasAsync().ConfigureAwait(false);
                var anios = lista.Select(c => c.Anio).Distinct().OrderByDescending(a => a).ToList();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Cronogramas = new ObservableCollection<CronogramaMantenimientoDto>(lista);
                    AniosDisponibles = new ObservableCollection<int>(anios);
                    if (!AniosDisponibles.Contains(AnioSeleccionado))
                        AnioSeleccionado = anios.FirstOrDefault(DateTime.Now.Year);
                    FiltrarPorAnio();
                });

                // PASO 5: Reagrupar por semanas
                _logger.LogInformation("[CronogramaViewModel]  PASO 5: Agrupando por semanas...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 5/5: Agrupando por semanas...";
                });
                await AgruparPorSemanaAsync().ConfigureAwait(false);

                _logger.LogInformation("[CronogramaViewModel]  Refresco completado exitosamente");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $" Cronograma refrescado: {Cronogramas.Count} cronogramas y {Semanas.Count} semanas.";
                });
            }
            catch (OperationCanceledException)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "  Refresco cancelado.";
                });
                _logger.LogInformation("[CronogramaViewModel] Refresco cancelado por el usuario");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaViewModel] Error durante el refresco completo");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $" Error al refrescar: {ex.Message}";
                });
            }
            finally
            {                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                    _isRefreshing = false;
                    PlaceholderSemanas.Clear();
                });
            }
        }, cancellationToken);
    }    [RelayCommand(CanExecute = nameof(CanExportCronograma))]
    public async Task ExportarCronogramasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                FileName = $"CRONOGRAMA_{AnioSeleccionado}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                Title = "Exportar cronograma a Excel"
            };
            if (saveFileDialog.ShowDialog() != true)
                return;

            IsLoading = true;
            StatusMessage = "Exportando cronograma y seguimientos...";

            // Preparar datos tipados para el servicio
            var cronogramas = CronogramasFiltrados.ToList();

            var todosSeguimientos = await _seguimientoService.GetSeguimientosAsync();
            var seguimientosAnio = todosSeguimientos
                .Where(s => s.Anio == AnioSeleccionado && 
                    (s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo ||
                     s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo ||
                     s.Estado == EstadoSeguimientoMantenimiento.NoRealizado) &&
                    cronogramas.Any(c => c.Codigo == s.Codigo))
                .ToList();

            var realizadoPor = _currentUser?.FullName ?? _currentUser?.Username ?? "-";

            // Delegar toda la generación del Excel al servicio
            await _exportService.ExportAsync(cronogramas, seguimientosAnio, AnioSeleccionado, realizadoPor, saveFileDialog.FileName, cancellationToken).ConfigureAwait(false);

            StatusMessage = $"Exportacin completada: {saveFileDialog.FileName} ({cronogramas.Count} cronogramas, {seguimientosAnio.Count} seguimientos)";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Exportacin cancelada.";
            _logger.LogInformation("[CronogramaViewModel] Exportacin cancelada por el usuario");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CronogramaViewModel] Error al exportar cronograma a Excel");
            StatusMessage = $"Error al exportar: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }    /// <summary>
    /// Implementacin del m©todo abstracto para auto-refresh automtico
    /// </summary>
    protected override async Task RefreshDataAsync()
    {
        try
        {
            _logger.LogInformation("[CronogramaViewModel] Refrescando datos automticamente");
            await LoadCronogramasAsync();
            await AgruparPorSemanaAsync();
            _logger.LogInformation("[CronogramaViewModel] Datos refrescados exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CronogramaViewModel] Error al refrescar datos");
            throw;
        }
    }

    /// <summary>
    /// Override para manejar cuando se pierde la conexión especficamente para cronogramas
    /// </summary>
    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexión - Gestin de cronogramas no disponible";
    }
}
}

