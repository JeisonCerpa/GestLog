using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Configuration;

namespace GestLog.Services.Configuration;

/// <summary>
/// Interfaz para servicio unificado de persistencia SMTP con auditoría exhaustiva
/// Centraliza todas las operaciones de carga, guardado y auditoría de configuración SMTP
/// </summary>
public interface ISmtpPersistenceService
{
    /// <summary>
    /// Carga la configuración SMTP actual desde el almacenamiento
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Configuración SMTP cargada o null si no existe</returns>
    Task<SmtpSettings?> LoadSmtpConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la configuración SMTP en el almacenamiento con auditoría
    /// </summary>
    /// <param name="configuration">Configuración SMTP a guardar</param>
    /// <param name="operationSource">Fuente de la operación (ej: "UI", "AutoLoad", "Migration")</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>true si se guardó correctamente, false en caso contrario</returns>
    Task<bool> SaveSmtpConfigurationAsync(SmtpSettings configuration, string operationSource = "Unknown", CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la configuración SMTP actual (desde cache si está disponible)
    /// </summary>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Configuración SMTP actual o null</returns>
    Task<SmtpSettings?> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida que la configuración SMTP sea válida antes de guardar
    /// </summary>
    /// <param name="configuration">Configuración a validar</param>
    /// <returns>true si es válida, false en caso contrario</returns>
    bool ValidateConfiguration(SmtpSettings? configuration);

    /// <summary>
    /// Obtiene el historial de auditoría de cambios SMTP
    /// </summary>
    /// <param name="maxEntries">Número máximo de entradas a retornar (0 = todas)</param>
    /// <returns>Lista de entradas de auditoría en formato JSON</returns>
    Task<string[]> GetAuditTrailAsync(int maxEntries = 0);

    /// <summary>
    /// Limpia la configuración SMTP con auditoría
    /// </summary>
    /// <param name="operationSource">Fuente de la operación</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>true si se limpió correctamente</returns>
    Task<bool> ClearConfigurationAsync(string operationSource = "Unknown", CancellationToken cancellationToken = default);
}
