using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de permisos.
    /// </summary>
    public interface IPermisoRepository
    {
        Task<Permiso> AgregarAsync(Permiso permiso);
        Task<Permiso> ActualizarAsync(Permiso permiso);
        Task EliminarAsync(Guid idPermiso);
        Task<Permiso> ObtenerPorIdAsync(Guid idPermiso);
        Task<IEnumerable<Permiso>> ObtenerTodosAsync();
        Task<bool> ExisteNombreAsync(string nombre);
    }
}
