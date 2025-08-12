using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Servicio para detección automática de entorno de ejecución
/// Sigue SRP: Solo responsable de detectar el entorno actual
/// </summary>
public interface IEnvironmentDetectionService
{
    /// <summary>
    /// Nombre del entorno actual detectado
    /// </summary>
    string CurrentEnvironment { get; }

    /// <summary>
    /// Indica si la detección automática está habilitada
    /// </summary>
    bool AutoDetectionEnabled { get; }

    /// <summary>
    /// Detecta automáticamente el entorno basado en reglas configuradas
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Nombre del entorno detectado</returns>
    Task<string> DetectEnvironmentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuerza un entorno específico (útil para testing)
    /// </summary>
    /// <param name="environment">Nombre del entorno a forzar</param>
    void SetEnvironment(string environment);

    /// <summary>
    /// Valida si un entorno es válido según la configuración
    /// </summary>
    /// <param name="environment">Nombre del entorno a validar</param>
    /// <returns>True si es válido</returns>
    bool IsValidEnvironment(string environment);

    /// <summary>
    /// Obtiene la configuración específica para el entorno actual
    /// </summary>
    /// <typeparam name="T">Tipo de configuración</typeparam>
    /// <param name="sectionName">Nombre de la sección</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Configuración específica del entorno</returns>
    Task<T?> GetEnvironmentConfigAsync<T>(string sectionName, CancellationToken cancellationToken = default) where T : class;
}
