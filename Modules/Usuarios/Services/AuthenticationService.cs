using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Helpers;
using GestLog.Modules.Usuarios.Helpers.Exceptions;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Interfaces;
using Modules.Usuarios.Helpers;

namespace GestLog.Modules.Usuarios.Services
{
    /// <summary>
    /// Servicio de autenticación de usuarios
    /// Responsabilidad: Gestionar login, logout y verificación de credenciales
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IAuditoriaService _auditoriaService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IGestLogLogger _logger;

        public AuthenticationService(
            IUsuarioRepository usuarioRepository,
            IAuditoriaService auditoriaService,
            ICurrentUserService currentUserService,
            IGestLogLogger logger)
        {
            _usuarioRepository = usuarioRepository ?? throw new ArgumentNullException(nameof(usuarioRepository));
            _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResult> LoginAsync(LoginRequest loginRequest, CancellationToken cancellationToken = default)
        {
            if (loginRequest == null)
                throw new ArgumentNullException(nameof(loginRequest));

            if (string.IsNullOrWhiteSpace(loginRequest.Username))
                return AuthResult.FailureResult("El nombre de usuario es obligatorio", "EMPTY_USERNAME");

            if (string.IsNullOrWhiteSpace(loginRequest.Password))
                return AuthResult.FailureResult("La contraseña es obligatoria", "EMPTY_PASSWORD");

            try
            {
                _logger.LogInformation("Intento de login para usuario: {Username}", loginRequest.Username);

                // Buscar usuario por nombre de usuario
                var usuarios = await _usuarioRepository.BuscarAsync(loginRequest.Username);
                var usuario = usuarios.FirstOrDefault(u => 
                    u.NombreUsuario.Equals(loginRequest.Username, StringComparison.OrdinalIgnoreCase));

                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {Username}", loginRequest.Username);
                    await LogLoginAttemptAsync(loginRequest.Username, false, "Usuario no encontrado", cancellationToken);
                    return AuthResult.FailureResult("Usuario o contraseña incorrectos", "INVALID_CREDENTIALS");
                }

                // Verificar si el usuario está activo
                if (!usuario.Activo || usuario.Desactivado)
                {
                    _logger.LogWarning("Usuario desactivado: {Username}", loginRequest.Username);
                    await LogLoginAttemptAsync(loginRequest.Username, false, "Usuario desactivado", cancellationToken);
                    return AuthResult.FailureResult("El usuario está desactivado", "USER_DEACTIVATED");
                }                // Verificar contraseña: primero intentar con temporal (si existe y es válida)
                var isValidPassword = false;
                  // Verificar si existe contraseña temporal válida
                if (!string.IsNullOrEmpty(usuario.TemporaryPasswordHash) &&
                    !string.IsNullOrEmpty(usuario.TemporaryPasswordSalt) &&
                    usuario.TemporaryPasswordExpiration.HasValue &&
                    DateTime.UtcNow <= usuario.TemporaryPasswordExpiration.Value)
                {
                    // Verificar contra la contraseña temporal
                    isValidPassword = PasswordHelper.VerifyPassword(
                        loginRequest.Password,
                        usuario.TemporaryPasswordHash,
                        usuario.TemporaryPasswordSalt);
                    
                    if (isValidPassword)
                    {
                        _logger.LogInformation("Login con contraseña temporal para usuario: {Username}", loginRequest.Username);
                        // Marcar que debe cambiar contraseña
                        usuario.IsFirstLogin = true;
                    }
                }
                
                // Si no es temporal válida, verificar contra contraseña permanente
                if (!isValidPassword)
                {
                    isValidPassword = PasswordHelper.VerifyPassword(loginRequest.Password, usuario.HashContrasena, usuario.Salt);
                }
                
                if (!isValidPassword)
                {
                    _logger.LogWarning("Contraseña incorrecta para usuario: {Username}", loginRequest.Username);
                    await LogLoginAttemptAsync(loginRequest.Username, false, "Contraseña incorrecta", cancellationToken);
                    return AuthResult.FailureResult("Usuario o contraseña incorrectos", "INVALID_CREDENTIALS");
                }

                // Actualizar fecha de último acceso
                usuario.FechaUltimoAcceso = DateTime.UtcNow;
                await _usuarioRepository.ActualizarAsync(usuario);

                // Cargar roles y permisos del usuario
                var userInfo = await BuildCurrentUserInfoAsync(usuario, cancellationToken);

                // Establecer usuario actual en sesión
                _currentUserService.SetCurrentUser(userInfo);

                _logger.LogInformation("Login exitoso para usuario: {Username}", loginRequest.Username);
                await LogLoginAttemptAsync(loginRequest.Username, true, "Login exitoso", cancellationToken);

                return AuthResult.SuccessResult(usuario, userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el login para usuario: {Username}", loginRequest.Username);
                await LogLoginAttemptAsync(loginRequest.Username, false, $"Error del sistema: {ex.Message}", cancellationToken);
                return AuthResult.FailureResult("Error interno del sistema. Contacte al administrador", "SYSTEM_ERROR");
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = _currentUserService.Current;
            if (currentUser != null)
            {
                _logger.LogInformation("Cerrando sesión para usuario: {Username}", currentUser.Username);

                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Sesión",
                    IdEntidad = currentUser.UserId,
                    Accion = "Logout",
                    UsuarioResponsable = currentUser.Username,
                    FechaHora = DateTime.UtcNow,
                    Detalle = $"Cierre de sesión del usuario: {currentUser.Username}"
                });

                _currentUserService.ClearCurrentUser();
                _logger.LogInformation("Sesión cerrada exitosamente para: {Username}", currentUser.Username);
            }
        }

