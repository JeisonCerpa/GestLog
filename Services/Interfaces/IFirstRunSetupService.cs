using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Interface for handling first run setup operations
/// </summary>
public interface IFirstRunSetupService
{
    /// <summary>
    /// Checks if this is the first run (no environment variables configured)
    /// </summary>
    Task<bool> IsFirstRunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures environment variables automatically using fallback values from appsettings.json
    /// Supports both SQL Server Authentication and Windows Authentication
    /// </summary>
    Task ConfigureAutomaticEnvironmentVariablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests database connection using fallback credentials from appsettings.json
    /// </summary>
    Task<bool> TestAutomaticConnectionAsync(CancellationToken cancellationToken = default);
}