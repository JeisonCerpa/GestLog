using System;

namespace GestLog.Models.Exceptions;

/// <summary>
/// Excepción para errores de operaciones de base de datos (CRUD)
/// </summary>
public class DatabaseOperationException : DatabaseException
{
    public string Operation { get; }
    public string TableName { get; }

    public DatabaseOperationException(string message, string operation, string tableName) 
        : base(message, "DATABASE_OPERATION_ERROR", $"Operation: {operation}, Table: {tableName}")
    {
        Operation = operation;
        TableName = tableName;
    }

    public DatabaseOperationException(string message, string operation, string tableName, Exception innerException) 
        : base(message, "DATABASE_OPERATION_ERROR", innerException, $"Operation: {operation}, Table: {tableName}")
    {
        Operation = operation;
        TableName = tableName;
    }
}
