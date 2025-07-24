using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class PermisoRepository : IPermisoRepository
    {
        public PermisoRepository()
        {
            // Inicialización de recursos de datos
        }

        public Task<Permiso> AgregarAsync(Permiso permiso)
        {
            throw new NotImplementedException();
        }

        public Task<Permiso> ActualizarAsync(Permiso permiso)
        {
            throw new NotImplementedException();
        }

        public Task EliminarAsync(Guid idPermiso)
        {
            throw new NotImplementedException();
        }

        public Task<Permiso> ObtenerPorIdAsync(Guid idPermiso)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Permiso>> ObtenerTodosAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExisteNombreAsync(string nombre)
        {
            throw new NotImplementedException();
        }
    }
}
