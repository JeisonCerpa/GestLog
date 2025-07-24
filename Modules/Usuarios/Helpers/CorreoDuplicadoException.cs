using System;

namespace Modules.Usuarios.Helpers
{
    public class CorreoDuplicadoException : Exception
    {
        public CorreoDuplicadoException(string correo)
            : base($"El correo electrónico '{correo}' ya está registrado.")
        {
        }
    }
}
