using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GestLog.Services.Core;

public sealed class BootstrapProvisioningResult
{
    public bool Skipped { get; set; }
    public bool BootstrapCreated { get; set; }
    public bool BootstrapUpdated { get; set; }
    public int EnvironmentVariablesCreated { get; set; }
    public int EnvironmentVariablesUpdated { get; set; }
    public string BootstrapPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

internal sealed class BootstrapState
{
    public string ConfigVersion { get; set; } = "1.0.0";
    public bool Provisioned { get; set; }
    public string Environment { get; set; } = "Production";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class BootstrapProvisioningService
{
    private const string BootstrapFileName = "gestlog.bootstrap.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly string[] ManagedVariables =
    {
        "GESTLOG_ENVIRONMENT",
        "GESTLOG_DB_SERVER",
        "GESTLOG_DB_NAME",
        "GESTLOG_DB_USER",
        "GESTLOG_DB_PASSWORD",
        "GESTLOG_DB_INTEGRATED_SECURITY",
        "GESTLOG_SMTP_SERVER",
        "GESTLOG_SMTP_PORT",
        "GESTLOG_SENDER_EMAIL",
        "GESTLOG_EMAIL_USERNAME",
        "GESTLOG_EMAIL_PASSWORD",
        "GESTLOG_PASSWORD_RESET_EMAIL_USERNAME",
        "GESTLOG_PASSWORD_RESET_EMAIL_PASSWORD"
    };

    public static async Task<BootstrapProvisioningResult> EnsureBootstrapAndMigrateAsync(string environment, string appBaseDirectory)
    {
        var result = new BootstrapProvisioningResult();

        if (!environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            result.Skipped = true;
            result.Message = "Bootstrap omitido: entorno no es Production.";
            return result;
        }

        try
        {
            var bootstrapPath = ResolveBootstrapPath(appBaseDirectory);
            result.BootstrapPath = bootstrapPath;

            var bootstrapState = await LoadBootstrapAsync(bootstrapPath) ?? new BootstrapState
            {
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            var previousSnapshot = JsonSerializer.Serialize(bootstrapState, JsonOptions);
            var discoveredValues = ReadCandidateValues(appBaseDirectory);

            foreach (var variable in ManagedVariables)
            {
                if (discoveredValues.TryGetValue(variable, out var value) && !string.IsNullOrWhiteSpace(value) && !IsPlaceholderSecret(value))
                {
                    bootstrapState.Variables[variable] = value;
                }
            }

            bootstrapState.Environment = "Production";
            bootstrapState.ConfigVersion = "1.0.0";
            bootstrapState.Provisioned = HasMinimumDatabaseConfiguration(bootstrapState.Variables);
            bootstrapState.UpdatedAtUtc = DateTimeOffset.UtcNow;

            var currentSnapshot = JsonSerializer.Serialize(bootstrapState, JsonOptions);
            var bootstrapExisted = File.Exists(bootstrapPath);
            var hasChanges = !string.Equals(previousSnapshot, currentSnapshot, StringComparison.Ordinal);

            if (!bootstrapExisted)
            {
                await SaveBootstrapAsync(bootstrapPath, bootstrapState);
                result.BootstrapCreated = true;
            }
            else if (hasChanges)
            {
                await SaveBootstrapAsync(bootstrapPath, bootstrapState);
                result.BootstrapUpdated = true;
            }

            foreach (var variable in ManagedVariables)
            {
                if (!bootstrapState.Variables.TryGetValue(variable, out var expectedValue) || string.IsNullOrWhiteSpace(expectedValue))
                {
                    continue;
                }

                var currentValue = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    Environment.SetEnvironmentVariable(variable, expectedValue, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable(variable, expectedValue, EnvironmentVariableTarget.Process);
                    result.EnvironmentVariablesCreated++;
                }
                else if (!string.Equals(currentValue, expectedValue, StringComparison.Ordinal))
                {
                    Environment.SetEnvironmentVariable(variable, expectedValue, EnvironmentVariableTarget.User);
                    Environment.SetEnvironmentVariable(variable, expectedValue, EnvironmentVariableTarget.Process);
                    result.EnvironmentVariablesUpdated++;
                }
            }

            result.Message = "Bootstrap validado y variables migradas.";
            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Error en bootstrap: {ex.Message}";
            return result;
        }
    }

    public static IReadOnlyDictionary<string, string> LoadBootstrapVariables(string appBaseDirectory, out string bootstrapPath)
    {
        bootstrapPath = ResolveBootstrapPath(appBaseDirectory);

        if (!File.Exists(bootstrapPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(bootstrapPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var state = JsonSerializer.Deserialize<BootstrapState>(json, JsonOptions);
            if (state?.Variables == null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return new Dictionary<string, string>(state.Variables, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static async Task<BootstrapState?> LoadBootstrapAsync(string bootstrapPath)
    {
        if (!File.Exists(bootstrapPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(bootstrapPath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<BootstrapState>(json, JsonOptions);
    }

    private static async Task SaveBootstrapAsync(string bootstrapPath, BootstrapState bootstrapState)
    {
        var directory = Path.GetDirectoryName(bootstrapPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(bootstrapState, JsonOptions);
        await File.WriteAllTextAsync(bootstrapPath, json);
    }

    private static Dictionary<string, string> ReadCandidateValues(string appBaseDirectory)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var variable in ManagedVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                values[variable] = value;
            }
        }

        values["GESTLOG_ENVIRONMENT"] = "Production";

        var databaseConfigPath = Path.Combine(appBaseDirectory, "config", "database-production.json");
        if (!File.Exists(databaseConfigPath))
        {
            databaseConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "config", "database-production.json");
        }

        if (File.Exists(databaseConfigPath))
        {
            using var dbJson = JsonDocument.Parse(File.ReadAllText(databaseConfigPath));
            if (dbJson.RootElement.TryGetProperty("Database", out var dbSection))
            {
                SetIfPresent(dbSection, "Server", values, "GESTLOG_DB_SERVER");
                SetIfPresent(dbSection, "Database", values, "GESTLOG_DB_NAME");
                SetIfPresent(dbSection, "Username", values, "GESTLOG_DB_USER");
                SetIfPresent(dbSection, "Password", values, "GESTLOG_DB_PASSWORD");

                if (dbSection.TryGetProperty("UseIntegratedSecurity", out var integratedSecurityElement))
                {
                    values["GESTLOG_DB_INTEGRATED_SECURITY"] = integratedSecurityElement.GetBoolean().ToString();
                }
            }
        }

        var appSettingsPath = Path.Combine(appBaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (File.Exists(appSettingsPath))
        {
            using var appJson = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            if (appJson.RootElement.TryGetProperty("EmailServices", out var emailServices)
                && emailServices.TryGetProperty("PasswordReset", out var passwordReset))
            {
                SetIfPresent(passwordReset, "SmtpServer", values, "GESTLOG_SMTP_SERVER");
                SetIfPresent(passwordReset, "SmtpPort", values, "GESTLOG_SMTP_PORT");
                SetIfPresent(passwordReset, "SenderEmail", values, "GESTLOG_SENDER_EMAIL");
                SetIfPresent(passwordReset, "Username", values, "GESTLOG_EMAIL_USERNAME");
                SetIfPresent(passwordReset, "Password", values, "GESTLOG_EMAIL_PASSWORD");

                if (values.TryGetValue("GESTLOG_EMAIL_USERNAME", out var emailUsername) && !string.IsNullOrWhiteSpace(emailUsername))
                {
                    values["GESTLOG_PASSWORD_RESET_EMAIL_USERNAME"] = emailUsername;
                }

                if (values.TryGetValue("GESTLOG_EMAIL_PASSWORD", out var emailPassword) && !string.IsNullOrWhiteSpace(emailPassword))
                {
                    values["GESTLOG_PASSWORD_RESET_EMAIL_PASSWORD"] = emailPassword;
                }
            }
        }

        return values;
    }

    private static void SetIfPresent(JsonElement section, string propertyName, IDictionary<string, string> destination, string targetVariable)
    {
        if (!section.TryGetProperty(propertyName, out var property))
        {
            return;
        }

        var value = property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(value))
        {
            destination[targetVariable] = value;
        }
    }

    private static bool HasMinimumDatabaseConfiguration(IReadOnlyDictionary<string, string> values)
    {
        var hasServer = values.TryGetValue("GESTLOG_DB_SERVER", out var server) && !string.IsNullOrWhiteSpace(server);
        var hasDatabase = values.TryGetValue("GESTLOG_DB_NAME", out var database) && !string.IsNullOrWhiteSpace(database);
        var hasUser = values.TryGetValue("GESTLOG_DB_USER", out var user) && !string.IsNullOrWhiteSpace(user);
        var usesIntegratedSecurity = values.TryGetValue("GESTLOG_DB_INTEGRATED_SECURITY", out var integrated) &&
                                     bool.TryParse(integrated, out var integratedSecurityEnabled) &&
                                     integratedSecurityEnabled;
        var hasPassword = values.TryGetValue("GESTLOG_DB_PASSWORD", out var password) && !string.IsNullOrWhiteSpace(password);

        return hasServer && hasDatabase && hasUser && (usesIntegratedSecurity || hasPassword);
    }

    private static bool IsPlaceholderSecret(string value)
    {
        return value.Contains("CONFIGURAR", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase) ||
               (value.Contains("<", StringComparison.Ordinal) && value.Contains(">", StringComparison.Ordinal));
    }

    private static string ResolveBootstrapPath(string appBaseDirectory)
    {
        var overrideFile = Environment.GetEnvironmentVariable("GESTLOG_BOOTSTRAP_PATH");
        if (!string.IsNullOrWhiteSpace(overrideFile))
        {
            return overrideFile;
        }

        var updateServerDirectory = ResolveUpdateServerDirectory(appBaseDirectory);
        if (!string.IsNullOrWhiteSpace(updateServerDirectory))
        {
            return Path.Combine(updateServerDirectory, BootstrapFileName);
        }

        var installerDirectory = FindInstallerDirectory(appBaseDirectory) ?? appBaseDirectory;
        return Path.Combine(installerDirectory, BootstrapFileName);
    }

    private static string? ResolveUpdateServerDirectory(string appBaseDirectory)
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("GESTLOG_SETUP_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(overrideDirectory) && Directory.Exists(overrideDirectory))
        {
            return overrideDirectory;
        }

        var envUpdateServerPath = Environment.GetEnvironmentVariable("GESTLOG_UPDATE_SERVER_PATH");
        if (!string.IsNullOrWhiteSpace(envUpdateServerPath) && Directory.Exists(envUpdateServerPath))
        {
            return envUpdateServerPath;
        }

        var appSettingsPath = Path.Combine(appBaseDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (!File.Exists(appSettingsPath))
        {
            return null;
        }

        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            if (!json.RootElement.TryGetProperty("Updater", out var updaterSection))
            {
                return null;
            }

            if (!updaterSection.TryGetProperty("UpdateServerPath", out var updateServerPathProperty))
            {
                return null;
            }

            var updateServerPath = updateServerPathProperty.GetString();
            if (string.IsNullOrWhiteSpace(updateServerPath))
            {
                return null;
            }

            return Directory.Exists(updateServerPath) ? updateServerPath : null;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindInstallerDirectory(string appBaseDirectory)
    {
        var startDirectories = new List<string>
        {
            appBaseDirectory,
            Directory.GetCurrentDirectory()
        }
        .Where(Directory.Exists)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        foreach (var startDirectory in startDirectories)
        {
            var directoryInfo = new DirectoryInfo(startDirectory);
            for (var level = 0; level < 8 && directoryInfo != null; level++)
            {
                var setupPath = Path.Combine(directoryInfo.FullName, "setup.exe");
                if (File.Exists(setupPath))
                {
                    return directoryInfo.FullName;
                }

                directoryInfo = directoryInfo.Parent;
            }
        }

        return null;
    }
}