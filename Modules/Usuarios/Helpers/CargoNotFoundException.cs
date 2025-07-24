using System;

namespace Modules.Usuarios.Helpers
{
    public class CargoNotFoundException : Exception
    {
        public CargoNotFoundException(Guid idCargo)
            : base($"No se encontró el cargo con identificador '{idCargo}'.")
        {
        }
    }
}
