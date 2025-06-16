using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Events;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Interfaz para gestión de conexiones a base de datos con monitoreo y reconexión automática
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>
    /// Evento que se dispara cuando el estado de conexión cambia
    /// </summary>
    event EventHandler<DatabaseConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>
    /// Indica si la conexión está actualmente activa
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Estado actual de la conexión
    /// </summary>
    DatabaseConnectionState CurrentState { get; }

    /// <summary>
    /// Indica si el servicio está configurado para reconexión automática
    /// </summary>
    bool AutoReconnectEnabled { get; set; }

    /// <summary>
    /// Intervalo de monitoreo de conexión en millisegundos (por defecto 30000ms = 30s)
    /// </summary>
    int MonitoringIntervalMs { get; set; }

    /// <summary>
    /// Inicia el servicio de conexión con monitoreo automático
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Task que representa la operación asíncrona</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detiene el servicio de conexión y monitoreo
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Task que representa la operación asíncrona</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Obtiene una conexión a la base de datos
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Conexión SQL abierta</returns>
    Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prueba la conexión a la base de datos
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>True si la conexión es exitosa</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una consulta SQL y retorna el número de filas afectadas
    /// </summary>
    /// <param name="sql">Consulta SQL</param>
    /// <param name="parameters">Parámetros de la consulta</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Número de filas afectadas</returns>
    Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un valor escalar
    /// </summary>
    /// <param name="sql">Consulta SQL</param>
    /// <param name="parameters">Parámetros de la consulta</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Valor escalar resultado de la consulta</returns>
    Task<T?> ExecuteScalarAsync<T>(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un DataTable
    /// </summary>
    /// <param name="sql">Consulta SQL</param>
    /// <param name="parameters">Parámetros de la consulta</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>DataTable con los resultados</returns>
    Task<DataTable> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene información del servidor de base de datos
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Información del servidor</returns>
    Task<string> GetServerInfoAsync(CancellationToken cancellationToken = default);
}
