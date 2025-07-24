using System;

namespace Modules.Usuarios.Helpers
{
    public class UsuarioDuplicadoException : Exception
    {
        public UsuarioDuplicadoException(string nombreUsuario)
            : base($"El nombre de usuario '{nombreUsuario}' ya está registrado.")
        {
        }
    }
}
