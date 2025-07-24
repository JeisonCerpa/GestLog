using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class RolRepository : IRolRepository
    {
        public RolRepository()
        {
            // Inicialización de recursos de datos
        }

        public Task<Rol> AgregarAsync(Rol rol)
        {
            throw new NotImplementedException();
        }

        public Task<Rol> ActualizarAsync(Rol rol)
        {
            throw new NotImplementedException();
        }

        public Task EliminarAsync(Guid idRol)
        {
            throw new NotImplementedException();
        }

        public Task<Rol> ObtenerPorIdAsync(Guid idRol)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExisteNombreAsync(string nombre)
        {
            throw new NotImplementedException();
        }
    }
}
