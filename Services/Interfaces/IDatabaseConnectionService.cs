using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Events;
using GestLog.Models.Enums;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Servicio para gestión de conexiones a base de datos con resiliencia avanzada
/// </summary>
public interface IDatabaseConnectionService : IDisposable
{
    /// <summary>
    /// Evento que se dispara cuando cambia el estado de conexión
    /// </summary>
    event EventHandler<DatabaseConnectionStateChangedEventArgs>? ConnectionStateChanged;
    
    /// <summary>
    /// Evento que se dispara cuando cambia el estado del Circuit Breaker
    /// </summary>
    event EventHandler<CircuitBreakerStateChangedEventArgs>? CircuitBreakerStateChanged;
    
    /// <summary>
    /// Evento que se dispara cuando cambia la conectividad de red
    /// </summary>
    event EventHandler<NetworkConnectivityChangedEventArgs>? NetworkConnectivityChanged;

    /// <summary>
    /// Indica si la conexión está activa
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Estado actual de la conexión
    /// </summary>
    DatabaseConnectionState CurrentState { get; }
    
    /// <summary>
    /// Estado actual del Circuit Breaker
    /// </summary>
    CircuitBreakerState CircuitBreakerState { get; }
    
    /// <summary>
    /// Estado actual de conectividad de red
    /// </summary>
    NetworkConnectivityState NetworkState { get; }

    /// <summary>
    /// Indica si la reconexión automática está habilitada
    /// </summary>
    bool AutoReconnectEnabled { get; set; }

    /// <summary>
    /// Obtiene una conexión a la base de datos con resiliencia
    /// </summary>
    Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prueba la conexión a la base de datos
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un DataTable
    /// </summary>
    Task<DataTable> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un valor escalar
    /// </summary>
    Task<T?> ExecuteScalarAsync<T>(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia el servicio de conexión y monitoreo
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detiene el servicio de conexión y monitoreo
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fuerza una verificación inmediata de la conexión
    /// </summary>
    Task<bool> ForceHealthCheckAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reinicia el Circuit Breaker (para testing/recovery manual)
    /// </summary>
    void ResetCircuitBreaker();
    
    /// <summary>
    /// Obtiene métricas de la conexión
    /// </summary>
    Task<ConnectionMetrics> GetMetricsAsync();
}

/// <summary>
/// Métricas de conexión
/// </summary>
public record ConnectionMetrics(
    TimeSpan Uptime,
    int TotalConnections,
    int SuccessfulConnections,
    int FailedConnections,
    double SuccessRate,
    TimeSpan AverageConnectionTime,
    DateTime LastSuccessfulConnection,
    DateTime LastFailedConnection,
    int CircuitBreakerTrips);
