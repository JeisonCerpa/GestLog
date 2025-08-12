using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Interfaces;
using GestLog.Models.Exceptions;

namespace GestLog.Services;

/// <summary>
/// Servicio unificado de configuración de base de datos con detección automática de entorno
/// Combina variables de entorno, archivos específicos y valores fallback
/// Sigue SRP: Solo responsable de la configuración de base de datos
/// </summary>
public class UnifiedDatabaseConfigurationService : IUnifiedDatabaseConfigurationService, IDatabaseConfigurationProvider
{
    private readonly IGestLogLogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IEnvironmentDetectionService _environmentService;

    // Environment variable names
    private const string ENV_DB_SERVER = "GESTLOG_DB_SERVER";
    private const string ENV_DB_NAME = "GESTLOG_DB_NAME";
    private const string ENV_DB_USER = "GESTLOG_DB_USER";
    private const string ENV_DB_PASSWORD = "GESTLOG_DB_PASSWORD";
    private const string ENV_DB_USE_INTEGRATED = "GESTLOG_DB_USE_INTEGRATED_SECURITY";
    private const string ENV_DB_CONNECTION_TIMEOUT = "GESTLOG_DB_CONNECTION_TIMEOUT";
    private const string ENV_DB_TRUST_CERTIFICATE = "GESTLOG_DB_TRUST_CERTIFICATE";

