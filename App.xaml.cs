using System.Configuration;
using System.Data;
using System.Windows;
using GestLog.Services.Core.Logging;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

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
            _logger.LogConfiguration("WorkingDirectory", Environment.CurrentDirectory);            // CORRECCIÓN: Cargar configuración automáticamente al inicio
            await LoadApplicationConfigurationAsync();

            // Inicializar conexión a base de datos automáticamente
            await InitializeDatabaseConnectionAsync();

            // Configurar manejo global de excepciones
            SetupGlobalExceptionHandling();
        }
        catch (Exception ex)        {
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
            {                // Si ni siquiera el logging de emergencia funciona, salir
                System.Windows.Application.Current.Shutdown(1);
                return;
            }        }
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
    }    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.Logger.LogInformation("🛑 Aplicación GestLog cerrándose");
            
            // Detener servicio de base de datos de forma síncrona
            try
            {
                var databaseService = LoggingService.GetService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                // Usar GetAwaiter().GetResult() para llamada síncrona
                databaseService.StopAsync().GetAwaiter().GetResult();
                _logger?.Logger.LogInformation("💾 Servicio de base de datos detenido");
            }
            catch (Exception dbEx)
            {
                _logger?.Logger.LogWarning(dbEx, "Error al detener servicio de base de datos");
            }
            
            LoggingService.Shutdown();
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
    }    private void SetupGlobalExceptionHandling()
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
        };

        // Excepciones no observadas en Tasks
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
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
}

