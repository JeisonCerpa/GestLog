using System;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuración avanzada del sistema de resiliencia de base de datos
/// </summary>
public class DatabaseResilienceConfiguration
{
    /// <summary>
    /// Configuración del Circuit Breaker
    /// </summary>
    public CircuitBreakerConfig CircuitBreaker { get; set; } = new();
    
    /// <summary>
    /// Configuración de backoff exponencial
    /// </summary>
    public BackoffConfig Backoff { get; set; } = new();
    
    /// <summary>
    /// Configuración de Health Checks
    /// </summary>
    public HealthCheckConfig HealthCheck { get; set; } = new();
    
    /// <summary>
    /// Configuración de red
    /// </summary>
    public NetworkConfig Network { get; set; } = new();
}

/// <summary>
/// Configuración del Circuit Breaker
/// </summary>
public class CircuitBreakerConfig
{
    /// <summary>
    /// Número de fallos consecutivos antes de abrir el circuito
    /// </summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Tiempo que permanece abierto antes de pasar a HalfOpen
    /// </summary>
    public TimeSpan OpenToHalfOpenDelay { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Máximo número de intentos en estado HalfOpen
    /// </summary>
    public int HalfOpenMaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Número de éxitos consecutivos necesarios para cerrar el circuito
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;
}

/// <summary>
/// Configuración de backoff exponencial
/// </summary>
public class BackoffConfig
{
    /// <summary>
    /// Delay base para el primer intento
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Delay máximo entre intentos
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// Máximo número de reintentos
    /// </summary>
    public int MaxRetries { get; set; } = 10;
    
    /// <summary>
    /// Factor de multiplicación para backoff exponencial
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Jitter máximo en milisegundos para evitar sincronización
    /// </summary>
    public int MaxJitterMs { get; set; } = 1000;
}

/// <summary>
/// Configuración de Health Checks
/// </summary>
public class HealthCheckConfig
{
    /// <summary>
    /// Intervalo entre health checks
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(2);
    
    /// <summary>
    /// Timeout para health checks
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Habilitar modo adaptativo
    /// </summary>
    public bool AdaptiveMode { get; set; } = true;
    
    /// <summary>
    /// Timeout para consultas de health check
    /// </summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Configuración de red
/// </summary>
public class NetworkConfig
{
    /// <summary>
    /// Habilitar monitoreo de eventos de red
    /// </summary>
    public bool EnableNetworkMonitoring { get; set; } = true;
    
    /// <summary>
    /// Delay después de detectar cambio de red antes de verificar conexión
    /// </summary>
    public TimeSpan NetworkChangeDelay { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Habilitar verificación de conectividad a internet
    /// </summary>
    public bool EnableInternetConnectivityCheck { get; set; } = true;
    
    /// <summary>
    /// Host para verificar conectividad a internet
    /// </summary>
    public string ConnectivityCheckHost { get; set; } = "8.8.8.8";
    
    /// <summary>
    /// Puerto para verificar conectividad
    /// </summary>
    public int ConnectivityCheckPort { get; set; } = 53;
    
    /// <summary>
    /// Timeout para verificación de conectividad
    /// </summary>
    public TimeSpan ConnectivityCheckTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
