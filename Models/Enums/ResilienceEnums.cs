using System;

namespace GestLog.Models.Enums;

/// <summary>
/// Estados del Circuit Breaker para resiliencia de conexiones
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuito cerrado - operación normal
    /// </summary>
    Closed,
    
    /// <summary>
    /// Circuito abierto - falla rápida, no permite intentos
    /// </summary>
    Open,
    
    /// <summary>
    /// Circuito semi-abierto - permite intentos limitados para probar recuperación
    /// </summary>
    HalfOpen
}

/// <summary>
/// Estados de conectividad de red
/// </summary>
public enum NetworkConnectivityState
{
    /// <summary>
    /// Estado desconocido
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Red disponible
    /// </summary>
    Available,
    
    /// <summary>
    /// Red no disponible
    /// </summary>
    Unavailable,
    
    /// <summary>
    /// Conectividad limitada
    /// </summary>
    Limited
}
