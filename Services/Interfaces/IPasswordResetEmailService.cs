using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Services.Interfaces
{
    /// <summary>
    /// Servicio para enviar emails de reseteo/cambio de contraseña
    /// </summary>
    public interface IPasswordResetEmailService
    {
        /// <summary>
        /// Envía un email con la contraseña temporal al usuario
        /// </summary>
        /// <param name="userEmail">Email del usuario</param>
        /// <param name="userName">Nombre de usuario</param>
        /// <param name="temporaryPassword">Contraseña temporal generada</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>true si se envió correctamente, false en caso contrario</returns>
        Task<bool> SendPasswordResetEmailAsync(
            string userEmail,
            string userName,
            string temporaryPassword,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida que la configuración del servicio sea correcta
        /// </summary>
        /// <returns>true si la configuración es válida</returns>
        bool ValidateConfiguration();
    }
}
