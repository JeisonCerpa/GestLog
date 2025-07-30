using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GestLog.Modules.Usuarios.Models;

namespace GestLog.Modules.Personas.Models
{
    public class Persona
    {
        [Key]
        public Guid IdPersona { get; set; }
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public Guid TipoDocumentoId { get; set; }
        public GestLog.Modules.Usuarios.Models.TipoDocumento? TipoDocumento { get; set; }
        public required string NumeroDocumento { get; set; }
        public required string Correo { get; set; }
        public required string Telefono { get; set; }
        public Guid CargoId { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public string NombreCompleto => $"{Nombres} {Apellidos}";
        
        // Propiedad auxiliar para binding en la UI (no persistente)
        public Cargo? Cargo { get; set; }
        
        [NotMapped]
        public bool TieneUsuario { get; set; } // No persistente, solo para la UI
    }
}
