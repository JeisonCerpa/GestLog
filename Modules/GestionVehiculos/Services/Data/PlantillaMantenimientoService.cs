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
    /// Servicio para operaciones CRUD de Plantillas de Mantenimiento
    /// </summary>
    public class PlantillaMantenimientoService : IPlantillaMantenimientoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public PlantillaMantenimientoService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PlantillaMantenimientoDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var plantillas = await context.Set<PlantillaMantenimiento>()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.Nombre)
                    .ToListAsync(cancellationToken);

                return plantillas.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plantillas de mantenimiento");
                throw;
            }
        }

        public async Task<PlantillaMantenimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var plantilla = await context.Set<PlantillaMantenimiento>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

                return plantilla != null ? MapToDto(plantilla) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plantilla por ID: {PlantillaId}", id);
                throw;
            }
        }

        public async Task<PlantillaMantenimientoDto> CreateAsync(PlantillaMantenimientoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plantilla = new PlantillaMantenimiento
                {
                    Nombre = dto.Nombre,
                    Descripcion = dto.Descripcion,
                    IntervaloKM = dto.IntervaloKM,
                    IntervaloDias = dto.IntervaloDias,
                    TipoIntervalo = dto.TipoIntervalo,
                    Activo = dto.Activo,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                context.Add(plantilla);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(plantilla);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plantilla de mantenimiento: {Nombre}", dto.Nombre);
                throw;
            }
        }

        public async Task<PlantillaMantenimientoDto> UpdateAsync(int id, PlantillaMantenimientoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plantilla = await context.Set<PlantillaMantenimiento>()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

                if (plantilla == null)
                    throw new InvalidOperationException($"Plantilla con ID {id} no encontrada");

                plantilla.Nombre = dto.Nombre;
                plantilla.Descripcion = dto.Descripcion;
                plantilla.IntervaloKM = dto.IntervaloKM;
                plantilla.IntervaloDias = dto.IntervaloDias;
                plantilla.TipoIntervalo = dto.TipoIntervalo;
                plantilla.Activo = dto.Activo;
                plantilla.FechaActualizacion = DateTime.UtcNow;

                context.Update(plantilla);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(plantilla);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plantilla de mantenimiento: {PlantillaId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plantilla = await context.Set<PlantillaMantenimiento>()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

                if (plantilla == null)
                    throw new InvalidOperationException($"Plantilla con ID {id} no encontrada");

                plantilla.IsDeleted = true;
                plantilla.FechaActualizacion = DateTime.UtcNow;

                context.Update(plantilla);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Plantilla de mantenimiento eliminada: {PlantillaId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plantilla de mantenimiento: {PlantillaId}", id);
                throw;
            }
        }

        private static PlantillaMantenimientoDto MapToDto(PlantillaMantenimiento entity)
        {
            return new PlantillaMantenimientoDto
            {
                Id = entity.Id,
                Nombre = entity.Nombre,
                Descripcion = entity.Descripcion,
                IntervaloKM = entity.IntervaloKM,
                IntervaloDias = entity.IntervaloDias,
                TipoIntervalo = entity.TipoIntervalo,
                Activo = entity.Activo,
                FechaCreacion = entity.FechaCreacion,
                FechaActualizacion = entity.FechaActualizacion
            };
        }
    }
}
