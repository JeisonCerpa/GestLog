using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using MessageBox = System.Windows.MessageBox;
using GestLog.Views.Tools.GestionEquipos;
using GestLog.Services;
using GestLog.Services.Interfaces;
using GestLog.Models.Events;
using GestLog.ViewModels.Base;
using GestLog.Modules.GestionMantenimientos.Messages;
using CommunityToolkit.Mvvm.Messaging;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels
{    /// <summary>
    /// ViewModel para la gestión de periféricos informáticos
    /// Respeta SRP: solo coordina la gestión CRUD de periféricos
    /// Hereda auto-refresh automático de DatabaseAwareViewModel
    /// </summary>
    public partial class PerifericosViewModel : DatabaseAwareViewModel
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<PerifericoEquipoInformaticoDto> _perifericos = new();        [ObservableProperty]
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
        public PerifericosViewModel(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory, IDatabaseConnectionService databaseService)
            : base(databaseService, logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            
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
        }        /// <summary>
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
        }        /// <summary>
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
        }        /// <summary>
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
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("[PerifericosViewModel] Cargados {Count} periféricos", entities.Count);

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Perifericos.Clear();
                    foreach (var entity in entities)
                    {
                        var dto = ConvertirEntityADto(entity);
                        Perifericos.Add(dto);
                    }

                    ActualizarEstadisticas();
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
        [RelayCommand]
        public async Task AgregarPerifericoAsync()
        {
            try
            {
                var dialog = new Views.Tools.GestionEquipos.PerifericoDialog(_dbContextFactory);

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
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al agregar periférico";
                });
            }
        }        /// <summary>
        /// Comando para editar el periférico seleccionado
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditarEliminarPeriferico))]
        public async Task EditarPerifericoAsync()
        {
            if (PerifericoSeleccionado == null) return;

            try
            {
                var dialog = new Views.Tools.GestionEquipos.PerifericoDialog(PerifericoSeleccionado, _dbContextFactory);

                var originalCodigo = PerifericoSeleccionado.Codigo; // guardar código original antes de editar

                if (dialog.ShowDialog() == true)
                {
                    var perifericoEditado = dialog.ViewModel.PerifericoActual;
                    await GuardarPerifericoAsync(perifericoEditado, esNuevo: false, originalCodigo: originalCodigo);
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
        /// Guarda un periférico en la base de datos
        /// </summary>
        private async Task GuardarPerifericoAsync(PerifericoEquipoInformaticoDto dto, bool esNuevo, string? originalCodigo = null)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            try
            {
                // Normalizar valores para evitar advertencias nullable
                var codigo = dto.Codigo ?? "-";
                string actualCodigo = dto.Codigo ?? string.Empty;
                string original = originalCodigo ?? string.Empty;

                // Variables non-null para uso en EF/SQL y logs
                string actualCodigoNonNull = actualCodigo;
                string originalNonNull = original;

                // Usar DbContextFactory en lugar de crear manualmente
                using var dbContext = _dbContextFactory.CreateDbContext();

                PerifericoEquipoInformaticoEntity entity;

                if (esNuevo)
                {
                    // Verificar si ya existe un periférico con el mismo código
                    var existe = await dbContext.PerifericosEquiposInformaticos
                        .AnyAsync(p => p.Codigo == actualCodigoNonNull);

                    if (existe)
                    {
                        _logger.LogWarning("[PerifericosViewModel] Ya existe un periférico con código {Codigo}", actualCodigoNonNull);
                        MessageBox.Show("Ya existe un periférico con ese código.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    entity = new PerifericoEquipoInformaticoEntity();
                    dbContext.PerifericosEquiposInformaticos.Add(entity);
                }
                else
                {
                    // Limpiar ChangeTracker para evitar conflictos: considerar tanto el código actual como el original
                    var codesToDetach = new List<string>();
                    if (!string.IsNullOrWhiteSpace(actualCodigoNonNull)) codesToDetach.Add(actualCodigoNonNull);
                    if (!string.IsNullOrWhiteSpace(originalNonNull) && !codesToDetach.Contains(originalNonNull)) codesToDetach.Add(originalNonNull);

                    var tracked = dbContext.ChangeTracker.Entries<PerifericoEquipoInformaticoEntity>()
                        .Where(e => codesToDetach.Contains(e.Entity.Codigo))
                        .ToList();

                    foreach (var trackedEntity in tracked)
                    {
                        dbContext.Entry(trackedEntity.Entity).State = EntityState.Detached;
                    }

                    var searchCodigo = string.IsNullOrWhiteSpace(originalNonNull) ? actualCodigoNonNull : originalNonNull;

                    var existingEntity = await dbContext.PerifericosEquiposInformaticos
                        .FirstOrDefaultAsync(p => p.Codigo == searchCodigo);

                    // Fallback: si no se encuentra por originalCodigo (posible race/estado), intentar buscar por el nuevo código
                    if (existingEntity == null && !string.Equals(searchCodigo, actualCodigoNonNull, StringComparison.OrdinalIgnoreCase))
                    {
                        // usar variable local non-null para evitar advertencias del compilador en la expresión EF
                        var fallbackKey = actualCodigoNonNull;
                        existingEntity = await dbContext.PerifericosEquiposInformaticos
                            .FirstOrDefaultAsync(p => p.Codigo == fallbackKey!);
                        if (existingEntity != null)
                        {
                            _logger.LogWarning("[PerifericosViewModel] No se encontró por OriginalCodigo={Original} pero se encontró por CodigoActual={Actual}. Usando la entidad encontrada.", searchCodigo, actualCodigoNonNull);
                        }
                    }

                    if (existingEntity == null)
                    {
                        _logger.LogWarning("[PerifericosViewModel] No se encontró el periférico a actualizar. Original={Original} Actual={Actual}", originalNonNull, actualCodigoNonNull);
                        MessageBox.Show($"No se encontró el periférico a actualizar. Códigos probados: Original={originalNonNull}, Actual={actualCodigoNonNull}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    entity = existingEntity;
                }

                // Si el código (PK) cambió, EF no permite marcar la PK como modificada en una entidad trackeada.
                // Hacemos un UPDATE directo en la BD y recargamos la entidad.
                var didPkChange = !esNuevo && !string.IsNullOrWhiteSpace(originalNonNull) &&
                                  !string.Equals(originalNonNull, actualCodigoNonNull, StringComparison.OrdinalIgnoreCase);

                if (didPkChange)
                {
#pragma warning disable CS8600 // Evitar advertencia de conversión nullable en expresiones EF locales
#pragma warning disable CS8602
                    _logger.LogInformation("[PerifericosViewModel] Detectado cambio de PK: {Original} -> {Nuevo}. Ejecutando UPDATE directo.", originalNonNull, actualCodigoNonNull);

                    // Ejecutar UPDATE directo para cambiar PK y demás campos en una sola operación atómica
                    await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        UPDATE PerifericosEquiposInformaticos
                        SET Codigo = {actualCodigoNonNull},
                            Dispositivo = {dto.Dispositivo ?? string.Empty},
                            FechaCompra = {dto.FechaCompra},
                            Costo = {dto.Costo},
                            Marca = {dto.Marca ?? string.Empty},
                            Modelo = {dto.Modelo ?? string.Empty},
                            SerialNumber = {dto.Serial ?? string.Empty},
                            CodigoEquipoAsignado = {(string.IsNullOrWhiteSpace(dto.CodigoEquipoAsignado) ? null : dto.CodigoEquipoAsignado)},
                            UsuarioAsignado = {(string.IsNullOrWhiteSpace(dto.UsuarioAsignado) ? null : dto.UsuarioAsignado)},
                            Sede = {dto.Sede},
                            Estado = {dto.Estado},
                            Observaciones = {dto.Observaciones ?? string.Empty},
                            FechaModificacion = {DateTime.Now}
                        WHERE Codigo = {originalNonNull}");

                    // Recargar la entidad actualizada
                    var reloadKey = actualCodigoNonNull;
                    entity = await dbContext.PerifericosEquiposInformaticos.FirstOrDefaultAsync(p => p.Codigo == reloadKey);
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
                    // Mapear campos cuando no hay cambio de PK
                    entity.Dispositivo = dto.Dispositivo ?? string.Empty;
                    entity.FechaCompra = dto.FechaCompra;
                    entity.Costo = dto.Costo;
                    entity.Marca = dto.Marca;
                    entity.Modelo = dto.Modelo;
                    entity.SerialNumber = dto.Serial;
                    entity.CodigoEquipoAsignado = string.IsNullOrWhiteSpace(dto.CodigoEquipoAsignado) ? null : dto.CodigoEquipoAsignado;
                    entity.UsuarioAsignado = string.IsNullOrWhiteSpace(dto.UsuarioAsignado) ? null : dto.UsuarioAsignado;
                    entity.Sede = dto.Sede;
                    entity.Estado = dto.Estado;
                    entity.Observaciones = dto.Observaciones;

                    await dbContext.SaveChangesAsync();
                }

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (esNuevo)
                    {
                        var nuevoDto = ConvertirEntityADto(entity);
                        Perifericos.Add(nuevoDto);
                    }
                    else
                    {
                        // Para ediciones, reemplazar completamente el DTO para asegurar que se actualice la vista
                        var searchCodigo = string.IsNullOrWhiteSpace(originalNonNull) ? actualCodigoNonNull : originalNonNull;
                        var indice = Perifericos.ToList().FindIndex(p => p.Codigo == searchCodigo);
                        if (indice >= 0)
                        {
                            var dtoActualizado = ConvertirEntityADto(entity);
                            Perifericos[indice] = dtoActualizado;

                            // Actualizar la selección si es necesario
                            if (PerifericoSeleccionado?.Codigo == searchCodigo)
                            {
                                PerifericoSeleccionado = dtoActualizado;
                            }
                        }
                    }

                    ActualizarEstadisticas();
                    StatusMessage = esNuevo ? "Periférico agregado exitosamente" : "Periférico actualizado exitosamente";
                });

                _logger.LogInformation("[PerifericosViewModel] Periférico {Accion}: {Codigo}", esNuevo ? "agregado" : "actualizado", codigo);
            }
            catch (Exception ex)
            {
                var codigoCatch = dto?.Codigo ?? "-";
                _logger.LogError(ex, "[PerifericosViewModel] Error al guardar periférico Codigo={Codigo}", codigoCatch);

                // Actualizar UI de forma asíncrona
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Error al guardar periférico";
                });

                MessageBox.Show("Error al guardar el periférico. Ver logs para más detalles.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        /// <summary>
        /// Convierte una entidad a DTO
        /// </summary>
        private PerifericoEquipoInformaticoDto ConvertirEntityADto(PerifericoEquipoInformaticoEntity entity)
        {
            return new PerifericoEquipoInformaticoDto(entity);
        }        /// <summary>
        /// Actualiza un DTO existente con datos de la entidad
        /// </summary>
        private void ActualizarDto(PerifericoEquipoInformaticoDto dto, PerifericoEquipoInformaticoEntity entity)
        {
            dto.Dispositivo = entity.Dispositivo;
            dto.FechaCompra = entity.FechaCompra;
            dto.Costo = entity.Costo;
            dto.Marca = entity.Marca;
            dto.Modelo = entity.Modelo;
            dto.Serial = entity.SerialNumber;
            dto.CodigoEquipoAsignado = entity.CodigoEquipoAsignado;
            dto.UsuarioAsignado = entity.UsuarioAsignado;
            dto.Sede = entity.Sede;
            dto.Estado = entity.Estado;
            dto.Observaciones = entity.Observaciones;
            dto.FechaModificacion = entity.FechaModificacion;
            dto.NombreEquipoAsignado = entity.EquipoAsignado?.NombreEquipo;
        }/// <summary>
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
    }
}
