using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;

namespace Modules.Usuarios.Services
{
    public class TipoDocumentoRepository : ITipoDocumentoRepository
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;

        public TipoDocumentoRepository(IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<TipoDocumento>> ObtenerTodosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.TiposDocumento.ToListAsync();
        }

        public async Task<TipoDocumento?> ObtenerPorIdAsync(Guid id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.TiposDocumento.FindAsync(id);
        }

        public async Task<TipoDocumento?> ObtenerPorCodigoAsync(string codigo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.TiposDocumento.FirstOrDefaultAsync(t => t.Codigo == codigo);
        }

        public async Task AgregarAsync(TipoDocumento tipoDocumento)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            if (tipoDocumento.IdTipoDocumento == Guid.Empty)
                tipoDocumento.IdTipoDocumento = Guid.NewGuid();
            
            dbContext.TiposDocumento.Add(tipoDocumento);
            await dbContext.SaveChangesAsync();
        }

        public async Task ActualizarAsync(TipoDocumento tipoDocumento)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.TiposDocumento.Update(tipoDocumento);
            await dbContext.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var tipoDocumento = await dbContext.TiposDocumento.FindAsync(id);
            if (tipoDocumento != null)
            {
                dbContext.TiposDocumento.Remove(tipoDocumento);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
