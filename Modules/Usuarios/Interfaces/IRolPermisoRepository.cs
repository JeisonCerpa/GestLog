using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    public interface IRolPermisoRepository
    {
        Task AsignarPermisosAsync(Guid idRol, IEnumerable<Guid> permisosIds);
        Task<IEnumerable<RolPermiso>> ObtenerPorRolAsync(Guid idRol);
        Task EliminarPermisosDeRolAsync(Guid idRol);
    }
}
