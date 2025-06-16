using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Resilience;

/// <summary>
/// Servicio para implementar backoff exponencial con jitter
/// </summary>
public class ExponentialBackoffService
{
    private readonly BackoffConfig _config;
    private readonly IGestLogLogger _logger;
    private readonly Random _random = new();

    public ExponentialBackoffService(IOptions<DatabaseResilienceConfiguration> config, IGestLogLogger logger)
    {
        _config = config.Value.Backoff;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _logger.LogDebug("📈 ExponentialBackoff inicializado - Base: {Base}, Max: {Max}, Retries: {Retries}", 
            _config.BaseDelay, _config.MaxDelay, _config.MaxRetries);
    }

    /// <summary>
    /// Ejecuta una operación con backoff exponencial
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var attemptNumber = 0;
        Exception? lastException = null;

        while (attemptNumber <= _config.MaxRetries)
        {
            try
            {
                if (attemptNumber > 0)
                {
                    var delay = CalculateDelay(attemptNumber);
                    _logger.LogDebug("⏳ Backoff delay: {Delay}ms para intento {Attempt}/{Max}", 
                        delay.TotalMilliseconds, attemptNumber + 1, _config.MaxRetries + 1);
                    
                    await Task.Delay(delay, cancellationToken);
                }

                var result = await operation(attemptNumber, cancellationToken);
                
                if (attemptNumber > 0)
                {
                    _logger.LogInformation("✅ Operación exitosa tras {Attempts} intentos", attemptNumber + 1);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("❌ Operación cancelada en intento {Attempt}", attemptNumber + 1);
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                attemptNumber++;
                
                if (attemptNumber > _config.MaxRetries)
                {
                    _logger.LogWarning("❌ Todos los intentos fallaron tras {Attempts} retries. Último error: {Error}", 
                        _config.MaxRetries + 1, ex.Message);
                    break;
                }
                
                _logger.LogDebug("⚠️ Intento {Attempt} falló: {Error}. Siguiente intento en {Delay}ms", 
                    attemptNumber, ex.Message, CalculateDelay(attemptNumber).TotalMilliseconds);
            }
        }

        throw lastException ?? new InvalidOperationException("Operación falló sin excepción específica");
    }

    /// <summary>
    /// Ejecuta una operación simple con backoff exponencial
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync((attempt, ct) => operation(ct), cancellationToken);
    }

    /// <summary>
    /// Ejecuta una operación sin retorno con backoff exponencial
    /// </summary>
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async (ct) => 
        {
            await operation(ct);
            return true; // Dummy return
        }, cancellationToken);
    }

    /// <summary>
    /// Calcula el delay para el intento especificado
    /// </summary>
    private TimeSpan CalculateDelay(int attemptNumber)
    {
        if (attemptNumber <= 0)
            return TimeSpan.Zero;

        // Backoff exponencial: base * (multiplier ^ attempt)
        var exponentialDelay = TimeSpan.FromTicks(
            (long)(_config.BaseDelay.Ticks * Math.Pow(_config.BackoffMultiplier, attemptNumber - 1)));

        // Aplicar jitter para evitar thundering herd
        var jitterMs = _random.Next(0, _config.MaxJitterMs);
        var jitter = TimeSpan.FromMilliseconds(jitterMs);

        // Combinar delay exponencial + jitter
        var totalDelay = exponentialDelay.Add(jitter);

        // Limitar al máximo configurado
        return totalDelay > _config.MaxDelay ? _config.MaxDelay : totalDelay;
    }

    /// <summary>
    /// Obtiene el delay calculado para un intento específico (para testing/logging)
    /// </summary>
    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        return CalculateDelay(attemptNumber);
    }

    /// <summary>
    /// Obtiene el número máximo de reintentos configurado
    /// </summary>
    public int MaxRetries => _config.MaxRetries;
}
