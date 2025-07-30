using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;

namespace Modules.Usuarios.Interfaces
{
    /// <summary>
    /// Contrato para la gestión de usuarios.
    /// </summary>
    public interface IUsuarioService
    {
        Task<Usuario> AsignarUsuarioAPersonaAsync(Guid idPersona, string nombreUsuario, string contraseña);
        Task<Usuario> EditarUsuarioAsync(Usuario usuario);
        Task DesactivarUsuarioAsync(Guid idUsuario);
        Task<Usuario> ObtenerUsuarioPorIdAsync(Guid idUsuario);
        Task<IEnumerable<Usuario>> BuscarUsuariosAsync(string filtro);
        Task RestablecerContraseñaAsync(Guid idUsuario, string nuevaContraseña);
        Task AsignarRolesAsync(Guid idUsuario, IEnumerable<Guid> rolesIds);
        Task AsignarPermisosAsync(Guid idUsuario, IEnumerable<Guid> permisosIds);
        Task<Usuario> RegistrarUsuarioAsync(Usuario usuario);
    }
}
