using System;
using System.Linq;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;

namespace GestLog.Modules.Usuarios.Services
{
    /// <summary>
    /// Servicio para gestionar la información del usuario autenticado actual
    /// Responsabilidad: Mantener el estado de la sesión del usuario
    /// </summary>
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IGestLogLogger _logger;
        private CurrentUserInfo? _currentUser;        public CurrentUserService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public CurrentUserInfo? Current => _currentUser;

        public bool IsAuthenticated => _currentUser != null;

        public event EventHandler<CurrentUserInfo?>? CurrentUserChanged;        public void SetCurrentUser(CurrentUserInfo userInfo, bool rememberMe = false)
        {
            if (userInfo == null)
                throw new ArgumentNullException(nameof(userInfo));

            _currentUser = userInfo;
            _logger.LogInformation("Usuario establecido en sesión: {Username} ({UserId})", 
                userInfo.Username, userInfo.UserId);
            if (rememberMe)
                UserSessionPersistence.SaveSession(userInfo);
            CurrentUserChanged?.Invoke(this, _currentUser);
        }        public void RestoreSessionIfExists()
        {
            var restored = UserSessionPersistence.LoadSession();
            if (restored != null)
            {
                _currentUser = restored;
                _logger.LogInformation("Sesión restaurada automáticamente para usuario: {Username}", restored.Username);
                
                // Refrescar permisos y roles desde la base de datos para asegurar información actualizada
                _ = RefreshUserPermissionsAsync();
                
                CurrentUserChanged?.Invoke(this, _currentUser);
            }
        }

        public void ClearCurrentUser()
        {
            var previousUser = _currentUser?.Username ?? "Unknown";
            _currentUser = null;
            UserSessionPersistence.ClearSession();
            _logger.LogInformation("Sesión cerrada para usuario: {Username}", previousUser);
            CurrentUserChanged?.Invoke(this, null);
        }

        public void UpdateActivity()
        {
            if (_currentUser != null)
            {
                _currentUser.UpdateActivity();
                _logger.LogDebug("Actividad actualizada para usuario: {Username}", _currentUser.Username);
            }
        }

        public bool HasPermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
                return false;

            var hasPermission = _currentUser?.HasPermission(permission) ?? false;
            
            _logger.LogDebug("Verificación de permiso '{Permission}' para usuario '{Username}': {HasPermission}", 
                permission, _currentUser?.Username ?? "None", hasPermission);
            
            return hasPermission;
        }

        public bool HasRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            var hasRole = _currentUser?.HasRole(roleName) ?? false;
            
            _logger.LogDebug("Verificación de rol '{Role}' para usuario '{Username}': {HasRole}", 
                roleName, _currentUser?.Username ?? "None", hasRole);
            
            return hasRole;
        }        public string GetCurrentUserFullName()
        {
            return _currentUser?.FullName ?? "Usuario no autenticado";
        }        public Guid? GetCurrentUserId()
        {
            return _currentUser?.UserId;
        }

        /// <summary>
        /// Refresca los permisos y roles del usuario actual desde la base de datos
        /// </summary>
        private async Task RefreshUserPermissionsAsync()
        {
            if (_currentUser == null) return;

            try
            {
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());

                // Obtener roles del usuario
                var roles = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == _currentUser.UserId)
                    .Join(db.Roles, ur => ur.IdRol, r => r.IdRol, (ur, r) => r.Nombre)
                    .ToListAsync();

                // Obtener permisos directos
                var directPermissions = await db.UsuarioPermisos
                    .Where(up => up.IdUsuario == _currentUser.UserId)
                    .Join(db.Permisos, up => up.IdPermiso, p => p.IdPermiso, (up, p) => p.Nombre)
                    .ToListAsync();

                // Obtener permisos por roles
                var rolePermissions = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == _currentUser.UserId)
                    .Join(db.RolPermisos, ur => ur.IdRol, rp => rp.IdRol, (ur, rp) => rp.IdPermiso)
                    .Join(db.Permisos, rp => rp, p => p.IdPermiso, (rp, p) => p.Nombre)
                    .ToListAsync();

                // Combinar todos los permisos
                var allPermissions = directPermissions.Concat(rolePermissions).Distinct().ToList();

                // Actualizar los permisos y roles en la sesión actual
                _currentUser.Roles = roles;
                _currentUser.Permissions = allPermissions;
                _currentUser.LastActivity = DateTime.UtcNow;

                _logger.LogInformation("Permisos y roles actualizados para usuario: {Username}. Roles: {RoleCount}, Permisos: {PermissionCount}", 
                    _currentUser.Username, roles.Count, allPermissions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refrescando permisos para usuario: {Username}", _currentUser.Username);
            }
        }
    }
}
