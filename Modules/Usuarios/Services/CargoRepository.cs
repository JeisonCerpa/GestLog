using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace Modules.Usuarios.Services
{
    public class CargoRepository : ICargoRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public CargoRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<Cargo> AgregarAsync(Cargo cargo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Cargos.Add(cargo);
            await dbContext.SaveChangesAsync();
            return cargo;
        }

        public async Task<Cargo> ActualizarAsync(Cargo cargo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Cargos.Update(cargo);
            await dbContext.SaveChangesAsync();
            return cargo;
        }

        public async Task EliminarAsync(Guid idCargo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var cargo = await dbContext.Cargos.FindAsync(idCargo);
            if (cargo != null)
            {
                dbContext.Cargos.Remove(cargo);
                await dbContext.SaveChangesAsync();
            }
        }

        public async Task<Cargo?> ObtenerPorIdAsync(Guid idCargo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Cargos.FindAsync(idCargo);
        }

        public async Task<IEnumerable<Cargo>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Cargos.ToListAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Cargos.AnyAsync(c => c.Nombre == nombre);
        }
    }
}
