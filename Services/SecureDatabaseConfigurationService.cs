using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Exceptions;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GestLog.Services;

/// <summary>
/// Service for secure database configuration management using environment variables
/// Follows SRP: Only responsible for database configuration security
/// </summary>
public class SecureDatabaseConfigurationService : ISecureDatabaseConfigurationService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfiguration _configuration;

    // Environment variable names
    private const string ENV_DB_SERVER = "GESTLOG_DB_SERVER";
    private const string ENV_DB_NAME = "GESTLOG_DB_NAME";
    private const string ENV_DB_USER = "GESTLOG_DB_USER";
    private const string ENV_DB_PASSWORD = "GESTLOG_DB_PASSWORD";
    private const string ENV_DB_USE_INTEGRATED = "GESTLOG_DB_USE_INTEGRATED_SECURITY";
    private const string ENV_DB_CONNECTION_TIMEOUT = "GESTLOG_DB_CONNECTION_TIMEOUT";
    private const string ENV_DB_COMMAND_TIMEOUT = "GESTLOG_DB_COMMAND_TIMEOUT";
    private const string ENV_DB_TRUST_CERTIFICATE = "GESTLOG_DB_TRUST_CERTIFICATE";

    public SecureDatabaseConfigurationService(IGestLogLogger logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Building secure connection string from environment variables");

            await ValidateConfigurationAsync(cancellationToken);

            var server = GetDatabaseServer();
            var database = GetDatabaseName();
            var useIntegratedSecurity = UseIntegratedSecurity();
            var connectionTimeout = GetConnectionTimeout();
            var trustCertificate = GetTrustServerCertificate();

            string connectionString;

            if (useIntegratedSecurity)
            {
                connectionString = $"Data Source={server};" +
                                 $"Initial Catalog={database};" +
                                 $"Integrated Security=true;" +
                                 $"Connection Timeout={connectionTimeout};" +
                                 $"MultipleActiveResultSets=True;" +
                                 $"TrustServerCertificate={trustCertificate};" +
                                 $"Persist Security Info=False;";

                _logger.LogInformation("Connection string built using integrated security");
            }            else
            {
                var fallbackUsername = _configuration["Database:FallbackUsername"] ?? "";
                var fallbackPassword = _configuration["Database:FallbackPassword"] ?? "";
                
                var username = GetEnvironmentVariable(ENV_DB_USER, defaultValue: fallbackUsername, isRequired: false);
                var password = GetEnvironmentVariable(ENV_DB_PASSWORD, defaultValue: fallbackPassword, isRequired: false);

                connectionString = $"Data Source={server};" +
                                 $"Initial Catalog={database};" +
                                 $"User Id={username};" +
                                 $"Password={password};" +
                                 $"Connection Timeout={connectionTimeout};" +
                                 $"MultipleActiveResultSets=True;" +
                                 $"TrustServerCertificate={trustCertificate};" +
                                 $"Persist Security Info=True;";

                _logger.LogInformation("Connection string built using SQL Server authentication");
            }

            return connectionString;
        }        catch (Exception ex) when (!(ex is DatabaseConfigurationException))
        {
            _logger.LogError(ex, "Unexpected error building connection string");
            throw new DatabaseConfigurationException(
                "Error configurando conexión a base de datos", 
                "ConnectionString", 
                ex);
        }
    }    public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating database security configuration");

            // Simular operación async para cumplir con la signatura
            await Task.CompletedTask;

            // Check required environment variables using fallback values
            var server = GetDatabaseServer();
            var database = GetDatabaseName();
            var useIntegratedSecurity = UseIntegratedSecurity();

            if (!useIntegratedSecurity)
            {
                var fallbackUsername = _configuration["Database:FallbackUsername"] ?? "";
                var fallbackPassword = _configuration["Database:FallbackPassword"] ?? "";
                
                var username = GetEnvironmentVariable(ENV_DB_USER, defaultValue: fallbackUsername, isRequired: false);
                var password = GetEnvironmentVariable(ENV_DB_PASSWORD, defaultValue: fallbackPassword, isRequired: false);

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new SecurityConfigurationException(
                        "La contraseña de base de datos no puede estar vacía", 
                        "DatabasePassword");
                }

                if (password.Length < 8)
                {
                    _logger.LogWarning("Database password is shorter than recommended minimum length");
                }
            }

            _logger.LogInformation("Database security configuration validated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database security configuration validation failed");
            throw;
        }
    }    public async Task<bool> HasValidConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking if valid configuration exists");

            await Task.CompletedTask; // Para cumplir con la signatura async

            // Verificar variables básicas requeridas SIN usar valores fallback
            var serverFromEnv = GetEnvironmentVariableDirectly(ENV_DB_SERVER);
            var databaseFromEnv = GetEnvironmentVariableDirectly(ENV_DB_NAME);

            _logger.LogDebug("Environment variables check - Server: '{Server}', Database: '{Database}'", 
                serverFromEnv ?? "NULL", databaseFromEnv ?? "NULL");

            if (string.IsNullOrWhiteSpace(serverFromEnv) || string.IsNullOrWhiteSpace(databaseFromEnv))
            {
                _logger.LogDebug("Basic configuration variables not found in environment - Server: '{Server}', Database: '{Database}'", 
                    serverFromEnv ?? "NULL", databaseFromEnv ?? "NULL");
                return false;
            }

            // Si no usa seguridad integrada, verificar credenciales
            var useIntegratedFromEnv = GetEnvironmentVariableDirectly(ENV_DB_USE_INTEGRATED);
            var useIntegratedSecurity = bool.TryParse(useIntegratedFromEnv, out var result) && result;
            
            _logger.LogDebug("Integrated security from environment: '{Value}', Parsed: {UseIntegrated}", 
                useIntegratedFromEnv ?? "NULL", useIntegratedSecurity);
            
            if (!useIntegratedSecurity)
            {
                var usernameFromEnv = GetEnvironmentVariableDirectly(ENV_DB_USER);
                var passwordFromEnv = GetEnvironmentVariableDirectly(ENV_DB_PASSWORD);

                _logger.LogDebug("Credentials from environment - Username: '{Username}', Password: '{Password}'", 
                    usernameFromEnv ?? "NULL", 
                    string.IsNullOrWhiteSpace(passwordFromEnv) ? "NULL" : "***MASKED***");

                if (string.IsNullOrWhiteSpace(usernameFromEnv) || string.IsNullOrWhiteSpace(passwordFromEnv))
                {
                    _logger.LogDebug("Database credentials not found in environment - Username: '{Username}', Password: '{Password}'", 
                        usernameFromEnv ?? "NULL", 
                        string.IsNullOrWhiteSpace(passwordFromEnv) ? "NULL" : "***MASKED***");
                    return false;
                }
            }

            _logger.LogDebug("Valid configuration found in environment variables");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration validity");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing database connection with current configuration");

            var connectionString = await GetConnectionStringAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Connection string is empty or null");
                return false;
            }

            using var connection = new System.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            _logger.LogDebug("Database connection test successful");
            return true;
        }
        catch (System.Data.SqlClient.SqlException ex)
        {
            _logger.LogWarning(ex, "Database connection test failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing database connection");
            return false;
        }
    }    public string GetDatabaseServer()
    {
        var fallback = _configuration["Database:FallbackServer"] ?? "";
        return GetEnvironmentVariable(ENV_DB_SERVER, defaultValue: fallback, isRequired: false);
    }

    public string GetDatabaseName()
    {
        var fallback = _configuration["Database:FallbackDatabase"] ?? "";
        return GetEnvironmentVariable(ENV_DB_NAME, defaultValue: fallback, isRequired: false);
    }

    public bool UseIntegratedSecurity()
    {
        var fallback = _configuration["Database:FallbackUseIntegratedSecurity"] ?? "false";
        var value = GetEnvironmentVariable(ENV_DB_USE_INTEGRATED, defaultValue: fallback, isRequired: false);
        return bool.TryParse(value, out var result) && result;
    }

    private int GetConnectionTimeout()
    {
        var fallback = _configuration["Database:FallbackConnectionTimeout"] ?? "30";
        var value = GetEnvironmentVariable(ENV_DB_CONNECTION_TIMEOUT, defaultValue: fallback, isRequired: false);
        return int.TryParse(value, out var result) ? result : 30;
    }

    private bool GetTrustServerCertificate()
    {
        var fallback = _configuration["Database:FallbackTrustServerCertificate"] ?? "true";
        var value = GetEnvironmentVariable(ENV_DB_TRUST_CERTIFICATE, defaultValue: fallback, isRequired: false);
        return bool.TryParse(value, out var result) ? result : true;
    }private string GetEnvironmentVariable(string variableName, string? defaultValue = null, bool isRequired = true)
    {
        try
        {
            // First try process environment variables
            var value = Environment.GetEnvironmentVariable(variableName);
            
            // If not found in process, try user environment variables (system registry)
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
            }
            
            // If still not found, try machine environment variables
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                if (isRequired)
                {
                    throw new EnvironmentVariableException(
                        $"La variable de entorno '{variableName}' es requerida pero no está configurada",
                        variableName,
                        isRequired);
                }

                if (!string.IsNullOrEmpty(defaultValue))
                {
                    _logger.LogDebug("Using default value for environment variable: {VariableName}", variableName);
                    return defaultValue;
                }

                throw new EnvironmentVariableException(
                    $"La variable de entorno '{variableName}' no está configurada y no tiene valor por defecto",
                    variableName,
                    isRequired);
            }

            return value;
        }
        catch (Exception ex) when (!(ex is EnvironmentVariableException))
        {
            _logger.LogError(ex, "Error accessing environment variable: {VariableName}", variableName);
            throw new EnvironmentVariableException(
                $"Error accediendo a la variable de entorno '{variableName}'",
                variableName,
                isRequired,
                ex);
        }
    }

    private string? GetEnvironmentVariableDirectly(string variableName)
    {
        try
        {
            // First try process environment variables
            var value = Environment.GetEnvironmentVariable(variableName);
            
            // If not found in process, try user environment variables (system registry)
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.User);
            }
            
            // If still not found, try machine environment variables
            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accessing environment variable directly: {VariableName}", variableName);
            return null;
        }
    }
}
