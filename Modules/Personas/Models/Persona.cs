using System;
using System.ComponentModel.DataAnnotations;
using GestLog.Modules.Usuarios.Models;

namespace GestLog.Modules.Personas.Models
{
    public class Persona
    {
        [Key]
        public Guid IdPersona { get; set; }
        public required string Nombres { get; set; }
        public required string Apellidos { get; set; }
        public required string TipoDocumento { get; set; }
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
        // Propiedad auxiliar para binding de estado en la UI ("Activo"/"Inactivo")
        public string Estado
        {
            get => Activo ? "Activo" : "Inactivo";
            set => Activo = value == "Activo";
        }
    }
}
