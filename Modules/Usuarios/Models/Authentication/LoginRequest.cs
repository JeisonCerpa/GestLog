using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.Usuarios.Models.Authentication
{
    /// <summary>
    /// Datos requeridos para el login
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public required string Password { get; set; }

        public bool RememberMe { get; set; } = false;
    }
}
