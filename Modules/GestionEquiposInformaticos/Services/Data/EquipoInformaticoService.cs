using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Data
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

        public async Task<IEnumerable<EquipoInformaticoEntity>> GetAllAsync()
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var equipos = await context.EquiposInformaticos
                    .OrderBy(e => e.Codigo)
                    .ToListAsync();
                
                _logger.LogDebug("[EquipoInformaticoService] Obtenidos {cantidad} equipos informáticos", equipos.Count);
                
                return equipos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoInformaticoService] Error al obtener todos los equipos");
                return new List<EquipoInformaticoEntity>();
            }
        }
    }
}
