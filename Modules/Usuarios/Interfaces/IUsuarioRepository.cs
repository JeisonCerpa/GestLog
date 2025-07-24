using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para acceso a datos de usuarios.
    /// </summary>
    public interface IUsuarioRepository
    {
        Task<Usuario> AgregarAsync(Usuario usuario);
        Task<Usuario> ActualizarAsync(Usuario usuario);
        Task DesactivarAsync(Guid idUsuario);
        Task<Usuario> ObtenerPorIdAsync(Guid idUsuario);
        Task<IEnumerable<Usuario>> BuscarAsync(string filtro);
        Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario);
    }
}
