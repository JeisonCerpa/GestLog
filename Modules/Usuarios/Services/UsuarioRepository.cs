using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Services
{
    public class UsuarioRepository : IUsuarioRepository
    {
        public UsuarioRepository()
        {
            // Inicialización de recursos de datos
        }

        public Task<Usuario> AgregarAsync(Usuario usuario)
        {
            throw new NotImplementedException();
        }

        public Task<Usuario> ActualizarAsync(Usuario usuario)
        {
            throw new NotImplementedException();
        }

        public Task DesactivarAsync(Guid idUsuario)
        {
            throw new NotImplementedException();
        }

        public Task<Usuario> ObtenerPorIdAsync(Guid idUsuario)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Usuario>> BuscarAsync(string filtro)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            throw new NotImplementedException();
        }
    }
}
