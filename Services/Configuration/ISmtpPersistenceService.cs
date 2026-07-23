using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Configuration;

namespace GestLog.Services.Configuration;

/// <summary>
/// Almacén único de la configuración de envío de correo de Gestión de Cartera.
/// Es la ÚNICA autoridad: lee/escribe los datos no secretos en app-config.json
/// (modules.gestionCartera.smtp) y la contraseña en Windows Credential Manager,
/// siempre bajo la misma llave. No cachea ni audita.
/// </summary>
public interface ISmtpPersistenceService
{
    /// <summary>
    /// Carga la configuración completa (JSON + contraseña desde Credential Manager,
    /// migrando llaves antiguas si hace falta). Devuelve null si no hay config.
    /// </summary>
    Task<SmtpSettings?> LoadSmtpConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Guarda la configuración: datos no secretos en JSON y la contraseña
    /// (si viene) en Credential Manager, bajo la llave unificada.
    /// </summary>
    Task<bool> SaveSmtpConfigurationAsync(SmtpSettings configuration, string operationSource = "Unknown", CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida que la configuración tenga los datos mínimos y correos BCC/CC válidos.
    /// </summary>
    bool ValidateConfiguration(SmtpSettings? configuration);
}
