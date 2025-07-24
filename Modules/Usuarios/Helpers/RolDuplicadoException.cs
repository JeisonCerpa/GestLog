using System;

namespace Modules.Usuarios.Helpers
{
    public class RolDuplicadoException : Exception
    {
        public RolDuplicadoException(string nombre)
            : base($"El rol '{nombre}' ya está registrado.")
        {
        }
    }
}
