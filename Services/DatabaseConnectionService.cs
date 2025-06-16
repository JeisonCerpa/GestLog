using GestLog.Services.Core.Logging;
using GestLog.Models.Configuration;
using GestLog.Models.Exceptions;
using GestLog.Models.Events;
using GestLog.Models.Enums;
using GestLog.Services.Interfaces;
using GestLog.Services.Resilience;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services;

/// <summary>
/// Servicio avanzado para gestión de conexiones a base de datos con resiliencia completa
/// </summary>
public class DatabaseConnectionService : IDatabaseConnectionService, IDisposable
{
    private readonly DatabaseConfiguration _config;
    private readonly DatabaseResilienceConfiguration _resilienceConfig;
    private readonly IGestLogLogger _logger;
    
    // Servicios de resiliencia
    private readonly CircuitBreakerService _circuitBreaker;
    private readonly ExponentialBackoffService _exponentialBackoff;
    private readonly NetworkMonitoringService _networkMonitoring;
      // Health Check y monitoreo
    private readonly System.Threading.Timer _healthCheckTimer;
    private readonly SemaphoreSlim _healthCheckSemaphore;
    
    // Estado y métricas
    private DatabaseConnectionState _currentState;
    private readonly ConnectionMetricsCollector _metricsCollector;
    private CancellationTokenSource? _serviceTokenSource;
    private bool _disposed;

