using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Service for secure database configuration management using environment variables
/// </summary>
public interface ISecureDatabaseConfigurationService
{
    /// <summary>
    /// Gets the secure connection string from environment variables
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secure connection string</returns>
    Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all required environment variables are set
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if configuration is valid</returns>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets database server from environment variables
    /// </summary>
    string GetDatabaseServer();

    /// <summary>
    /// Gets database name from environment variables
    /// </summary>
    string GetDatabaseName();    /// <summary>
    /// Checks if integrated security should be used
    /// </summary>
    bool UseIntegratedSecurity();

    /// <summary>
    /// Checks if there is a valid configuration available
    /// </summary>
    Task<bool> HasValidConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests database connection with current configuration
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
