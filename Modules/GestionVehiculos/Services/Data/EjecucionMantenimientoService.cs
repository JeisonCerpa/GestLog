using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Entities;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;

namespace GestLog.Modules.GestionVehiculos.Services.Data
{
    /// <summary>
    /// Servicio para operaciones CRUD de Ejecuciones de Mantenimiento
    /// </summary>
    public class EjecucionMantenimientoService : IEjecucionMantenimientoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public EjecucionMantenimientoService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<EjecucionMantenimientoDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ejecuciones = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .Where(e => !e.IsDeleted)
                    .OrderByDescending(e => e.FechaEjecucion)
                    .ToListAsync(cancellationToken);

                return ejecuciones.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ejecuciones de mantenimiento");
                throw;
            }
        }

        public async Task<EjecucionMantenimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ejecucion = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

                return ejecucion != null ? MapToDto(ejecucion) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ejecución por ID: {EjecucionId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<EjecucionMantenimientoDto>> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ejecuciones = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .Where(e => e.PlacaVehiculo == placaVehiculo && !e.IsDeleted)
                    .OrderByDescending(e => e.FechaEjecucion)
                    .ToListAsync(cancellationToken);

                return ejecuciones.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ejecuciones por placa: {PlacaVehiculo}", placaVehiculo);
                throw;
            }
        }

        public async Task<IEnumerable<EjecucionMantenimientoDto>> GetByPlanAsync(int planId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ejecuciones = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .Where(e => e.PlanMantenimientoId == planId && !e.IsDeleted)
                    .OrderByDescending(e => e.FechaEjecucion)
                    .ToListAsync(cancellationToken);

                return ejecuciones.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener ejecuciones por plan: {PlanId}", planId);
                throw;
            }
        }

        public async Task<EjecucionMantenimientoDto> CreateAsync(EjecucionMantenimientoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var ejecucion = new EjecucionMantenimiento
                {
                    PlacaVehiculo = dto.PlacaVehiculo,
                    PlanMantenimientoId = dto.PlanMantenimientoId,
                    FechaEjecucion = dto.FechaEjecucion,
                    KMAlMomento = dto.KMAlMomento,
                    ObservacionesTecnico = dto.ObservacionesTecnico,
                    Costo = dto.Costo,
                    RutaFactura = dto.RutaFactura,
                    ResponsableEjecucion = dto.ResponsableEjecucion,
                    Proveedor = dto.Proveedor,
                    Estado = (Models.Enums.EstadoEjecucion)dto.Estado,
                    FechaRegistro = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                context.Add(ejecucion);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Ejecución de mantenimiento registrada para: {PlacaVehiculo}", dto.PlacaVehiculo);

                return MapToDto(ejecucion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear ejecución de mantenimiento para: {PlacaVehiculo}", dto.PlacaVehiculo);
                throw;
            }
        }

        public async Task<EjecucionMantenimientoDto> UpdateAsync(int id, EjecucionMantenimientoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var ejecucion = await context.Set<EjecucionMantenimiento>()
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

                if (ejecucion == null)
                    throw new InvalidOperationException($"Ejecución con ID {id} no encontrada");

                ejecucion.FechaEjecucion = dto.FechaEjecucion;
                ejecucion.KMAlMomento = dto.KMAlMomento;
                ejecucion.ObservacionesTecnico = dto.ObservacionesTecnico;
                ejecucion.Costo = dto.Costo;
                ejecucion.RutaFactura = dto.RutaFactura;
                ejecucion.ResponsableEjecucion = dto.ResponsableEjecucion;
                ejecucion.Proveedor = dto.Proveedor;
                ejecucion.Estado = (Models.Enums.EstadoEjecucion)dto.Estado;
                ejecucion.FechaActualizacion = DateTime.UtcNow;

                context.Update(ejecucion);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(ejecucion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar ejecución de mantenimiento: {EjecucionId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var ejecucion = await context.Set<EjecucionMantenimiento>()
                    .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);

                if (ejecucion == null)
                    throw new InvalidOperationException($"Ejecución con ID {id} no encontrada");

                ejecucion.IsDeleted = true;
                ejecucion.FechaActualizacion = DateTime.UtcNow;

                context.Update(ejecucion);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Ejecución de mantenimiento eliminada: {EjecucionId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar ejecución de mantenimiento: {EjecucionId}", id);
                throw;
            }
        }

        public async Task<EjecucionMantenimientoDto?> GetUltimaEjecucionAsync(int planId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ultimaEjecucion = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .Where(e => e.PlanMantenimientoId == planId && !e.IsDeleted)
                    .OrderByDescending(e => e.FechaEjecucion)
                    .FirstOrDefaultAsync(cancellationToken);

                return ultimaEjecucion != null ? MapToDto(ultimaEjecucion) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener última ejecución del plan: {PlanId}", planId);
                throw;
            }
        }

        public async Task<IEnumerable<EjecucionMantenimientoDto>> GetHistorialVehiculoAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var historial = await context.Set<EjecucionMantenimiento>()
                    .AsNoTracking()
                    .Where(e => e.PlacaVehiculo == placaVehiculo && !e.IsDeleted)
                    .OrderByDescending(e => e.FechaEjecucion)
                    .ToListAsync(cancellationToken);

                return historial.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de mantenimiento del vehículo: {PlacaVehiculo}", placaVehiculo);
                throw;
            }
        }

        private static EjecucionMantenimientoDto MapToDto(EjecucionMantenimiento entity)
        {
            return new EjecucionMantenimientoDto
            {
                Id = entity.Id,
                PlacaVehiculo = entity.PlacaVehiculo,
                PlanMantenimientoId = entity.PlanMantenimientoId,
                FechaEjecucion = entity.FechaEjecucion,
                KMAlMomento = entity.KMAlMomento,
                ObservacionesTecnico = entity.ObservacionesTecnico,
                Costo = entity.Costo,
                RutaFactura = entity.RutaFactura,
                ResponsableEjecucion = entity.ResponsableEjecucion,
                Proveedor = entity.Proveedor,
                Estado = (int)entity.Estado,
                FechaRegistro = entity.FechaRegistro,
                FechaActualizacion = entity.FechaActualizacion
            };
        }
    }
}
