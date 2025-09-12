using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Services
{
    public class EquipoInformaticoService : IEquipoInformaticoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public EquipoInformaticoService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<EquipoInformaticoEntity?> GetByCodigoAsync(string codigo)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var equipo = await context.EquiposInformaticos
                    .FirstOrDefaultAsync(e => e.Codigo == codigo);
                
                _logger.LogDebug("[EquipoInformaticoService] Búsqueda equipo {codigo}: encontrado={encontrado}, NombreEquipo={nombre}, UsuarioAsignado={usuario}", 
                    codigo, equipo != null, equipo?.NombreEquipo ?? "NULL", equipo?.UsuarioAsignado ?? "NULL");
                
                return equipo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoInformaticoService] Error al obtener equipo {codigo}", codigo);
                return null;
            }
        }
    }
}
