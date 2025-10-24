using System;
using GestLog.Modules.DaaterProccesor.Exceptions;

namespace GestLog.Services.Exceptions
{
    /// <summary>
    /// Excepción lanzada cuando no hay acceso a la carpeta de actualizaciones del servidor.
    /// Indica problemas de permisos, credenciales faltantes, ruta inalcanzable, etc.
    /// </summary>
    public class UpdateServerAccessException : GestLogException
    {
        /// <summary>
        /// Ruta del servidor de actualizaciones donde ocurrió el error
        /// </summary>
        public string? UpdateServerPath { get; }

        /// <summary>
        /// Tipo de excepción interna que causó el error (UnauthorizedAccessException, IOException, etc.)
        /// </summary>
        public string? InnerExceptionType { get; }

        /// <summary>
        /// Constructor con mensaje y detalles de la excepción interna
        /// </summary>
        public UpdateServerAccessException(
            string message,
            string? updateServerPath = null,
            string? innerExceptionType = null,
            Exception? innerException = null)
            : base(message, "UPDATE_SERVER_ACCESS_ERROR", innerException)
        {
            UpdateServerPath = updateServerPath;
            InnerExceptionType = innerExceptionType;
        }
    }

    /// <summary>
    /// Resultado de verificación de actualizaciones con información de diagnóstico
    /// </summary>
    public class UpdateCheckResult
    {
        /// <summary>
        /// ¿Hay actualizaciones disponibles?
        /// </summary>
        public bool HasUpdatesAvailable { get; set; }

        /// <summary>
        /// ¿Hay un problema de permisos/acceso al servidor?
        /// </summary>
        public bool HasAccessError { get; set; }

        /// <summary>
        /// Mensaje descriptivo del estado
        /// </summary>
        public string StatusMessage { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de error si ocurrió alguno (UnauthorizedAccess, NetworkError, etc.)
        /// </summary>
        public string? ErrorType { get; set; }

        /// <summary>
        /// Excepción interna si ocurrió algún error
        /// </summary>
        public Exception? InnerException { get; set; }
    }
}
