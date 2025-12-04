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
                    }                    var codigosProgramados = CronogramasFiltrados
                        .Where(c => c.Semanas != null && c.Semanas.Length >= i && c.Semanas[i - 1])
                        .Select(c => c.Codigo)
                        .ToHashSet();
                    
                    // NUEVA LÓGICA: Mostrar seguimientos que NO están programados en el cronograma
                    // O que son Correctivos (incluso si hay un Preventivo programado)
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
    }    [RelayCommand(CanExecute = nameof(CanExportCronograma))]
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
            StatusMessage = "Exportando cronograma y seguimientos...";

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
             }            // Obtener seguimientos del año seleccionado (solo realizados o no realizados, excluyendo pendientes y atrasados)
            var todosSeguimientos = await _seguimientoService.GetSeguimientosAsync();
            var seguimientosAnio = todosSeguimientos
                .Where(s => s.Anio == AnioSeleccionado && 
                    (s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo ||
                     s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo ||
                     s.Estado == EstadoSeguimientoMantenimiento.NoRealizado) &&
                    cronogramas.Any(c => c.Codigo == s.Codigo))
                .ToList();            using var workbook = new XLWorkbook();
              // ========== HOJA 1: CRONOGRAMA ==========
            var ws = workbook.Worksheets.Add($"Cronograma {AnioSeleccionado}");
              // ===== CONFIGURAR ANCHO DE COLUMNAS =====
            // IMPORTANTE: NO asignar anchos fijos a A-E, se ajustarán automáticamente con AdjustToContents()
            // al final después de llenar los datos
            
            // Configurar ancho para columnas de semanas (ajustado a lo que cabe). Semanas empezarán en la columna F (índice 6).
            try
            {
                // Ajustar en bloque todas las columnas de semanas para que tengan el mismo ancho
                ws.Columns(6, 5 + weeksInYearExport).Width = 8;
            }
            catch
            {
                // Fallback: iterar si la asignación en bloque falla por alguna razón
                for (int s = 1; s <= weeksInYearExport; s++)
                    ws.Column(5 + s).Width = 8;
            }
            
            // Ocultar líneas de cuadrícula para apariencia más limpia
            ws.ShowGridLines = false;
            
            // ===== FILAS 1-2: LOGO (izquierda) + TÍTULO (derecha) =====
            ws.Row(1).Height = 35;
            ws.Row(2).Height = 35;
            
            // Combinar celdas A1:B2 para el logo
            ws.Range(1, 1, 2, 2).Merge();
            
            // Agregar logo
            var logoCronPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
            try
            {
                if (System.IO.File.Exists(logoCronPath))
                {
                    var pictureCron = ws.AddPicture(logoCronPath);
                    pictureCron.MoveTo(ws.Cell(1, 1), 15, 15);
                    pictureCron.Scale(0.10);
                }
            }
            catch
            {
                // Si hay error al cargar el logo, continuar sin él
            }
            
            // Agregar título en C1:K2+ (ajustado según número de semanas)
            int lastWeekCol = 4 + weeksInYearExport; // Semanas empiezan en columna E (4 + 1 = 5)
            // No extender el título sobre todas las columnas de semanas si hay muchas.
            // Limitar la mezcla hasta la columna 11 (K) como máximo para mantener el título visible.
            int titleEndCol = Math.Min(11, lastWeekCol);

            var titleRangeCron = ws.Range(1, 3, 2, titleEndCol);
            titleRangeCron.Merge();
            var titleCellCron = titleRangeCron.FirstCell();
            titleCellCron.Value = "CRONOGRAMA DE MANTENIMIENTOS";
            titleCellCron.Style.Font.Bold = true;
            titleCellCron.Style.Font.FontSize = 18;
            titleCellCron.Style.Font.FontColor = XLColor.Black; // Negro
            titleCellCron.Style.Fill.BackgroundColor = XLColor.White;
            titleCellCron.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleCellCron.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Dibujar una línea horizontal (border) justo debajo del título para separar visualmente
            try
            {
                ws.Range(2, 1, 2, titleEndCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            catch { }

            // Agregar borde derecho al título
            titleRangeCron.Style.Border.RightBorder = XLBorderStyleValues.Thin;            // ===== CUADROS DE INFORMACIÓN ABAJO DEL TÍTULO =====
            // Cajas sin merge horizontal: A1=REALIZADO POR (merge vertical), B1=NOMBRE (merge vertical), C1=AÑO (merge vertical)
            int infoStartRow = 3;
            int infoEndRow = 7; // filas 3,4,5,6,7            // Caja 1: REALIZADO POR (columna A, merge vertical)
            ws.Range(infoStartRow, 1, infoEndRow, 1).Merge();
            var box1 = ws.Cell(infoStartRow, 1);
            box1.Value = "REALIZADO POR:";
            box1.Style.Font.Bold = true;
            box1.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            box1.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            // SIN BORDES - Solo la leyenda debe tener bordes

            // Caja 2: NOMBRE DEL USUARIO (columna B, merge vertical)
            ws.Range(infoStartRow, 2, infoEndRow, 2).Merge();
            var box2 = ws.Cell(infoStartRow, 2);
            box2.Value = _currentUser?.FullName ?? _currentUser?.Username ?? "-";
            box2.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            box2.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            // SIN BORDES - Solo la leyenda debe tener bordes

            // Caja 3: AÑO (columna C, merge vertical)
            ws.Range(infoStartRow, 3, infoEndRow, 3).Merge();
            var box3 = ws.Cell(infoStartRow, 3);
            box3.Value = $"AÑO: {AnioSeleccionado}";
            box3.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            box3.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            // SIN BORDES - Solo la leyenda debe tener bordes// Asegurar alturas para las filas de info
            for (int rr = infoStartRow; rr <= infoEndRow; rr++) ws.Row(rr).Height = 18;            // ===== LEYENDA DE COLORES (columnas D-E, filas 3-7, sin merge vertical) =====
            // Leyenda en D (caja de color) y E (etiqueta), sin combinar verticalmente
            int legendColBox = 4;      // Columna D
            int legendLabelCol = 5;    // Columna E

            var legendItems = new (string label, XLColor color)[]
            {
                ("Realizado en Tiempo", XLColorFromEstado(EstadoSeguimientoMantenimiento.RealizadoEnTiempo)),
                ("Realizado fuera de tiempo", XLColorFromEstado(EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo)),
                ("No realizado", XLColorFromEstado(EstadoSeguimientoMantenimiento.NoRealizado)),
                ("Pendiente / Programado", XLColorFromEstado(EstadoSeguimientoMantenimiento.Pendiente)),
                ("Correctivo", XLColor.FromHtml("#7E57C2"))
            };

            for (int i = 0; i < legendItems.Length; i++)
            {
                int r = infoStartRow + i; // filas 3..7
                // Caja de color (sin merge)
                ws.Cell(r, legendColBox).Style.Fill.BackgroundColor = legendItems[i].color;
                ws.Cell(r, legendColBox).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(r, legendColBox).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(r, legendColBox).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Para Correctivo, agregar la 'C' en negrita blanca centrada
                if (legendItems[i].label == "Correctivo")
                {
                    ws.Cell(r, legendColBox).Value = "C";
                    ws.Cell(r, legendColBox).Style.Font.Bold = true;
                    ws.Cell(r, legendColBox).Style.Font.FontColor = XLColor.White;
                }                // Etiqueta al lado (sin merge, SIN BORDES)
                ws.Cell(r, legendLabelCol).Value = legendItems[i].label;
                ws.Cell(r, legendLabelCol).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Cell(r, legendLabelCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                ws.Cell(r, legendLabelCol).Style.Font.FontSize = 9;
                // SIN BORDES - Solo las cajas de color tienen bordes
            }            // ===== ENCABEZADOS DE TABLA =====
            // currentRowCron comienza justo después del bloque informativo (infoEndRow)
            int currentRowCron = infoEndRow + 1;
            var headersCron = new[] { "Equipo", "Nombre", "Marca", "Frecuencia", "Sede" };
            for (int col = 1; col <= headersCron.Length; col++)
            {
                var headerCell = ws.Cell(currentRowCron, col);
                headerCell.Value = headersCron[col - 1];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938); // Verde oscuro
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // Encabezados de semanas (a partir de la columna F, índice 6 => 5 + s)
            for (int s = 1; s <= weeksInYearExport; s++)
            {
                var weekCell = ws.Cell(currentRowCron, 5 + s);
                weekCell.Value = $"S{s}";
                weekCell.Style.Font.Bold = true;
                weekCell.Style.Font.FontColor = XLColor.White;
                weekCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938); // Verde oscuro
                weekCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                weekCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // NO aplicar borde grueso a los encabezados de semana (solo las celdas con mantenimiento deben mostrar el borde)
            }
            // Congelar encabezados y las primeras 5 columnas (Equipo, Nombre, Marca, Frecuencia, Sede)
            try
            {
                int headerRow = currentRowCron; // fila con los encabezados
                ws.SheetView.FreezeRows(headerRow);   // mantiene la(s) fila(s) superiores fijas al hacer scroll vertical
                ws.SheetView.FreezeColumns(5);        // mantiene fijas las primeras 5 columnas al hacer scroll horizontal
            }
            catch { }

            ws.Row(currentRowCron).Height = 22;
            currentRowCron++;            // ===== FILAS DE DATOS =====
            int rowCountCron = 0;
            foreach (var c in cronogramas)
            {
                ws.Cell(currentRowCron, 1).Value = c.Codigo;
                ws.Cell(currentRowCron, 2).Value = c.Nombre;
                ws.Cell(currentRowCron, 3).Value = c.Marca;
                ws.Cell(currentRowCron, 4).Value = c.FrecuenciaMtto?.ToString() ?? "-";
                ws.Cell(currentRowCron, 5).Value = c.Sede;

                for (int s = 1; s <= weeksInYearExport; s++)
                {
                    if (estadosPorEquipo.TryGetValue(c.Codigo!, out var estadosSemana) && estadosSemana.TryGetValue(s, out var estado))
                    {
                        var cell = ws.Cell(currentRowCron, 5 + s);
                        // Si es correctivo: fondo púrpura y una 'C' en mayúscula y negrita centrada
                        if (estado.Seguimiento?.TipoMtno == TipoMantenimiento.Correctivo)
                        {
                            cell.Value = "C";
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#7E57C2"); // Púrpura recomendado
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            // Borde exterior normal para resaltar
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }
                        else
                        {
                            // No mostrar texto en la celda coloreada: solo el color visual debe indicar el estado
                            cell.Value = string.Empty;
                            cell.Style.Fill.BackgroundColor = XLColorFromEstado(estado.Estado);
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                            cell.Style.Font.FontColor = XLColor.White;
                            // Borde exterior normal solo para la celda de estado (cuando existe mantenimiento)
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        }
                    }
                    else
                    {
                        var cell = ws.Cell(currentRowCron, 5 + s);
                        cell.Value = "-";
                        cell.Style.Fill.BackgroundColor = XLColor.White;
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Font.FontColor = XLColor.Black;
                        // NO aplicar borde grueso a celdas sin mantenimiento
                    }
                }

                // Filas alternas con color gris claro ahora para columnas 1..5
                if (rowCountCron % 2 == 0)
                {
                    for (int col = 1; col <= 5; col++)
                    {
                        ws.Cell(currentRowCron, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                    }
                }

                ws.Row(currentRowCron).Height = 22;
                currentRowCron++;
                rowCountCron++;
            }            // Agregar filtros automáticos a la tabla
            if (cronogramas.Count > 0)
            {
                int headerRowCron = currentRowCron - cronogramas.Count - 1;
                ws.Range(headerRowCron, 1, currentRowCron - 1, 5).SetAutoFilter();
            }            // ===== AJUSTAR ANCHO DE COLUMNAS A-E AL CONTENIDO =====
            // Primero calcular ancho automático
            ws.Column(1).AdjustToContents();
            ws.Column(2).AdjustToContents();
            ws.Column(3).AdjustToContents();
            ws.Column(4).AdjustToContents();
            ws.Column(5).AdjustToContents();

            // CORRECCIÓN CRÍTICA: Asegurar que los anchos sean suficientes para el contenido visible
            // "REALIZADO POR:" tiene aproximadamente 14 caracteres (ancho mínimo 17-18 unidades)
            // "NOMBRE" o nombres largos típicamente necesitan 15-20
            // "MARCA", "FRECUENCIA", "SEDE" necesitan 12-15 cada uno
            
            double colAWidth = Math.Max(ws.Column(1).Width, 18);     // Mínimo 18 para "REALIZADO POR:"
            double colBWidth = Math.Max(ws.Column(2).Width, 16);     // Mínimo 16 para nombres
            double colCWidth = Math.Max(ws.Column(3).Width, 14);     // Mínimo 14 para marcas
            double colDWidth = Math.Max(ws.Column(4).Width, 14);     // Mínimo 14 para frecuencia
            double colEWidth = Math.Max(ws.Column(5).Width, 14);     // Mínimo 14 para sede

            ws.Column(1).Width = colAWidth + 2;     // Agregar margen de seguridad
            ws.Column(2).Width = colBWidth + 2;
            ws.Column(3).Width = colCWidth + 1.5;
            ws.Column(4).Width = colDWidth + 1.5;
            ws.Column(5).Width = colEWidth + 1.5;

            // ===== PIE DE PÁGINA =====
            currentRowCron += 2;
            var footerCellCron = ws.Cell(currentRowCron, 1);
            footerCellCron.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} • Año {AnioSeleccionado} • Sistema GestLog © SIMICS Group SAS";
            footerCellCron.Style.Font.Italic = true;
            footerCellCron.Style.Font.FontSize = 9;
            footerCellCron.Style.Font.FontColor = XLColor.Gray;
            ws.Range(currentRowCron, 1, currentRowCron, 5 + weeksInYearExport).Merge();            // Configurar página para exportación
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            // NO usar AdjustTo() o FitToPages() porque invalidan los anchos de columna ajustados manualmente
            // Usar scaling directo al 90% para que quepa todo sin comprimir las columnas
            ws.PageSetup.Scale = 90;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;
            ws.PageSetup.Margins.Left = 0.5;
            ws.PageSetup.Margins.Right = 0.5;// ========== HOJA 2: SEGUIMIENTOS ==========
            var wsSeguimientos = workbook.Worksheets.Add($"Seguimientos {AnioSeleccionado}");
              // ===== CONFIGURAR ANCHO DE COLUMNAS =====
            wsSeguimientos.Column("A").Width = 12;
            wsSeguimientos.Column("B").Width = 16;
            // Configurar resto de columnas
            wsSeguimientos.Column("C").Width = 12;
            wsSeguimientos.Column("D").Width = 15;
            wsSeguimientos.Column("E").Width = 35;  // Más ancho para descripción
            wsSeguimientos.Column("F").Width = 20;
            wsSeguimientos.Column("G").Width = 15;
            wsSeguimientos.Column("H").Width = 18;
            wsSeguimientos.Column("I").Width = 18;
            wsSeguimientos.Column("J").Width = 15;
            wsSeguimientos.Column("K").Width = 35;  // Más ancho para observaciones
              // Ocultar líneas de cuadrícula para apariencia más limpia
            wsSeguimientos.ShowGridLines = false;            // ===== FILAS 1-2: LOGO (izquierda) + TÍTULO (derecha) =====
            // PRIMERO: Aumentar altura de filas 1-2 para el logo y título
            wsSeguimientos.Row(1).Height = 35;
            wsSeguimientos.Row(2).Height = 35;
            
            // SEGUNDO: Combinar celdas A1:B2 para el logo (solo lado izquierdo)
            wsSeguimientos.Range(1, 1, 2, 2).Merge();            // TERCERO: Agregar y posicionar la imagen
            var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
            try
            {                if (System.IO.File.Exists(logoPath))
                {
                    var picture = wsSeguimientos.AddPicture(logoPath);
                    // Posicionar en A1 con offset de 10 píxeles desde los bordes izquierdo y superior
                    picture.MoveTo(wsSeguimientos.Cell(1, 1), 10, 10);
                    // Escalar para que se ajuste a las 2 filas (70px cada una = 140px total)
                    picture.Scale(0.15); // Escalar al 10% del tamaño original
                }
            }
            catch
            {
                // Si hay error al cargar el logo, continuar sin él
            }
            
            // CUARTO: Agregar título en C1:K2 (lado derecho, centrado vertical y horizontal)
            var titleRange = wsSeguimientos.Range(1, 3, 2, 11);
            titleRange.Merge();            var titleCellSeg = titleRange.FirstCell();
            titleCellSeg.Value = "SEGUIMIENTOS DE MANTENIMIENTOS";
            titleCellSeg.Style.Font.Bold = true;
            titleCellSeg.Style.Font.FontSize = 18;
            titleCellSeg.Style.Font.FontColor = XLColor.Black; // Negro
            titleCellSeg.Style.Fill.BackgroundColor = XLColor.White;
            titleCellSeg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCellSeg.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            
            int currentRowSeg = 3; // Comenzar desde fila 3 (encabezados)
            
            // ===== ENCABEZADOS DE TABLA =====
            var headersSeg = new[] { "Equipo", "Nombre", "Semana", "Tipo", "Descripción", "Responsable", "Estado", "Fecha Registro", "Fecha Realización", "Costo", "Observaciones" };
            for (int col = 1; col <= headersSeg.Length; col++)
            {
                var headerCell = wsSeguimientos.Cell(currentRowSeg, col);
                headerCell.Value = headersSeg[col - 1];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938); // Verde oscuro
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            wsSeguimientos.Row(currentRowSeg).Height = 22;
            currentRowSeg++;
              // ===== FILAS DE DATOS =====
            int rowCountSeg = 0;
            foreach (var seg in seguimientosAnio.OrderBy(s => s.Semana).ThenBy(s => s.Codigo))
            {
                wsSeguimientos.Cell(currentRowSeg, 1).Value = seg.Codigo;
                wsSeguimientos.Cell(currentRowSeg, 2).Value = seg.Nombre;
                wsSeguimientos.Cell(currentRowSeg, 3).Value = seg.Semana;
                wsSeguimientos.Cell(currentRowSeg, 4).Value = seg.TipoMtno?.ToString() ?? "-";
                
                // Descripción con word wrap
                var descCell = wsSeguimientos.Cell(currentRowSeg, 5);
                descCell.Value = seg.Descripcion;
                descCell.Style.Alignment.WrapText = true;
                
                wsSeguimientos.Cell(currentRowSeg, 6).Value = seg.Responsable;
                
                // Estado con color de fondo
                var estadoCell = wsSeguimientos.Cell(currentRowSeg, 7);
                estadoCell.Value = EstadoToTexto(seg.Estado);
                estadoCell.Style.Fill.BackgroundColor = XLColorFromEstado(seg.Estado);
                estadoCell.Style.Font.FontColor = XLColor.White;
                estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                wsSeguimientos.Cell(currentRowSeg, 8).Value = seg.FechaRegistro?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                wsSeguimientos.Cell(currentRowSeg, 9).Value = seg.FechaRealizacion?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                
                // Costo formateado
                var costoCell = wsSeguimientos.Cell(currentRowSeg, 10);
                costoCell.Value = seg.Costo ?? 0;
                costoCell.Style.NumberFormat.Format = "$#,##0";
                  // Observaciones con word wrap y padding izquierdo
                var obsCell = wsSeguimientos.Cell(currentRowSeg, 11);
                obsCell.Value = seg.Observaciones ?? "-";
                obsCell.Style.Alignment.WrapText = true;
                obsCell.Style.Alignment.Indent = 2; // Agregar indentación para separación visual
                
                // Filas alternas con color gris claro (excluyendo columna de estado)
                if (rowCountSeg % 2 == 0)
                {
                    for (int col = 1; col <= 11; col++)
                    {
                        if (col != 7) // No colorear la columna de estado
                            wsSeguimientos.Cell(currentRowSeg, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                    }
                }
                
                // Centrar columnas numéricas
                wsSeguimientos.Cell(currentRowSeg, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Cell(currentRowSeg, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Cell(currentRowSeg, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Ajustar altura de fila automáticamente para textos largos
                wsSeguimientos.Row(currentRowSeg).Height = 30; // Altura mínima para acomodar textos
                
                currentRowSeg++;
                rowCountSeg++;
            }
            
            // Agregar filtros automáticos a la tabla
            if (seguimientosAnio.Count > 0)
            {
                int headerRow = currentRowSeg - seguimientosAnio.Count - 1;
                wsSeguimientos.Range(headerRow, 1, currentRowSeg - 1, 11).SetAutoFilter();
            }
            
            // ===== RESUMEN DE ESTADÍSTICAS =====
            if (seguimientosAnio.Count > 0)
            {
                currentRowSeg += 2;
                
                var statsTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                statsTitle.Value = "RESUMEN DE ESTADÍSTICAS";
                statsTitle.Style.Font.Bold = true;
                statsTitle.Style.Font.FontSize = 12;
                statsTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F); // Verde medio
                statsTitle.Style.Font.FontColor = XLColor.White;
                statsTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                wsSeguimientos.Row(currentRowSeg).Height = 20;
                currentRowSeg += 2;
                
                // Calcular estadísticas
                var realizadosEnTiempo = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
                var realizadosFueraTiempo = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
                var noRealizados = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
                var pendientes = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
                var totalCosto = seguimientosAnio.Sum(s => s.Costo ?? 0);
                
                var stats = new (string label, int count, string color)[]
                {
                    ("Realizados en Tiempo", realizadosEnTiempo, "27AE60"),
                    ("Realizados Fuera de Tiempo", realizadosFueraTiempo, "F9B233"),
                    ("No Realizados", noRealizados, "C0392B"),
                    ("Pendientes", pendientes, "BDBDBD"),
                };
                
                // Mostrar estadísticas en formato 2x2
                for (int i = 0; i < stats.Length; i++)
                {
                    int colOffset = (i % 2) * 5; // 0 para primera columna, 5 para segunda
                    int rowOffset = i / 2; // 0 para primera fila, 1 para segunda
                    
                    var labelCell = wsSeguimientos.Cell(currentRowSeg + rowOffset, 1 + colOffset);
                    labelCell.Value = stats[i].label;
                    labelCell.Style.Font.Bold = true;
                    labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF8F9FA);
                    
                    var valueCell = wsSeguimientos.Cell(currentRowSeg + rowOffset, 2 + colOffset);
                    valueCell.Value = stats[i].count;
                    valueCell.Style.Font.Bold = true;
                    valueCell.Style.Font.FontSize = 11;
                    valueCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + stats[i].color);
                    valueCell.Style.Font.FontColor = XLColor.White;
                    valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                
                // Mostrar total costo a la derecha del resumen
                var totalCostoLabel = wsSeguimientos.Cell(currentRowSeg, 9);
                totalCostoLabel.Value = "Total Costo";
                totalCostoLabel.Style.Font.Bold = true;
                totalCostoLabel.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF8F9FA);
                
                var totalCostoValue = wsSeguimientos.Cell(currentRowSeg, 10);
                totalCostoValue.Value = totalCosto;
                totalCostoValue.Style.NumberFormat.Format = "$#,##0";
                totalCostoValue.Style.Font.Bold = true;
                totalCostoValue.Style.Fill.BackgroundColor = XLColor.FromArgb(0x0193B5); // Azul
                totalCostoValue.Style.Font.FontColor = XLColor.White;
                totalCostoValue.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
            
            // ===== PIE DE PÁGINA =====
            currentRowSeg += 2;
            var footerCellSeg = wsSeguimientos.Cell(currentRowSeg, 1);
            footerCellSeg.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} • Sistema GestLog © SIMICS Group SAS";
            footerCellSeg.Style.Font.Italic = true;
            footerCellSeg.Style.Font.FontSize = 9;
            footerCellSeg.Style.Font.FontColor = XLColor.Gray;
            wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
            
            // Agregar borde exterior grueso
            wsSeguimientos.Range(1, 1, currentRowSeg, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            
            // Configurar página para exportación a PDF
            wsSeguimientos.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            wsSeguimientos.PageSetup.AdjustTo(100);
            wsSeguimientos.PageSetup.FitToPages(1, 0);
            wsSeguimientos.PageSetup.Margins.Top = 0.5;
            wsSeguimientos.PageSetup.Margins.Bottom = 0.5;
            wsSeguimientos.PageSetup.Margins.Left = 0.5;
            wsSeguimientos.PageSetup.Margins.Right = 0.5;

            workbook.SaveAs(saveFileDialog.FileName);
            StatusMessage = $"Exportación completada: {saveFileDialog.FileName} ({cronogramas.Count} cronogramas, {seguimientosAnio.Count} seguimientos)";
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
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Pendiente => XLColor.FromHtml("#B3E5FC"), // Azul cielo pastel
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
