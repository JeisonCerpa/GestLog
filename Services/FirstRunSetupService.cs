using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using GestLog.Models.Exceptions;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Services;

/// <summary>
/// Service for handling first run setup
/// Follows SRP: Only responsible for first run detection and automatic setup
/// </summary>
public class FirstRunSetupService : IFirstRunSetupService
{
    private readonly ISecureDatabaseConfigurationService _databaseConfig;
    private readonly IGestLogLogger _logger;
    private readonly IConfiguration _configuration;

    public FirstRunSetupService(
        ISecureDatabaseConfigurationService databaseConfig,
        IGestLogLogger logger,
        IConfiguration configuration)
    {
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Checks if this is the first run (no environment variables configured)
    /// </summary>
    public async Task<bool> IsFirstRunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if this is first run");
            
            var hasValidConfig = await _databaseConfig.HasValidConfigurationAsync(cancellationToken);
            
            if (!hasValidConfig)
            {
                _logger.LogInformation("First run detected - no valid configuration found");
                return true;
            }

            _logger.LogInformation("Valid configuration found - not first run");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking first run status");
            return true; // Si hay error, asumir que necesita configuración
        }
    }

    /// <summary>
    /// Configures environment variables automatically using fallback values from appsettings.json
    /// Supports both SQL Server Authentication and Windows Authentication
    /// </summary>
    public async Task ConfigureAutomaticEnvironmentVariablesAsync(CancellationToken cancellationToken = default)
    {        try
        {
            _logger.LogInformation("🔧 Iniciando configuración automática con sistema híbrido");

            // ESTRATEGIA HÍBRIDA: Intentar cargar configuración en orden de prioridad
            var (fallbackServer, fallbackDatabase, fallbackUsername, fallbackPassword, useIntegratedSecurity) = 
                await LoadProductionConfigurationAsync();
            
            _logger.LogInformation("📋 Configuración de producción cargada - Server: {Server}, Database: {Database}", 
                fallbackServer, fallbackDatabase);

            _logger.LogDebug("Using configuration values - Server: {Server}, Database: {Database}, UseIntegratedSecurity: {UseIntegratedSecurity}",
                fallbackServer!, fallbackDatabase!, useIntegratedSecurity);

            // Configurar variables de entorno
            await Task.Run(() =>
            {
                Environment.SetEnvironmentVariable("GESTLOG_DB_SERVER", fallbackServer, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_NAME", fallbackDatabase, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_USE_INTEGRATED_SECURITY", useIntegratedSecurity.ToString().ToLower(), EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_CONNECTION_TIMEOUT", "30", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_COMMAND_TIMEOUT", "300", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_TRUST_CERTIFICATE", "true", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("GESTLOG_DB_ENABLE_SSL", "true", EnvironmentVariableTarget.User);
                
                if (!useIntegratedSecurity)
                {
                    // Configurar credenciales de SQL Server
                    Environment.SetEnvironmentVariable("GESTLOG_DB_USER", fallbackUsername, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("GESTLOG_DB_PASSWORD", fallbackPassword, EnvironmentVariableTarget.User);
                }
                else
                {
                    // Para Windows Auth, limpiar cualquier credencial previa
                    Environment.SetEnvironmentVariable("GESTLOG_DB_USER", null, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable("GESTLOG_DB_PASSWORD", null, EnvironmentVariableTarget.User);
                }
            }, cancellationToken);

            _logger.LogInformation("✅ Variables de entorno configuradas automáticamente");

            // Probar la conexión con los valores configurados
            var connectionWorks = await TestAutomaticConnectionAsync(cancellationToken);
            if (!connectionWorks)
            {
                var authType = useIntegratedSecurity ? "Windows Authentication" : "SQL Server Authentication";
                _logger.LogWarning("⚠️ Conexión con {AuthType} falló", authType);
                throw new SecurityConfigurationException(
                    $"No se pudo establecer conexión con {authType}. " +
                    "Verifique que SQL Server esté corriendo y las credenciales sean correctas.",
                    "FirstRunSetup_AutoConfig");
            }

            _logger.LogInformation("✅ Configuración automática completada exitosamente");
        }
        catch (Exception ex) when (!(ex is SecurityConfigurationException))
        {
            _logger.LogError(ex, "❌ Error configurando automáticamente variables de entorno");
            throw new SecurityConfigurationException(
                "Error durante la configuración automática",
                "FirstRunSetup_Auto",
                ex);
        }
    }    /// <summary>
    /// Tests database connection using hybrid configuration strategy
    /// </summary>
    public async Task<bool> TestAutomaticConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {            _logger.LogDebug("🧪 Probando conexión automática con configuración híbrida");

            // Usar la misma estrategia híbrida para obtener configuración
            var (server, database, username, password, useIntegratedSecurity) = 
                await LoadProductionConfigurationAsync();

            // Si aún los valores están vacíos, fallar inmediatamente
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            {
                _logger.LogWarning("❌ Valores de configuración vacíos - no se puede probar conexión automática");
                return false;
            }

            if (!useIntegratedSecurity && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
            {
                _logger.LogWarning("❌ Credenciales SQL Server vacías - no se puede probar conexión automática");
                return false;
            }

            string connectionString;
            
            if (useIntegratedSecurity)
            {
                connectionString = $"Server={server};Database={database};Integrated Security=true;Connection Timeout=5;TrustServerCertificate=true;";
                _logger.LogDebug("Testing Windows Authentication connection");
            }
            else
            {
                connectionString = $"Server={server};Database={database};User Id={username};Password={password};Connection Timeout=5;TrustServerCertificate=true;";
                _logger.LogDebug("Testing SQL Server Authentication connection");
            }

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            _logger.LogInformation("✅ Prueba de conexión automática exitosa");
            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "❌ Prueba de conexión automática falló: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)        {
            _logger.LogError(ex, "❌ Error inesperado probando conexión automática");
            return false;
        }
    }

    /// <summary>
    /// Loads production configuration using hybrid strategy:
    /// 1. Try config/database-development.json file
    /// 2. Fall back to hardcoded production values
    /// 3. Last resort: appsettings.json values
    /// </summary>
    private async Task<(string server, string database, string username, string password, bool useIntegrated)> LoadProductionConfigurationAsync()
    {
        try
        {
            // PRIORIDAD 1: Intentar cargar desde archivo de configuración de producción
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "database-development.json");
            
            if (File.Exists(configPath))
            {
                _logger.LogInformation("📄 Cargando configuración desde: {ConfigPath}", configPath);
                
                var jsonContent = await File.ReadAllTextAsync(configPath);
                var prodConfig = System.Text.Json.JsonSerializer.Deserialize<ProductionConfig>(jsonContent);
                
                if (prodConfig?.Database != null && !string.IsNullOrWhiteSpace(prodConfig.Database.Server))
                {
                    _logger.LogInformation("✅ Configuración de producción cargada desde archivo");
                    return (
                        prodConfig.Database.Server,
                        prodConfig.Database.Database,
                        prodConfig.Database.Username,
                        prodConfig.Database.Password,
                        prodConfig.Database.UseIntegratedSecurity
                    );
                }
            }
            
            _logger.LogWarning("📄 Archivo de configuración no encontrado o inválido: {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Error leyendo archivo de configuración de producción");
        }

        // PRIORIDAD 2: Usar valores hardcodeados de producción (SIMICS Group)
        _logger.LogInformation("⚙️ Usando valores hardcodeados de producción para SIMICS Group");
        return (
            "SIMICSGROUPWKS1\\SIMICSBD",
            "BD_ Pruebas", 
            "sa",
            "REMOVED_SECRET",
            false
        );
    }

    /// <summary>
    /// Configuration model for production database settings
    /// </summary>
    private class ProductionConfig
    {
        public DatabaseConfig? Database { get; set; }
        
        public class DatabaseConfig
        {
            public string Server { get; set; } = string.Empty;
            public string Database { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool UseIntegratedSecurity { get; set; }
        }
    }
}