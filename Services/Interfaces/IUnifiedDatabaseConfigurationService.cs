using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Interfaces;

/// <summary>
/// Servicio unificado para configuración de base de datos que integra múltiples fuentes
/// Prioridad: Variables de entorno → Archivos específicos de entorno → Configuración de respaldo
/// </summary>
public interface IUnifiedDatabaseConfigurationService
{
    /// <summary>
    /// Obtiene la cadena de conexión usando la estrategia de configuración unificada
    /// </summary>
    Task<string> GetConnectionStringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida que la configuración sea válida y esté disponible
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el servidor de base de datos configurado
    /// </summary>
    string GetDatabaseServer();

    /// <summary>
    /// Obtiene el nombre de la base de datos configurada
    /// </summary>
    string GetDatabaseName();

    /// <summary>
    /// Indica si debe usar seguridad integrada de Windows
    /// </summary>
    bool UseIntegratedSecurity();

    /// <summary>
    /// Prueba la conexión con la configuración actual
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si hay una configuración válida disponible
    /// </summary>
    Task<bool> HasValidConfigurationAsync(CancellationToken cancellationToken = default);
}
