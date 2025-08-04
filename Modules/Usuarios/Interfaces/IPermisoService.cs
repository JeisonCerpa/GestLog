using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de permisos.
    /// </summary>
    public interface IPermisoService
    {
        Task<Permiso> CrearPermisoAsync(Permiso permiso);
        Task<Permiso> EditarPermisoAsync(Permiso permiso);
        Task EliminarPermisoAsync(Guid idPermiso);
        Task<Permiso> ObtenerPermisoPorIdAsync(Guid idPermiso);
        Task<IEnumerable<Permiso>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
        Task<IEnumerable<Permiso>> ObtenerPorModuloAsync(string modulo);
    }
}
