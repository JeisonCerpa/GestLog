using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.Usuarios.Models;
using Microsoft.EntityFrameworkCore;
using Modules.Usuarios.Helpers;
using Modules.Usuarios.Interfaces;

namespace Modules.Usuarios.Services
{
    public class RolRepository : IRolRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public RolRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Rol> AgregarAsync(Rol rol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (rol == null)
                throw new ArgumentNullException(nameof(rol), "El rol no puede ser nulo");
            if (string.IsNullOrWhiteSpace(rol.Nombre))
                throw new ArgumentException("El nombre del rol es obligatorio", nameof(rol.Nombre));
            if (await dbContext.Roles.AnyAsync(r => r.Nombre == rol.Nombre))
                throw new RolDuplicadoException(rol.Nombre);
            rol.IdRol = Guid.NewGuid();
            dbContext.Roles.Add(rol);
            await dbContext.SaveChangesAsync();
            return rol;
        }

        public async Task<Rol> ActualizarAsync(Rol rol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Roles.FindAsync(rol.IdRol);
            if (existente == null)
                throw new RolNotFoundException(rol.IdRol);
            if (await dbContext.Roles.AnyAsync(r => r.Nombre == rol.Nombre && r.IdRol != rol.IdRol))
                throw new RolDuplicadoException(rol.Nombre);
            existente.Nombre = rol.Nombre;
            existente.Descripcion = rol.Descripcion;
            await dbContext.SaveChangesAsync();
            return existente;
        }

        public async Task EliminarAsync(Guid idRol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existente = await dbContext.Roles.FindAsync(idRol);
            if (existente == null)
                throw new RolNotFoundException(idRol);
            dbContext.Roles.Remove(existente);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Rol> ObtenerPorIdAsync(Guid idRol)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var rol = await dbContext.Roles.FindAsync(idRol);
            if (rol == null)
                throw new RolNotFoundException(idRol);
            return rol;
        }        public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Roles.ToListAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Roles.AnyAsync(r => r.Nombre == nombre);
        }
    }
}
