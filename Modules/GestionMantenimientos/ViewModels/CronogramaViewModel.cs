using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Services.Core.Logging;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using ClosedXML.Excel;
using Microsoft.Win32;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{    /// <summary>
    /// ViewModel para la gestión del cronograma de mantenimientos.
    /// </summary>
    public partial class CronogramaViewModel : DatabaseAwareViewModel
{    private readonly ICronogramaService _cronogramaService;
    private readonly ISeguimientoService _seguimientoService;
    private readonly ICurrentUserService _currentUserService;
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
    private ObservableCollection<CronogramaMantenimientoDto> cronogramas = new();

    [ObservableProperty]
    private CronogramaMantenimientoDto? selectedCronograma;    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? statusMessage;    // Flag para controlar cargas múltiples
    private bool _isInitialized;
    private readonly object _initializationLock = new object();
    
    // Flag para evitar agrupación duplicada durante refresh
    private bool _isRefreshing = false;

    // Flag atómico para evitar reentradas en AgruparPorSemana
    private int _isGroupingFlag = 0;

    [ObservableProperty]
    private ObservableCollection<SemanaViewModel> semanas = new();

    // Colección de placeholders para reservar el espacio visual mientras carga
    [ObservableProperty]
    private ObservableCollection<int> placeholderSemanas = new();

    [ObservableProperty]
    private int anioSeleccionado = DateTime.Now.Year;

    [ObservableProperty]
    private ObservableCollection<int> aniosDisponibles = new();

    [ObservableProperty]
    private ObservableCollection<CronogramaMantenimientoDto> cronogramasFiltrados = new();    public CronogramaViewModel(
        ICronogramaService cronogramaService, 
        ISeguimientoService seguimientoService, 
        ICurrentUserService currentUserService,
        IDatabaseConnectionService databaseService,
        IGestLogLogger logger)
        : base(databaseService, logger)
    {
        try
        {
            _cronogramaService = cronogramaService;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;            // Suscribirse a mensajes de actualización de cronogramas y seguimientos
            WeakReferenceMessenger.Default.Register<CronogramasActualizadosMessage>(this, async (r, m) => 
            {
                // Solo recargar si ya está inicializado
                lock (_initializationLock)
                {
                    if (!_isInitialized) return;
                }
                await LoadCronogramasAsync();
            });            WeakReferenceMessenger.Default.Register<SeguimientosActualizadosMessage>(this, async (r, m) =>
            {
                // Solo recargar si ya está inicializado
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
                {
                    _logger.LogError(ex, "[CronogramaViewModel] Error en mensaje SeguimientosActualizados");
                }
            });
              // Suscribirse a mensaje cuando se agrega/actualiza un equipo
            WeakReferenceMessenger.Default.Register<EquiposActualizadosMessage>(this, async (r, m) =>
            {
                // Solo recargar si ya está inicializado
                lock (_initializationLock)
                {
                    if (!_isInitialized) return;
                }
                
                try
                {
                    _logger.LogInformation("[CronogramaViewModel] 📋 Equipos actualizados - regenerando cronogramas y seguimientos");
                    
                    // PASO 1: Asegurar que los cronogramas estén actualizados para todos los equipos activos
                    _logger.LogInformation("[CronogramaViewModel] ✓ PASO 1: Actualizando cronogramas...");
                    await _cronogramaService.EnsureAllCronogramasUpToDateAsync();
                    
                    // PASO 2: Generar los seguimientos para las semanas programadas
                    _logger.LogInformation("[CronogramaViewModel] ✓ PASO 2: Generando seguimientos...");
                    await _cronogramaService.GenerarSeguimientosFaltantesAsync();
                    
                    // PASO 3: Recargar la vista con los nuevos datos
                    _logger.LogInformation("[CronogramaViewModel] ✓ PASO 3: Recargando vista...");
                    await LoadCronogramasAsync();
                      // PASO 4: Agrupar por semana
                    _logger.LogInformation("[CronogramaViewModel] ✓ PASO 4: Agrupando por semanas...");
                    await AgruparPorSemanaAsync();
                    
                    _logger.LogInformation("[CronogramaViewModel] ✅ Actualización completada exitosamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaViewModel] ❌ Error en mensaje EquiposActualizados");
                }
            });
            
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
                  try
                {
                    await LoadCronogramasAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaViewModel] Error en carga inicial de cronogramas");
                }
            });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "[CronogramaViewModel] Error crítico en constructor");
            throw; // Re-lanzar para que se capture en el nivel superior
        }
    }

    private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
    {
        _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
        RecalcularPermisos();
    }    private void RecalcularPermisos()
    {
        CanAddCronograma = _currentUser.HasPermission("GestionMantenimientos.AgregarCronograma");
        CanEditCronograma = _currentUser.HasPermission("GestionMantenimientos.EditarCronograma");
        CanDeleteCronograma = _currentUser.HasPermission("GestionMantenimientos.EliminarCronograma");
        CanExportCronograma = _currentUser.HasPermission("GestionMantenimientos.ExportarExcel");
    }

    partial void OnAnioSeleccionadoChanged(int value)
    {
        FiltrarPorAnio();
    }    private void FiltrarPorAnio()
    {
        if (Cronogramas == null) return;
          var filtrados = Cronogramas.Where(c => c.Anio == AnioSeleccionado).ToList();
        CronogramasFiltrados = new ObservableCollection<CronogramaMantenimientoDto>(filtrados);
        
        // Solo ejecutar AgruparPorSemana si ya está inicializado, NO está refrescando Y no es la carga inicial
        lock (_initializationLock)
        {
            if (_isInitialized && !_isRefreshing)
            {
                // No esperar aquí, se ejecutará en background
                _ = AgruparPorSemanaAsync();
            }
        }
    }[RelayCommand]
    public async Task LoadCronogramasAsync()
    {
        // Evitar cargas múltiples simultáneas
        lock (_initializationLock)
        {
            if (IsLoading) return;
        }        IsLoading = true;
        StatusMessage = "Cargando cronogramas...";

        // Mostrar placeholders que ocupen el ancho completo según semanas del año seleccionado
        try
        {
            var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(AnioSeleccionado);
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                PlaceholderSemanas.Clear();
                foreach (var i in Enumerable.Range(1, weeksInYear)) PlaceholderSemanas.Add(i);
            });

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
                // Marcar como inicializado después de la primera carga exitosa
                lock (_initializationLock)
                {
                    _isInitialized = true;
                }
            });
            
            // Ejecutar AgruparPorSemana automáticamente después de la carga inicial (fuera del Dispatcher)
            await AgruparPorSemanaAsync();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error al cargar cronogramas");
            StatusMessage = "Error al cargar cronogramas.";
        }
        finally
        {
            // Limpiar placeholders al finalizar la carga
            System.Windows.Application.Current.Dispatcher.Invoke(() => PlaceholderSemanas.Clear());
            IsLoading = false;
        }    }

    [RelayCommand(CanExecute = nameof(CanAddCronograma))]
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
    }    [RelayCommand(CanExecute = nameof(CanEditCronograma))]
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
    }    [RelayCommand(CanExecute = nameof(CanDeleteCronograma))]
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

    // Utilidad para obtener el primer día de la semana ISO8601
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
        return result.AddDays(-3);    }    // Agrupa los cronogramas por semana del año (ISO 8601)
    public async Task AgruparPorSemanaAsync()
    {
        // Evitar reentradas simultáneas
        if (System.Threading.Interlocked.CompareExchange(ref _isGroupingFlag, 1, 0) == 1)
        {
            _logger.LogDebug("[CronogramaViewModel] Agrupación ya en progreso, evitando reentrada");
            return;
        }

        try
        {
            // Verificar que tengamos datos para procesar
            if (CronogramasFiltrados == null || !CronogramasFiltrados.Any())
            {
                _logger.LogDebug("[CronogramaViewModel] ⚠️ No hay cronogramas filtrados para agrupar");
                return;
            }

            _logger.LogDebug("[CronogramaViewModel] 🔄 Iniciando agrupación por semana para {Count} cronogramas", CronogramasFiltrados.Count);
            
            // Limpiar en el hilo de UI
            System.Windows.Application.Current.Dispatcher.Invoke(() => Semanas.Clear());
            
            var añoActual = AnioSeleccionado;
            var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(añoActual);
            var seguimientos = (await _seguimientoService.GetSeguimientosAsync())
                .Where(s => s.Anio == añoActual)
                .ToList();        var tareas = new List<Task>();
            var semaphore = new System.Threading.SemaphoreSlim(3); // 🔴 Reducido a 3 para evitar agotamiento del pool
            var semanasTemp = new List<SemanaViewModel>(); // Lista temporal para construir fuera del UI thread
            
            for (int i = 1; i <= weeksInYear; i++)
            {
                var fechaInicio = FirstDateOfWeekISO8601(añoActual, i);
                var fechaFin = fechaInicio.AddDays(6);
                var semanaVM = new SemanaViewModel(i, fechaInicio, fechaFin, _cronogramaService, AnioSeleccionado);
                if (CronogramasFiltrados != null)
                {
                    foreach (var c in CronogramasFiltrados)
                    {
                        if (c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        {
                            semanaVM.Mantenimientos.Add(c);
                        }
                    }
                    var codigosProgramados = CronogramasFiltrados
                        .Where(c => c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        .Select(c => c.Codigo)
                        .ToHashSet();
                    var seguimientosSemana = seguimientos.Where(s => s.Semana == i && !codigosProgramados.Contains(s.Codigo)).ToList();
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
                        };
                        semanaVM.Mantenimientos.Add(noProgramado);
                    }
                }
                // Inicializar estados de mantenimientos para la semana (carga asíncrona, con manejo de errores y semáforo)
                tareas.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await semanaVM.CargarEstadosMantenimientosAsync(añoActual, _cronogramaService);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Timeout"))
                    {
                        _logger.LogWarning(ex, $"⚠️ Timeout al cargar estados de la semana {i} - Pool agotado");
                        // No fallar todo, solo registrar el aviso
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError(ex, $"❌ Error al cargar estados de la semana {i}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
                semanasTemp.Add(semanaVM);
            }
            await Task.WhenAll(tareas);
              // Agregar todas las semanas a la colección observable en el hilo de UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var semana in semanasTemp)            {
                    Semanas.Add(semana);
                }
                
                _logger.LogDebug("[CronogramaViewModel] Agrupación completada: {Count} semanas agregadas", semanasTemp.Count);
            });
        }
        finally
        {
            System.Threading.Interlocked.Exchange(ref _isGroupingFlag, 0);
        }
    }    [RelayCommand]
    public async Task AgruparSemanalmente()
    {
        // Solo ejecutar si ya está inicializado para evitar cargas múltiples
        lock (_initializationLock)
        {
            if (!_isInitialized) return;
        }
        
        await AgruparPorSemanaAsync();
    }    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Evitar múltiples refrescos simultáneos
        if (IsLoading) return;

        // 🔵 Ejecutar en background thread para NO bloquear la UI
        await Task.Run(async () =>
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = true;
                    _isRefreshing = true;  // 🔴 Indicar que estamos refrescando
                    StatusMessage = "🔄 Refrescando cronograma...";

                    // Inicializar placeholders mientras se realiza el refresco
                    try
                    {
                        var weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(AnioSeleccionado);
                        PlaceholderSemanas.Clear();
                        foreach (var i in Enumerable.Range(1, weeksInYear)) PlaceholderSemanas.Add(i);
                    }
                    catch { /* swallo contenido si falla */ }
                });

                _logger.LogInformation("[CronogramaViewModel] 🔄 Iniciando refresco completo del cronograma desde 0");

                // PASO 1: Limpiar toda la UI
                _logger.LogInformation("[CronogramaViewModel] ✓ PASO 1: Limpiando datos previos...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 1/5: Limpiando datos previos...";
                    Cronogramas.Clear();
                    CronogramasFiltrados.Clear();
                    Semanas.Clear();
                    AniosDisponibles.Clear();
                });

                // PASO 2: Actualizar cronogramas (asegurar que estén completos)
                _logger.LogInformation("[CronogramaViewModel] ✓ PASO 2: Actualizando cronogramas en BD...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 2/5: Actualizando cronogramas en BD...";
                });
                await _cronogramaService.EnsureAllCronogramasUpToDateAsync().ConfigureAwait(false);

                // PASO 3: Generar seguimientos faltantes
                _logger.LogInformation("[CronogramaViewModel] ✓ PASO 3: Generando seguimientos faltantes...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 3/5: Generando seguimientos faltantes...";
                });
                await _cronogramaService.GenerarSeguimientosFaltantesAsync().ConfigureAwait(false);

                // PASO 4: Cargar cronogramas desde BD
                _logger.LogInformation("[CronogramaViewModel] ✓ PASO 4: Cargando cronogramas desde BD...");
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
                _logger.LogInformation("[CronogramaViewModel] ✓ PASO 5: Agrupando por semanas...");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "Paso 5/5: Agrupando por semanas...";
                });
                await AgruparPorSemanaAsync().ConfigureAwait(false);

                _logger.LogInformation("[CronogramaViewModel] ✅ Refresco completado exitosamente");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"✅ Cronograma refrescado: {Cronogramas.Count} cronogramas y {Semanas.Count} semanas.";
                });
            }
            catch (OperationCanceledException)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = "⚠️ Refresco cancelado.";
                });
                _logger.LogInformation("[CronogramaViewModel] Refresco cancelado por el usuario");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaViewModel] Error durante el refresco completo");
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"❌ Error al refrescar: {ex.Message}";
                });
            }
            finally
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLoading = false;
                    _isRefreshing = false;  // 🟢 Finalizar refresco, permitir agrupación normal
                    PlaceholderSemanas.Clear();
                });
            }
        }, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExportCronograma))]
    public async Task ExportarCronogramasAsync()
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
            StatusMessage = "Exportando cronograma...";

            // Obtener todos los equipos y semanas del año seleccionado
            var cronogramas = CronogramasFiltrados.ToList();
            var weeksInYearExport = System.Globalization.ISOWeek.GetWeeksInYear(AnioSeleccionado);
            var semanas = Enumerable.Range(1, weeksInYearExport).ToList();
            // Diccionario: [equipo][semana] = estado
            var estadosPorEquipo = new Dictionary<string, Dictionary<int, MantenimientoSemanaEstadoDto>>();
            // Obtener estados por semana primero (una llamada por semana)
            var estadosPorSemana = new Dictionary<int, List<MantenimientoSemanaEstadoDto>>();
            for (int s = 1; s <= weeksInYearExport; s++)
            {
                try
                {
                    var listaEstados = await _cronogramaService.GetEstadoMantenimientosSemanaAsync(s, AnioSeleccionado);
                    estadosPorSemana[s] = listaEstados.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error obteniendo estados para semana {Semana}", s);
                    estadosPorSemana[s] = new List<MantenimientoSemanaEstadoDto>();
                }
            }
             foreach (var c in cronogramas)
             {
                 estadosPorEquipo[c.Codigo!] = new Dictionary<int, MantenimientoSemanaEstadoDto>();
                for (int s = 1; s <= weeksInYearExport; s++)
                {
                    var estados = estadosPorSemana.ContainsKey(s) ? estadosPorSemana[s] : new List<MantenimientoSemanaEstadoDto>();
                    var estado = estados.FirstOrDefault(e => e.CodigoEquipo == c.Codigo);
                    if (estado != null)
                        estadosPorEquipo[c.Codigo!][s] = estado;
                }
             }

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add($"Cronograma {AnioSeleccionado}");
            // Encabezados
            ws.Cell(1, 1).Value = "Equipo";
            ws.Cell(1, 2).Value = "Nombre";
            ws.Cell(1, 3).Value = "Marca";
            ws.Cell(1, 4).Value = "Sede";
            for (int s = 1; s <= weeksInYearExport; s++)
            {
                ws.Cell(1, 4 + s).Value = $"S{s}";
                ws.Cell(1, 4 + s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            ws.Row(1).Style.Font.Bold = true;
            int row = 2;
            foreach (var c in cronogramas)
            {
                ws.Cell(row, 1).Value = c.Codigo;
                ws.Cell(row, 2).Value = c.Nombre;
                ws.Cell(row, 3).Value = c.Marca;
                ws.Cell(row, 4).Value = c.Sede;
                for (int s = 1; s <= weeksInYearExport; s++)
                {
                    if (estadosPorEquipo.TryGetValue(c.Codigo!, out var estadosSemana) && estadosSemana.TryGetValue(s, out var estado))
                    {
                        ws.Cell(row, 4 + s).Value = EstadoToTexto(estado.Estado);
                        ws.Cell(row, 4 + s).Style.Fill.BackgroundColor = XLColorFromEstado(estado.Estado);
                        ws.Cell(row, 4 + s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 4 + s).Style.Font.FontColor = XLColor.White;
                    }
                    else
                    {
                        ws.Cell(row, 4 + s).Value = "-";
                        ws.Cell(row, 4 + s).Style.Fill.BackgroundColor = XLColor.White;
                        ws.Cell(row, 4 + s).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(row, 4 + s).Style.Font.FontColor = XLColor.Black;
                    }
                }
                row++;
            }
            ws.Columns().AdjustToContents();
            workbook.SaveAs(saveFileDialog.FileName);
            StatusMessage = $"Exportación completada: {saveFileDialog.FileName}";
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
    }

    private static string EstadoToTexto(GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento estado)
    {
        return estado switch
        {
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado => "No realizado",
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Atrasado => "Atrasado",
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo => "Realizado",
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => "Realizado fuera de tiempo",
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Pendiente => "Pendiente",
                    _ => "-"
        };
    }

    private static XLColor XLColorFromEstado(GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento estado)
    {
        return estado switch
        {
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.NoRealizado => XLColor.FromHtml("#C80000"), // Rojo
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Atrasado => XLColor.FromHtml("#FFB300"), // Ámbar
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo => XLColor.FromHtml("#388E3C"), // Verde
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => XLColor.FromHtml("#FFB300"), // Ámbar
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Pendiente => XLColor.FromHtml("#BDBDBD"), // Gris
            _ => XLColor.White        };
    }    /// <summary>
    /// Implementación del método abstracto para auto-refresh automático
    /// </summary>
    protected override async Task RefreshDataAsync()
    {
        try
        {
            _logger.LogInformation("[CronogramaViewModel] Refrescando datos automáticamente");
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
    /// Override para manejar cuando se pierde la conexión específicamente para cronogramas
    /// </summary>
    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexión - Gestión de cronogramas no disponible";
    }
}
}
