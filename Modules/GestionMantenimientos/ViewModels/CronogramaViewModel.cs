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
using ClosedXML.Excel;
using Microsoft.Win32;
using GestLog.Modules.GestionMantenimientos.Models.Enums;

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{    /// <summary>
    /// ViewModel para la gestin del cronograma de mantenimientos.
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
        IGestLogLogger logger)
        : base(databaseService, logger)
    {
        try
        {
            _cronogramaService = cronogramaService;
            _seguimientoService = seguimientoService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
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

            // Obtener todos los equipos y semanas del ao seleccionado
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
             }            // Obtener seguimientos del ao seleccionado (solo realizados o no realizados, excluyendo pendientes y atrasados)
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
            // IMPORTANTE: NO asignar anchos fijos a A-E, se ajustarn automticamente con AdjustToContents()
            // al final despu©s de llenar los datos
            
            // Configurar ancho para columnas de semanas (ajustado a lo que cabe). Semanas empezarn en la columna F (ndice 6).
            try
            {
                // Ajustar en bloque todas las columnas de semanas para que tengan el mismo ancho
                ws.Columns(6, 5 + weeksInYearExport).Width = 8;
            }
            catch
            {
                // Fallback: iterar si la asignacin en bloque falla por alguna razn
                for (int s = 1; s <= weeksInYearExport; s++)
                    ws.Column(5 + s).Width = 8;
            }
            
            // Ocultar lneas de cuadrcula para apariencia ms limpia
            ws.ShowGridLines = false;
            
            // ===== FILAS 1-2: LOGO (izquierda) + TTULO (derecha) =====
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
                // Si hay error al cargar el logo, continuar sin ©l
            }
            
            // Agregar ttulo en C1:K2+ (ajustado segn nmero de semanas)
            int lastWeekCol = 4 + weeksInYearExport; // Semanas empiezan en columna E (4 + 1 = 5)
            // No extender el ttulo sobre todas las columnas de semanas si hay muchas.
            // Limitar la mezcla hasta la columna 11 (K) como mximo para mantener el ttulo visible.
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

            // Dibujar una lnea horizontal (border) justo debajo del ttulo para separar visualmente
            try
            {
                ws.Range(2, 1, 2, titleEndCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            catch { }

            // Agregar borde derecho al ttulo
            titleRangeCron.Style.Border.RightBorder = XLBorderStyleValues.Thin;            // ===== CUADROS DE INFORMACIN ABAJO DEL TTULO =====
            // Cajas sin merge horizontal: A1=REALIZADO POR (merge vertical), B1=NOMBRE (merge vertical), C1=AO (merge vertical)
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

            // Caja 3: AO (columna C, merge vertical)
            ws.Range(infoStartRow, 3, infoEndRow, 3).Merge();
            var box3 = ws.Cell(infoStartRow, 3);
            box3.Value = $"AO: {AnioSeleccionado}";
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
            // currentRowCron comienza justo despu©s del bloque informativo (infoEndRow)
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

            // Encabezados de semanas (a partir de la columna F, ndice 6 => 5 + s)
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
                        // Si es correctivo: fondo prpura y una 'C' en mayscula y negrita centrada
                        if (estado.Seguimiento?.TipoMtno == TipoMantenimiento.Correctivo)
                        {
                            cell.Value = "C";
                            cell.Style.Font.Bold = true;
                            cell.Style.Font.FontColor = XLColor.White;
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#7E57C2"); // Prpura recomendado
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
            }            // Agregar filtros automticos a la tabla
            if (cronogramas.Count > 0)
            {
                int headerRowCron = currentRowCron - cronogramas.Count - 1;
                ws.Range(headerRowCron, 1, currentRowCron - 1, 5).SetAutoFilter();
            }            // ===== AJUSTAR ANCHO DE COLUMNAS A-E AL CONTENIDO =====
            // Primero calcular ancho automtico
            ws.Column(1).AdjustToContents();
            ws.Column(2).AdjustToContents();
            ws.Column(3).AdjustToContents();
            ws.Column(4).AdjustToContents();
            ws.Column(5).AdjustToContents();

            // CORRECCIN CRTICA: Asegurar que los anchos sean suficientes para el contenido visible
            // "REALIZADO POR:" tiene aproximadamente 14 caracteres (ancho mnimo 17-18 unidades)
            // "NOMBRE" o nombres largos tpicamente necesitan 15-20
            // "MARCA", "FRECUENCIA", "SEDE" necesitan 12-15 cada uno
            
            double colAWidth = Math.Max(ws.Column(1).Width, 18);     // Mnimo 18 para "REALIZADO POR:"
            double colBWidth = Math.Max(ws.Column(2).Width, 16);     // Mnimo 16 para nombres
            double colCWidth = Math.Max(ws.Column(3).Width, 14);     // Mnimo 14 para marcas
            double colDWidth = Math.Max(ws.Column(4).Width, 14);     // Mnimo 14 para frecuencia
            double colEWidth = Math.Max(ws.Column(5).Width, 14);     // Mnimo 14 para sede

            ws.Column(1).Width = colAWidth + 2;     // Agregar margen de seguridad
            ws.Column(2).Width = colBWidth + 2;
            ws.Column(3).Width = colCWidth + 1.5;
            ws.Column(4).Width = colDWidth + 1.5;
            ws.Column(5).Width = colEWidth + 1.5;

            // ===== PIE DE PGINA =====
            currentRowCron += 2;
            var footerCellCron = ws.Cell(currentRowCron, 1);
            footerCellCron.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}  Ao {AnioSeleccionado}  Sistema GestLog © SIMICS Group SAS";
            footerCellCron.Style.Font.Italic = true;
            footerCellCron.Style.Font.FontSize = 9;
            footerCellCron.Style.Font.FontColor = XLColor.Gray;
            ws.Range(currentRowCron, 1, currentRowCron, 5 + weeksInYearExport).Merge();            // Configurar pgina para exportacin
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
            wsSeguimientos.Column("E").Width = 35;  // Ms ancho para descripcin
            wsSeguimientos.Column("F").Width = 20;
            wsSeguimientos.Column("G").Width = 15;
            wsSeguimientos.Column("H").Width = 18;
            wsSeguimientos.Column("I").Width = 18;
            wsSeguimientos.Column("J").Width = 15;
            wsSeguimientos.Column("K").Width = 35;  // Ms ancho para observaciones
              // Ocultar lneas de cuadrcula para apariencia ms limpia
            wsSeguimientos.ShowGridLines = false;            // ===== FILAS 1-2: LOGO (izquierda) + TTULO (derecha) =====
            // PRIMERO: Aumentar altura de filas 1-2 para el logo y ttulo
            wsSeguimientos.Row(1).Height = 35;
            wsSeguimientos.Row(2).Height = 35;
            
            // SEGUNDO: Combinar celdas A1:B2 para el logo (solo lado izquierdo)
            wsSeguimientos.Range(1, 1, 2, 2).Merge();            // TERCERO: Agregar y posicionar la imagen
            var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
            try
            {                if (System.IO.File.Exists(logoPath))
                {
                    var picture = wsSeguimientos.AddPicture(logoPath);
                    // Posicionar en A1 con offset de 10 pxeles desde los bordes izquierdo y superior
                    picture.MoveTo(wsSeguimientos.Cell(1, 1), 10, 10);
                    // Escalar para que se ajuste a las 2 filas (70px cada una = 140px total)
                    picture.Scale(0.15); // Escalar al 10% del tamao original
                }
            }
            catch
            {
                // Si hay error al cargar el logo, continuar sin ©l
            }
            
            // CUARTO: Agregar ttulo en C1:K2 (lado derecho, centrado vertical y horizontal)
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
            var headersSeg = new[] { "Equipo", "Nombre", "Semana", "Tipo", "Descripcin", "Responsable", "Estado", "Fecha Registro", "Fecha Realizacin", "Costo", "Observaciones" };
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
                
                // Descripcin con word wrap
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
                obsCell.Style.Alignment.Indent = 2; // Agregar indentacin para separacin visual
                
                // Filas alternas con color gris claro (excluyendo columna de estado)
                if (rowCountSeg % 2 == 0)
                {
                    for (int col = 1; col <= 11; col++)
                    {
                        if (col != 7) // No colorear la columna de estado
                            wsSeguimientos.Cell(currentRowSeg, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                    }
                }
                
                // Centrar columnas num©ricas
                wsSeguimientos.Cell(currentRowSeg, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Cell(currentRowSeg, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Cell(currentRowSeg, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                // Ajustar altura de fila automticamente para textos largos
                wsSeguimientos.Row(currentRowSeg).Height = 30; // Altura mnima para acomodar textos
                
                currentRowSeg++;
                rowCountSeg++;
            }
            
            // Agregar filtros automticos a la tabla
            if (seguimientosAnio.Count > 0)
            {
                int headerRow = currentRowSeg - seguimientosAnio.Count - 1;
                wsSeguimientos.Range(headerRow, 1, currentRowSeg - 1, 11).SetAutoFilter();
            }
              // ===== PANEL DE KPIs (INDICADORES CLAVE) =====
            if (seguimientosAnio.Count > 0)
            {
                currentRowSeg += 2;
                
                // Calcular m©tricas clave
                var preventivos = seguimientosAnio.Count(s => s.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Preventivo);
                var correctivos = seguimientosAnio.Count(s => s.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Correctivo);
                var realizadosEnTiempo = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
                var realizadosFueraTiempo = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
                var noRealizados = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
                var pendientes = seguimientosAnio.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
                var totalCosto = seguimientosAnio.Sum(s => s.Costo ?? 0);
                var costoPreventivoTotal = seguimientosAnio.Where(s => s.TipoMtno == GestLog.Modules.GestionMantenimientos.Models.Enums.TipoMantenimiento.Preventivo).Sum(s => s.Costo ?? 0);
                var costoCorrectivo = totalCosto - costoPreventivoTotal;
                
                // Calcular porcentajes
                var totalMtto = seguimientosAnio.Count;
                var pctCumplimiento = totalMtto > 0 ? (realizadosEnTiempo + realizadosFueraTiempo) / (decimal)totalMtto * 100 : 0;
                var pctCorrectivos = totalMtto > 0 ? correctivos / (decimal)totalMtto * 100 : 0;
                var pctPreventivos = totalMtto > 0 ? preventivos / (decimal)totalMtto * 100 : 0;
                
                // ===== TTULO KPIs =====
                var kpiTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                kpiTitle.Value = "INDICADORES DE DESEMPEO - AO " + AnioSeleccionado;
                kpiTitle.Style.Font.Bold = true;
                kpiTitle.Style.Font.FontSize = 14;
                kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                kpiTitle.Style.Font.FontColor = XLColor.White;
                kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                wsSeguimientos.Row(currentRowSeg).Height = 22;
                currentRowSeg++;                  // ===== KPI ROW =====
                var kpiLabels = new[] { "Cumplimiento", "Total Mtos", "Correctivos", "Preventivos" };
                var kpiValues = new object[] 
                { 
                    $"{pctCumplimiento:F1}%", 
                    totalMtto, 
                    $"{correctivos} ({pctCorrectivos:F1}%)",
                    $"{preventivos} ({pctPreventivos:F1}%)"
                };
                  for (int col = 0; col < kpiLabels.Length; col++)
                {
                    // Etiqueta
                    var labelCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                    labelCell.Value = kpiLabels[col];
                    labelCell.Style.Font.Bold = true;
                    labelCell.Style.Font.FontSize = 10;
                    labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                    labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    
                    // Valor
                    var valueCell = wsSeguimientos.Cell(currentRowSeg + 1, col + 1);
                    
                    // Convertir el valor apropiadamen segn su tipo
                    if (kpiValues[col] is string strVal)
                        valueCell.Value = strVal;
                    else if (kpiValues[col] is int intVal)
                        valueCell.Value = intVal;
                    else if (kpiValues[col] is decimal decVal)
                    {
                        valueCell.Value = decVal;
                        valueCell.Style.NumberFormat.Format = "$#,##0";
                    }
                    else
                        valueCell.Value = kpiValues[col]?.ToString() ?? "-";
                    
                    valueCell.Style.Font.Bold = true;
                    valueCell.Style.Font.FontSize = 12;
                    valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                    valueCell.Style.Font.FontColor = XLColor.White;
                    valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                }
                currentRowSeg += 2;
                
                // ===== RESUMEN POR TIPO DE MANTENIMIENTO =====
                currentRowSeg += 1;
                var tipoTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                tipoTitle.Value = "RESUMEN POR TIPO DE MANTENIMIENTO";
                tipoTitle.Style.Font.Bold = true;
                tipoTitle.Style.Font.FontSize = 12;
                tipoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                tipoTitle.Style.Font.FontColor = XLColor.White;
                tipoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;                wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 5).Merge();
                wsSeguimientos.Row(currentRowSeg).Height = 20;
                currentRowSeg++;
                
                // Headers
                var tipoHeaders = new[] { "Tipo", "Cantidad", "%", "Costo Total", "% Costo" };
                for (int col = 0; col < tipoHeaders.Length; col++)
                {
                    var headerCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                    headerCell.Value = tipoHeaders[col];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                currentRowSeg++;
                
                // Data rows
                var tipoData = new (string tipo, int cantidad, decimal costo)[]
                {
                    ("Preventivo", preventivos, costoPreventivoTotal),
                    ("Correctivo", correctivos, costoCorrectivo),
                    ("TOTAL", totalMtto, totalCosto)
                };
                
                foreach (var data in tipoData)
                {
                    int col = 1;
                    var tipoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    tipoCell.Value = data.tipo;
                    tipoCell.Style.Font.Bold = data.tipo == "TOTAL";
                    tipoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                    
                    var cantCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    cantCell.Value = data.cantidad;
                    cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cantCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                      var pctCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    if (data.tipo != "TOTAL" && totalMtto > 0)
                        pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                    pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                    pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pctCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                      var costoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    costoCell.Value = data.costo;
                    costoCell.Style.NumberFormat.Format = "$#,##0";
                    costoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    costoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                    
                    var pctCostoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    if (data.tipo != "TOTAL" && totalCosto > 0)
                        pctCostoCell.Value = (data.costo / totalCosto * 100);
                    pctCostoCell.Style.NumberFormat.Format = "0.0\"%\"";
                    pctCostoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    pctCostoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;
                    
                    currentRowSeg++;
                }
                
                // ===== ANLISIS DE ESTADOS =====
                currentRowSeg += 1;
                var estadoTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                estadoTitle.Value = "ANLISIS DE CUMPLIMIENTO POR ESTADO";
                estadoTitle.Style.Font.Bold = true;
                estadoTitle.Style.Font.FontSize = 12;
                estadoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                estadoTitle.Style.Font.FontColor = XLColor.White;
                estadoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 6).Merge();
                wsSeguimientos.Row(currentRowSeg).Height = 20;
                currentRowSeg++;                // Headers
                var estadoHeaders = new[] { "Estado", "Cantidad", "%", "Color" };
                for (int col = 0; col < estadoHeaders.Length; col++)
                {
                    var headerCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                    headerCell.Value = estadoHeaders[col];
                    headerCell.Style.Font.Bold = true;
                    headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                    headerCell.Style.Font.FontColor = XLColor.White;
                    headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                currentRowSeg++;
                
                // Data rows
                var estadoData = new (string estado, int cantidad, decimal costo, string colorHex)[]
                {
                    ("Realizado en Tiempo", realizadosEnTiempo, seguimientosAnio.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo).Sum(s => s.Costo ?? 0), "388E3C"),
                    ("Realizado Fuera de Tiempo", realizadosFueraTiempo, seguimientosAnio.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado).Sum(s => s.Costo ?? 0), "FFB300"),
                    ("No Realizado", noRealizados, seguimientosAnio.Where(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado).Sum(s => s.Costo ?? 0), "C80000"),
                    ("Pendiente", pendientes, seguimientosAnio.Where(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente).Sum(s => s.Costo ?? 0), "B3E5FC")
                };
                  foreach (var data in estadoData)
                {
                    if (data.cantidad == 0) continue; // Saltar si no hay datos
                    
                    int col = 1;
                    var estadoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    estadoCell.Value = data.estado;
                    
                    var cantCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    cantCell.Value = data.cantidad;
                    cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                      var pctCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    if (totalMtto > 0)
                        pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                    pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                    pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                      // Columna Color - rectngulo coloreado
                    var colorCell = wsSeguimientos.Cell(currentRowSeg, col++);
                    colorCell.Value = " "; // Smbolo de rectngulo
                    colorCell.Style.Font.FontSize = 14;
                    var colorValue = XLColor.FromArgb(int.Parse(data.colorHex, System.Globalization.NumberStyles.HexNumber));
                    colorCell.Style.Fill.BackgroundColor = colorValue;
                    colorCell.Style.Font.FontColor = colorValue;
                    colorCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    colorCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    
                    currentRowSeg++;
                }
            }
            
            // ===== PIE DE PGINA =====
            currentRowSeg += 2;
            var footerCellSeg = wsSeguimientos.Cell(currentRowSeg, 1);
            footerCellSeg.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}  Sistema GestLog © SIMICS Group SAS";
            footerCellSeg.Style.Font.Italic = true;
            footerCellSeg.Style.Font.FontSize = 9;
            footerCellSeg.Style.Font.FontColor = XLColor.Gray;
            wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
            
            // Agregar borde exterior grueso
            wsSeguimientos.Range(1, 1, currentRowSeg, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
            
            // Configurar pgina para exportacin a PDF
            wsSeguimientos.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            wsSeguimientos.PageSetup.AdjustTo(100);
            wsSeguimientos.PageSetup.FitToPages(1, 0);
            wsSeguimientos.PageSetup.Margins.Top = 0.5;
            wsSeguimientos.PageSetup.Margins.Bottom = 0.5;
            wsSeguimientos.PageSetup.Margins.Left = 0.5;
            wsSeguimientos.PageSetup.Margins.Right = 0.5;

            workbook.SaveAs(saveFileDialog.FileName);
            StatusMessage = $"Exportacin completada: {saveFileDialog.FileName} ({cronogramas.Count} cronogramas, {seguimientosAnio.Count} seguimientos)";
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
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Atrasado => XLColor.FromHtml("#FFB300"), // mbar
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoEnTiempo => XLColor.FromHtml("#388E3C"), // Verde
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => XLColor.FromHtml("#FFB300"), // mbar
            GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoSeguimientoMantenimiento.Pendiente => XLColor.FromHtml("#B3E5FC"), // Azul cielo pastel
            _ => XLColor.White        };
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
    /// Override para manejar cuando se pierde la conexin especficamente para cronogramas
    /// </summary>
    protected override void OnConnectionLost()
    {
        StatusMessage = "Sin conexin - Gestin de cronogramas no disponible";
    }
}
}

