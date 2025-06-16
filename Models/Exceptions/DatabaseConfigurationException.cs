using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Excepción para errores de configuración de base de datos
/// </summary>
public class DatabaseConfigurationException : DatabaseException
{
    public string ConfigurationKey { get; }

    public DatabaseConfigurationException(string message, string configurationKey) 
        : base(message, "DATABASE_CONFIGURATION_ERROR", $"ConfigKey: {configurationKey}")
    {
        ConfigurationKey = configurationKey;
    }

    public DatabaseConfigurationException(string message, string configurationKey, Exception innerException) 
        : base(message, "DATABASE_CONFIGURATION_ERROR", innerException, $"ConfigKey: {configurationKey}")
    {
        ConfigurationKey = configurationKey;
    }
}
