using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.IO;
using GestLog.Services.Core.Error;
using GestLog.Models.Configuration;
using GestLog.Services.Interfaces;
using GestLog.ViewModels;
using GestLog.Services;

namespace GestLog.Services.Core.Logging;

/// <summary>
/// Servicio responsable de configurar e inicializar el sistema de logging
/// </summary>
public static class LoggingService
{
    private static IServiceProvider? _serviceProvider;
    private static bool _isInitialized = false;

    /// <summary>
    /// Inicializa el sistema de logging y configuración
    /// </summary>
    public static IServiceProvider InitializeServices()
    {
        if (_isInitialized && _serviceProvider != null)
            return _serviceProvider;

        try
        {
            // Crear directorio de logs si no existe
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            // Configurar el builder de configuración
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = configurationBuilder.Build();

            // Configurar Serilog desde la configuración
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            // Registrar servicios
            var services = new ServiceCollection();
            
            // Configuración
            services.AddSingleton<IConfiguration>(configuration);
            
            // Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger);
            });            
            // Servicios custom
            services.AddSingleton<IGestLogLogger, GestLogLogger>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            services.AddSingleton<Configuration.IConfigurationService, Configuration.ConfigurationService>();
            services.AddSingleton<Security.ICredentialService, Security.WindowsCredentialService>();
            // 🔒 SERVICIOS DE SEGURIDAD Y CONFIGURACIÓN PROFESIONAL
            services.AddSingleton<IEnvironmentDetectionService, EnvironmentDetectionService>();
            services.AddSingleton<IUnifiedDatabaseConfigurationService, UnifiedDatabaseConfigurationService>();
            services.AddSingleton<IDatabaseConfigurationProvider, UnifiedDatabaseConfigurationService>();
            services.AddSingleton<SecurityStartupValidationService>();            // 🚀 SERVICIOS DE FIRST RUN SETUP
            services.AddSingleton<IFirstRunSetupService, FirstRunSetupService>();
            services.AddTransient<FirstRunSetupViewModel>();
            
