using System;

namespace Modules.Usuarios.Helpers
{
    public class PersonaNotFoundException : Exception
    {
        public PersonaNotFoundException(string identificador)
            : base($"No se encontró la persona con identificador '{identificador}'.")
        {
        }
    }
}
