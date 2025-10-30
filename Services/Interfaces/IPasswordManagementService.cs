using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models.DTOs;

namespace GestLog.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de gestión de cambio de contraseña
    /// Maneja cambios de contraseña obligatorios en primer login y recuperación
    /// </summary>
    public interface IPasswordManagementService
    {
        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        /// <param name="userId">ID del usuario</param>
        /// <param name="request">DTO con datos de cambio de contraseña</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si el cambio fue exitoso, False en caso contrario</returns>
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Genera una contraseña temporal y la envía al usuario por email
        /// </summary>
        /// <param name="usernameOrEmail">Nombre de usuario o correo del usuario</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Objeto con resultado de la operación</returns>
        Task<ForgotPasswordResponse> SendPasswordResetEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida si una contraseña cumple con los requisitos de seguridad
        /// </summary>
        /// <param name="password">Contraseña a validar</param>
        /// <returns>Tuple con validez y mensaje de error (si aplica)</returns>
        (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password);

        /// <summary>
        /// Genera una contraseña temporal sencilla (números y letras)
        /// </summary>
        /// <param name="length">Longitud de la contraseña temporal (default: 10)</param>
        /// <returns>Contraseña temporal generada</returns>
        string GenerateTemporaryPassword(int length = 10);

        /// <summary>
        /// Obtiene el email de un usuario por su nombre de usuario o email
        /// </summary>
        /// <param name="usernameOrEmail">Nombre de usuario o correo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Información de usuario (nombre completo y email) o null si no existe</returns>
        Task<(string UserName, string Email)?> GetUserEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
    }
}
