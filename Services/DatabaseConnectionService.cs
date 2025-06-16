using GestLog.Services.Core.Logging;
using GestLog.Models.Configuration;
using GestLog.Models.Exceptions;
using GestLog.Models.Events;
using GestLog.Services.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services;

/// <summary>
/// Servicio para gestión de conexiones a base de datos SQL Server con monitoreo y reconexión automática
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService, IDisposable
{
    private readonly DatabaseConfiguration _config;
    private readonly IGestLogLogger _logger;
    private readonly System.Threading.Timer _connectionMonitor;
    private readonly SemaphoreSlim _reconnectSemaphore;
    private DatabaseConnectionState _currentState;
    private bool _disposed;
    private CancellationTokenSource? _serviceTokenSource;

    public DatabaseConnectionService(IOptions<DatabaseConfiguration> config, IGestLogLogger logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _reconnectSemaphore = new SemaphoreSlim(1, 1);
        _currentState = DatabaseConnectionState.Unknown;
        
        // Configuración por defecto
        AutoReconnectEnabled = true;
        MonitoringIntervalMs = 30000; // 30 segundos
        
        ValidateConfiguration();
        
        // Inicializar timer (inactivo hasta que se llame Start)
        _connectionMonitor = new System.Threading.Timer(MonitorConnection, null, Timeout.Infinite, Timeout.Infinite);
        
        _logger.LogInformation("💾 DatabaseConnectionService inicializado con reconexión automática");
    }

    #region Propiedades Públicas

    public event EventHandler<DatabaseConnectionStateChangedEventArgs>? ConnectionStateChanged;

    public bool IsConnected => _currentState == DatabaseConnectionState.Connected;

    public DatabaseConnectionState CurrentState => _currentState;

    public bool AutoReconnectEnabled { get; set; }

    public int MonitoringIntervalMs { get; set; }

    #endregion    /// <summary>
    /// Obtiene una conexión a la base de datos
    /// </summary>
    public async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await GetConnectionInternalAsync(cancellationToken, logDebugMessages: true);
    }

    /// <summary>
    /// Obtiene una conexión a la base de datos (versión interna con control de logging)
    /// </summary>
    private async Task<SqlConnection> GetConnectionInternalAsync(CancellationToken cancellationToken, bool logDebugMessages)
    {
        try
        {
            if (logDebugMessages)
                _logger.LogDebug("Creating database connection to server: {Server}", _config.Server);
            
            var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            
            if (logDebugMessages)
                _logger.LogDebug("Database connection opened successfully");
            
            return connection;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            if (logDebugMessages)
                _logger.LogError(ex, "SQL connection failed: {Error}", errorMessage);
            throw new DatabaseConnectionException(errorMessage, _config.ConnectionString, _config.Server, ex);
        }
        catch (Exception ex)
        {
            if (logDebugMessages)
                _logger.LogError(ex, "Unexpected error creating database connection");
            throw new DatabaseConnectionException("Error inesperado al conectar con la base de datos", _config.ConnectionString, _config.Server, ex);
        }
    }

    /// <summary>
    /// Prueba la conexión a la base de datos
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing database connection");
              using var connection = await GetConnectionAsync(cancellationToken);
            var serverInfo = await GetServerInfoAsync(cancellationToken);
            
            _logger.LogInformation("Database connection test successful. Server: {ServerInfo}", serverInfo);
            return true;
        }        catch (DatabaseConnectionException ex)
        {
            _logger.LogWarning("Database connection test failed: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during connection test");
            return false;
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL y retorna el número de filas afectadas
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new DatabaseQueryException("La consulta SQL no puede estar vacía", sql);

        try
        {
            _logger.LogDebug("Executing non-query SQL: {Sql}", sql);
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var result = await command.ExecuteNonQueryAsync(cancellationToken);
            
            _logger.LogDebug("Non-query executed successfully. Rows affected: {RowsAffected}", result);
            return result;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            _logger.LogError(ex, "SQL execution failed: {Error}", errorMessage);
            throw new DatabaseQueryException(errorMessage, sql, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing SQL query");
            throw new DatabaseQueryException("Error inesperado al ejecutar la consulta", sql, ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un valor escalar
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new DatabaseQueryException("La consulta SQL no puede estar vacía", sql);

        try
        {
            _logger.LogDebug("Executing scalar SQL: {Sql}", sql);
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result == null || result == DBNull.Value)
            {
                _logger.LogDebug("Scalar query returned null");
                return default(T);
            }

            var convertedResult = (T)Convert.ChangeType(result, typeof(T));
            _logger.LogDebug("Scalar query executed successfully. Result: {Result}", convertedResult);
            
            return convertedResult;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            _logger.LogError(ex, "SQL scalar execution failed: {Error}", errorMessage);
            throw new DatabaseQueryException(errorMessage, sql, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing scalar query");
            throw new DatabaseQueryException("Error inesperado al ejecutar la consulta escalar", sql, ex);
        }
    }

    /// <summary>
    /// Ejecuta una consulta SQL y retorna un DataTable
    /// </summary>
    public async Task<DataTable> ExecuteQueryAsync(string sql, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new DatabaseQueryException("La consulta SQL no puede estar vacía", sql);

        try
        {
            _logger.LogDebug("Executing query SQL: {Sql}", sql);
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            
            await Task.Run(() => adapter.Fill(dataTable), cancellationToken);
            
            _logger.LogDebug("Query executed successfully. Rows returned: {RowCount}", dataTable.Rows.Count);
            return dataTable;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            _logger.LogError(ex, "SQL query execution failed: {Error}", errorMessage);
            throw new DatabaseQueryException(errorMessage, sql, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing query");
            throw new DatabaseQueryException("Error inesperado al ejecutar la consulta", sql, ex);
        }
    }

    /// <summary>
    /// Obtiene información del servidor de base de datos
    /// </summary>
    public async Task<string> GetServerInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            const string sql = "SELECT @@VERSION";
            var version = await ExecuteScalarAsync<string>(sql, null, cancellationToken);
            return version ?? "Información del servidor no disponible";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve server information");
            return "No se pudo obtener información del servidor";
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Server))
            throw new DatabaseConfigurationException("El servidor de base de datos no está configurado", "Server");

        if (string.IsNullOrWhiteSpace(_config.Database))
            throw new DatabaseConfigurationException("El nombre de la base de datos no está configurado", "Database");

        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
            throw new DatabaseConfigurationException("La cadena de conexión no está configurada", "ConnectionString");
    }    private static string GetSqlErrorMessage(SqlException ex)
    {
        return ex.Number switch
        {
            2 => "No se puede conectar al servidor. Verifique que el servidor esté disponible",
            18456 => "Error de autenticación. Verifique usuario y contraseña",
            4060 => "No se puede abrir la base de datos solicitada",
            53 => "Error de red. Verifique la conectividad",
            -2 => "Tiempo de espera agotado al conectar",
            _ => $"Error de SQL Server: {ex.Message}"
        };
    }

    #region Gestión del Servicio

    /// <summary>
    /// Inicia el servicio de conexión con monitoreo automático
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DatabaseConnectionService));

        _logger.LogInformation("🚀 Iniciando servicio de conexión a base de datos...");
        
        _serviceTokenSource = new CancellationTokenSource();
          try
        {
            // Cambiar estado a conectando
            ChangeState(DatabaseConnectionState.Connecting, "Iniciando conexión inicial...");
            
            // Realizar conexión inicial usando método interno (sin logs de debug)
            var connected = await TestConnectionInternalAsync(cancellationToken);
            
            if (connected)
            {
                ChangeState(DatabaseConnectionState.Connected, "Conexión inicial exitosa");
                _logger.LogInformation("✅ Conexión inicial a base de datos establecida exitosamente");
            }
            else
            {
                ChangeState(DatabaseConnectionState.Error, "Falló la conexión inicial");
                _logger.LogWarning("⚠️ No se pudo establecer la conexión inicial, pero el servicio continuará intentando");
            }
            
            // Iniciar monitoreo si está habilitado
            if (AutoReconnectEnabled)
            {
                _connectionMonitor.Change(MonitoringIntervalMs, MonitoringIntervalMs);
                _logger.LogInformation("🔄 Monitoreo de conexión iniciado cada {Interval}ms", MonitoringIntervalMs);
            }
        }
        catch (Exception ex)
        {
            ChangeState(DatabaseConnectionState.Error, $"Error durante inicio: {ex.Message}", ex);
            _logger.LogError(ex, "❌ Error al iniciar servicio de conexión a base de datos");
            
            // Aún así iniciar monitoreo para intentos de reconexión
            if (AutoReconnectEnabled)
            {
                _connectionMonitor.Change(MonitoringIntervalMs, MonitoringIntervalMs);
            }
        }
    }

    /// <summary>
    /// Detiene el servicio de conexión y monitoreo
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🛑 Deteniendo servicio de conexión a base de datos...");
        
        // Detener monitoreo
        _connectionMonitor.Change(Timeout.Infinite, Timeout.Infinite);
        
        // Cancelar operaciones en curso
        _serviceTokenSource?.Cancel();
        
        ChangeState(DatabaseConnectionState.Disconnected, "Servicio detenido por solicitud");
        
        await Task.CompletedTask;
        _logger.LogInformation("✅ Servicio de conexión detenido");
    }

    #endregion

    #region Monitoreo y Reconexión

    /// <summary>
    /// Método del timer para monitorear la conexión
    /// </summary>
    private async void MonitorConnection(object? state)
    {
        if (_disposed || !AutoReconnectEnabled)
            return;

        try
        {
            // Evitar múltiples ejecuciones simultáneas
            if (!await _reconnectSemaphore.WaitAsync(100))
                return;

            try
            {
                var isConnected = await TestConnectionInternalAsync();
                
                if (isConnected && _currentState != DatabaseConnectionState.Connected)
                {
                    ChangeState(DatabaseConnectionState.Connected, "Conexión restaurada");
                    _logger.LogInformation("✅ Conexión a base de datos restaurada");
                }
                else if (!isConnected && _currentState == DatabaseConnectionState.Connected)
                {
                    ChangeState(DatabaseConnectionState.Error, "Conexión perdida");
                    _logger.LogWarning("⚠️ Conexión a base de datos perdida, intentando reconectar...");
                    
                    // Intentar reconexión inmediata
                    await AttemptReconnection();
                }
                else if (!isConnected && _currentState == DatabaseConnectionState.Error)
                {
                    // Intentar reconexión
                    await AttemptReconnection();
                }
            }
            finally
            {
                _reconnectSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante monitoreo de conexión");
        }
    }    /// <summary>
    /// Prueba la conexión a la base de datos (versión interna para monitoreo)
    /// </summary>
    private async Task<bool> TestConnectionInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await GetConnectionInternalAsync(cancellationToken, logDebugMessages: false);
            
            // Verificar que realmente podemos ejecutar comandos
            using var command = new SqlCommand("SELECT @@VERSION", connection);
            command.CommandTimeout = _config.CommandTimeout;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Intenta reconectar a la base de datos
    /// </summary>
    private async Task AttemptReconnection()
    {
        if (_currentState == DatabaseConnectionState.Reconnecting)
            return;

        ChangeState(DatabaseConnectionState.Reconnecting, "Intentando reconectar...");
        _logger.LogInformation("🔄 Intentando reconexión a base de datos...");

        try
        {
            var connected = await TestConnectionInternalAsync();
            
            if (connected)
            {
                ChangeState(DatabaseConnectionState.Connected, "Reconexión exitosa");
                _logger.LogInformation("✅ Reconexión a base de datos exitosa");
            }
            else
            {
                ChangeState(DatabaseConnectionState.Error, "Reconexión falló");
                _logger.LogWarning("⚠️ Reconexión falló, intentará nuevamente en {Interval}ms", MonitoringIntervalMs);
            }
        }
        catch (Exception ex)
        {
            ChangeState(DatabaseConnectionState.Error, $"Error durante reconexión: {ex.Message}", ex);
            _logger.LogError(ex, "❌ Error durante intento de reconexión");
        }
    }

    /// <summary>
    /// Cambiar el estado de conexión y notificar
    /// </summary>
    private void ChangeState(DatabaseConnectionState newState, string? message = null, Exception? exception = null)
    {
        var previousState = _currentState;
        _currentState = newState;
        
        var args = new DatabaseConnectionStateChangedEventArgs(previousState, newState, message, exception);
        ConnectionStateChanged?.Invoke(this, args);
        
        _logger.LogDebug("🔄 Estado de conexión cambiado: {PreviousState} → {CurrentState} | {Message}",
            previousState, newState, message ?? "Sin mensaje");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _connectionMonitor?.Dispose();
            _serviceTokenSource?.Cancel();
            _serviceTokenSource?.Dispose();
            _reconnectSemaphore?.Dispose();
            _disposed = true;
        }
    }

    #endregion
}