    public DatabaseConnectionService(
        IOptions<DatabaseConfiguration> config,
        IOptions<DatabaseResilienceConfiguration> resilienceConfig,
        IGestLogLogger logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        _resilienceConfig = resilienceConfig.Value ?? throw new ArgumentNullException(nameof(resilienceConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Inicializar servicios de resiliencia
        _circuitBreaker = new CircuitBreakerService(resilienceConfig, logger);
        _exponentialBackoff = new ExponentialBackoffService(resilienceConfig, logger);
        _networkMonitoring = new NetworkMonitoringService(resilienceConfig, logger);
        
        // Inicializar estado y métricas
        _currentState = DatabaseConnectionState.Unknown;
        _metricsCollector = new ConnectionMetricsCollector();
        _healthCheckSemaphore = new SemaphoreSlim(1, 1);
          // Configurar timer de health checks
        _healthCheckTimer = new System.Threading.Timer(
            ExecuteHealthCheck,
            null,
            Timeout.Infinite,
            Timeout.Infinite);
        
        // Configuración inicial
        AutoReconnectEnabled = true;
        
        ValidateConfiguration();
        SubscribeToEvents();
        
        _logger.LogInformation("💾 DatabaseConnectionService con resiliencia avanzada inicializado");
    }

    #region Propiedades Públicas

    public event EventHandler<DatabaseConnectionStateChangedEventArgs>? ConnectionStateChanged;
    public event EventHandler<CircuitBreakerStateChangedEventArgs>? CircuitBreakerStateChanged;
    public event EventHandler<NetworkConnectivityChangedEventArgs>? NetworkConnectivityChanged;

    public bool IsConnected => _currentState == DatabaseConnectionState.Connected;
    public DatabaseConnectionState CurrentState => _currentState;
    public CircuitBreakerState CircuitBreakerState => _circuitBreaker.State;
    public NetworkConnectivityState NetworkState => _networkMonitoring.CurrentState;
    public bool AutoReconnectEnabled { get; set; }

    #endregion

    #region Conexión Principal

    /// <summary>
    /// Obtiene una conexión a la base de datos con resiliencia completa
    /// </summary>
    public async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("🔗 Solicitando conexión con resiliencia");
            
            // Registrar actividad para métricas
            _metricsCollector.RegisterConnectionAttempt();
            
            var connection = await _circuitBreaker.ExecuteAsync(async (ct) =>
            {
                return await _exponentialBackoff.ExecuteAsync(async (attempt, ct2) =>
                {
                    if (attempt > 0)
                    {
                        _logger.LogDebug("🔄 Intento de conexión #{Attempt}", attempt + 1);
                    }
                    
                    return await CreateConnectionInternalAsync(ct2);
                }, ct);
            }, cancellationToken);
            
            stopwatch.Stop();
            _metricsCollector.RegisterSuccessfulConnection(stopwatch.Elapsed);
            
            // Actualizar estado si es necesario
            if (_currentState != DatabaseConnectionState.Connected)
            {
                ChangeState(DatabaseConnectionState.Connected, "Conexión establecida exitosamente");
            }
            
            _logger.LogDebug("✅ Conexión establecida en {Duration}ms", stopwatch.ElapsedMilliseconds);
            return connection;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _metricsCollector.RegisterFailedConnection(stopwatch.Elapsed, ex);
            
            ChangeState(DatabaseConnectionState.Error, $"Error de conexión: {ex.Message}", ex);
            
            _logger.LogError(ex, "❌ Error obteniendo conexión tras {Duration}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Crea una conexión interna sin resiliencia (para uso del Circuit Breaker)
    /// </summary>
    private async Task<SqlConnection> CreateConnectionInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var connection = new SqlConnection(_config.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            throw new DatabaseConnectionException(errorMessage, _config.ConnectionString, _config.Server, ex);
        }
        catch (Exception ex)
        {
            throw new DatabaseConnectionException("Error inesperado al conectar con la base de datos", 
                _config.ConnectionString, _config.Server, ex);
        }
    }

    #endregion

    #region Operaciones de Base de Datos

    /// <summary>
    /// Prueba la conexión a la base de datos
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("🧪 Probando conexión a base de datos");
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand("SELECT 1", connection);
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            _logger.LogDebug("✅ Prueba de conexión exitosa");
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Prueba de conexión falló");
            return false;
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
            _logger.LogDebug("📊 Ejecutando consulta SQL: {Sql}", sql);
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            
            await Task.Run(() => adapter.Fill(dataTable), cancellationToken);
            
            _logger.LogDebug("✅ Consulta ejecutada exitosamente. Filas: {Rows}", dataTable.Rows.Count);
            return dataTable;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            _logger.LogError(ex, "❌ Error SQL ejecutando consulta: {Error}", errorMessage);
            throw new DatabaseQueryException(errorMessage, sql, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado ejecutando consulta");
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
            _logger.LogDebug("🔢 Ejecutando consulta escalar: {Sql}", sql);
            
            using var connection = await GetConnectionAsync(cancellationToken);
            using var command = new SqlCommand(sql, connection);
            
            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            
            if (result == null || result == DBNull.Value)
            {
                _logger.LogDebug("📊 Consulta escalar retornó null");
                return default(T);
            }

            var convertedResult = (T)Convert.ChangeType(result, typeof(T));
            _logger.LogDebug("✅ Consulta escalar ejecutada. Resultado: {Result}", convertedResult);
            
            return convertedResult;
        }
        catch (SqlException ex)
        {
            var errorMessage = GetSqlErrorMessage(ex);
            _logger.LogError(ex, "❌ Error SQL en consulta escalar: {Error}", errorMessage);
            throw new DatabaseQueryException(errorMessage, sql, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inesperado en consulta escalar");
            throw new DatabaseQueryException("Error inesperado al ejecutar la consulta", sql, ex);
        }
    }

    #endregion

    #region Ciclo de Vida del Servicio

    /// <summary>
    /// Inicia el servicio de conexión y monitoreo
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🚀 Iniciando servicio de resiliencia de base de datos...");
        
        _serviceTokenSource = new CancellationTokenSource();
        
        try
        {
            // Cambiar estado a conectando
            ChangeState(DatabaseConnectionState.Connecting, "Iniciando conexión con resiliencia");
            
            // Intentar conexión inicial
            var connected = await TestConnectionAsync(cancellationToken);
            
            if (connected)
            {
                ChangeState(DatabaseConnectionState.Connected, "Conexión inicial con resiliencia establecida");
                _logger.LogInformation("✅ Conexión inicial establecida exitosamente");
            }
            else
            {
                ChangeState(DatabaseConnectionState.Error, "Falló la conexión inicial");
                _logger.LogWarning("⚠️ Conexión inicial falló, continuará intentando con resiliencia");
            }
            
            // Iniciar health checks si está habilitado
            if (AutoReconnectEnabled && _resilienceConfig.HealthCheck.AdaptiveMode)
            {
                var interval = _resilienceConfig.HealthCheck.Interval;
                _healthCheckTimer.Change(interval, interval);
                _logger.LogInformation("🔄 Health checks iniciados cada {Interval}", interval);
            }
        }
        catch (Exception ex)
        {
            ChangeState(DatabaseConnectionState.Error, $"Error durante inicio: {ex.Message}", ex);
            _logger.LogError(ex, "❌ Error al iniciar servicio de resiliencia");
            
            // Aún así iniciar health checks para recuperación
            if (AutoReconnectEnabled)
            {
                var interval = _resilienceConfig.HealthCheck.Interval;
                _healthCheckTimer.Change(interval, interval);
            }
        }
    }

    /// <summary>
    /// Detiene el servicio de conexión y monitoreo
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🛑 Deteniendo servicio de resiliencia...");
        
        // Detener health checks
        _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
        
        // Cancelar operaciones en curso
        _serviceTokenSource?.Cancel();
        
        // Cambiar estado
        ChangeState(DatabaseConnectionState.Disconnected, "Servicio detenido manualmente");
        
        await Task.Delay(100, cancellationToken); // Dar tiempo a operaciones pendientes
        
        _logger.LogInformation("✅ Servicio de resiliencia detenido");
    }

