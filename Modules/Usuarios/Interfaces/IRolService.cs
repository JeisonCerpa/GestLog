using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de roles.
    /// </summary>
    public interface IRolService
    {
        Task<Rol> CrearRolAsync(Rol rol);
        Task<Rol> EditarRolAsync(Rol rol);
        Task EliminarRolAsync(Guid idRol);
        Task<Rol> ObtenerRolPorIdAsync(Guid idRol);
        Task<IEnumerable<Rol>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
    }
}
