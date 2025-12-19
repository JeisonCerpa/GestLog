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
    public class PerifericoService : IPerifericoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public PerifericoService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }        public async Task<PerifericoEquipoInformaticoEntity?> GetByCodigoAsync(string codigo)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var periferico = await context.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == codigo);

                _logger.LogDebug("[PerifericoService] Búsqueda periférico {codigo}: encontrado={encontrado}, Dispositivo={dispositivo}", 
                    codigo, periferico != null, periferico?.Dispositivo ?? "NULL");

                return periferico;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericoService] Error al obtener periférico {codigo}", codigo);
                return null;
            }
        }

        public async Task<IEnumerable<PerifericoEquipoInformaticoEntity>> GetAllAsync()
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var perifericos = await context.PerifericosEquiposInformaticos
                    .OrderBy(p => p.Codigo)
                    .ToListAsync();

                _logger.LogDebug("[PerifericoService] Obtenidos {cantidad} periféricos", perifericos.Count);

                return perifericos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericoService] Error al obtener todos los periféricos");
                return new List<PerifericoEquipoInformaticoEntity>();
            }
        }

        public async Task<bool> CambiarEstadoAsync(string codigo, EstadoPeriferico nuevoEstado, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                var periferico = await context.PerifericosEquiposInformaticos
                    .FirstOrDefaultAsync(p => p.Codigo == codigo, cancellationToken);

                if (periferico == null)
                {
                    _logger.LogWarning("[PerifericoService] No se encontró periférico con código {codigo}", codigo);
                    return false;
                }

                periferico.Estado = nuevoEstado;
                context.PerifericosEquiposInformaticos.Update(periferico);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[PerifericoService] Estado del periférico {codigo} cambiado a {nuevoEstado}", codigo, nuevoEstado);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericoService] Error al cambiar estado del periférico {codigo}", codigo);
                return false;
            }
        }
    }
}
