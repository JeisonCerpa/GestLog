using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Excepción para errores de conexión a base de datos
/// </summary>
public class DatabaseConnectionException : DatabaseException
{
    public string ConnectionString { get; }
    public string ServerName { get; }

    public DatabaseConnectionException(string message, string connectionString, string serverName) 
        : base(message, "DATABASE_CONNECTION_ERROR", $"Server: {serverName}")
    {
        ConnectionString = connectionString;
        ServerName = serverName;
    }

    public DatabaseConnectionException(string message, string connectionString, string serverName, Exception innerException) 
        : base(message, "DATABASE_CONNECTION_ERROR", innerException, $"Server: {serverName}")
    {
        ConnectionString = connectionString;
        ServerName = serverName;
    }
}
