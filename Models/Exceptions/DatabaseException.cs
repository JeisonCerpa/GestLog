using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Excepción base para errores específicos de base de datos en GestLog
/// </summary>
public abstract class DatabaseException : Exception
{
    /// <summary>
    /// Código de error específico del dominio
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Información adicional del contexto del error
    /// </summary>
    public string Context { get; }

    protected DatabaseException(string message, string errorCode, string context = "") 
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context;
    }

    protected DatabaseException(string message, string errorCode, Exception innerException, string context = "") 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Context = context;
    }
}