    #endregion

    #region Health Checks y Monitoreo

    /// <summary>
    /// Ejecuta health check programado
    /// </summary>
    private async void ExecuteHealthCheck(object? state)
    {
        if (_disposed || !AutoReconnectEnabled)
            return;

        try
        {
            if (!await _healthCheckSemaphore.WaitAsync(100))
                return;

            try
            {
                var isHealthy = await TestConnectionInternalAsync();
                
                if (isHealthy && _currentState != DatabaseConnectionState.Connected)
                {
                    ChangeState(DatabaseConnectionState.Connected, "Health check exitoso - conexión restaurada");
                    _logger.LogInformation("✅ Conexión restaurada vía health check");
                }
                else if (!isHealthy && _currentState == DatabaseConnectionState.Connected)
                {
                    ChangeState(DatabaseConnectionState.Error, "Health check falló - conexión perdida");
                    _logger.LogWarning("⚠️ Conexión perdida detectada vía health check");
                }
                
                _metricsCollector.RegisterHealthCheck(isHealthy);
            }
            finally
            {
                _healthCheckSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante health check");
            _metricsCollector.RegisterHealthCheck(false);
        }
    }

    /// <summary>
    /// Fuerza una verificación inmediata de la conexión
    /// </summary>
    public async Task<bool> ForceHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Forzando health check inmediato");
        
        try
        {
            var isHealthy = await TestConnectionInternalAsync(cancellationToken);
            _metricsCollector.RegisterHealthCheck(isHealthy);
            
            var previousState = _currentState;
            var newState = isHealthy ? DatabaseConnectionState.Connected : DatabaseConnectionState.Error;
            
            if (previousState != newState)
            {
                ChangeState(newState, "Health check forzado");
            }
            
            _logger.LogInformation("🔍 Health check forzado completado: {Result}", isHealthy ? "Exitoso" : "Falló");
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error en health check forzado");
            _metricsCollector.RegisterHealthCheck(false);
            return false;
        }
    }

    /// <summary>
    /// Prueba la conexión internamente (para health checks)
    /// </summary>
    private async Task<bool> TestConnectionInternalAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await CreateConnectionInternalAsync(cancellationToken);
            using var command = new SqlCommand("SELECT 1", connection);
            command.CommandTimeout = (int)_resilienceConfig.HealthCheck.QueryTimeout.TotalSeconds;
            
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Eventos y Estado

    /// <summary>
    /// Suscribe a eventos de los servicios de resiliencia
    /// </summary>
    private void SubscribeToEvents()
    {
        _circuitBreaker.StateChanged += OnCircuitBreakerStateChanged;
        _networkMonitoring.ConnectivityChanged += OnNetworkConnectivityChanged;
    }

    /// <summary>
    /// Maneja cambios de estado del Circuit Breaker
    /// </summary>
    private void OnCircuitBreakerStateChanged(object? sender, CircuitBreakerStateChangedEventArgs e)
    {
        _logger.LogInformation("🔒 Circuit Breaker: {Previous} → {New} | {Reason}", 
            e.PreviousState, e.NewState, e.Reason);
        
        CircuitBreakerStateChanged?.Invoke(this, e);
        
        // Actualizar métricas
        if (e.NewState == CircuitBreakerState.Open)
        {
            _metricsCollector.RegisterCircuitBreakerTrip();
        }
    }

