using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestLog.Modules.Usuarios.Models
{
    public class Usuario
    {
        [Key]
        public Guid IdUsuario { get; set; }
        public Guid PersonaId { get; set; }
        public required string NombreUsuario { get; set; }
        public required string HashContrasena { get; set; }
        public required string Salt { get; set; }
        public bool Activo { get; set; }
        public bool Desactivado { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        
        // Propiedad extendida para mostrar el correo de la persona asociada
        [NotMapped]
        public string? Correo { get; set; }
    }
}
