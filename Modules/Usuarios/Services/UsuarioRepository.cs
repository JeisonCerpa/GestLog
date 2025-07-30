using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.DatabaseConnection;

namespace Modules.Usuarios.Services
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public UsuarioRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Usuario> AgregarAsync(Usuario usuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            usuario.IdUsuario = usuario.IdUsuario == Guid.Empty ? Guid.NewGuid() : usuario.IdUsuario;
            usuario.FechaCreacion = DateTime.UtcNow;
            usuario.FechaModificacion = DateTime.UtcNow;
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();
            return usuario;
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

        public async Task<IEnumerable<Usuario>> BuscarAsync(string filtro)
        {
            using var db = _dbContextFactory.CreateDbContext();
            // Unir Usuarios con Personas para obtener el correo
            var query = from u in db.Usuarios
                        join p in db.Personas on u.PersonaId equals p.IdPersona
                        select new
                        {
                            Usuario = u,
                            Correo = p.Correo
                        };
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(x => x.Usuario.NombreUsuario.Contains(filtro) || x.Correo.Contains(filtro));
            }
            var result = await query.ToListAsync();
            // Mapear resultado a Usuario, agregando el correo como propiedad extendida si es necesario
            var usuarios = result.Select(x =>
            {
                var usuario = x.Usuario;
                // Si tienes una propiedad extendida para el correo en el ViewModel, asígnala aquí
                // usuario.Correo = x.Correo;
                return usuario;
            }).ToList();
            return usuarios;
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        }
    }
}
