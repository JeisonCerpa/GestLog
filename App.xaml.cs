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
using GestLog.Services.Core;

namespace GestLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IGestLogLogger? _logger;    
    public IServiceProvider ServiceProvider => LoggingService.GetServiceProvider();    protected override async void OnStartup(StartupEventArgs e)
    {
        // ✅ PRIMERO: Resolver ambiente y bootstrap ANTES de cualquier otra operación
        var environment = ResolveRuntimeEnvironment();

        Environment.SetEnvironmentVariable("GESTLOG_ENVIRONMENT", environment, EnvironmentVariableTarget.Process);

        var bootstrapResult = await BootstrapProvisioningService.EnsureBootstrapAndMigrateAsync(environment, AppContext.BaseDirectory);

        // ✅ Configurar manejo global de excepciones ANTES de cualquier otra lógica
        SetupGlobalExceptionHandling();
        
        // Configurar tooltip delay a 150ms (SOLO UNA VEZ en toda la aplicación)
        ConfigureTooltipDelay();
        
        // Cargar configuración de la aplicación ANTES de cualquier acceso a configuración
        await LoadApplicationConfigurationAsync();
        
        // Mostrar SplashScreen antes de cualquier lógica
        var splash = new GestLog.Modules.Shell.Views.SplashScreen();
        splash.Show();

        base.OnStartup(e); // Llamar primero según buenas prácticas WPF
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            LoggingService.InitializeServices();
            _logger = LoggingService.GetLogger();            splash.ShowStatus("Verificando conexión a la base de datos...");

            _logger?.Logger.LogInformation("🧩 Bootstrap: {Message}. Path={Path}, Created={Created}, Updated={Updated}, EnvCreated={EnvCreated}, EnvUpdated={EnvUpdated}, Skipped={Skipped}",
                bootstrapResult.Message,
                bootstrapResult.BootstrapPath,
                bootstrapResult.BootstrapCreated,
                bootstrapResult.BootstrapUpdated,
                bootstrapResult.EnvironmentVariablesCreated,
                bootstrapResult.EnvironmentVariablesUpdated,
                bootstrapResult.Skipped);

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
            }
            else
            {
                splash.ShowStatus("Conexión a la base de datos OK");
            }

            splash.ShowStatus("Verificando sesión guardada...");
            var currentUserService = LoggingService.GetService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
            if (currentUserService != null)
            {
                currentUserService.RestoreSessionIfExists();

                if (currentUserService.IsAuthenticated)
                {
                    splash.ShowStatus("Sesión encontrada. Preparando acceso...");
                }
                else
                {
                    splash.ShowStatus("Sin sesión guardada. Mostrando acceso...");
                }
            }

            splash.ShowStatus("Verificando actualizaciones...");
            var configurationService = LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();
            var updateService = LoggingService.GetService<GestLog.Services.VelopackUpdateService>();
            if (configurationService?.Current?.Updater?.Enabled == true &&
                !string.IsNullOrWhiteSpace(configurationService.Current.Updater.UpdateServerPath) &&
                updateService != null)
            {
                var updateHandled = await updateService.NotifyAndPromptForUpdateAsync();
                if (updateHandled)
                {
                    splash.Close();
                    return;
                }
            }

            splash.ShowStatus("Cargando interfaz principal...");

            // Bloque try-catch adicional para inicialización de ventana principal y restauración de sesión
            try
            {
                var mainWindow = new MainWindow();
                this.MainWindow = mainWindow;
                mainWindow.Show();
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                splash.Close();

                _ = Task.Run(async () => await RunDeferredStartupTasksAsync());
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
    /// Ejecuta tareas no críticas después de mostrar la interfaz principal.
    /// </summary>
    private async Task RunDeferredStartupTasksAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("🚀 Iniciando tareas diferidas de arranque...");

            var envVarService = LoggingService.GetService<GestLog.Services.Core.IEnvironmentVariableService>();
            if (envVarService != null)
            {
                var syncResult = await envVarService.SyncEnvironmentVariablesAsync();
                _logger?.Logger.LogInformation("📊 Resultado diferido de sincronización: {Created} creadas, {Updated} actualizadas, {Unchanged} sin cambios, {Failed} errores",
                    syncResult.Created, syncResult.Updated, syncResult.Unchanged, syncResult.Failed);
            }

            var migrationService = LoggingService.GetService<GestLog.Services.Core.IMigrationService>();
            if (migrationService != null)
            {
                using var migrationTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var migrationTask = Task.Run(async () => await migrationService.EnsureDatabaseUpdatedAsync(), migrationTimeoutCts.Token);
                var completed = await Task.WhenAny(migrationTask, Task.Delay(TimeSpan.FromSeconds(30), migrationTimeoutCts.Token));

                if (completed == migrationTask)
                {
                    await migrationTask;
                    _logger?.Logger.LogInformation("✅ Migraciones verificadas en segundo plano");
                }
                else
                {
                    _logger?.Logger.LogWarning("⚠️ Migraciones en segundo plano excedieron el tiempo. Se continuará sin bloquear la UI.");
                }
            }

            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
            if (databaseService != null)
            {
                await InitializeDatabaseConnectionAsync();
            }

            _logger?.Logger.LogInformation("✅ Tareas diferidas de arranque completadas");
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error en tareas diferidas de arranque");
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

                // Error conocido de WPF con temas/plantillas - no crítico, no mostrar al usuario
                e.Handled = true;
                return;
            }

            errorHandler.HandleException(
                e.Exception,
                "DispatcherUnhandledException",
                showToUser: true); // Mostrar al usuario excepciones inesperadas en el hilo UI

            e.Handled = true; // Permitir que la aplicación continúe
        };

        // Excepciones no manejadas en hilos secundarios
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Unknown exception");
            errorHandler.HandleException(exception, "AppDomain.UnhandledException");

            if (e.IsTerminating)
            {
                _logger?.Logger.LogCritical(exception, "💥 La aplicación se está cerrando debido a una excepción no manejada en hilo secundario");

                // Mostrar mensaje al usuario antes del cierre forzado para que no sea silencioso
                try
                {
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        dispatcher.Invoke(() =>
                        {
                            System.Windows.MessageBox.Show(
                                $"GestLog encontró un error inesperado y debe cerrarse.\n\n" +
                                $"Error: {exception.GetBaseException().Message}\n\n" +
                                $"Revise los archivos de log en la carpeta Logs/ para más detalles.",
                                "Error Crítico - GestLog",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Error);
                        }, System.Windows.Threading.DispatcherPriority.Send);
                    }
                }
                catch { /* Si el dispatcher ya no está disponible, no se puede mostrar nada */ }

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

            // Registrar sin diálogo: una excepción no observada viene de una tarea en segundo plano
            // que ningún flujo de usuario esperaba; un MessageBox aquí nunca es accionable
            errorHandler.HandleException(e.Exception, "TaskScheduler.UnobservedTaskException", showToUser: false);
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

    private static string ResolveRuntimeEnvironment()
    {
        var launchProfile = Environment.GetEnvironmentVariable("DOTNET_LAUNCH_PROFILE");
        if (!string.IsNullOrWhiteSpace(launchProfile))
        {
            if (launchProfile.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                return "Development";
            }

            if (launchProfile.Equals("Testing", StringComparison.OrdinalIgnoreCase))
            {
                return "Testing";
            }

            if (launchProfile.Equals("Production", StringComparison.OrdinalIgnoreCase))
            {
                return "Production";
            }
        }

        var environment = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(environment))
        {
#if DEBUG
            return "Development";
#else
            return "Production";
#endif
        }

        return environment;
    }
}
