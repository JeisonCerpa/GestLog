using System;

namespace GestLog.Models.Events;

/// <summary>
/// Estados posibles de la conexión a base de datos
/// </summary>
public enum DatabaseConnectionState
{
    /// <summary>
    /// Estado desconocido o inicial
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Conectando a la base de datos
    /// </summary>
    Connecting,
    
    /// <summary>
    /// Conectado exitosamente
    /// </summary>
    Connected,
    
    /// <summary>
    /// Desconectado (sin error)
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// Error de conexión
    /// </summary>
    Error,
    
    /// <summary>
    /// Intentando reconectar
    /// </summary>
    Reconnecting
}

/// <summary>
/// Argumentos del evento de cambio de estado de conexión
/// </summary>
public class DatabaseConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Estado anterior de la conexión
    /// </summary>
    public DatabaseConnectionState PreviousState { get; }
    
    /// <summary>
    /// Estado actual de la conexión
    /// </summary>
    public DatabaseConnectionState CurrentState { get; }
    
    /// <summary>
    /// Información adicional sobre el cambio de estado
    /// </summary>
    public string? Message { get; }
    
    /// <summary>
    /// Excepción que causó el cambio de estado (si aplica)
    /// </summary>
    public Exception? Exception { get; }
    
    /// <summary>
    /// Timestamp del cambio de estado
    /// </summary>
    public DateTime Timestamp { get; }

    public DatabaseConnectionStateChangedEventArgs(
        DatabaseConnectionState previousState,
        DatabaseConnectionState currentState,
        string? message = null,
        Exception? exception = null)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        Message = message;
        Exception = exception;
        Timestamp = DateTime.Now;
    }
}
