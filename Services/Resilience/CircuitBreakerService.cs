using GestLog.Models.Configuration;
using GestLog.Models.Enums;
using GestLog.Models.Events;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Resilience;

/// <summary>
/// Implementación del patrón Circuit Breaker para resiliencia de conexiones
/// </summary>
public class CircuitBreakerService : IDisposable
{
    private readonly CircuitBreakerConfig _config;
    private readonly IGestLogLogger _logger;
    private readonly object _lockObject = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private int _halfOpenAttempts = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private Exception? _lastException;
    private bool _disposed;

    public event EventHandler<CircuitBreakerStateChangedEventArgs>? StateChanged;

    public CircuitBreakerService(IOptions<DatabaseResilienceConfiguration> config, IGestLogLogger logger)
    {
        _config = config.Value.CircuitBreaker;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogInformation("🔒 CircuitBreaker inicializado - Threshold: {Threshold}, Delay: {Delay}", 
            _config.FailureThreshold, _config.OpenToHalfOpenDelay);
    }

    /// <summary>
    /// Estado actual del Circuit Breaker
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lockObject)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Número de fallos consecutivos
    /// </summary>
    public int FailureCount
    {
        get
        {
            lock (_lockObject)
            {
                return _failureCount;
            }
        }
    }

    /// <summary>
    /// Verifica si el circuito permite ejecución
    /// </summary>
    public bool CanExecute
    {
        get
        {
            lock (_lockObject)
            {
                return _state switch
                {
                    CircuitBreakerState.Closed => true,
                    CircuitBreakerState.Open => ShouldAttemptReset(),
                    CircuitBreakerState.HalfOpen => _halfOpenAttempts < _config.HalfOpenMaxAttempts,
                    _ => false
                };
            }
        }
    }

    /// <summary>
    /// Ejecuta una operación a través del Circuit Breaker
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (!CanExecute)
        {
            var exception = new InvalidOperationException($"Circuit Breaker está en estado {_state}. Operación bloqueada.");
            _logger.LogWarning("🚫 Operación bloqueada por Circuit Breaker - Estado: {State}, Fallos: {Failures}", 
                _state, _failureCount);
            throw exception;
        }

        try
        {
            var result = await operation(cancellationToken);
            await OnSuccessAsync();
            return result;
        }
        catch (Exception ex)
        {
            await OnFailureAsync(ex);
            throw;
        }
    }

    /// <summary>
    /// Maneja ejecución exitosa
    /// </summary>
    private async Task OnSuccessAsync()
    {
        await Task.Run(() =>
        {
            lock (_lockObject)
            {
                _successCount++;
                _lastException = null;

                switch (_state)
                {
                    case CircuitBreakerState.HalfOpen:
                        if (_successCount >= _config.SuccessThreshold)
                        {
                            ChangeState(CircuitBreakerState.Closed, "Suficientes éxitos consecutivos");
                            _failureCount = 0;
                            _successCount = 0;
                            _halfOpenAttempts = 0;
                        }
                        break;

                    case CircuitBreakerState.Closed:
                        if (_failureCount > 0)
                        {
                            _failureCount = Math.Max(0, _failureCount - 1); // Reducir gradualmente
                            _logger.LogDebug("🔄 Fallo recuperado - Contador reducido a {Count}", _failureCount);
                        }
                        break;
                }
            }
        });
    }

    /// <summary>
    /// Maneja fallo de ejecución
    /// </summary>
    private async Task OnFailureAsync(Exception exception)
    {
        await Task.Run(() =>
        {
            lock (_lockObject)
            {
                _failureCount++;
                _lastFailureTime = DateTime.UtcNow;
                _lastException = exception;
                _successCount = 0;

                switch (_state)
                {
                    case CircuitBreakerState.Closed:
                        if (_failureCount >= _config.FailureThreshold)
                        {
                            ChangeState(CircuitBreakerState.Open, $"Threshold alcanzado: {_failureCount} fallos");
                        }
                        break;

                    case CircuitBreakerState.HalfOpen:
                        _halfOpenAttempts++;
                        ChangeState(CircuitBreakerState.Open, "Fallo durante prueba de recuperación");
                        break;
                }
            }
        });
    }

    /// <summary>
    /// Verifica si debe intentar reset del circuito
    /// </summary>
    private bool ShouldAttemptReset()
    {
        if (_state != CircuitBreakerState.Open)
            return false;

        var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
        if (timeSinceLastFailure >= _config.OpenToHalfOpenDelay)
        {
            ChangeState(CircuitBreakerState.HalfOpen, "Tiempo de espera cumplido");
            _halfOpenAttempts = 0;
            _successCount = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cambia el estado del Circuit Breaker
    /// </summary>
    private void ChangeState(CircuitBreakerState newState, string reason)
    {
        var previousState = _state;
        _state = newState;

        _logger.LogInformation("🔄 Circuit Breaker: {Previous} → {New} | Razón: {Reason} | Fallos: {Failures}", 
            previousState, newState, reason, _failureCount);

        StateChanged?.Invoke(this, new CircuitBreakerStateChangedEventArgs(
            previousState, newState, reason, _failureCount, _lastException));
    }

    /// <summary>
    /// Fuerza el reset del Circuit Breaker (para testing)
    /// </summary>
    public void Reset()
    {
        lock (_lockObject)
        {
            ChangeState(CircuitBreakerState.Closed, "Reset manual");
            _failureCount = 0;
            _successCount = 0;
            _halfOpenAttempts = 0;
            _lastException = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogDebug("🔒 CircuitBreaker disposed");
            _disposed = true;
        }
    }
}
