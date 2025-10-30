using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de envío de correos para autenticación
    /// Servicio independiente para cambios de contraseña y recuperación
    /// </summary>
    public interface IAuthenticationEmailService
    {
        /// <summary>
        /// Envía contraseña temporal al usuario
        /// </summary>
        /// <param name="userEmail">Email del usuario</param>
        /// <param name="userName">Nombre del usuario</param>
        /// <param name="temporaryPassword">Contraseña temporal generada</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si se envió correctamente</returns>
        Task<bool> SendTemporaryPasswordAsync(string userEmail, string userName, string temporaryPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía confirmación de cambio de contraseña exitoso
        /// </summary>
        /// <param name="userEmail">Email del usuario</param>
        /// <param name="userName">Nombre del usuario</param>
        /// <param name="timestamp">Timestamp del cambio</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si se envió correctamente</returns>
        Task<bool> SendPasswordChangeConfirmationAsync(string userEmail, string userName, System.DateTime timestamp, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía alerta de intento de recuperación de contraseña
        /// </summary>
        /// <param name="userEmail">Email del usuario</param>
        /// <param name="userName">Nombre del usuario</param>
        /// <param name="timestamp">Timestamp del intento</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si se envió correctamente</returns>
        Task<bool> SendPasswordRecoveryAlertAsync(string userEmail, string userName, System.DateTime timestamp, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si el servicio está configurado correctamente
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si está disponible</returns>
        Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
    }
}
