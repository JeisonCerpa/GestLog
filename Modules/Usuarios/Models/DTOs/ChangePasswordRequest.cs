using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.Usuarios.Models.DTOs
{
    /// <summary>
    /// DTO para solicitudes de cambio de contraseña
    /// </summary>
    public class ChangePasswordRequest
    {
        /// <summary>
        /// Contraseña actual del usuario (para validación en cambio normaly)
        /// En primer login, este puede ser la contraseña temporal
        /// </summary>
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [StringLength(256, MinimumLength = 1, ErrorMessage = "La contraseña debe tener entre 1 y 256 caracteres")]
        public string CurrentPassword { get; set; } = string.Empty;

        /// <summary>
        /// Nueva contraseña
        /// </summary>
        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(256, MinimumLength = 8, ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$",
            ErrorMessage = "La contraseña debe contener mayúsculas, minúsculas, números y caracteres especiales (@$!%*?&)")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Confirmación de la nueva contraseña (debe coincidir con NewPassword)
        /// </summary>
        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>
        /// Indicador de si es cambio en primer login (por defecto false)
        /// </summary>
        public bool IsFirstLoginChange { get; set; } = false;
    }
}
