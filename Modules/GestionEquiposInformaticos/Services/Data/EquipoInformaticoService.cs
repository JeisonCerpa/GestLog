using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
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

        public async Task<bool> CambiarEstadoAsync(string codigo, EstadoEquipoInformatico nuevoEstado, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var equipo = await context.EquiposInformaticos
                    .FirstOrDefaultAsync(e => e.Codigo == codigo, cancellationToken);

                if (equipo == null)
                {
                    _logger.LogWarning("[EquipoInformaticoService] No se encontró equipo con código {codigo}", codigo);
                    return false;
                }

                equipo.Estado = nuevoEstado.ToString();
                context.EquiposInformaticos.Update(equipo);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[EquipoInformaticoService] Estado del equipo {codigo} cambiado a {nuevoEstado}", codigo, nuevoEstado);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoInformaticoService] Error al cambiar estado del equipo {codigo}", codigo);
                return false;
            }
        }
    }
}
