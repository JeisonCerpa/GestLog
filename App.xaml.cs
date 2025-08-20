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
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Inicializar Velopack al arranque
        VelopackApp.Build().Run();
          // AUTO-ELEVACIÓN DESHABILITADA: Ya no manejamos parámetros de actualización
        // La verificación de actualizaciones se hace manualmente
        
        // --- SOLUCIÓN: Evitar cierre automático al cerrar LoginWindow ---
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        try
        {
            // Inicializar el sistema de logging y servicios
            LoggingService.InitializeServices();
            _logger = LoggingService.GetLogger();

            _logger.Logger.LogInformation("🚀 === INICIANDO GESTLOG v1.0.0 ===");
            await LoadApplicationConfigurationAsync();
            _logger.LogConfiguration("Version", "1.0.0");
            _logger.LogConfiguration("Environment", Environment.OSVersion.ToString());
            _logger.LogConfiguration("WorkingDirectory", Environment.CurrentDirectory);
            await ValidateSecurityConfigurationAsync();
            await CheckFirstRunSetupAsync();
            await InitializeDatabaseConnectionAsync();            // --- VERIFICACIÓN DE ACTUALIZACIONES (Sin auto-elevación) ---
            var env = Environment.GetEnvironmentVariable("GESTLOG_ENVIRONMENT");
            _logger?.Logger.LogInformation($"🔍 Environment detectado: '{env ?? "null"}'");
            
            // Inicializar verificación de actualizaciones de forma silenciosa y mostrar diálogo solo si hay actualizaciones
            await InitializeUpdateServiceAsync();
            // --- FIN VERIFICACIÓN DE ACTUALIZACIONES ---

            // Asegurar cronogramas completos para todos los equipos activos
            try
            {
                var cronogramaService = LoggingService.GetService<GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService>();
                if (cronogramaService != null)
                {
                    _logger?.Logger.LogInformation("⏳ Verificando y generando cronogramas de mantenimiento al inicio...");
                    await cronogramaService.EnsureAllCronogramasUpToDateAsync();
                    _logger?.Logger.LogInformation("✅ Cronogramas de mantenimiento verificados/generados correctamente al inicio");
                }
            }
            catch (Exception cronEx)
            {
                _logger?.Logger.LogError(cronEx, "❌ Error al verificar/generar cronogramas de mantenimiento al inicio");
                // No es crítico, la aplicación puede continuar
            }            
            // Configurar manejo global de excepciones
            SetupGlobalExceptionHandling();            
            // 🔐 MOSTRAR LOGIN ANTES DEL MAINWINDOW
            // if (!ShowAuthentication())
            // {
            //     _logger?.Logger.LogInformation("🚪 Usuario canceló login, cerrando aplicación");
            //     System.Windows.Application.Current.Shutdown(0);
            //     return;
            // }
            // --- CREAR MainWindow MANUALMENTE DESPUÉS DE AUTENTICACIÓN EXITOSA ---
            // Restaurar sesión si existe
            var currentUserService = LoggingService.GetService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>() as GestLog.Modules.Usuarios.Services.CurrentUserService;
            currentUserService?.RestoreSessionIfExists();

            // Crear ventana principal
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;

            // Actualizar estado de autenticación en el ViewModel principal
            string nombrePersona = currentUserService?.Current?.FullName ?? string.Empty;
            if (mainWindow.DataContext is GestLog.ViewModels.MainWindowViewModel vm)
            {
                vm.SetAuthenticated(currentUserService?.IsAuthenticated ?? false, nombrePersona);
                // Notificar cambio de usuario restaurado para actualizar el binding del nombre
                vm.NotificarCambioNombrePersona();
            }

            // Mostrar vista principal si está autenticado
            if (currentUserService?.IsAuthenticated == true)
            {
                mainWindow.LoadHomeView();
            }

            mainWindow.Show();
            // --- Restaurar modo de cierre automático después de mostrar MainWindow ---
            this.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            // Manejo de emergencia si falla la inicialización del logging
            System.Windows.MessageBox.Show($"Error crítico al inicializar la aplicación:\n{ex.Message}",
                "Error de Inicialización", MessageBoxButton.OK, MessageBoxImage.Error);

            // Intentar logging de emergencia
            try
            {
                LoggingService.InitializeServices();
                _logger = LoggingService.GetLogger();
                _logger.LogUnhandledException(ex, "App.OnStartup");
            }
            catch
            {
                // Si ni siquiera el logging de emergencia funciona, salir
                System.Windows.Application.Current.Shutdown(1);
                return;
            }
        }
    }    /// <summary>
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
    private async Task InitializeDatabaseConnectionAsync()
    {
        try
        {
            _logger?.Logger.LogInformation("💾 Inicializando conexión a base de datos...");

            // Obtener el servicio de base de datos
            var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();

            // Iniciar el servicio con monitoreo automático
            await databaseService.StartAsync();

            // Suscribirse a cambios de estado para logging
            databaseService.ConnectionStateChanged += OnDatabaseConnectionStateChanged;

            _logger?.Logger.LogInformation("✅ Servicio de base de datos inicializado");
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
            _logger?.Logger.LogInformation("🔍 Iniciando verificación silenciosa de actualizaciones...");

            // Obtener el servicio de configuración para verificar si las actualizaciones están habilitadas
            var configurationService = LoggingService.GetService<GestLog.Services.Configuration.IConfigurationService>();
            var config = configurationService?.Current;

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
