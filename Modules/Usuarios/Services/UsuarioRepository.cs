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

        public async Task<Usuario> ActualizarAsync(Usuario usuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var existente = await db.Usuarios.FindAsync(usuario.IdUsuario);
            if (existente == null)
                throw new Exception("Usuario no encontrado");
            existente.NombreUsuario = usuario.NombreUsuario;
            existente.Activo = usuario.Activo;
            existente.Desactivado = usuario.Desactivado;
            existente.FechaModificacion = DateTime.UtcNow;
            // No se actualiza la contraseña aquí
            await db.SaveChangesAsync();
            // Obtener correo actualizado
            var persona = await db.Personas.FindAsync(existente.PersonaId);
            existente.Correo = persona?.Correo;
            return existente;
        }

        public async Task DesactivarAsync(Guid idUsuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var usuario = await db.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
                throw new Exception("Usuario no encontrado");
            usuario.Activo = false;
            usuario.Desactivado = true;
            usuario.FechaModificacion = DateTime.UtcNow;
            await db.SaveChangesAsync();
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
                            CorreoPersona = p.Correo // Renombrar para evitar conflicto
                        };
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                query = query.Where(x => x.Usuario.NombreUsuario.Contains(filtro) || x.CorreoPersona.Contains(filtro));
            }
            var result = await query.ToListAsync();
            // Mapear resultado a Usuario, agregando el correo como propiedad extendida si es necesario
            var usuarios = result.Select(x =>
            {
                var usuario = x.Usuario;
                usuario.Correo = x.CorreoPersona; // Asignar correo extendido
                return usuario;
            }).ToList();
            return usuarios;
        }

        public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            return await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        }        public async Task EliminarAsync(Guid idUsuario)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var strategy = db.Database.CreateExecutionStrategy();
            
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    // Eliminar relaciones UsuarioRol
                    var roles = db.UsuarioRoles.Where(ur => ur.IdUsuario == idUsuario);
                    db.UsuarioRoles.RemoveRange(roles);
                    // Eliminar relaciones UsuarioPermiso
                    var permisos = db.UsuarioPermisos.Where(up => up.IdUsuario == idUsuario);
                    db.UsuarioPermisos.RemoveRange(permisos);
                    // Eliminar usuario
                    var usuario = await db.Usuarios.FindAsync(idUsuario);
                    if (usuario != null)
                        db.Usuarios.Remove(usuario);
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task RestablecerContrasenaAsync(Guid idUsuario, string nuevoHash, string nuevoSalt)
        {
            using var db = _dbContextFactory.CreateDbContext();
            var usuario = await db.Usuarios.FindAsync(idUsuario);
            if (usuario == null)
                throw new Exception("Usuario no encontrado");
            
            usuario.HashContrasena = nuevoHash;
            usuario.Salt = nuevoSalt;
            usuario.FechaModificacion = DateTime.UtcNow;
            
            await db.SaveChangesAsync();
        }
    }
}
