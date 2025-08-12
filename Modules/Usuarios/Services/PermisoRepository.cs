using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Helpers;

namespace Modules.Usuarios.Services
{
    public class PermisoRepository : IPermisoRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public PermisoRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Permiso> AgregarAsync(Permiso permiso)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (permiso == null)
                throw new ArgumentNullException(nameof(permiso), "El permiso no puede ser nulo");
            if (string.IsNullOrWhiteSpace(permiso.Nombre))
                throw new ArgumentException("El nombre del permiso es obligatorio", nameof(permiso.Nombre));
            if (await dbContext.Permisos.AnyAsync(p => p.Nombre == permiso.Nombre))
                throw new PermisoDuplicadoException(permiso.Nombre);
            permiso.IdPermiso = Guid.NewGuid();
            dbContext.Permisos.Add(permiso);
            await dbContext.SaveChangesAsync();
            return permiso;
        }

        public async Task<Permiso> ActualizarAsync(Permiso permiso)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Permisos.FindAsync(permiso.IdPermiso);
            if (existente == null)
                throw new PermisoNotFoundException(permiso.IdPermiso);
            if (await dbContext.Permisos.AnyAsync(p => p.Nombre == permiso.Nombre && p.IdPermiso != permiso.IdPermiso))
                throw new PermisoDuplicadoException(permiso.Nombre);
            existente.Nombre = permiso.Nombre;
            existente.Descripcion = permiso.Descripcion;
            existente.PermisoPadreId = permiso.PermisoPadreId;
            await dbContext.SaveChangesAsync();
            return existente;
        }

        public async Task EliminarAsync(Guid idPermiso)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Permisos.FindAsync(idPermiso);
            if (existente == null)
                throw new PermisoNotFoundException(idPermiso);
            dbContext.Permisos.Remove(existente);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Permiso> ObtenerPorIdAsync(Guid idPermiso)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var permiso = await dbContext.Permisos.FindAsync(idPermiso);
            if (permiso == null)
                throw new PermisoNotFoundException(idPermiso);
            return permiso;
        }

        public async Task<IEnumerable<Permiso>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            // Filtrar permisos con campos requeridos no nulos y no vacíos
            return await dbContext.Permisos
                .Where(p => !string.IsNullOrWhiteSpace(p.Nombre)
                         && !string.IsNullOrWhiteSpace(p.Descripcion)
                         && !string.IsNullOrWhiteSpace(p.Modulo))
                .ToListAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Permisos.AnyAsync(p => p.Nombre == nombre);
        }

        public async Task<IEnumerable<Permiso>> ObtenerPorModuloAsync(string modulo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Permisos
                .Where(p => p.Modulo == modulo)
                .ToListAsync();
        }
    }
}
