using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Exceptions;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Services;

/// <summary>
/// Service for validating security configuration at application startup
/// Follows SRP: Only responsible for security validation at startup
/// </summary>
public class SecurityStartupValidationService
{
    private readonly ISecureDatabaseConfigurationService _databaseConfig;
    private readonly IGestLogLogger _logger;

    public SecurityStartupValidationService(
        ISecureDatabaseConfigurationService databaseConfig,
        IGestLogLogger logger)
    {
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates all security configurations at application startup
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all security validations pass</returns>
    public async Task<bool> ValidateAllSecurityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting security configuration validation");

            // Validate database configuration
            var databaseValid = await ValidateDatabaseSecurityAsync(cancellationToken);            if (!databaseValid)
            {
                _logger.LogWarning("Database security validation failed");
                return false;
            }

            // Test database connection
            var connectionValid = await TestDatabaseConnectionAsync(cancellationToken);
            
            if (!connectionValid)
            {
                _logger.LogWarning("Database connection test failed");
                return false;
            }

            _logger.LogInformation("All security validations completed successfully");
            return true;
        }        catch (Exception ex)
        {
            _logger.LogError(ex, "Security validation failed with unexpected error");
            return false;
        }
    }

    /// <summary>
    /// Validates database security configuration
    /// </summary>
    private async Task<bool> ValidateDatabaseSecurityAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Validating database security configuration");
            
            var isValid = await _databaseConfig.ValidateConfigurationAsync(cancellationToken);
            
            if (isValid)
            {
                _logger.LogInformation("Database security configuration is valid");
                
                // Log configuration details (without sensitive data)
                var server = _databaseConfig.GetDatabaseServer();
                var database = _databaseConfig.GetDatabaseName();
                var useIntegratedSecurity = _databaseConfig.UseIntegratedSecurity();
                
                _logger.LogInformation("Database configuration - Server: {Server}, Database: {Database}, IntegratedSecurity: {IntegratedSecurity}", 
                    server, database, useIntegratedSecurity);
            }
            
            return isValid;
        }
        catch (DatabaseConfigurationException ex)
        {
            _logger.LogError(ex, "Database configuration error: {Message}", ex.Message);
            return false;
        }
        catch (EnvironmentVariableException ex)
        {
            _logger.LogError(ex, "Environment variable error: {Message}", ex.Message);
            return false;
        }
        catch (SecurityConfigurationException ex)
        {
            _logger.LogError(ex, "Security configuration error: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during database security validation");
            return false;
        }
    }    /// <summary>
    /// Tests database connection using secure configuration
    /// </summary>
    private async Task<bool> TestDatabaseConnectionAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Testing database connection with secure configuration");
            
            // Debug: Log configuration details before building connection string
            var server = _databaseConfig.GetDatabaseServer();
            var database = _databaseConfig.GetDatabaseName();
            var useIntegratedSecurity = _databaseConfig.UseIntegratedSecurity();
            
            _logger.LogDebug("Security validation - Server: {Server}, Database: {Database}, IntegratedSecurity: {IntegratedSecurity}", 
                server, database, useIntegratedSecurity);
            
            var connectionString = await _databaseConfig.GetConnectionStringAsync(cancellationToken);
            
            // Here you would test the actual connection
            // For now, we'll just validate that we can build the connection string
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogWarning("Connection string is empty or null in security validation");
                _logger.LogDebug("Security validation failed - empty connection string");
                return false;
            }

            _logger.LogInformation("Database connection string built successfully in security validation");
            _logger.LogDebug("Security validation passed - connection string length: {Length}", connectionString.Length);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed in security validation: {Message}", ex.Message);
            return false;
        }
    }/// <summary>
    /// Shows security configuration guidance to user
    /// </summary>
    public async Task ShowSecurityGuidanceAsync()
    {
        try
        {
            _logger.LogInformation("Showing security configuration guidance");
            
            // Simular operación async para cumplir con la signatura
            await Task.CompletedTask;
            
            var message = "Para configurar las credenciales de base de datos de forma segura:\n\n" +
                         "1. Ejecute el script: config\\setup-environment-variables.ps1\n" +
                         "2. Reinicie Visual Studio Code\n" +
                         "3. Las credenciales estarán protegidas en variables de entorno\n\n" +
                         "Variables requeridas:\n" +
                         "- GESTLOG_DB_SERVER\n" +
                         "- GESTLOG_DB_NAME\n" +
                         "- GESTLOG_DB_USER\n" +
                         "- GESTLOG_DB_PASSWORD";

            // This would typically show a message box or dialog
            // For now, we'll just log the guidance
            _logger.LogInformation("Security guidance: {Guidance}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing security guidance");
        }
    }
}
