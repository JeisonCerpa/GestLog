using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.Usuarios.Models;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Rol>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Roles.ToListAsync();
        }

        public Task<bool> ExisteNombreAsync(string nombre)
        {
            throw new NotImplementedException();
        }
    }
}
