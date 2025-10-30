using System;
using System.Collections.Generic;
using System.Linq;

namespace GestLog.Modules.Usuarios.Models.Authentication
{
    /// <summary>
    /// Información del usuario autenticado actualmente en sesión
    /// </summary>
    public class CurrentUserInfo
    {
        public Guid UserId { get; set; }
        public required string Username { get; set; }
        public required string FullName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime LastActivity { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
        
        /// <summary>
        /// Indica si es el primer login del usuario (requiere cambio obligatorio de contraseña)
        /// </summary>
        public bool IsFirstLogin { get; set; } = false;

        /// <summary>
        /// Verifica si el usuario tiene un rol específico
        /// </summary>
        public bool HasRole(string roleName)
        {
            return Roles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si el usuario tiene un permiso específico
        /// </summary>
        public bool HasPermission(string permissionName)
        {
            return Permissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifica si el usuario tiene alguno de los roles especificados
        /// </summary>
        public bool HasAnyRole(params string[] roleNames)
        {
            return roleNames.Any(role => HasRole(role));
        }

        /// <summary>
        /// Verifica si el usuario tiene alguno de los permisos especificados
        /// </summary>
        public bool HasAnyPermission(params string[] permissionNames)
        {
            return permissionNames.Any(permission => HasPermission(permission));
        }

        /// <summary>
        /// Actualiza la hora de última actividad
        /// </summary>
        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}
