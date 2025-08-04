using System;
using System.Collections.Generic;

namespace GestLog.Modules.Usuarios.Models
{
    public class Permiso
    {
        public Guid IdPermiso { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        public Guid? PermisoPadreId { get; set; }
        public List<Permiso> SubPermisos { get; set; } = new();
        public required string Modulo { get; set; } // Ej: "Usuarios", "Personas", "Mantenimientos"
    }
}
