using System;

namespace Modules.Usuarios.Helpers
{
    public class RolNotFoundException : Exception
    {
        public RolNotFoundException(Guid idRol)
            : base($"No se encontró el rol con identificador '{idRol}'.")
        {
        }
    }
}
