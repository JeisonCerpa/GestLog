using System.Configuration;
using System.Data;
using System.Windows;
using GestLog.Services.Core.Logging;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using GestLog.Services;
using GestLog.Services.Interfaces;
using System.Threading;
using System.Net.Sockets;

namespace GestLog;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IGestLogLogger? _logger;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Inicializar el sistema de logging y servicios
            LoggingService.InitializeServices();
            _logger = LoggingService.GetLogger();
            
            _logger.Logger.LogInformation("🚀 Aplicación GestLog iniciada");
            _logger.LogConfiguration("Version", "1.0.0");
            _logger.LogConfiguration("Environment", Environment.OSVersion.ToString());
            _logger.LogConfiguration("WorkingDirectory", Environment.CurrentDirectory);
            
            // CORRECCIÓN: Cargar configuración automáticamente al inicio
            await LoadApplicationConfigurationAsync();
            
            // 🔒 VALIDAR SEGURIDAD AL STARTUP
            await ValidateSecurityConfigurationAsync();

            // 🚀 VERIFICAR FIRST RUN SETUP
            await CheckFirstRunSetupAsync();

            // Inicializar conexión a base de datos automáticamente
            await InitializeDatabaseConnectionAsync();

            // Configurar manejo global de excepciones
            SetupGlobalExceptionHandling();
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
    }    /// <summary>
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
        var errorHandler = LoggingService.GetErrorHandler();

        // Excepciones no manejadas en el hilo principal (UI)
        DispatcherUnhandledException += (sender, e) =>
        {
            errorHandler.HandleException(
                e.Exception, 
                "DispatcherUnhandledException",
                showToUser: true);
            
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
        };        // Excepciones no observadas en Tasks
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
}
