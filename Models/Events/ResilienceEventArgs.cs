using GestLog.Models.Enums;
using System;

namespace GestLog.Models.Events;

/// <summary>
/// Argumentos de evento para cambios de estado del Circuit Breaker
/// </summary>
public class CircuitBreakerStateChangedEventArgs : EventArgs
{
    public CircuitBreakerState PreviousState { get; }
    public CircuitBreakerState NewState { get; }
    public string Reason { get; }
    public int FailureCount { get; }
    public DateTime Timestamp { get; }
    public Exception? LastException { get; }

    public CircuitBreakerStateChangedEventArgs(
        CircuitBreakerState previousState,
        CircuitBreakerState newState,
        string reason,
        int failureCount,
        Exception? lastException = null)
    {
        PreviousState = previousState;
        NewState = newState;
        Reason = reason;
        FailureCount = failureCount;
        Timestamp = DateTime.UtcNow;
        LastException = lastException;
    }
}

/// <summary>
/// Argumentos de evento para cambios de conectividad de red
/// </summary>
public class NetworkConnectivityChangedEventArgs : EventArgs
{
    public NetworkConnectivityState PreviousState { get; }
    public NetworkConnectivityState NewState { get; }
    public bool IsAvailable { get; }
    public DateTime Timestamp { get; }
    public string? NetworkInterface { get; }

    public NetworkConnectivityChangedEventArgs(
        NetworkConnectivityState previousState,
        NetworkConnectivityState newState,
        bool isAvailable,
        string? networkInterface = null)
    {
        PreviousState = previousState;
        NewState = newState;
        IsAvailable = isAvailable;
        Timestamp = DateTime.UtcNow;
        NetworkInterface = networkInterface;
    }
}

/// <summary>
/// Argumentos de evento para métricas de conexión
/// </summary>
public class ConnectionMetricsEventArgs : EventArgs
{
    public TimeSpan ConnectionDuration { get; }
    public bool IsSuccessful { get; }
    public string Operation { get; }
    public DateTime Timestamp { get; }
    public Exception? Exception { get; }

    public ConnectionMetricsEventArgs(
        TimeSpan connectionDuration,
        bool isSuccessful,
        string operation,
        Exception? exception = null)
    {
        ConnectionDuration = connectionDuration;
        IsSuccessful = isSuccessful;
        Operation = operation;
        Timestamp = DateTime.UtcNow;
        Exception = exception;
    }
}
