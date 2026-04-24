using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using GestLog.Modules.GestionEquiposInformaticos.Views.Equipos;
using GestLog.Modules.GestionEquiposInformaticos.Views.Cronograma;
using GestLog.Modules.GestionEquiposInformaticos.Views.Perifericos;
using GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento;
using GestLog.Services;
using GestLog.Services.Interfaces;
using GestLog.Models.Events;
using GestLog.ViewModels.Base;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
using GestLog.Modules.GestionMantenimientos.Messages.Equipos;
using CommunityToolkit.Mvvm.Messaging;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;
using Microsoft.Win32;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Perifericos
{    /// <summary>
    /// ViewModel para la gestión de periféricos informáticos
    /// Respeta SRP: solo coordina la gestión CRUD de periféricos
    /// Hereda auto-refresh automático de DatabaseAwareViewModel
    /// </summary>
    public partial class PerifericosViewModel : DatabaseAwareViewModel
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IPerifericoExportService _exportService;

        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> _perifericos = new();
        [ObservableProperty]
        private ICollectionView? _perifericosView;        [ObservableProperty]
        private bool _showDadoDeBaja = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditarPerifericoCommand))]
        [NotifyCanExecuteChangedFor(nameof(EliminarPerifericoCommand))]
        private PerifericoEquipoInformaticoDto? _perifericoSeleccionado;

        [ObservableProperty]
        private string _filtro = string.Empty;

        [ObservableProperty]
        private int _totalPerifericos;

        [ObservableProperty]
        private int _perifericosEnUso;

        [ObservableProperty]
        private int _perifericosAlmacenados;

        [ObservableProperty]
        private int _perifericosDadosBaja;

        /// <summary>
        /// Lista de todos los estados disponibles para el ComboBox
        /// </summary>
        public Array EstadosDisponibles { get; } = Enum.GetValues(typeof(EstadoPeriferico));

        /// <summary>
        /// Lista de todas las sedes disponibles para el ComboBox
        /// </summary>
        public Array SedesDisponibles { get; } = Enum.GetValues(typeof(SedePeriferico));        /// <summary>
        /// Constructor principal con inyección de dependencias
        /// </summary>
        public PerifericosViewModel(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory, IDatabaseConnectionService databaseService, IPerifericoExportService exportService)
            : base(databaseService, logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            
            // NO inicializar PerifericosView aquí - se hará después de cargar datos en InicializarAsync
            // Esto evita problemas de rendering cuando el filtro se aplica sobre una colección vacía

            // Cuando cambie la colección, recalcular estadísticas y refrescar la vista
            Perifericos.CollectionChanged += (s, e) =>
            {
                ActualizarEstadisticas();
                PerifericosView?.Refresh();
            };
              // Suscribirse a mensaje de periféricos actualizados para recargar datos cuando otro VM modifique asignaciones
            try
            {
                WeakReferenceMessenger.Default.Register<PerifericosActualizadosMessage>(this, (recipient, message) =>
                {
                    // Fire-and-forget: recarga cuando otro VM actualice periféricos
                    _ = CargarPerifericosAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PerifericosViewModel] No se pudo registrar el handler de PerifericosActualizadosMessage");
            }
            
            // 📬 Suscribirse a cambios en mantenimientos correctivos para refrescar estados
            try
            {
                WeakReferenceMessenger.Default.Register<MantenimientosCorrectivosActualizadosMessage>(this, (recipient, message) =>
                {
                    // Refrescar los datos cuando cambia el estado de reparación
                    _ = CargarPerifericosAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PerifericosViewModel] No se pudo registrar el handler de MantenimientosCorrectivosActualizadosMessage");
            }
        }
        /// <summary>
        /// Inicializa el ViewModel con detección ultrarrápida de problemas de conexión
        /// </summary>
        public async Task InicializarAsync(CancellationToken cancellationToken = default)
        {
            // Verificar si ya está inicializado o hay una operación en curso
            if (_isInitialized || IsLoading)
            {
                _logger.LogDebug("[PerifericosViewModel] Ya inicializado, omitiendo");
                return;
            }

            try
            {
                _logger.LogDebug("[PerifericosViewModel] Inicializando");
                
                // TIMEOUT ULTRARRÁPIDO de solo 1 segundo para experiencia fluida
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                // Llamar directamente sin Task.Run para evitar deadlocks
                await CargarPerifericosInternoAsync(combinedCts.Token);
                
                _isInitialized = true;
                _logger.LogDebug("[PerifericosViewModel] Inicialización completada");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("[PerifericosViewModel] Timeout - sin conexión BD");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Sin conexión - Módulo no disponible";
                    _isInitialized = true; // Marcar como inicializado para evitar reintentos
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("[PerifericosViewModel] Inicialización cancelada");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Operación cancelada";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al inicializar");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al inicializar módulo";
                    _isInitialized = true; // Marcar como inicializado para evitar reintentos
                });
            }
        }
        /// <summary>
        /// Carga todos los periféricos desde la base de datos
        /// </summary>
        [RelayCommand]
        public async Task CargarPerifericosAsync(CancellationToken cancellationToken = default)
        {
            // Verificar si ya hay una operación en curso
            if (IsLoading)
            {
                _logger.LogDebug("[PerifericosViewModel] Carga ya en curso, omitiendo");
                return;
            }

            // Usar timeout para evitar bloqueos prolongados
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);            await CargarPerifericosInternoAsync(combinedCts.Token);
        }

        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            // Reset estado de inicialización para permitir recarga
            _isInitialized = false;
            
            // Recargar datos usando el método público
            await CargarPerifericosAsync();
        }
        /// <summary>
        /// Método interno para cargar periféricos con detección ultrarrápida de problemas de conexión
        /// </summary>
        private async Task CargarPerifericosInternoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Actualizar UI inmediatamente
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = true;
                    StatusMessage = "Verificando conexión...";
                });

                _logger.LogDebug("[PerifericosViewModel] Consultando periféricos");

                // Usar DbContextFactory con timeout ultrarrápido
                using var dbContext = _dbContextFactory.CreateDbContext();
                  // Timeout balanceado: suficiente para SSL handshake
                dbContext.Database.SetCommandTimeout(15);

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Consultando periféricos...";
                });
                
                var entities = await dbContext.PerifericosEquiposInformaticos
                    .Include(p => p.EquipoAsignado)
                    .OrderBy(p => p.Codigo)
                    .ToListAsync(cancellationToken);                _logger.LogDebug("[PerifericosViewModel] Cargados {Count} periféricos", entities.Count);

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Perifericos.Clear();
                    foreach (var entity in entities)
                    {
                        var dto = ConvertirEntityADto(entity);
                        Perifericos.Add(dto);
                    }

                    // IMPORTANTE: Inicializar el CollectionView DESPUÉS de cargar datos
                    // Si se inicializa antes (en el constructor), el filtro se aplica sobre colección vacía
                    // causando problemas de rendering en el DataGrid
                    if (PerifericosView == null)
                    {
                        PerifericosView = CollectionViewSource.GetDefaultView(Perifericos);
                        if (PerifericosView != null)
                            PerifericosView.Filter = new Predicate<object>(FiltrarPerifericos);
                    }

                    // Actualizar estadísticas y refrescar la vista filtrada
                    ActualizarEstadisticas();
                    PerifericosView?.Refresh();
                    StatusMessage = $"Cargados {Perifericos.Count} periféricos";
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug("[PerifericosViewModel] Carga cancelada");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Operación cancelada";
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("[PerifericosViewModel] Timeout - sin conexión");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Sin conexión a base de datos";
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == -1 || ex.Number == 26 || ex.Number == 10060)
            {
                // Errores específicos de conexión - no generar logs verbosos
                _logger.LogDebug("[PerifericosViewModel] Sin conexión BD (Error {Number})", ex.Number);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Base de datos no disponible";
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("pool") || ex.Message.Contains("timeout"))
            {
                _logger.LogDebug("[PerifericosViewModel] Pool de conexiones saturado");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Servidor saturado - Intente más tarde";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error inesperado al cargar periféricos");
                
                // Actualizar UI de forma asíncrona en caso de error
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Error: {ex.Message}";
                });
            }
            finally
            {
                // Actualizar UI de forma asíncrona al finalizar
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    IsLoading = false;
                });
            }
        }/// <summary>
        /// Comando para agregar un nuevo periférico
        /// </summary>
        [RelayCommand]        public async Task AgregarPerifericoAsync()
        {
            try
            {
                var dialog = new Views.Perifericos.PerifericoDialog(_dbContextFactory);

                if (dialog.ShowDialog() == true)
                {
                    var nuevoPeriferico = dialog.ViewModel.PerifericoActual;
                    await GuardarPerifericoAsync(nuevoPeriferico, esNuevo: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al agregar periférico");

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>                {
                    StatusMessage = "Error al agregar periférico";
                });
            }
        }

        /// <summary>
        /// Comando para editar el periférico seleccionado
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditarEliminarPeriferico))]
        public async Task EditarPerifericoAsync()
        {
            if (PerifericoSeleccionado == null) return;

            try
            {
                var codigoOriginal = PerifericoSeleccionado!.Codigo ?? "N/A";
                  _logger.LogInformation($"[PerifericosViewModel] EditarPerifericoAsync: abriendo diálogo para Codigo={codigoOriginal}");

                var dialog = new Views.Perifericos.PerifericoDialog(PerifericoSeleccionado!, _dbContextFactory);

                _logger.LogInformation($"[PerifericosViewModel] Antes de ShowDialog() para Codigo={codigoOriginal}");
                if (dialog.ShowDialog() == true)
                {
                    _logger.LogInformation($"[PerifericosViewModel] ShowDialog() devolvió TRUE para Codigo={codigoOriginal}. Llamando GuardarPerifericoAsync...");
                    var perifericoEditado = dialog.ViewModel.PerifericoActual;
                    await GuardarPerifericoAsync(perifericoEditado, esNuevo: false, originalCodigo: codigoOriginal);
                    _logger.LogInformation($"[PerifericosViewModel] GuardarPerifericoAsync completado para Codigo={codigoOriginal}");
                }
                else
                {
                    _logger.LogInformation($"[PerifericosViewModel] ShowDialog() devolvió FALSE o NULL para Codigo={codigoOriginal}. No se guardará.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al editar periférico");

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al editar periférico";
                });
            }
        }

        /// <summary>
        /// Comando para eliminar el periférico seleccionado
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditarEliminarPeriferico))]
        public async Task EliminarPerifericoAsync()
        {
            if (PerifericoSeleccionado == null) return;

            try
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro de que desea eliminar el periférico '{PerifericoSeleccionado.Codigo}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    // Usar DbContextFactory en lugar de crear manualmente
                    using var dbContext = _dbContextFactory.CreateDbContext();
                    
                    var entity = await dbContext.PerifericosEquiposInformaticos
                        .FirstOrDefaultAsync(p => p.Codigo == PerifericoSeleccionado.Codigo);

                    if (entity != null)
                    {
                        dbContext.PerifericosEquiposInformaticos.Remove(entity);
                        await dbContext.SaveChangesAsync();

                        // Actualizar UI de forma asíncrona
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Perifericos.Remove(PerifericoSeleccionado);
                            ActualizarEstadisticas();
                            StatusMessage = "Periférico eliminado exitosamente";
                        });
                        
                        _logger.LogInformation("[PerifericosViewModel] Periférico eliminado: {Codigo}", PerifericoSeleccionado.Codigo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al eliminar periférico");
                
                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al eliminar periférico";
                });
            }
        }
        /// <summary>
        /// Verifica si se puede editar o eliminar un periférico
        /// </summary>
        public bool CanEditarEliminarPeriferico => PerifericoSeleccionado != null;

        /// <summary>
        /// Abre el diálogo de detalles para el periférico seleccionado (modo solo lectura)
        /// </summary>
        [RelayCommand] // SIN CanExecute - el botón siempre debe estar habilitado
        public async Task VerDetallesPerifericoAsync(PerifericoEquipoInformaticoDto? periferico = null)
        {
            _logger.LogInformation("========== [PerifericosViewModel] VerDetallesPerifericoAsync INICIO ==========");
            
            var p = periferico ?? PerifericoSeleccionado;
            if (p == null)
            {
                _logger.LogWarning("[PerifericosViewModel] VerDetallesPerifericoAsync: periférico es NULL, retornando");
                return;
            }

            try
            {
                // Guardar código original por si el usuario cambia el código durante la edición
                var codigoOriginal = p.Codigo ?? "N/A";                _logger.LogInformation($"[PerifericosViewModel] VerDetallesPerifericoAsync: Abriendo detalle para periférico {codigoOriginal}");                // Abrir vista de detalle como modal centrado sobre el owner y con overlay
                var detalleView = new Views.Perifericos.PerifericoDetalleView(p, _dbContextFactory, canEdit: CanEditarEliminarPeriferico);

                var ownerWindow = System.Windows.Application.Current?.MainWindow;
                if (ownerWindow != null)
                {
                    detalleView.Owner = ownerWindow;
                    detalleView.ConfigurarParaVentanaPadre(ownerWindow);
                }

                // Mostrar modal
                var resultado = detalleView.ShowDialog();

                _logger.LogInformation($"[PerifericosViewModel] VerDetallesPerifericoAsync: Vista de detalle cerrada. RequestEdit={detalleView.RequestEdit}");                // Si el usuario solicitó editar desde la vista de detalle, abrir el editor (PerifericoDialog)
                if (detalleView.RequestEdit)
                {
                    _logger.LogInformation($"[PerifericosViewModel] VerDetallesPerifericoAsync: Usuario solicitó editar periférico {codigoOriginal}. Abriendo editor...");                    var dialog = new Views.Perifericos.PerifericoDialog(p, _dbContextFactory);

                    // Solo establecer el Owner para relación padre-hijo, sin forzar tamaño de pantalla completa
                    // PerifericoDialog usa sus propios tamaños definidos en XAML (Height="700" Width="900")
                    if (ownerWindow != null)
                    {
                        dialog.Owner = ownerWindow;
                    }                    if (dialog.ShowDialog() == true)
                    {
                        _logger.LogInformation($"[PerifericosViewModel] VerDetallesPerifericoAsync: Editor devolvió TRUE para {codigoOriginal}. Guardando cambios...");
                        var perifericoEditado = dialog.ViewModel.PerifericoActual;
                        await GuardarPerifericoAsync(perifericoEditado, esNuevo: false, originalCodigo: codigoOriginal);
                        // No recargar: GuardarPerifericoAsync ya actualiza la colección correctamente
                        // await CargarPerifericosAsync() causaría duplicación visual temporal
                        StatusMessage = "Periférico actualizado correctamente";
                    }
                    else
                    {
                        _logger.LogInformation($"[PerifericosViewModel] VerDetallesPerifericoAsync: Editor cerrado sin guardar para {codigoOriginal}.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, $"[PerifericosViewModel] Error al abrir detalle del periférico {p?.Codigo ?? "N/A"}");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al abrir detalles del periférico";
                });
            }
        }

        /// <summary>
        /// Guarda un periférico en la base de datos
        /// </summary>
        private async Task GuardarPerifericoAsync(PerifericoEquipoInformaticoDto dto, bool esNuevo, string? originalCodigo = null)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("[PerifericosViewModel] GuardarPerifericoAsync llamado. esNuevo={EsNuevo}, CodigoDto={CodigoDto}, OriginalCodigoParam={OriginalParam}",
                    new object[] { esNuevo, dto.Codigo ?? string.Empty, originalCodigo ?? string.Empty });

                string actualCodigo = dto.Codigo ?? string.Empty;
                string original = originalCodigo ?? string.Empty;

                static string NormalizeKey(string? s)
                {
                    if (string.IsNullOrEmpty(s)) return string.Empty;
                    return s.Replace('\u00A0', ' ').Trim();
                }

                actualCodigo = NormalizeKey(actualCodigo);
                original = NormalizeKey(original);

                string actualCodigoNonNull = actualCodigo;
                string originalNonNull = original;

                if (string.IsNullOrWhiteSpace(dto.CodigoEquipoAsignado))
                {
                    try
                    {
                        var posibleOrigen = dto.UsuarioAsignado ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(posibleOrigen) && !string.IsNullOrWhiteSpace(dto.NombreEquipoAsignado))
                        {
                            posibleOrigen = dto.NombreEquipoAsignado;
                        }

                        if (!string.IsNullOrWhiteSpace(posibleOrigen))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(posibleOrigen, @"\b[A-Za-z0-9]+(?:-[A-Za-z0-9]+)+\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                dto.CodigoEquipoAsignado = match.Value.Trim();
                                _logger.LogInformation("[PerifericosViewModel] Fallback: extraído CodigoEquipoAsignado='{Codigo}' desde '{Origen}'",
                                    new object[] { match.Value.Trim(), posibleOrigen });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("[PerifericosViewModel] Error al intentar extraer CodigoEquipoAsignado desde texto: {Error}", ex.Message);
                    }
                }

                using var dbContext = _dbContextFactory.CreateDbContext();

                PerifericoEquipoInformaticoEntity entity;

                if (esNuevo)
                {
                    var existe = await dbContext.PerifericosEquiposInformaticos
                        .AnyAsync(p => p.Codigo == actualCodigoNonNull);

                    if (existe)
                    {
                        _logger.LogWarning("[PerifericosViewModel] Ya existe un periférico con código {Codigo}", actualCodigoNonNull);
                        MessageBox.Show("Ya existe un periférico con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    entity = new PerifericoEquipoInformaticoEntity
                    {
                        Codigo = actualCodigoNonNull
                    };

                    dbContext.PerifericosEquiposInformaticos.Add(entity);
                }
                else
                {
                    var codesToDetach = new List<string>();
                    if (!string.IsNullOrWhiteSpace(actualCodigoNonNull)) codesToDetach.Add(actualCodigoNonNull);
                    if (!string.IsNullOrWhiteSpace(originalNonNull) && !codesToDetach.Contains(originalNonNull)) codesToDetach.Add(originalNonNull);

                    _logger.LogInformation("[PerifericosViewModel] Editando periférico. Original='{Original}', Actual='{Actual}', CodesToDetach={Codes}",
                        originalNonNull, actualCodigoNonNull, string.Join(',', codesToDetach));

                    var tracked = dbContext.ChangeTracker.Entries<PerifericoEquipoInformaticoEntity>()
                        .Where(e => codesToDetach.Contains(e.Entity.Codigo))
                        .ToList();

                    foreach (var trackedEntity in tracked)
                    {
                        dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
                    }

                    var searchCodigo = string.IsNullOrWhiteSpace(originalNonNull) ? actualCodigoNonNull : originalNonNull;

                    _logger.LogInformation("[PerifericosViewModel] Buscando entidad en BD con CodigoSearch='{SearchCodigo}'", searchCodigo);

                    var existingEntity = await dbContext.PerifericosEquiposInformaticos
                        .FirstOrDefaultAsync(p => p.Codigo == searchCodigo);

                    if (existingEntity == null && !string.Equals(searchCodigo, actualCodigoNonNull, StringComparison.OrdinalIgnoreCase))
                    {
                        var fallbackKey = actualCodigoNonNull;
                        existingEntity = await dbContext.PerifericosEquiposInformaticos
                            .FirstOrDefaultAsync(p => p.Codigo == fallbackKey!);

                        if (existingEntity != null)
                        {
                            _logger.LogInformation("[PerifericosViewModel] No se encontró por SearchCodigo={Search} pero se encontró por CodigoActual={Actual}. Usando la entidad encontrada.",
                                searchCodigo, actualCodigoNonNull);
                        }
                    }

                    if (existingEntity == null)
                    {
                        var existsOriginal = !string.IsNullOrWhiteSpace(originalNonNull) && await dbContext.PerifericosEquiposInformaticos.AnyAsync(p => p.Codigo == originalNonNull);
                        var existsActual = !string.IsNullOrWhiteSpace(actualCodigoNonNull) && await dbContext.PerifericosEquiposInformaticos.AnyAsync(p => p.Codigo == actualCodigoNonNull);

                        _logger.LogInformation("[PerifericosViewModel] No se encontró el periférico a actualizar. Original={Original} (exists={ExistsOriginal}), Actual={Actual} (exists={ExistsActual})",
                            originalNonNull, existsOriginal, actualCodigoNonNull, existsActual);

                        MessageBox.Show($"No se encontró el periférico a actualizar. Códigos probados: Original={originalNonNull}, Actual={actualCodigoNonNull}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    entity = existingEntity;
                }

                var usuarioIngresado = string.IsNullOrWhiteSpace(dto.UsuarioAsignado) ? null : dto.UsuarioAsignado.Trim();
                var equipoIngresado = string.IsNullOrWhiteSpace(dto.CodigoEquipoAsignado) ? null : dto.CodigoEquipoAsignado.Trim();
                var nombreEquipoIngresado = string.IsNullOrWhiteSpace(dto.NombreEquipoAsignado) ? null : dto.NombreEquipoAsignado.Trim();
                var tieneAsignacion = !string.IsNullOrWhiteSpace(usuarioIngresado)
                                      || !string.IsNullOrWhiteSpace(equipoIngresado)
                                      || !string.IsNullOrWhiteSpace(nombreEquipoIngresado);
                var esDadoDeBaja = dto.Estado == EstadoPeriferico.DadoDeBaja;

                if (esDadoDeBaja)
                {
                    var entityActual = entity ?? throw new InvalidOperationException("La entidad del periférico no puede ser nula.");

                    if (string.IsNullOrWhiteSpace(dto.UsuarioAsignadoAnterior) && !string.IsNullOrWhiteSpace(entityActual.UsuarioAsignado))
                        dto.UsuarioAsignadoAnterior = entityActual.UsuarioAsignado;

                    if (string.IsNullOrWhiteSpace(dto.CodigoEquipoAsignadoAnterior) && !string.IsNullOrWhiteSpace(entityActual.CodigoEquipoAsignado))
                        dto.CodigoEquipoAsignadoAnterior = entityActual.CodigoEquipoAsignado;

                    usuarioIngresado = null;
                    equipoIngresado = null;
                    nombreEquipoIngresado = null;
                }
                else if (tieneAsignacion)
                {
                    dto.Estado = EstadoPeriferico.EnUso;
                }

                var didPkChange = !esNuevo && !string.IsNullOrWhiteSpace(originalNonNull) &&
                                  !string.Equals(originalNonNull, actualCodigoNonNull, StringComparison.OrdinalIgnoreCase);

                if (didPkChange)
                {
#pragma warning disable CS8600
#pragma warning disable CS8602
                    _logger.LogInformation("[PerifericosViewModel] Detectado cambio de PK: {Original} -> {Nuevo}. Ejecutando UPDATE directo.", originalNonNull, actualCodigoNonNull);

                    var usuarioAnterior = entity.UsuarioAsignado;
                    var equipoAnterior = entity.CodigoEquipoAsignado;
                    bool usuarioCambio = !string.Equals(usuarioAnterior, usuarioIngresado, StringComparison.OrdinalIgnoreCase);
                    bool equipoCambio = !string.Equals(equipoAnterior, equipoIngresado, StringComparison.OrdinalIgnoreCase);

                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE PerifericosEquiposInformaticos
                        SET Codigo = {actualCodigoNonNull},
                            Dispositivo = {dto.Dispositivo ?? string.Empty},
                            FechaCompra = {dto.FechaCompra},
                            Costo = {dto.Costo},
                            Marca = {dto.Marca ?? string.Empty},
                            Modelo = {dto.Modelo ?? string.Empty},
                            SerialNumber = {dto.Serial ?? string.Empty},
                            CodigoEquipoAsignado = {equipoIngresado},
                            UsuarioAsignado = {usuarioIngresado},
                            UsuarioAsignadoAnterior = {(usuarioCambio && !string.IsNullOrWhiteSpace(usuarioAnterior) ? usuarioAnterior : null)},
                            CodigoEquipoAsignadoAnterior = {(equipoCambio && !string.IsNullOrWhiteSpace(equipoAnterior) ? equipoAnterior : null)},
                            Sede = {dto.Sede},
                            Estado = {dto.Estado},
                            Observaciones = {dto.Observaciones ?? string.Empty},
                            FechaModificacion = {DateTime.Now}
                        WHERE Codigo = {originalNonNull}");

                    if (usuarioCambio)
                        _logger.LogInformation("[PerifericosViewModel] Auditoria: Usuario anterior={Anterior} para periférico {Codigo}", usuarioAnterior ?? "(ninguno)", actualCodigoNonNull);

                    if (equipoCambio)
                        _logger.LogInformation("[PerifericosViewModel] Auditoria: Equipo anterior={Anterior} para periférico {Codigo}", equipoAnterior ?? "(ninguno)", actualCodigoNonNull);

                    entity = await dbContext.PerifericosEquiposInformaticos.FirstOrDefaultAsync(p => p.Codigo == actualCodigoNonNull);
                    if (entity == null)
                    {
                        _logger.LogWarning("[PerifericosViewModel] Error tras UPDATE directo: no se pudo recargar la entidad con Codigo={Codigo}", actualCodigoNonNull);
                        MessageBox.Show("No se pudo recargar el periférico tras cambiar el código.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
#pragma warning restore CS8602
#pragma warning restore CS8600
                }
                else
                {
                    var entityActual = entity ?? throw new InvalidOperationException("La entidad del periférico no puede ser nula.");
                    var usuarioAnterior = entityActual.UsuarioAsignado;
                    var equipoAnterior = entityActual.CodigoEquipoAsignado;

                    if (!string.Equals(entityActual.UsuarioAsignado, usuarioIngresado, StringComparison.OrdinalIgnoreCase))
                    {
                        entityActual.UsuarioAsignadoAnterior = entityActual.UsuarioAsignado;
                        _logger.LogInformation("[PerifericosViewModel] Auditoria: Usuario anterior={Anterior} para periférico {Codigo}", entityActual.UsuarioAsignado ?? "(ninguno)", actualCodigoNonNull);
                    }

                    if (!string.Equals(entityActual.CodigoEquipoAsignado, equipoIngresado, StringComparison.OrdinalIgnoreCase))
                    {
                        entityActual.CodigoEquipoAsignadoAnterior = entityActual.CodigoEquipoAsignado;
                        _logger.LogInformation("[PerifericosViewModel] Auditoria: Equipo anterior={Anterior} para periférico {Codigo}", entityActual.CodigoEquipoAsignado ?? "(ninguno)", actualCodigoNonNull);
                    }

                    entityActual.Dispositivo = dto.Dispositivo ?? string.Empty;
                    entityActual.FechaCompra = dto.FechaCompra ?? default;
                    entityActual.Costo = dto.Costo ?? 0;
                    entityActual.Marca = dto.Marca;
                    entityActual.Modelo = dto.Modelo;
                    entityActual.SerialNumber = dto.Serial;
                    entityActual.CodigoEquipoAsignado = equipoIngresado;
                    entityActual.UsuarioAsignado = usuarioIngresado;

                    if (esDadoDeBaja)
                    {
                        entityActual.CodigoEquipoAsignadoAnterior ??= equipoAnterior;
                        entityActual.UsuarioAsignadoAnterior ??= usuarioAnterior;
                        entityActual.CodigoEquipoAsignado = null;
                        entityActual.UsuarioAsignado = null;
                    }

                    entityActual.Sede = dto.Sede;
                    entityActual.Estado = dto.Estado;
                    entityActual.Observaciones = dto.Observaciones;
                    entityActual.FechaModificacion = DateTime.Now;

                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation("[PerifericosViewModel] SaveChangesAsync completado para Codigo={Codigo}. Actualización persistida.", actualCodigoNonNull);
                    entity = entityActual;
                }

                ActualizarDto(dto, entity);

                var dtoExistente = Perifericos.FirstOrDefault(p => string.Equals(p.Codigo, originalNonNull, StringComparison.OrdinalIgnoreCase))
                                   ?? Perifericos.FirstOrDefault(p => string.Equals(p.Codigo, actualCodigoNonNull, StringComparison.OrdinalIgnoreCase));

                if (dtoExistente != null && !ReferenceEquals(dtoExistente, dto))
                {
                    ActualizarDto(dtoExistente, entity);
                }
                else if (esNuevo && !Perifericos.Any(p => string.Equals(p.Codigo, dto.Codigo, StringComparison.OrdinalIgnoreCase)))
                {
                    Perifericos.Add(dto);
                }

                ActualizarEstadisticas();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PerifericosView?.Refresh();
                    StatusMessage = esNuevo ? "Periférico agregado correctamente" : "Periférico actualizado correctamente";
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericosViewModel] Error al guardar periférico");

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al guardar periférico";
                });

                MessageBox.Show("Error al guardar el periférico. Ver logs para más detalles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Convierte una entidad a DTO
        /// </summary>
        private PerifericoEquipoInformaticoDto ConvertirEntityADto(PerifericoEquipoInformaticoEntity entity)
        {
            return new PerifericoEquipoInformaticoDto(entity);
        }
        /// <summary>
        /// Actualiza un DTO existente con datos de la entidad
        /// </summary>
        private void ActualizarDto(PerifericoEquipoInformaticoDto dto, PerifericoEquipoInformaticoEntity entity)
        {
            dto.Codigo = entity.Codigo;
            dto.Dispositivo = entity.Dispositivo;
            dto.FechaCompra = entity.FechaCompra;
            dto.Costo = entity.Costo;
            dto.Marca = entity.Marca;
            dto.Modelo = entity.Modelo;
            dto.Serial = entity.SerialNumber;
            dto.CodigoEquipoAsignado = entity.CodigoEquipoAsignado;
            dto.UsuarioAsignado = entity.UsuarioAsignado;
            dto.CodigoEquipoAsignadoAnterior = entity.CodigoEquipoAsignadoAnterior;
            dto.UsuarioAsignadoAnterior = entity.UsuarioAsignadoAnterior;
            dto.Sede = entity.Sede;
            dto.Estado = entity.Estado;
            dto.Observaciones = entity.Observaciones;
            dto.FechaModificacion = entity.FechaModificacion;
            dto.NombreEquipoAsignado = entity.EquipoAsignado?.NombreEquipo;
        }

        /// <summary>
        /// Actualiza las estadísticas mostradas en la vista
        /// </summary>
        private void ActualizarEstadisticas()
        {
            TotalPerifericos = Perifericos.Count;
            PerifericosEnUso = Perifericos.Count(p => p.Estado == EstadoPeriferico.EnUso);
            PerifericosAlmacenados = Perifericos.Count(p => p.Estado == EstadoPeriferico.AlmacenadoFuncionando);
            PerifericosDadosBaja = Perifericos.Count(p => p.Estado == EstadoPeriferico.DadoDeBaja);
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para periféricos
        /// </summary>
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Módulo de periféricos no disponible";
        }

        // Filtrado usado por ICollectionView
        private bool FiltrarPerifericos(object obj)
        {
            if (obj is not PerifericoEquipoInformaticoDto p) return false;

            // Ocultar dados de baja si toggle desactivado
            if (!ShowDadoDeBaja && p.Estado == EstadoPeriferico.DadoDeBaja)
                return false;

            if (string.IsNullOrWhiteSpace(Filtro)) return true;

            // ✅ Soportar filtro múltiple: separados por ';' (ej: "mouse; logitech; admin")
            // Todos los términos deben coincidir (AND lógico)
            var terminos = Filtro.Split(';')
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToArray();            // Campos a buscar
            var campos = new[]
            {
                (p.Codigo ?? "").ToLowerInvariant(),
                (p.Dispositivo ?? "").ToLowerInvariant(),
                (p.Marca ?? "").ToLowerInvariant(),
                (p.Modelo ?? "").ToLowerInvariant(),
                (p.TextoAsignacion ?? "").ToLowerInvariant(),
                (p.NombreEquipoAsignado ?? "").ToLowerInvariant(),
                (p.Serial ?? "").ToLowerInvariant(),
                p.Sede.ToString().ToLowerInvariant(),  // Agregar búsqueda por Sede (enum)
            };

            // Todos los términos deben encontrarse en al menos un campo
            return terminos.All(term => campos.Any(campo => campo.Contains(term)));
        }

        // Refrescar vista cuando cambian filtros
        partial void OnFiltroChanged(string value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => PerifericosView?.Refresh());
        }

        partial void OnShowDadoDeBajaChanged(bool value)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() => PerifericosView?.Refresh());
        }
        /// <summary>
        /// Comando para exportar periféricos a archivo Excel
        /// </summary>
        [RelayCommand]
        private async Task ExportarPerifericosAsync()
        {
            try
            {
                // Mostrar diálogo para seleccionar ubicación del archivo
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
                    DefaultExt = ".xlsx",
                    FileName = $"Perifericos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Exportar periféricos a Excel"
                };

                if (dialog.ShowDialog() != true)
                    return;

                IsLoading = true;
                StatusMessage = "Exportando periféricos...";

                // Exportar usando el servicio
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                await _exportService.ExportarPerifericosAExcelAsync(dialog.FileName, Perifericos, cts.Token);

                StatusMessage = $"Periféricos exportados exitosamente en {dialog.FileName}";
                MessageBox.Show(
                    "Periféricos exportados exitosamente",
                    "Éxito",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("[PerifericosViewModel] Periféricos exportados: {RutaArchivo}", dialog.FileName);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Exportación cancelada";
                _logger.LogInformation("[PerifericosViewModel] Exportación cancelada por el usuario");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error al exportar periféricos";
                MessageBox.Show($"Error al exportar periféricos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _logger.LogError(ex, "[PerifericosViewModel] Error al exportar periféricos");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
    }
}

