using System;
using System.Collections.Generic;

namespace GestLog.Modules.Usuarios.Models
{
    public class Rol
    {
        public Guid IdRol { get; set; }
        public required string Nombre { get; set; }
        public required string Descripcion { get; set; }
        // Permisos asignados al rol, por módulo
        public List<Permiso> Permisos { get; set; } = new();
    }
}
