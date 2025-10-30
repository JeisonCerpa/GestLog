using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models.DTOs;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Helpers;

namespace GestLog.Services.Core
{
    /// <summary>
    /// Servicio para gestión de cambio de contraseña y recuperación de contraseña olvidada
    /// </summary>
    public class PasswordManagementService : IPasswordManagementService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IPasswordResetEmailService _emailService;
        private readonly IGestLogLogger _logger;

        // Expresión regular para validar fortaleza de contraseña
        // Requisitos: Mayúsculas, minúsculas, números, caracteres especiales, mínimo 8 caracteres
        private const string PasswordStrengthPattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?])(?=.{8,})";

        public PasswordManagementService(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IPasswordResetEmailService emailService,
            IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Intento de cambio de contraseña con userId vacío");
                    return false;
                }

                if (request == null)
                {
                    _logger.LogWarning("ChangePasswordRequest es nulo para usuario: {UserId}", userId);
                    return false;
                }

                // Validar fortaleza de la nueva contraseña
                var (isValid, errorMessage) = ValidatePasswordStrength(request.NewPassword);
                if (!isValid)
                {
                    _logger.LogWarning("Contraseña débil para usuario: {UserId}. Error: {Error}", userId, errorMessage);
                    return false;
                }

                using var db = _dbContextFactory.CreateDbContext();

                if (!Guid.TryParse(userId, out var guidUserId))
                {
                    _logger.LogWarning("UserId inválido (no es GUID): {UserId}", userId);
                    return false;
                }

                var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == guidUserId, cancellationToken);
                if (usuario == null)
                {
                    _logger.LogWarning("Usuario no encontrado: {UserId}", userId);
                    return false;
                }                // Verificar contraseña actual
                var isCurrentPasswordValid = PasswordHelper.VerifyPassword(
                    request.CurrentPassword,
                    usuario.HashContrasena,
                    usuario.Salt);

                if (!isCurrentPasswordValid && !request.IsFirstLoginChange)
                {
                    _logger.LogWarning("Contraseña actual incorrecta para usuario: {UserId}", userId);
                    return false;
                }                // Hash la nueva contraseña
                var (hash, salt) = HashPassword(request.NewPassword);

                // Actualizar usuario
                usuario.HashContrasena = hash;
                usuario.Salt = salt;
                usuario.PasswordChangedAt = DateTime.UtcNow;
                usuario.IsFirstLogin = false;
                usuario.LastPasswordChangeAttempt = DateTime.UtcNow;
                
                // Limpiar contraseña temporal (ya no es válida)
                usuario.TemporaryPasswordHash = null;
                usuario.TemporaryPasswordSalt = null;
                usuario.TemporaryPasswordExpiration = null;

                db.Usuarios.Update(usuario);
                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Cambio de contraseña exitoso para usuario: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña para usuario: {UserId}", userId);
                return false;
            }
        }        /// <summary>
        /// Genera una contraseña temporal y envía email al usuario
        /// </summary>
        public async Task<ForgotPasswordResponse> SendPasswordResetEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(usernameOrEmail))
                {
                    _logger.LogWarning("Solicitud de reset de contraseña con usuario/email vacío");
                    return ForgotPasswordResponse.FailureResponse("El usuario o email es requerido");
                }

                // Obtener información del usuario
                var userInfo = await GetUserEmailAsync(usernameOrEmail, cancellationToken);
                if (userInfo == null)
                {
                    _logger.LogWarning("Usuario no encontrado para reset: {UsernameOrEmail}", usernameOrEmail);
                    return ForgotPasswordResponse.FailureResponse("Usuario o email no encontrado");
                }

                // Generar contraseña temporal
                var temporaryPassword = GenerateTemporaryPassword();                // Buscar usuario y actualizar con la contraseña temporal
                using var db = _dbContextFactory.CreateDbContext();

                var usuario = await db.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == usernameOrEmail ||
                                               db.Personas.Any(p => p.IdPersona == u.PersonaId && p.Correo == usernameOrEmail),
                        cancellationToken);

                if (usuario == null)
                {
                    _logger.LogWarning("❌ Usuario no encontrado en BD para reset: {UsernameOrEmail}", usernameOrEmail);
                    return ForgotPasswordResponse.FailureResponse("Usuario no encontrado");
                }

                _logger.LogInformation("📌 Usuario encontrado: ID={UserId}, Nombre={Username}", usuario.IdUsuario, usuario.NombreUsuario);// Hash la contraseña temporal (usar columnas temporales, no reemplazar la actual)
                var (tempHash, tempSalt) = HashPassword(temporaryPassword);

                usuario.TemporaryPasswordHash = tempHash;
                usuario.TemporaryPasswordSalt = tempSalt;
                usuario.TemporaryPasswordExpiration = DateTime.UtcNow.AddHours(24); // Válida por 24 horas
                usuario.LastPasswordChangeAttempt = DateTime.UtcNow;

                _logger.LogInformation("🔐 Guardando contraseña temporal para usuario: {Username}", usernameOrEmail);
                _logger.LogInformation("   - Hash: {Hash}", string.IsNullOrEmpty(tempHash) ? "NULO" : tempHash.Substring(0, Math.Min(20, tempHash.Length)) + "...");
                _logger.LogInformation("   - Expiration: {Expiration}", usuario.TemporaryPasswordExpiration);

                db.Usuarios.Update(usuario);
                var rowsAffected = await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("✅ Filas afectadas en SaveChanges: {RowsAffected}", rowsAffected);// Enviar email con la contraseña temporal
                var emailRequest = new ForgotPasswordRequest
                {
                    UsernameOrEmail = usernameOrEmail
                };

                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    userInfo.Value.Email,          // ← userEmail (correo)
                    userInfo.Value.UserName,       // ← userName (nombre completo)
                    temporaryPassword,
                    cancellationToken);

                if (!emailSent)
                {
                    _logger.LogWarning("Error al enviar email de reset a: {Email}", userInfo.Value.Email);
                    return ForgotPasswordResponse.FailureResponse("Error al enviar email. Intente más tarde.");
                }

                _logger.LogInformation("Email de reset de contraseña enviado a: {Email}", userInfo.Value.Email);
                return ForgotPasswordResponse.SuccessResponse(
                    $"Se ha enviado una contraseña temporal a {userInfo.Value.Email}. Revise su correo.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SendPasswordResetEmailAsync para: {UsernameOrEmail}", usernameOrEmail);
                return ForgotPasswordResponse.FailureResponse("Error interno del sistema");
            }
        }        /// <summary>
        /// Valida si una contraseña cumple con los requisitos de seguridad
        /// Requisito: Mínimo 4 caracteres
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "La contraseña no puede estar vacía");

            if (password.Length < 4)
                return (false, "La contraseña debe tener al menos 4 caracteres");

            return (true, string.Empty);
        }

        /// <summary>
        /// Genera una contraseña temporal (números y letras)
        /// </summary>
        public string GenerateTemporaryPassword(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new System.Text.StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Obtiene información del usuario (nombre y email)
        /// </summary>
        public async Task<(string UserName, string Email)?> GetUserEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            try
            {
                using var db = _dbContextFactory.CreateDbContext();

                // Buscar por nombre de usuario
                var usuario = await db.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NombreUsuario == usernameOrEmail, cancellationToken);

                if (usuario == null)
                {
                    // Buscar por email en tabla Personas
                    var usuarioByEmail = await db.Usuarios
                        .AsNoTracking()
                        .Where(u => db.Personas.Any(p => p.IdPersona == u.PersonaId && p.Correo == usernameOrEmail))
                        .FirstOrDefaultAsync(cancellationToken);

                    if (usuarioByEmail == null)
                        return null;

                    usuario = usuarioByEmail;
                }                // Obtener información de Persona
                var personaInfo = await db.Personas
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.IdPersona == usuario.PersonaId, cancellationToken);

                if (personaInfo == null || string.IsNullOrWhiteSpace(personaInfo.Correo))
                {
                    _logger.LogWarning("No se encontró email válido para usuario: {UsernameOrEmail}", usernameOrEmail);
                    return null;
                }

                var fullName = $"{personaInfo.Nombres} {personaInfo.Apellidos}".Trim();
                return (fullName, personaInfo.Correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener email del usuario: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        /// <summary>
        /// Hashea una contraseña usando PBKDF2-SHA256
        /// </summary>
        /// <param name="password">Contraseña en texto plano</param>
        /// <returns>Tupla con hash y salt en Base64</returns>
        private (string Hash, string Salt) HashPassword(string password)
        {
            // Generar un salt aleatorio de 32 bytes
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            // Generar el hash usando PBKDF2-SHA256
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hashBytes = pbkdf2.GetBytes(32);
                string hash = Convert.ToBase64String(hashBytes);
                return (hash, salt);
            }
        }
    }
}
