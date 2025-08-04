using System;

namespace GestLog.Modules.Usuarios.Models
{
    public class RolPermiso
    {
        // Relación para asignación de permisos a roles
        public Guid IdRol { get; set; }
        public Guid IdPermiso { get; set; }
    }
}
