using System;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de la sesión del usuario actual
    /// Responsabilidad: Mantener y proporcionar información del usuario autenticado
    /// </summary>
    public interface ICurrentUserService
    {
        /// <summary>
        /// Información del usuario actual (null si no está autenticado)
        /// </summary>
        CurrentUserInfo? Current { get; }

        /// <summary>
        /// Indica si hay un usuario autenticado
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Establece el usuario actual después de un login exitoso
        /// </summary>
        void SetCurrentUser(CurrentUserInfo userInfo);

        /// <summary>
        /// Limpia la información del usuario actual (logout)
        /// </summary>
        void ClearCurrentUser();

        /// <summary>
        /// Actualiza la última actividad del usuario
        /// </summary>
        void UpdateActivity();

        /// <summary>
        /// Verifica si el usuario actual tiene un permiso específico
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Verifica si el usuario actual tiene un rol específico
        /// </summary>
        bool HasRole(string roleName);

        /// <summary>
        /// Obtiene el nombre completo del usuario actual
        /// </summary>
        string GetCurrentUserFullName();

        /// <summary>
        /// Obtiene el ID del usuario actual
        /// </summary>
        Guid? GetCurrentUserId();

        /// <summary>
        /// Evento que se dispara cuando cambia el usuario actual
        /// </summary>
        event EventHandler<CurrentUserInfo?> CurrentUserChanged;
    }
}
