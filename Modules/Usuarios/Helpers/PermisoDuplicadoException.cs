using System;

namespace Modules.Usuarios.Helpers
{
    public class PermisoDuplicadoException : Exception
    {
        public PermisoDuplicadoException(string nombre)
            : base($"El permiso '{nombre}' ya está registrado.")
        {
        }
    }
}
