using System;

namespace Modules.Usuarios.Helpers
{
    public class CargoDuplicadoException : Exception
    {
        public CargoDuplicadoException(string nombre)
            : base($"El cargo '{nombre}' ya está registrado.")
        {
        }
    }
}
