using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para el servicio de autenticación
    /// Responsabilidad: Gestionar el login, logout y validación de credenciales
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Autentica un usuario con sus credenciales
        /// </summary>
        Task<AuthResult> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cierra la sesión del usuario actual
        /// </summary>
        Task LogoutAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica las credenciales sin iniciar sesión
        /// </summary>
        Task<bool> VerifyCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si un usuario tiene un rol específico
        /// </summary>
        Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registra un intento de login (exitoso o fallido) para auditoría
        /// </summary>
        Task LogLoginAttemptAsync(string username, bool success, string? errorMessage = null, CancellationToken cancellationToken = default);
    }
}
