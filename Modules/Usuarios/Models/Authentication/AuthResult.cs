using System;

namespace GestLog.Modules.Usuarios.Models.Authentication
{
    /// <summary>
    /// Resultado de un intento de autenticación
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Usuario? User { get; set; }
        public DateTime? LoginTime { get; set; }
        public string? ErrorCode { get; set; }

        public static AuthResult SuccessResult(Usuario user)
        {
            return new AuthResult
            {
                Success = true,
                User = user,
                LoginTime = DateTime.UtcNow
            };
        }

        public static AuthResult FailureResult(string errorMessage, string? errorCode = null)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ErrorCode = errorCode
            };
        }
    }
}