    public UnifiedDatabaseConfigurationService(
        IGestLogLogger logger, 
        IConfiguration configuration,
        IEnvironmentDetectionService environmentService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🔧 Construyendo cadena de conexión unificada...");

            var currentEnv = await _environmentService.DetectEnvironmentAsync(cancellationToken);
            _logger.LogInformation("📍 Entorno detectado: {Environment}", currentEnv);

            // Prioridad 1: Variables de entorno
            var config = await GetConfigFromEnvironmentVariablesAsync();
            if (config.IsComplete)
            {
                _logger.LogInformation("✅ Configuración obtenida de variables de entorno");
                return BuildConnectionString(config);
            }

            // Prioridad 2: Archivo específico del entorno
            config = await GetConfigFromEnvironmentFileAsync(currentEnv, cancellationToken);
            if (config.IsComplete)
            {
                _logger.LogInformation("✅ Configuración obtenida de archivo específico: {Environment}", currentEnv);
                return BuildConnectionString(config);
            }

            // Prioridad 3: Valores fallback de appsettings.json
            config = GetConfigFromFallbackSettings();
            if (config.IsComplete)
            {
                _logger.LogInformation("✅ Configuración obtenida de valores fallback");
                return BuildConnectionString(config);
            }

            throw new DatabaseConfigurationException(
                "No se pudo obtener una configuración válida de base de datos desde ninguna fuente",
                "UnifiedConfiguration");
        }
        catch (Exception ex) when (!(ex is DatabaseConfigurationException))
        {
            _logger.LogError(ex, "❌ Error construyendo cadena de conexión unificada");
            throw new DatabaseConfigurationException(
                "Error en configuración unificada de base de datos",
                "UnifiedConfiguration",
                ex);
        }
    }

    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = await GetConnectionStringAsync(cancellationToken);
            return !string.IsNullOrWhiteSpace(connectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error validando configuración unificada");
            return false;
        }
    }

    public async Task<bool> HasValidConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await ValidateConfigurationAsync(cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = await GetConnectionStringAsync(cancellationToken);
            
            using var connection = new System.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            _logger.LogInformation("✅ Prueba de conexión exitosa");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Prueba de conexión falló: {Message}", ex.Message);
            return false;
        }
    }

    public string GetDatabaseServer()
    {
        return GetEnvironmentVariable(ENV_DB_SERVER, 
            _configuration["Database:FallbackServer"], false) ?? "localhost";
    }

    public string GetDatabaseName()
    {
        return GetEnvironmentVariable(ENV_DB_NAME, 
            _configuration["Database:FallbackDatabase"], false) ?? "GestLog";
    }

    public bool UseIntegratedSecurity()
    {
        var value = GetEnvironmentVariable(ENV_DB_USE_INTEGRATED, 
            _configuration["Database:FallbackUseIntegratedSecurity"], false) ?? "false";
        return bool.TryParse(value, out var result) && result;
    }

    // Implementación explícita para IDatabaseConfigurationProvider
    async Task<string> IDatabaseConfigurationProvider.GetConnectionStringAsync()
    {
        return await GetConnectionStringAsync();
    }    private async Task<DatabaseConfig> GetConfigFromEnvironmentVariablesAsync()
    {
        try
        {
            await Task.CompletedTask; // Para cumplir con la signatura async
            
            var server = GetEnvironmentVariableDirectly(ENV_DB_SERVER);
            var database = GetEnvironmentVariableDirectly(ENV_DB_NAME);
            var useIntegrated = bool.TryParse(GetEnvironmentVariableDirectly(ENV_DB_USE_INTEGRATED), out var integrated) && integrated;
            
            var config = new DatabaseConfig
            {
                Server = server,
                Database = database,
                UseIntegratedSecurity = useIntegrated
            };

            if (!useIntegrated)
            {
                config.Username = GetEnvironmentVariableDirectly(ENV_DB_USER);
                config.Password = GetEnvironmentVariableDirectly(ENV_DB_PASSWORD);
            }

            config.ConnectionTimeout = int.TryParse(GetEnvironmentVariableDirectly(ENV_DB_CONNECTION_TIMEOUT), out var timeout) ? timeout : 30;
            config.TrustServerCertificate = bool.TryParse(GetEnvironmentVariableDirectly(ENV_DB_TRUST_CERTIFICATE), out var trust) ? trust : true;

            return config;        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo configuración de variables de entorno: {Message}", ex.Message);
            return new DatabaseConfig();
        }
    }

    private async Task<DatabaseConfig> GetConfigFromEnvironmentFileAsync(string environment, CancellationToken cancellationToken)
    {
        try
        {
            var envConfig = await _environmentService.GetEnvironmentConfigAsync<DatabaseFileConfig>("Database", cancellationToken);
            if (envConfig == null)
                return new DatabaseConfig();

            return new DatabaseConfig
            {
                Server = envConfig.Server,
                Database = envConfig.Database,
                Username = envConfig.Username,
                Password = envConfig.Password,
                UseIntegratedSecurity = envConfig.UseIntegratedSecurity,
                ConnectionTimeout = envConfig.ConnectionTimeout,
                TrustServerCertificate = envConfig.TrustServerCertificate
            };        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error obteniendo configuración de archivo de entorno: {Message}", ex.Message);
            return new DatabaseConfig();
        }
    }

    private DatabaseConfig GetConfigFromFallbackSettings()
    {
        return new DatabaseConfig
        {
            Server = _configuration["Database:FallbackServer"],
            Database = _configuration["Database:FallbackDatabase"],
            Username = _configuration["Database:FallbackUsername"],
            Password = _configuration["Database:FallbackPassword"],
            UseIntegratedSecurity = bool.TryParse(_configuration["Database:FallbackUseIntegratedSecurity"], out var integrated) && integrated,
            ConnectionTimeout = int.TryParse(_configuration["Database:FallbackConnectionTimeout"], out var timeout) ? timeout : 30,
            TrustServerCertificate = bool.TryParse(_configuration["Database:FallbackTrustServerCertificate"], out var trust) ? trust : true
        };
    }

    private string BuildConnectionString(DatabaseConfig config)
    {
        if (config.UseIntegratedSecurity)
        {
            return $"Data Source={config.Server};" +
                   $"Initial Catalog={config.Database};" +
                   $"Integrated Security=true;" +
                   $"Connection Timeout={config.ConnectionTimeout};" +
                   $"MultipleActiveResultSets=True;" +
                   $"TrustServerCertificate={config.TrustServerCertificate};" +
                   $"Persist Security Info=False;";
        }
        else
        {
            return $"Data Source={config.Server};" +
                   $"Initial Catalog={config.Database};" +
                   $"User Id={config.Username};" +
                   $"Password={config.Password};" +
                   $"Connection Timeout={config.ConnectionTimeout};" +
                   $"MultipleActiveResultSets=True;" +
                   $"TrustServerCertificate={config.TrustServerCertificate};" +
                   $"Persist Security Info=False;";
        }
    }

    private string? GetEnvironmentVariable(string variableName, string? defaultValue, bool isRequired)
    {
        var value = GetEnvironmentVariableDirectly(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return value;
    }

    private string? GetEnvironmentVariableDirectly(string variableName)
    {
        try
        {
            return Environment.GetEnvironmentVariable(variableName) ??
                   Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User) ??
                   Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accediendo a variable de entorno: {VariableName}", variableName);
            return null;
        }
    }

    private class DatabaseConfig
    {
        public string? Server { get; set; }
        public string? Database { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool UseIntegratedSecurity { get; set; }
        public int ConnectionTimeout { get; set; } = 30;
        public bool TrustServerCertificate { get; set; } = true;

        public bool IsComplete => 
            !string.IsNullOrWhiteSpace(Server) && 
            !string.IsNullOrWhiteSpace(Database) &&
            (UseIntegratedSecurity || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
    }

    private class DatabaseFileConfig
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseIntegratedSecurity { get; set; }
        public int ConnectionTimeout { get; set; } = 30;
        public int CommandTimeout { get; set; } = 30;
        public bool TrustServerCertificate { get; set; } = true;
    }
}
