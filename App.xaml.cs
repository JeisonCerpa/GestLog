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
using Velopack;

namespace GestLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IGestLogLogger? _logger;    
    public IServiceProvider ServiceProvider => LoggingService.GetServiceProvider();    protected override async void OnStartup(StartupEventArgs e)
    {
        // ✅ PRIMERO: Inicializar Velopack ANTES de cualquier otra operación
#if !DEBUG
        VelopackApp.Build().Run();
#endif

        // Configurar manejo global de excepciones ANTES de cualquier otra lógica
        SetupGlobalExceptionHandling();
        
        // Cargar configuración de la aplicación ANTES de cualquier acceso a configuración
        await LoadApplicationConfigurationAsync();
        
        // Mostrar SplashScreen antes de cualquier lógica
        var splash = new GestLog.Views.SplashScreen();
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
                hayActualizacion = await updateService.CheckForUpdatesAsync();
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
                splash = new GestLog.Views.SplashScreen();
                splash.Show();
                await System.Threading.Tasks.Task.Delay(300);
            }
            else
            {
                splash.ShowStatus("No hay actualizaciones");
                await System.Threading.Tasks.Task.Delay(500);
            }

            // Inicializar conexión a base de datos con monitoreo automático
            splash.ShowStatus("Inicializando servicio de base de datos...");

            // Crear un CTS con timeout para no bloquear indefinidamente el splash
            using (var dbInitCts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                // Reutilizar la variable `databaseService` ya declarada más arriba
                databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();

                EventHandler<GestLog.Models.Events.DatabaseConnectionStateChangedEventArgs>? localDbStateHandler = null;

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
    /// Verifica si es necesario ejecutar el First Run Setup
    /// </summary>
    private async Task CheckFirstRunSetupAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("🚀 Verificando necesidad de First Run Setup...");

            // Obtener el servicio de First Run Setup
            var firstRunSetupService = LoggingService.GetService<IFirstRunSetupService>();

            // Verificar si es la primera ejecución
            var isFirstRun = await firstRunSetupService.IsFirstRunAsync();

            if (isFirstRun)
            {
                _logger?.Logger.LogInformation("🔧 Primera ejecución detectada, configurando automáticamente...");

                // Configurar automáticamente usando valores de appsettings.json
                await firstRunSetupService.ConfigureAutomaticEnvironmentVariablesAsync();

                _logger?.Logger.LogInformation("✅ First Run Setup automático completado exitosamente");
            }
            else
            {
                _logger?.Logger.LogInformation("✅ Configuración existente encontrada, omitiendo First Run Setup");
            }
        }
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error durante la verificación del First Run Setup");

            // Mostrar error al usuario pero no cerrar la aplicación
            System.Windows.MessageBox.Show(
                $"Error durante la configuración automática de base de datos:\n{ex.Message}\n\n" +
                "La aplicación continuará pero es posible que tenga problemas de conectividad.\n" +
                "Verifique que SQL Server esté corriendo y revise los logs para más detalles.",
                "Error de Configuración Automática",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    /// Muestra el dialog de First Run Setup
    /// </summary>
    /// <returns>True si el setup se completó exitosamente, False si se canceló</returns>
    private bool ShowFirstRunSetup()
    {
        try
        {
            // Crear el dialog usando el factory method
            var setupDialog = Views.FirstRunSetupDialog.Create(LoggingService.GetServiceProvider());

            // Mostrar el dialog como modal
            var result = setupDialog.ShowDialog();

            return result == true;
        }        
        catch (Exception ex)
        {
            _logger?.Logger.LogError(ex, "❌ Error al mostrar First Run Setup Dialog");
            return false;
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
                    
                    var hasUpdate = await updateService.CheckForUpdatesAsync();
                    
                    if (hasUpdate)
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
