using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de roles.
    /// </summary>
    public interface IRolRepository
    {
        Task<Rol> AgregarAsync(Rol rol);
        Task<Rol> ActualizarAsync(Rol rol);
        Task EliminarAsync(Guid idRol);
        Task<Rol> ObtenerPorIdAsync(Guid idRol);
        Task<IEnumerable<Rol>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
    }
}
