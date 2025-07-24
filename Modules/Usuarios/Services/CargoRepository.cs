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
        private readonly GestLogDbContext _dbContext;

        public CargoRepository(GestLogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Cargo> AgregarAsync(Cargo cargo)
        {
            _dbContext.Cargos.Add(cargo);
            await _dbContext.SaveChangesAsync();
            return cargo;
        }

        public async Task<Cargo> ActualizarAsync(Cargo cargo)
        {
            _dbContext.Cargos.Update(cargo);
            await _dbContext.SaveChangesAsync();
            return cargo;
        }

        public async Task EliminarAsync(Guid idCargo)
        {
            var cargo = await _dbContext.Cargos.FindAsync(idCargo);
            if (cargo != null)
            {
                _dbContext.Cargos.Remove(cargo);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<Cargo?> ObtenerPorIdAsync(Guid idCargo)
        {
            return await _dbContext.Cargos.FindAsync(idCargo);
        }

        public async Task<IEnumerable<Cargo>> ObtenerTodosAsync()
        {
            return await _dbContext.Cargos.ToListAsync();
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            return await _dbContext.Cargos.AnyAsync(c => c.Nombre == nombre);
        }
    }
}