            // 🔄 SERVICIO DE ACTUALIZACIONES VELOPACK
            services.AddSingleton<VelopackUpdateService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<IGestLogLogger>();
                var configService = serviceProvider.GetRequiredService<Configuration.IConfigurationService>();
                var updateServerPath = configService.Current.Updater.UpdateServerPath ?? 
                                      "\\\\SIMICSGROUPWKS1\\Hackerland\\Programas\\GestLogUpdater";
                return new VelopackUpdateService(logger, updateServerPath);
            });
            
            // Configuración de base de datos
            services.Configure<Models.Configuration.DatabaseConfiguration>(config =>
            {
                var dbSection = configuration.GetSection("Database");
                if (dbSection.Exists())
                {
                    config.Server = dbSection["Server"] ?? "";
                    config.Database = dbSection["Database"] ?? "";
                    config.UserId = dbSection["UserId"] ?? "";
                    config.Password = dbSection["Password"] ?? "";
                    config.UseIntegratedSecurity = dbSection.GetValue<bool>("IntegratedSecurity");
                    config.ConnectionTimeout = dbSection.GetValue<int>("ConnectionTimeout", 30);
                    config.CommandTimeout = dbSection.GetValue<int>("CommandTimeout", 30);
                    config.EnableSsl = dbSection.GetValue<bool>("EnableSsl", true);
                    config.TrustServerCertificate = dbSection.GetValue<bool>("TrustServerCertificate", true);
                    config.ConnectionString = dbSection["ConnectionString"] ?? "";
                }            });            // Configuración de resiliencia de base de datos
            services.Configure<DatabaseResilienceConfiguration>(options =>
            {
                configuration.GetSection("DatabaseResilience").Bind(options);
            });
            
            // Servicio de conexión a base de datos
            services.AddSingleton<Interfaces.IDatabaseConnectionService, DatabaseConnectionService>();
              // Servicios del dominio
            services.AddTransient<Modules.DaaterProccesor.Services.IResourceLoaderService, 
                Modules.DaaterProccesor.Services.ResourceLoaderService>();
            services.AddTransient<Modules.DaaterProccesor.Services.IDataConsolidationService, 
                Modules.DaaterProccesor.Services.DataConsolidationService>();
            services.AddTransient<Modules.DaaterProccesor.Services.IExcelExportService, 
                Modules.DaaterProccesor.Services.ExcelExportService>();
            services.AddTransient<Modules.DaaterProccesor.Services.IExcelProcessingService, 
                Modules.DaaterProccesor.Services.ExcelProcessingService>();
            services.AddTransient<Modules.DaaterProccesor.Services.IConsolidatedFilterService, 
                Modules.DaaterProccesor.Services.ConsolidatedFilterService>();            // Servicios de Gestión de Cartera
            services.AddTransient<Modules.GestionCartera.Services.IPdfGeneratorService, 
                Modules.GestionCartera.Services.PdfGeneratorService>();
            services.AddTransient<Modules.GestionCartera.Services.IEmailService, 
                Modules.GestionCartera.Services.EmailService>();
            services.AddTransient<Modules.GestionCartera.Services.IExcelEmailService, 
                Modules.GestionCartera.Services.ExcelEmailService>();
            
            // Servicios de Envío de Catálogo
            services.AddTransient<Modules.EnvioCatalogo.Services.IEnvioCatalogoService, 
                Modules.EnvioCatalogo.Services.EnvioCatalogoService>();            // ViewModels
            services.AddTransient<Modules.GestionCartera.ViewModels.DocumentGenerationViewModel>();
            services.AddTransient<Modules.EnvioCatalogo.ViewModels.EnvioCatalogoViewModel>();
            
            // ViewModel de DaaterProccesor con DI (incluye CurrentUserInfo)
            services.AddTransient<GestLog.Modules.DaaterProccesor.ViewModels.MainViewModel>(sp =>
            {
                var excelSvc = sp.GetRequiredService<GestLog.Modules.DaaterProccesor.Services.IExcelProcessingService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var currentUser = sp.GetRequiredService<GestLog.Modules.Usuarios.Models.Authentication.CurrentUserInfo>();
                return new GestLog.Modules.DaaterProccesor.ViewModels.MainViewModel(excelSvc, logger, currentUser);
            });

            // Servicios de Gestión de Mantenimientos
            services.AddTransient<GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService, GestLog.Modules.GestionMantenimientos.Services.EquipoService>();            services.AddTransient<GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService, GestLog.Modules.GestionMantenimientos.Services.CronogramaService>();
            services.AddTransient<GestLog.Modules.GestionMantenimientos.Interfaces.ISeguimientoService, GestLog.Modules.GestionMantenimientos.Services.SeguimientoService>();

            // ViewModels de Gestión de Mantenimientos
            services.AddSingleton<GestLog.Modules.GestionMantenimientos.ViewModels.EquiposViewModel>(sp =>
            {
                var equipoService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                var cronogramaService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService>();
                var seguimientoService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ISeguimientoService>();
                var currentUserService = sp.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                return new GestLog.Modules.GestionMantenimientos.ViewModels.EquiposViewModel(equipoService, logger, cronogramaService, seguimientoService, currentUserService, databaseService);
            });

            services.AddSingleton<GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaViewModel>(sp =>
            {
                var cronogramaService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ICronogramaService>();
                var seguimientoService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ISeguimientoService>();
                var currentUserService = sp.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                return new GestLog.Modules.GestionMantenimientos.ViewModels.CronogramaViewModel(cronogramaService, seguimientoService, currentUserService, databaseService, logger);
            });

            // SeguimientoViewModel - ✅ ACTUALIZADO: Agregadas dependencias para DatabaseAwareViewModel
            services.AddSingleton<GestLog.Modules.GestionMantenimientos.ViewModels.SeguimientoViewModel>(sp =>
            {
                var seguimientoService = sp.GetRequiredService<GestLog.Modules.GestionMantenimientos.Interfaces.ISeguimientoService>();
                var currentUserService = sp.GetRequiredService<GestLog.Modules.Usuarios.Interfaces.ICurrentUserService>();
                var databaseService = sp.GetRequiredService<GestLog.Services.Interfaces.IDatabaseConnectionService>();
                var logger = sp.GetRequiredService<IGestLogLogger>();
                return new GestLog.Modules.GestionMantenimientos.ViewModels.SeguimientoViewModel(seguimientoService, currentUserService, databaseService, logger);
            });

            // Configuración de base de datos EF Core
            GestLog.Startup.ConfigureDatabase(services, configuration);

            // --- REGISTRO DE SERVICIOS DE USUARIOS Y PERSONAS ---
            GestLog.StartupUsuariosPersonas.ConfigureUsuariosPersonasServices(services);

            _serviceProvider = services.BuildServiceProvider();
            _isInitialized = true;

            // Log inicial del sistema
            var logger = _serviceProvider.GetRequiredService<IGestLogLogger>();
            logger.Logger.LogInformation("🚀 Sistema de logging inicializado correctamente");
            logger.LogConfiguration("BaseDirectory", AppContext.BaseDirectory);
            logger.LogConfiguration("LogsDirectory", logsDirectory);

            return _serviceProvider;
        }
        catch (Exception ex)
        {
            // Fallback logging en caso de error en la configuración
            Console.WriteLine($"❌ Error inicializando el sistema de logging: {ex.Message}");
            
            // Configuración mínima de emergencia
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("Logs/emergency-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            var emergencyServices = new ServiceCollection();
            emergencyServices.AddLogging(builder => builder.AddSerilog(Log.Logger));
            emergencyServices.AddSingleton<IGestLogLogger, GestLogLogger>();
            
            _serviceProvider = emergencyServices.BuildServiceProvider();
            _isInitialized = true;

            var emergencyLogger = _serviceProvider.GetRequiredService<IGestLogLogger>();
            emergencyLogger.Logger.LogWarning("⚠️ Sistema de logging inicializado en modo de emergencia");
            emergencyLogger.LogUnhandledException(ex, "LoggingService.InitializeServices");

            return _serviceProvider;
        }
    }

    /// <summary>
    /// Obtiene el proveedor de servicios global
    /// </summary>
    public static IServiceProvider GetServiceProvider()
    {
        return _serviceProvider ?? InitializeServices();
    }

    /// <summary>
    /// Obtiene un servicio específico
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        return GetServiceProvider().GetRequiredService<T>();
    }    /// <summary>
    /// Obtiene el logger principal
    /// </summary>
    public static IGestLogLogger GetLogger()
    {
        return GetService<IGestLogLogger>();
    }

    /// <summary>
    /// Obtiene el logger para una clase específica
    /// </summary>
    public static IGestLogLogger GetLogger<T>()
    {
        return GetService<IGestLogLogger>();
    }
    
    /// <summary>
    /// Obtiene el servicio de manejo de errores
    /// </summary>
    public static IErrorHandlingService GetErrorHandler()
    {
        return GetService<IErrorHandlingService>();
    }

    /// <summary>
    /// Limpia y cierra el sistema de logging
    /// </summary>
    public static void Shutdown()
    {
        try
        {
            if (_serviceProvider != null)
            {
                var logger = _serviceProvider.GetService<IGestLogLogger>();
                logger?.Logger.LogInformation("🛑 Cerrando sistema de logging");
                
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error durante el cierre del sistema de logging: {ex.Message}");
        }
        finally
        {
            Log.CloseAndFlush();
            _serviceProvider = null;
            _isInitialized = false;
        }
    }
}