    /// <summary>
    /// Maneja cambios de conectividad de red
    /// </summary>
    private void OnNetworkConnectivityChanged(object? sender, NetworkConnectivityChangedEventArgs e)
    {
        _logger.LogInformation("🌐 Red: {Previous} → {New} | Disponible: {Available}", 
            e.PreviousState, e.NewState, e.IsAvailable);
        
        NetworkConnectivityChanged?.Invoke(this, e);
        
        // Si la red se restaura, forzar health check
        if (e.IsAvailable && e.PreviousState != NetworkConnectivityState.Available)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(_resilienceConfig.Network.NetworkChangeDelay);
                await ForceHealthCheckAsync();
            });
        }
    }

    /// <summary>
    /// Cambia el estado de conexión y notifica
    /// </summary>
    private void ChangeState(DatabaseConnectionState newState, string reason, Exception? exception = null)
    {
        var previousState = _currentState;
        _currentState = newState;

        _logger.LogInformation("📊 Estado BD: {Previous} → {New} | {Reason}", 
            previousState, newState, reason);

        ConnectionStateChanged?.Invoke(this, new DatabaseConnectionStateChangedEventArgs(
            previousState, newState, reason, exception));
    }

    #endregion

    #region Métodos de Resiliencia

    /// <summary>
    /// Reinicia el Circuit Breaker (para testing/recovery manual)
    /// </summary>
    public void ResetCircuitBreaker()
    {
        _logger.LogInformation("🔄 Reiniciando Circuit Breaker manualmente");
        _circuitBreaker.Reset();
    }

    /// <summary>
    /// Obtiene métricas de la conexión
    /// </summary>
    public async Task<ConnectionMetrics> GetMetricsAsync()
    {
        return await Task.FromResult(_metricsCollector.GetMetrics());
    }

    #endregion

    #region Utilidades

    /// <summary>
    /// Valida la configuración al inicializar
    /// </summary>
    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
            throw new DatabaseConfigurationException("ConnectionString no puede estar vacío", "ConnectionString");
        
        if (string.IsNullOrWhiteSpace(_config.Server))
            throw new DatabaseConfigurationException("Server no puede estar vacío", "Server");
        
        if (string.IsNullOrWhiteSpace(_config.Database))
            throw new DatabaseConfigurationException("Database no puede estar vacío", "Database");

        _logger.LogDebug("✅ Configuración de base de datos validada exitosamente");
    }

    /// <summary>
    /// Obtiene mensaje de error específico para excepciones SQL
    /// </summary>
    private static string GetSqlErrorMessage(SqlException ex)
    {
        return ex.Number switch
        {
            2 => "El servidor SQL no está disponible o no se puede alcanzar",
            18 => "Error de autenticación con el servidor SQL",
            53 => "No se pudo establecer conexión con el servidor SQL",
            233 => "La conexión con el servidor SQL fue rechazada",
            10054 => "La conexión existente fue cerrada por el servidor",
            10060 => "Timeout al conectar con el servidor SQL",
            18456 => "Credenciales de autenticación inválidas",
            4060 => "La base de datos especificada no existe o no es accesible",
            _ => $"Error SQL #{ex.Number}: {ex.Message}"
        };
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _logger.LogDebug("🗑️ Disposing DatabaseConnectionService");
                
                _healthCheckTimer?.Dispose();
                _healthCheckSemaphore?.Dispose();
                _serviceTokenSource?.Cancel();
                _serviceTokenSource?.Dispose();
                
                _circuitBreaker?.Dispose();
                _networkMonitoring?.Dispose();
                
                _disposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Error durante dispose");
            }
        }
    }

    #endregion
}

/// <summary>
/// Colector de métricas para el servicio de conexión
/// </summary>
internal class ConnectionMetricsCollector
{
    private readonly object _lockObject = new();
    private readonly DateTime _startTime = DateTime.UtcNow;
    
    private int _totalConnections = 0;
    private int _successfulConnections = 0;
    private int _failedConnections = 0;
    private int _circuitBreakerTrips = 0;
    private long _totalConnectionTimeTicks = 0;
    private DateTime _lastSuccessfulConnection = DateTime.MinValue;
    private DateTime _lastFailedConnection = DateTime.MinValue;

    public void RegisterConnectionAttempt()
    {
        lock (_lockObject)
        {
            _totalConnections++;
        }
    }

    public void RegisterSuccessfulConnection(TimeSpan duration)
    {
        lock (_lockObject)
        {
            _successfulConnections++;
            _totalConnectionTimeTicks += duration.Ticks;
            _lastSuccessfulConnection = DateTime.UtcNow;
        }
    }

    public void RegisterFailedConnection(TimeSpan duration, Exception exception)
    {
        lock (_lockObject)
        {
            _failedConnections++;
            _totalConnectionTimeTicks += duration.Ticks;
            _lastFailedConnection = DateTime.UtcNow;
        }
    }

    public void RegisterCircuitBreakerTrip()
    {
        lock (_lockObject)
        {
            _circuitBreakerTrips++;
        }
    }

    public void RegisterHealthCheck(bool successful)
    {
        // Registrar health checks como conexiones regulares para métricas
        if (successful)
        {
            RegisterSuccessfulConnection(TimeSpan.Zero);
        }
        else
        {
            RegisterFailedConnection(TimeSpan.Zero, new Exception("Health check failed"));
        }
    }

    public ConnectionMetrics GetMetrics()
    {
        lock (_lockObject)
        {
            var uptime = DateTime.UtcNow - _startTime;
            var successRate = _totalConnections > 0 ? (double)_successfulConnections / _totalConnections * 100 : 0;
            var avgConnectionTime = _successfulConnections > 0 
                ? TimeSpan.FromTicks(_totalConnectionTimeTicks / _successfulConnections) 
                : TimeSpan.Zero;

            return new ConnectionMetrics(
                uptime,
                _totalConnections,
                _successfulConnections,
                _failedConnections,
                successRate,
                avgConnectionTime,
                _lastSuccessfulConnection,
                _lastFailedConnection,
                _circuitBreakerTrips);
        }
    }
}
