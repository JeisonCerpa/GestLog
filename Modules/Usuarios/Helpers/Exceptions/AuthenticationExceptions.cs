using System;

namespace GestLog.Modules.Usuarios.Helpers.Exceptions
{
    /// <summary>
    /// Excepción base para errores de autenticación
    /// </summary>
    public abstract class AuthenticationException : Exception
    {
        public string ErrorCode { get; }

        protected AuthenticationException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        protected AuthenticationException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Excepción cuando las credenciales son incorrectas
    /// </summary>
    public class InvalidCredentialsException : AuthenticationException
    {
        public InvalidCredentialsException(string username) 
            : base($"Credenciales incorrectas para el usuario '{username}'", "INVALID_CREDENTIALS")
        {
        }
    }

    /// <summary>
    /// Excepción cuando el usuario está desactivado
    /// </summary>
    public class UserDeactivatedException : AuthenticationException
    {
        public UserDeactivatedException(string username) 
            : base($"El usuario '{username}' está desactivado", "USER_DEACTIVATED")
        {
        }
    }

    /// <summary>
    /// Excepción cuando el usuario no existe
    /// </summary>
    public class UserNotFoundException : AuthenticationException
    {
        public UserNotFoundException(string username) 
            : base($"El usuario '{username}' no existe en el sistema", "USER_NOT_FOUND")
        {
        }
    }

    /// <summary>
    /// Excepción cuando el usuario no tiene permisos para una acción
    /// </summary>
    public class UnauthorizedAccessException : AuthenticationException
    {
        public UnauthorizedAccessException(string action) 
            : base($"No tiene permisos para realizar esta acción: {action}", "UNAUTHORIZED_ACCESS")
        {
        }
    }

    /// <summary>
    /// Excepción cuando la sesión ha expirado
    /// </summary>
    public class SessionExpiredException : AuthenticationException
    {
        public SessionExpiredException() 
            : base("La sesión ha expirado. Por favor, inicie sesión nuevamente", "SESSION_EXPIRED")
        {
        }
    }
}