        public async Task<bool> VerifyCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                var usuarios = await _usuarioRepository.BuscarAsync(username);
                var usuario = usuarios.FirstOrDefault(u => 
                    u.NombreUsuario.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (usuario == null || !usuario.Activo || usuario.Desactivado)
                    return false;

                return PasswordHelper.VerifyPassword(password, usuario.HashContrasena, usuario.Salt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando credenciales para usuario: {Username}", username);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());
                
                // Verificar permisos directos del usuario
                var hasDirectPermission = await db.UsuarioPermisos
                    .AnyAsync(up => up.IdUsuario == userId && 
                                   db.Permisos.Any(p => p.IdPermiso == up.IdPermiso && p.Nombre == permission), 
                             cancellationToken);

                if (hasDirectPermission)
                    return true;

                // Verificar permisos a través de roles
                var hasRolePermission = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == userId)
                    .AnyAsync(ur => db.RolPermisos
                        .Any(rp => rp.IdRol == ur.IdRol && 
                                  db.Permisos.Any(p => p.IdPermiso == rp.IdPermiso && p.Nombre == permission)), 
                             cancellationToken);

                return hasRolePermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando permiso {Permission} para usuario {UserId}", permission, userId);
                return false;
            }
        }

        public async Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());
                
                return await db.UsuarioRoles
                    .AnyAsync(ur => ur.IdUsuario == userId && 
                                   db.Roles.Any(r => r.IdRol == ur.IdRol && r.Nombre == roleName), 
                             cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando rol {Role} para usuario {UserId}", roleName, userId);
                return false;
            }
        }

        public async Task LogLoginAttemptAsync(string username, bool success, string? errorMessage = null, CancellationToken cancellationToken = default)
        {
            try
            {
                await _auditoriaService.RegistrarEventoAsync(new Auditoria
                {
                    IdAuditoria = Guid.NewGuid(),
                    EntidadAfectada = "Autenticación",
                    IdEntidad = Guid.Empty, // No tenemos ID específico para intentos de login
                    Accion = success ? "LoginExitoso" : "LoginFallido",
                    UsuarioResponsable = username,
                    FechaHora = DateTime.UtcNow,
                    Detalle = success ? 
                        $"Login exitoso para usuario: {username}" : 
                        $"Login fallido para usuario: {username}. Motivo: {errorMessage ?? "No especificado"}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando intento de login para usuario: {Username}", username);
            }
        }

        private async Task<CurrentUserInfo> BuildCurrentUserInfoAsync(Usuario usuario, CancellationToken cancellationToken)
        {
            try
            {
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());

                // Obtener información de la persona asociada
                var persona = await db.Personas.FirstOrDefaultAsync(p => p.IdPersona == usuario.PersonaId, cancellationToken);

                // Obtener roles del usuario
                var roles = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == usuario.IdUsuario)
                    .Join(db.Roles, ur => ur.IdRol, r => r.IdRol, (ur, r) => r.Nombre)
                    .ToListAsync(cancellationToken);

                // Obtener permisos directos
                var directPermissions = await db.UsuarioPermisos
                    .Where(up => up.IdUsuario == usuario.IdUsuario)
                    .Join(db.Permisos, up => up.IdPermiso, p => p.IdPermiso, (up, p) => p.Nombre)
                    .ToListAsync(cancellationToken);

                // Obtener permisos por roles
                var rolePermissions = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == usuario.IdUsuario)
                    .Join(db.RolPermisos, ur => ur.IdRol, rp => rp.IdRol, (ur, rp) => rp.IdPermiso)
                    .Join(db.Permisos, rp => rp, p => p.IdPermiso, (rp, p) => p.Nombre)
                    .ToListAsync(cancellationToken);

                // Combinar todos los permisos
                var allPermissions = directPermissions.Concat(rolePermissions).Distinct().ToList();                var fullName = persona != null ? $"{persona.Nombres} {persona.Apellidos}" : usuario.NombreUsuario;
                var email = persona?.Correo ?? "";

                return new CurrentUserInfo
                {
                    UserId = usuario.IdUsuario,
                    Username = usuario.NombreUsuario,
                    FullName = fullName,
                    Email = email,
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    Roles = roles,
                    Permissions = allPermissions,
                    IsFirstLogin = usuario.IsFirstLogin
                };
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error construyendo información del usuario actual para {Username}", usuario.NombreUsuario);
                
                // Fallback con información básica
                return new CurrentUserInfo
                {
                    UserId = usuario.IdUsuario,
                    Username = usuario.NombreUsuario,
                    FullName = usuario.NombreUsuario,
                    Email = "",
                    LoginTime = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    Roles = new(),
                    Permissions = new(),
                    IsFirstLogin = usuario.IsFirstLogin
                };
            }
        }
    }
}
