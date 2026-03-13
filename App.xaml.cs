using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using GestLog.Services.Core.Logging;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using GestLog.Services;
using GestLog.Services.Interfaces;
using System.Threading;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using AutoUpdaterDotNET;
using static AutoUpdaterDotNET.Mode;
using System.Reflection;
using System.Diagnostics;

namespace GestLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IGestLogLogger? _logger;    
    public IServiceProvider ServiceProvider => LoggingService.GetServiceProvider();    protected override async void OnStartup(StartupEventArgs e)
    {
        // ✅ PRIMERO: Establecer variables de entorno desde launchSettings.json ANTES de cualquier otra operación
        var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT");
        if (string.IsNullOrEmpty(environment))
        {
            // Si no está establecida, usar default de launchSettings.json
            environment = "Production";
        }
        
        // Establecer las variables de base de datos basadas en el ambiente
        var databaseConfigFileName = environment.ToLower() switch
        {
            "development" => "database-development.json",
            "testing" => "database-testing.json",
            _ => "database-production.json"
        };

        // Construir la ruta completa al archivo de configuración (relativa o absoluta)
        // Primero intenta con ruta relativa, luego con ruta absoluta desde el directorio de ejecución
        var appDirectory = AppContext.BaseDirectory;
        var databaseConfigFile = Path.Combine(appDirectory, "config", databaseConfigFileName);
        
        // Si no existe en la ruta esperada, intenta en el directorio de ejecución actual
        if (!File.Exists(databaseConfigFile))
        {
            databaseConfigFile = Path.Combine(Directory.GetCurrentDirectory(), "config", databaseConfigFileName);
        }
        
        // Fallback: intenta con ruta relativa simple
        if (!File.Exists(databaseConfigFile))
        {
            databaseConfigFile = Path.Combine("config", databaseConfigFileName);
        }

        // Leer configuración de BD desde el archivo correspondiente
        if (File.Exists(databaseConfigFile))
        {
            var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(databaseConfigFile));
            var dbSection = json.RootElement.GetProperty("Database");
            
            Environment.SetEnvironmentVariable("GESTLOG_DB_SERVER", dbSection.GetProperty("Server").GetString() ?? "");
            Environment.SetEnvironmentVariable("GESTLOG_DB_NAME", dbSection.GetProperty("Database").GetString() ?? "");
            Environment.SetEnvironmentVariable("GESTLOG_DB_USER", dbSection.GetProperty("Username").GetString() ?? "");
            Environment.SetEnvironmentVariable("GESTLOG_DB_PASSWORD", dbSection.GetProperty("Password").GetString() ?? "");
            Environment.SetEnvironmentVariable("GESTLOG_DB_INTEGRATED_SECURITY", dbSection.GetProperty("UseIntegratedSecurity").GetBoolean().ToString());
        }        // ✅ Configurar manejo global de excepciones ANTES de cualquier otra lógica
        SetupGlobalExceptionHandling();
        
        // Configurar tooltip delay a 150ms (SOLO UNA VEZ en toda la aplicación)
        ConfigureTooltipDelay();
        
        // Cargar configuración de la aplicación ANTES de cualquier acceso a configuración
        await LoadApplicationConfigurationAsync();
        
        // Mostrar SplashScreen antes de cualquier lógica
        var splash = new GestLog.Modules.Shell.Views.SplashScreen();
        splash.Show();
        await System.Threading.Tasks.Task.Delay(500); // Permitir renderizado

        base.OnStartup(e); // Llamar primero según buenas prácticas WPF
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            LoggingService.InitializeServices();
            _logger = LoggingService.GetLogger();            splash.ShowStatus("Verificando conexión a la base de datos...");
            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            bool conexionOk = false;
            if (databaseService != null)
            {
                // Usar timeout de 10 segundos para el splash screen con método rápido
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    conexionOk = await databaseService.TestConnectionQuickAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger?.Logger.LogWarning("⚠️ Timeout verificando conexión durante splash screen");
                    conexionOk = false;
                }
            }
            if (!conexionOk)
            {
                splash.ShowStatus("Sin conexión a la base de datos");
                await System.Threading.Tasks.Task.Delay(1500);
            }
            else
            {
                splash.ShowStatus("Conexión a la base de datos OK");
                await System.Threading.Tasks.Task.Delay(500);
            }            splash.ShowStatus("Verificando actualizaciones...");
            var updateService = LoggingService.GetService<GestLog.Services.VelopackUpdateService>();
            bool hayActualizacion = false;
            if (updateService != null)
            {
                var updateCheckResult = await updateService.CheckForUpdatesAsync();
                hayActualizacion = updateCheckResult.HasUpdatesAvailable && !updateCheckResult.HasAccessError;
            }
            
            if (hayActualizacion)
            {
                splash.ShowStatus("¡Actualización disponible!");
                await System.Threading.Tasks.Task.Delay(1000);
                
                // ✅ CERRAR el splash ANTES de mostrar el diálogo modal
                splash.Close();
                
                // Mostrar diálogo y procesar actualización
                if (updateService != null)
                {
                    var updatedAndRestarting = await updateService.NotifyAndPromptForUpdateAsync();
                    if (updatedAndRestarting)
                    {
                        // La aplicación se reiniciará automáticamente, no continuar
                        return;
                    }
                }
                
                // Si el usuario rechaza la actualización, recrear el splash para continuar
                splash = new GestLog.Modules.Shell.Views.SplashScreen();
                splash.Show();
                await System.Threading.Tasks.Task.Delay(300);
            }
            else
            {
                splash.ShowStatus("No hay actualizaciones");
                await System.Threading.Tasks.Task.Delay(500);
            }            // Inicializar conexión a base de datos con monitoreo automático
            splash.ShowStatus("Inicializando servicio de base de datos...");

            // Sincronizar variables de entorno automáticamente ANTES de conectar
            splash.ShowStatus("Sincronizando variables de entorno...");
            try
            {
                var envVarService = LoggingService.GetService<GestLog.Services.Core.IEnvironmentVariableService>();
                if (envVarService != null)
                {
                    var syncResult = await envVarService.SyncEnvironmentVariablesAsync();
                    _logger?.Logger.LogInformation("📊 Resultado de sincronización: {Created} creadas, {Updated} actualizadas, {Unchanged} sin cambios, {Failed} errores",
                        syncResult.Created, syncResult.Updated, syncResult.Unchanged, syncResult.Failed);
                    
                    if (syncResult.Failed > 0)
                    {
                        _logger?.Logger.LogWarning("⚠️ Hubo errores al sincronizar variables de entorno");
                    }
                    
                    splash.ShowStatus("Variables de entorno sincronizadas");
                    await System.Threading.Tasks.Task.Delay(500);
                }
            }
            catch (Exception exEnvVars)
            {
                _logger?.Logger.LogError(exEnvVars, "⚠️ Error al sincronizar variables de entorno (continuando)");
                // No es crítico, continuar con los valores actuales
            }

            // Aplicar migraciones pendientes automáticamente ANTES de cualquier acceso a la BD
            splash.ShowStatus("Aplicando migraciones de base de datos...");
            GestLog.Services.Core.IMigrationService? migrationService = LoggingService.GetService<GestLog.Services.Core.IMigrationService>();
            try
            {
                if (migrationService != null)
                {
                    // Ejecutar migraciones en un task de fondo y proteger con timeout para no bloquear la UI
                    var migrationTask = Task.Run(async () => await migrationService.EnsureDatabaseUpdatedAsync());
                    var completed = await Task.WhenAny(migrationTask, Task.Delay(TimeSpan.FromSeconds(30)));

                    if (completed != migrationTask)
                    {
                        _logger?.Logger.LogWarning("⚠️ Timeout aplicando migraciones (30s). Continuando sin bloquear la UI.");
                        splash.ShowStatus("Aplicación de migraciones excedió el tiempo. Continuando sin migrar.");
                        await System.Threading.Tasks.Task.Delay(500);
                    }
                    else
                    {
                        // Si el task falló, esta await propagará la excepción para manejarla abajo
                        await migrationTask;
                        splash.ShowStatus("Migraciones aplicadas exitosamente");
                        await System.Threading.Tasks.Task.Delay(500);
                    }
                }
                else
                {
                    _logger?.Logger.LogWarning("⚠️ Servicio de migraciones no disponible");
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.Logger.LogWarning("⚠️ Migraciones canceladas por token");
                splash.ShowStatus("Migraciones canceladas. Continuando...");
                await System.Threading.Tasks.Task.Delay(500);
            }
            catch (Exception exMigrations)
            {
                _logger?.Logger.LogError(exMigrations, "❌ Error al aplicar migraciones de base de datos");
                // Mostrar diálogo con opciones para que el usuario decida sin bloquear indefinidamente
                var mbResult = System.Windows.MessageBox.Show(
                    $"Error al aplicar migraciones:\n{exMigrations.Message}\n\nSeleccione Sí para reintentar, No para continuar sin migrar, Cancelar para salir.",
                    "Error de Migraciones",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (mbResult == MessageBoxResult.Yes)
                {
                    _logger?.Logger.LogInformation("Usuario eligió reintentar migraciones");
                    try
                    {
                        // Reintento (una única vez) en background con timeout
                        var retryTask = Task.Run(async () => await migrationService!.EnsureDatabaseUpdatedAsync());
                        var completedRetry = await Task.WhenAny(retryTask, Task.Delay(TimeSpan.FromSeconds(30)));
                        if (completedRetry != retryTask)
                        {
                            _logger?.Logger.LogWarning("⚠️ Timeout en reintento de migraciones");
                            System.Windows.MessageBox.Show("Reintento de migraciones excedió el tiempo. Se continuará sin aplicar migraciones.", "Reintento Timeout", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            await retryTask; // propagará excepción si falla
                            splash.ShowStatus("Migraciones aplicadas exitosamente (reintento)");
                            await System.Threading.Tasks.Task.Delay(500);
                        }
                    }
                    catch (Exception retryEx)
                    {
                        _logger?.Logger.LogError(retryEx, "❌ Reintento de migraciones falló");
                        System.Windows.MessageBox.Show($"Reintento falló: {retryEx.Message}", "Migraciones fallidas", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (mbResult == MessageBoxResult.No)
                {
                    _logger?.Logger.LogWarning("Usuario eligió continuar sin aplicar migraciones");
                    // Continuar la aplicación sin migrar
                }
                else
                {
                    _logger?.Logger.LogCritical("Usuario eligió cerrar la aplicación por error en migraciones");
                    splash.Close();
                    System.Windows.Application.Current.Shutdown(1);
                    return;
                }
            }

            // Crear un CTS con timeout para no bloquear indefinidamente el splash
            using (var dbInitCts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
            {
                // Reutilizar la variable `databaseService` ya declarada más arriba
                databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();

                EventHandler<GestLog.Models.Events.DatabaseConnectionStateChangedEventArgs>? localDbStateHandler = null;

                // Contador para mostrar tiempo restante en el splash
                var initTimeout = TimeSpan.FromSeconds(15);
                var endTime = DateTime.UtcNow + initTimeout;
                var countdown = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                System.EventHandler? countdownTickHandler = null;
                countdownTickHandler = (s, ev) =>
                {
                    try
                    {
                        var remaining = endTime - DateTime.UtcNow;
                        if (remaining <= TimeSpan.Zero)
                        {
                            // Timeout alcanzado: detener timer
                            countdown.Stop();
                            return;
                        }

                        // Solo mostrar el contador si aún no hay estado claro de conexión
                        var showCount = databaseService == null ||
                            databaseService.CurrentState == GestLog.Models.Events.DatabaseConnectionState.Unknown ||
                            databaseService.CurrentState == GestLog.Models.Events.DatabaseConnectionState.Connecting;

                        if (showCount)
                        {
                            var secs = (int)Math.Ceiling(remaining.TotalSeconds);
                            splash.Dispatcher.Invoke(() => splash.ShowStatus($"Inicializando servicio de base de datos... ({secs}s restantes)"));
                        }
                    }
                    catch { /* no romper el timer por excepciones */ }
                };
                countdown.Tick += countdownTickHandler;

                if (databaseService != null)
                {
                    // Suscribir un handler local para actualizar el splash en tiempo real
                    localDbStateHandler = (sender, evt) =>
                    {
                        try
                        {
                            var statusText = evt.CurrentState switch
                            {
                                GestLog.Models.Events.DatabaseConnectionState.Connected => "Conexión a la base de datos establecida",
                                GestLog.Models.Events.DatabaseConnectionState.Connecting => "Conectando a la base de datos...",
                                GestLog.Models.Events.DatabaseConnectionState.Reconnecting => "Reconectando a la base de datos...",
                                GestLog.Models.Events.DatabaseConnectionState.Disconnected => "Sin conexión a la base de datos",
                                GestLog.Models.Events.DatabaseConnectionState.Error => $"Error en conexión: {evt.Message ?? "Sin detalles"}",
                                _ => "Inicializando servicio de base de datos..."
                            };

                            // Asegurar actualización en el hilo de la UI
                            splash.Dispatcher.Invoke(() => splash.ShowStatus(statusText));
                        }
                        catch { /* evitar que el handler tire */ }
                    };

                    databaseService.ConnectionStateChanged += localDbStateHandler;
                }

                try
                {
                    // Iniciar el contador antes de llamar a la inicialización
                    countdown.Start();

                    // Pasar el token con timeout a la inicialización
                    await InitializeDatabaseConnectionAsync(dbInitCts.Token);
                    splash.ShowStatus("Servicio de base de datos inicializado");
                    await System.Threading.Tasks.Task.Delay(500);
                }
                catch (OperationCanceledException)
                {
                    _logger?.Logger.LogWarning("⚠️ Timeout durante inicialización del servicio de base de datos");
                    splash.ShowStatus("Inicialización de la base de datos excedió el tiempo. Continuando sin BD");
                    await System.Threading.Tasks.Task.Delay(1500);
                }
                catch (Exception exDbInit)
                {
                    _logger?.Logger.LogError(exDbInit, "❌ Error durante InitializeDatabaseConnectionAsync");
                    splash.ShowStatus($"Error inicializando BD: {exDbInit.Message}");
                    await System.Threading.Tasks.Task.Delay(1500);
                }
                finally
                {
                    // Desuscribir el handler local si fue registrado
                    try
                    {
                        if (databaseService != null && localDbStateHandler != null)
                            databaseService.ConnectionStateChanged -= localDbStateHandler;
                    }
                    catch { }

                    try
                    {
                        countdown.Stop();
                        if (countdownTickHandler != null)
                            countdown.Tick -= countdownTickHandler;
                        // endTime no requiere pararse
                    }
                    catch { }
                }
            }

            // Bloque try-catch adicional para inicialización de ventana principal y restauración de sesión
            try
            {
                var currentUserService = LoggingService.GetService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>() as GestLog.Modules.Usuarios.Services.CurrentUserService;
                currentUserService?.RestoreSessionIfExists();
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                string nombrePersona = currentUserService?.Current?.FullName ?? string.Empty;
                if (mainWindow.DataContext is GestLog.ViewModels.MainWindowViewModel vm)
                {
                    vm.SetAuthenticated(currentUserService?.IsAuthenticated ?? false, nombrePersona);
                    vm.NotificarCambioNombrePersona();
                }
                if (currentUserService?.IsAuthenticated == true)
                {
                    mainWindow.LoadHomeView();
                }
                mainWindow.Show();
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                splash.Close();
            }
            catch (Exception exWin)
            {
                _logger?.Logger.LogError(exWin, "❌ Error al inicializar la ventana principal o restaurar sesión");
                System.Windows.MessageBox.Show($"Error al inicializar la ventana principal:\n{exWin.Message}",
                    "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error crítico al inicializar la aplicación:\n{ex.Message}",
                "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);
            try
            {
                LoggingService.InitializeServices();
                _logger = LoggingService.GetLogger();
                _logger.LogUnhandledException(ex, "App.OnStartup");
            }            catch
            {
                System.Windows.Application.Current.Shutdown(1);
                return;
            }
        }

    }
    /// <summary>
    /// Carga la configuración de la aplicación al inicio
    /// </summary>
    private async Task LoadApplicationConfigurationAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("🔧 Cargando configuración de la aplicación...");

            // Obtener el servicio de configuración
            var configurationService = LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();

            // Cargar la configuración desde el archivo
            await configurationService.LoadAsync();

            _logger?.Logger.LogInformation("✅ Configuración de la aplicación cargada exitosamente");
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error al cargar la configuración de la aplicación");
            // No es crítico, la aplicación puede continuar con configuración por defecto
        }
    }

    /// <summary>
    /// Valida la configuración de seguridad al inicio de la aplicación
    /// </summary>
    private async Task ValidateSecurityConfigurationAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("🔒 Validando configuración de seguridad...");

            // Obtener el servicio de validación de seguridad
            var securityValidationService = LoggingService.GetService<SecurityStartupValidationService>();

            // Ejecutar validación completa
            var isValid = await securityValidationService.ValidateAllSecurityAsync();

            if (isValid)
            {
                _logger?.Logger.LogInformation("✅ Validación de seguridad completada exitosamente");
            }
            else
            {
                _logger?.Logger.LogWarning("⚠️ Se encontraron problemas en la configuración de seguridad");

                // Mostrar guía de configuración al usuario
                await securityValidationService.ShowSecurityGuidanceAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error durante la validación de seguridad");
            // No es crítico, la aplicación puede continuar
        }
    }

    /// <summary>
    /// Inicializa la conexión a base de datos automáticamente
    /// </summary>
    private async Task InitializeDatabaseConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.Logger.LogDebug("💾 Inicializando conexión a base de datos...");

            // Obtener el servicio de base de datos
            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();

            if (databaseService == null)
            {
                _logger?.Logger.LogWarning("⚠️ Servicio de base de datos no disponible en el contenedor DI");
                return;
            }

            // Iniciar el servicio con monitoreo automático (propagar token de cancelación)
            await databaseService.StartAsync(cancellationToken);

            // Suscribirse a cambios de estado para logging
            databaseService.ConnectionStateChanged += OnDatabaseConnectionStateChanged;

            _logger?.Logger.LogDebug("✅ Servicio de base de datos inicializado");
        }
        catch (OperationCanceledException)
        {
            _logger?.Logger.LogWarning("⚠️ Inicialización del servicio de base de datos cancelada por token");
            throw;
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error al inicializar la conexión a base de datos");
            // No es crítico, la aplicación puede continuar sin BD
        }
    }

    /// <summary>
    /// Maneja los cambios de estado de la conexión a base de datos
    /// </summary>
    private void OnDatabaseConnectionStateChanged(object? sender, GestLog.Models.Events.DatabaseConnectionStateChangedEventArgs e)
    {
        var statusIcon = e.CurrentState switch
        {
            GestLog.Models.Events.DatabaseConnectionState.Connected => "✅",
            GestLog.Models.Events.DatabaseConnectionState.Connecting => "🔄",
            GestLog.Models.Events.DatabaseConnectionState.Reconnecting => "🔄",
            GestLog.Models.Events.DatabaseConnectionState.Disconnected => "⏸️",
            GestLog.Models.Events.DatabaseConnectionState.Error => "❌",
            _ => "❓"
        };

        _logger?.Logger.LogInformation("{Icon} Base de datos: {PreviousState} → {CurrentState} | {Message}",
            statusIcon, e.PreviousState, e.CurrentState, e.Message ?? "Sin detalles");

        if (e.Exception != null)
        {
            _logger?.Logger.LogDebug(e.Exception, "Detalles del error de conexión a BD");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Logger.LogInformation("🛑 Aplicación GestLog cerrándose - Iniciando shutdown simplificado");

            // Shutdown simplificado directo
            PerformDirectShutdown();

            _logger?.Logger.LogInformation("✅ Shutdown simplificado completado");
        }
        catch (Exception ex)
        {
            // Log en consola como último recurso
            Console.WriteLine($"Error durante el cierre: {ex.Message}");
        }
        finally
        {
            base.OnExit(e);
        }
    }    
    /// <summary>
    /// Realiza un shutdown directo y simple sin servicios complejos
    /// </summary>
    private void PerformDirectShutdown()
    {
        try
        {
            _logger?.Logger.LogInformation("🔧 Ejecutando shutdown directo...");

            // Paso 1: Detener servicio de base de datos sin await
            try
            {
                var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                if (databaseService != null)
                {
                    _logger?.Logger.LogInformation("🛑 Deteniendo servicio de base de datos...");

                    // Desuscribirse de eventos
                    databaseService.ConnectionStateChanged -= OnDatabaseConnectionStateChanged;

                    // Solo disposar sin StopAsync para evitar bloqueos
                    databaseService.Dispose();

                    _logger?.Logger.LogInformation("✅ Servicio de base de datos dispuesto");
                }
            }
            catch (Exception dbEx)
            {
                _logger?.Logger.LogWarning(dbEx, "⚠️ Error deteniendo servicio de BD");
            }

            // Paso 2: Dar tiempo mínimo para operaciones pendientes
            Thread.Sleep(100);

            // Paso 3: Cerrar sistema de logging
            _logger?.Logger.LogInformation("🔄 Cerrando sistema de logging...");
            LoggingService.Shutdown();

            // Paso 4: Forzar terminación del proceso inmediatamente
            Console.WriteLine("🛑 Terminando proceso inmediatamente");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en shutdown directo: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private void SetupGlobalExceptionHandling()
    {
        // Obtener el servicio de manejo de errores
        var errorHandler = LoggingService.GetErrorHandler();        // Excepciones no manejadas en el hilo principal (UI)
        DispatcherUnhandledException += (sender, e) =>
        {
            // LiveChartsCore: NullReference conocido al liberar MotionCanvas durante Unloaded/Dispose.
            // No es crítico para la app y puede ocurrir al cerrar vistas con gráficos.
            if (e.Exception is NullReferenceException nullRefEx)
            {
                var stack = nullRefEx.StackTrace ?? string.Empty;
                var isLiveChartsDisposeIssue =
                    stack.Contains("LiveChartsCore.SkiaSharpView.WPF.Rendering.CompositionTargetTicker.DisposeTicker") ||
                    stack.Contains("LiveChartsCore.Motion.MotionCanvasComposer.Dispose") ||
                    stack.Contains("LiveChartsCore.SkiaSharpView.WPF.MotionCanvas.OnUnloaded");

                if (isLiveChartsDisposeIssue)
                {
                    _logger?.Logger.LogWarning(nullRefEx,
                        "⚠️ Excepción no crítica de LiveCharts al liberar recursos de render (se ignora para evitar cierre de la aplicación)");
                    e.Handled = true;
                    return;
                }
            }

            // Información adicional para errores de Background UnsetValue
            if (e.Exception is InvalidOperationException invalidOp && 
                invalidOp.Message.Contains("DependencyProperty.UnsetValue") &&
                invalidOp.Message.Contains("Background"))
            {
                _logger?.Logger.LogError(e.Exception, "❌ Error específico de Background UnsetValue detectado");
                
                // Intentar obtener información del control que causó el error
                try
                {
                    var targetSite = invalidOp.TargetSite?.DeclaringType?.Name;
                    var stackTrace = invalidOp.StackTrace;
                    
                    _logger?.Logger.LogError("🔍 Información del error Background:");
                    _logger?.Logger.LogError("  - Target Site: {TargetSite}", targetSite);
                    _logger?.Logger.LogError("  - Stack Trace contiene Border: {ContainsBorder}", stackTrace?.Contains("Border") ?? false);
                    _logger?.Logger.LogError("  - Stack Trace contiene DataGrid: {ContainsDataGrid}", stackTrace?.Contains("DataGrid") ?? false);
                    _logger?.Logger.LogError("  - Stack Trace contiene UserControl: {ContainsUserControl}", stackTrace?.Contains("UserControl") ?? false);
                }
                catch
                {
                    _logger?.Logger.LogError("❌ No se pudo obtener información adicional del error Background");
                }
            }

            errorHandler.HandleException(
                e.Exception,
                "DispatcherUnhandledException",
                showToUser: false); // Cambiado a false para evitar ventanas emergentes constantes

            e.Handled = true; // Permitir que la aplicación continúe
        };

        // Excepciones no manejadas en hilos secundarios
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown exception");
            errorHandler.HandleException(exception, "AppDomain.UnhandledException");

            if (e.IsTerminating)
            {
                _logger?.Logger.LogCritical("💥 La aplicación se está cerrando debido a una excepción no manejada");
                LoggingService.Shutdown();
            }
        };        
        // Excepciones no observadas en Tasks
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            // Filtrar excepciones de red que son comunes y no críticas
            var innerException = e.Exception.GetBaseException();

            if (innerException is SocketException socketEx)
            {
                // Error 995: Operación de E/S cancelada - común en cancelaciones de red
                if (socketEx.ErrorCode == 995)
                {
                    _logger?.Logger.LogDebug("🌐 Operación de red cancelada (Error 995) - esto es normal: {Message}", socketEx.Message);
                    e.SetObserved(); // Marcar como observada
                    return;
                }

                // Error 10054: Conexión cerrada por el servidor remoto
                if (socketEx.ErrorCode == 10054)
                {
                    _logger?.Logger.LogDebug("🌐 Conexión cerrada por servidor remoto (Error 10054): {Message}", socketEx.Message);
                    e.SetObserved();
                    return;
                }
            }

            // Para otras excepciones de cancelación
            if (innerException is OperationCanceledException || innerException is TaskCanceledException)
            {
                _logger?.Logger.LogDebug("⏹️ Tarea cancelada no observada: {Message}", innerException.Message);
                e.SetObserved();
                return;
            }

            // Para errores serios, usar el manejador normal
            errorHandler.HandleException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved(); // Marcar como observada para evitar el cierre de la aplicación
        };

        // Suscribirse al evento de errores para posibles notificaciones adicionales
        errorHandler.ErrorOccurred += (sender, e) =>
        {
            // Se puede usar para ejecutar acciones adicionales cuando ocurre un error
            // Por ejemplo, actualizar un contador de errores en la interfaz de usuario
            _logger?.Logger.LogDebug("Error registrado: {ErrorId} en {Context}", e.Error.Id, e.Error.Context);
        };
    }

    /// <summary>
    /// Configura el delay del tooltip a 150ms (reducido de 400ms por defecto).
    /// Se ejecuta una sola vez al inicio de la aplicación.
    /// </summary>
    private void ConfigureTooltipDelay()
    {
        try
        {
            // Registrar la metadata del delay solo una vez en toda la aplicación
            System.Windows.Controls.ToolTipService.InitialShowDelayProperty.OverrideMetadata(
                typeof(System.Windows.Controls.ToolTip),
                new System.Windows.FrameworkPropertyMetadata(150));
        }
        catch (ArgumentException ex)
        {
            // Si ya está registrada, esto es esperado en casos raros
            // No es crítico, continuamos sin problema
            _logger?.Logger.LogWarning(ex, "⚠️ Tooltip delay ya estaba configurado");
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error al configurar tooltip delay");
        }
    }

    /// <summary>
    /// Muestra la ventana de autenticación y maneja el proceso de login
    /// </summary>
    /// <returns>True si el login fue exitoso, False si se canceló</returns>
    private bool ShowAuthentication()
    {        
        try
        {
            _logger?.Logger.LogInformation("🔐 Iniciando proceso de autenticación");

            // Crear la ventana de login (el constructor maneja el ViewModel y DI)
            // Eliminar referencias y uso de LoginWindow, solo debe usarse LoginView como UserControl
            // var loginWindow = new Views.Authentication.LoginWindow();

            // Mostrar como dialog modal
            // var result = loginWindow.ShowDialog();

            // if (result == true)
            // {
            //     _logger?.Logger.LogInformation("✅ Autenticación exitosa");
            //     return true;
            // }
            // else
            // {
            //     _logger?.Logger.LogInformation("🚫 Login cancelado por el usuario");
            //     return false;
            // }
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error durante el proceso de autenticación");
            // Antes: MessageBox con error
            return false;
        }        
        return false;
    }    /// <summary>
    /// Inicializa el servicio de actualizaciones de forma silenciosa en segundo plano
    /// Solo muestra diálogo de actualización si realmente hay una actualización disponible
    /// </summary>
    private async Task InitializeUpdateServiceAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("🔍 Iniciando verificación silenciosa de actualizaciones...");            // Obtener el servicio de configuración y asegurar que esté cargado
            var configurationService = LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();
            if (configurationService == null)
            {
                _logger?.Logger.LogWarning("⚠️ Servicio de configuración no disponible");
                return;
            }

            // ASEGURAR que la configuración esté completamente cargada antes de verificar
            await configurationService.LoadAsync();
            var config = configurationService.Current;            // 🔍 DEBUG: Verificar valores exactos de configuración
            _logger?.Logger.LogInformation("🔍 DEBUG Updater Config: Enabled='{Enabled}', UpdateServerPath='{UpdateServerPath}' (Length={Length})", 
                config?.Updater?.Enabled, 
                config?.Updater?.UpdateServerPath ?? "NULL", 
                config?.Updater?.UpdateServerPath?.Length ?? 0);

            if (config?.Updater?.Enabled != true)
            {
                _logger?.Logger.LogInformation("⏭️ Sistema de actualizaciones deshabilitado en configuración");
                return;
            }            
            if (string.IsNullOrWhiteSpace(config.Updater.UpdateServerPath))
            {
                _logger?.Logger.LogWarning("⚠️ URL de actualizaciones no configurada");
                return;
            }

            // ✅ URL de actualizaciones configurada correctamente
            _logger?.Logger.LogInformation("✅ URL de actualizaciones configurada: '{UpdateServerPath}'", config.Updater.UpdateServerPath);

            // Crear el servicio de actualizaciones
            var updateService = LoggingService.GetService<GestLog.Services.VelopackUpdateService>();
            if (updateService == null)
            {
                _logger?.Logger.LogWarning("⚠️ Servicio de actualizaciones no disponible");
                return;
            }

            // Verificar en segundo plano si hay actualizaciones disponibles (SIN mostrar UI)
            _logger?.Logger.LogInformation("🔍 Verificando actualizaciones en segundo plano...");
            
            // Ejecutar verificación en background thread para no bloquear la UI
            _ = Task.Run(async () =>
            {
                try
                {
                    // Dar tiempo para que la aplicación cargue completamente
                    await Task.Delay(3000);
                      var updateCheckResult = await updateService.CheckForUpdatesAsync();
                    
                    if (updateCheckResult.HasUpdatesAvailable && !updateCheckResult.HasAccessError)
                    {
                        _logger?.Logger.LogInformation("✅ Actualización disponible - mostrando diálogo al usuario");
                          
                        // Solo ahora mostrar el diálogo porque SÍ hay una actualización
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            await updateService.NotifyAndPromptForUpdateAsync();
                        });
                    }
                    else
                    {
                        _logger?.Logger.LogInformation("ℹ️ No hay actualizaciones disponibles - continuando con inicio normal");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Logger.LogError(ex, "❌ Error verificando actualizaciones en segundo plano");
                    // No es crítico, la aplicación continúa normalmente
                }
            });            _logger?.Logger.LogInformation("✅ Verificación de actualizaciones iniciada en segundo plano");
            
            // Completar de forma asíncrona
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error inicializando servicio de actualizaciones");
            // No es crítico, la aplicación puede continuar
        }
    }
}
