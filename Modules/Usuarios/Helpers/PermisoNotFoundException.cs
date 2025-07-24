using System;

namespace Modules.Usuarios.Helpers
{
    public class PermisoNotFoundException : Exception
    {
        public PermisoNotFoundException(Guid idPermiso)
            : base($"No se encontró el permiso con identificador '{idPermiso}'.")
        {
        }
    }
}
