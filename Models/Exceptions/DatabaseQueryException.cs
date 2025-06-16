using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Excepción para errores de consulta a base de datos
/// </summary>
public class DatabaseQueryException : DatabaseException
{
    /// <summary>
    /// Consulta SQL que causó el error
    /// </summary>
    public string SqlQuery { get; }

    public DatabaseQueryException(string message, string sqlQuery) 
        : base(message, "DATABASE_QUERY_ERROR")
    {
        SqlQuery = sqlQuery;
    }

    public DatabaseQueryException(string message, string sqlQuery, Exception innerException) 
        : base(message, "DATABASE_QUERY_ERROR", innerException)
    {
        SqlQuery = sqlQuery;
    }
}
