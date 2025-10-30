using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.Usuarios.Models.DTOs
{
    /// <summary>
    /// DTO para solicitudes de recuperación de contraseña
    /// Permite al usuario solicitar una contraseña temporal via email
    /// </summary>
    public class ForgotPasswordRequest
    {
        /// <summary>
        /// Nombre de usuario o correo electrónico para identificar la cuenta
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario o correo es requerido")]
        [StringLength(256, MinimumLength = 1, ErrorMessage = "El nombre de usuario o correo debe tener entre 1 y 256 caracteres")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        /// <summary>
        /// Token CAPTCHA (opcional) para verificación anti-bot
        /// </summary>
        public string? CaptchaToken { get; set; }
    }    /// <summary>
    /// DTO para la respuesta de una solicitud de recuperación de contraseña exitosa
    /// </summary>
    public class ForgotPasswordResponse
    {
        /// <summary>
        /// Indicador de éxito de la operación
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje de respuesta al usuario
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Información adicional (ej. correo parcialmente oculto)
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Crea una respuesta exitosa
        /// </summary>
        public static ForgotPasswordResponse SuccessResponse(string message, string? additionalInfo = null)
        {
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = message,
                AdditionalInfo = additionalInfo
            };
        }

        /// <summary>
        /// Crea una respuesta de error/fallo
        /// </summary>
        public static ForgotPasswordResponse FailureResponse(string errorMessage, string? additionalInfo = null)
        {
            return new ForgotPasswordResponse
            {
                Success = false,
                Message = errorMessage,
                AdditionalInfo = additionalInfo
            };
        }
    }
}
