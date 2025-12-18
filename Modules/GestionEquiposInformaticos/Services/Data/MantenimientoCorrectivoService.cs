using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Data
{
    /// <summary>
    /// Servicio para gestión de mantenimientos correctivos (reactivos)
    /// </summary>
    public class MantenimientoCorrectivoService : IMantenimientoCorrectivoService
    {
        private readonly GestLogDbContext _context;
        private readonly IGestLogLogger _logger;

        public MantenimientoCorrectivoService(GestLogDbContext context, IGestLogLogger logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<List<MantenimientoCorrectivoDto>> ObtenerTodosAsync(
            bool includeDadosDeBaja = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.MantenimientosCorrectivos.AsQueryable();
                var mantenimientos = await query
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);
                return MantenimientosADtos(mantenimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los mantenimientos correctivos");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MantenimientoCorrectivoDto?> ObtenerPorIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
                return mantenimiento == null ? null : MantenimientoADto(mantenimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mantenimiento con ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MantenimientoCorrectivoDto>> ObtenerPorEquipoAsync(
            int equipoInformaticoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimientos = await _context.MantenimientosCorrectivos
                    .Where(m => m.TipoEntidad == "Equipo")
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);
                return MantenimientosADtos(mantenimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mantenimientos del equipo {EquipoId}", equipoInformaticoId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MantenimientoCorrectivoDto>> ObtenerPorPerifericoAsync(
            int perifericoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimientos = await _context.MantenimientosCorrectivos
                    .Where(m => m.TipoEntidad == "Periférico")
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);
                return MantenimientosADtos(mantenimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mantenimientos del periférico {PerifericoId}", perifericoId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MantenimientoCorrectivoDto>> ObtenerEnReparacionAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimientos = await _context.MantenimientosCorrectivos
                    .Where(m => m.Estado == EstadoMantenimientoCorrectivo.EnReparacion)
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);
                return MantenimientosADtos(mantenimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mantenimientos en reparación");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> CrearAsync(
            MantenimientoCorrectivoDto dto,
            int usuarioRegistroId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                var mantenimiento = new MantenimientoCorrectivoEntity
                {
                    TipoEntidad = dto.TipoEntidad ?? "Equipo",
                    Codigo = dto.Codigo,
                    FechaFalla = dto.FechaFalla,
                    DescripcionFalla = dto.DescripcionFalla,
                    ProveedorAsignado = dto.ProveedorAsignado,
                    Estado = EstadoMantenimientoCorrectivo.Pendiente,
                    FechaInicio = null,
                    Observaciones = null,
                    CostoReparacion = null,
                    FechaRegistro = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                _context.MantenimientosCorrectivos.Add(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo creado. ID: {Id}", mantenimiento.Id);
                return mantenimiento.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando mantenimiento correctivo");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ActualizarAsync(
            MantenimientoCorrectivoDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto?.Id == null)
                    return false;

                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == dto.Id.Value, cancellationToken);

                if (mantenimiento == null)
                    return false;

                mantenimiento.ProveedorAsignado = dto.ProveedorAsignado;
                mantenimiento.Observaciones = dto.Observaciones;
                mantenimiento.FechaActualizacion = DateTime.UtcNow;

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo actualizado. ID: {Id}", dto.Id.Value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando mantenimiento correctivo con ID {Id}", dto?.Id?.ToString() ?? "sin ID");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CompletarAsync(
            int id,
            string? observaciones = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                if (mantenimiento == null)
                    return false;

                mantenimiento.Estado = EstadoMantenimientoCorrectivo.Completado;
                mantenimiento.FechaCompletado = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(observaciones))
                    mantenimiento.Observaciones = observaciones;
                mantenimiento.FechaActualizacion = DateTime.UtcNow;

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo completado. ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completando mantenimiento correctivo con ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CancelarAsync(
            int id,
            string razonCancelacion,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                if (mantenimiento == null)
                    return false;

                mantenimiento.Estado = EstadoMantenimientoCorrectivo.Cancelado;
                if (!string.IsNullOrWhiteSpace(razonCancelacion))
                    mantenimiento.Observaciones = razonCancelacion;
                mantenimiento.FechaActualizacion = DateTime.UtcNow;

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo cancelado. ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando mantenimiento correctivo con ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> EliminarAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                if (mantenimiento == null)
                    return false;

                _context.MantenimientosCorrectivos.Remove(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo eliminado. ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando mantenimiento correctivo con ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DarDeBajaAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                if (mantenimiento == null)
                    return false;

                mantenimiento.Estado = EstadoMantenimientoCorrectivo.Cancelado;
                mantenimiento.FechaActualizacion = DateTime.UtcNow;

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo dado de baja. ID: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dando de baja mantenimiento correctivo con ID {Id}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExisteMantenimientoEnProgresoAsync(
            int equipoInformaticoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.MantenimientosCorrectivos
                    .AnyAsync(m => m.TipoEntidad == "Equipo" && 
                                   m.Estado == EstadoMantenimientoCorrectivo.EnReparacion,
                              cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando mantenimiento en progreso para equipo {EquipoId}", equipoInformaticoId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExisteMantenimientoPerifericoEnProgresoAsync(
            int perifericoId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.MantenimientosCorrectivos
                    .AnyAsync(m => m.TipoEntidad == "Periférico" && 
                                   m.Estado == EstadoMantenimientoCorrectivo.EnReparacion,
                              cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando mantenimiento periférico en progreso para periférico {PerifericoId}", perifericoId);
                throw;
            }
        }

        /// <summary>
        /// Convierte una entidad a DTO
        /// </summary>
        private MantenimientoCorrectivoDto MantenimientoADto(MantenimientoCorrectivoEntity entity)
        {
            return new MantenimientoCorrectivoDto
            {
                Id = entity.Id,
                TipoEntidad = entity.TipoEntidad,
                Codigo = entity.Codigo,
                FechaFalla = entity.FechaFalla,
                DescripcionFalla = entity.DescripcionFalla,
                ProveedorAsignado = entity.ProveedorAsignado,
                Estado = entity.Estado,
                FechaInicio = entity.FechaInicio,
                FechaCompletado = entity.FechaCompletado,
                Observaciones = entity.Observaciones,
                FechaRegistro = entity.FechaRegistro,
                FechaActualizacion = entity.FechaActualizacion,
                NombreEntidad = entity.Codigo,
                CostoReparacion = entity.CostoReparacion
            };
        }

        /// <summary>
        /// Convierte una lista de entidades a DTOs
        /// </summary>
        private List<MantenimientoCorrectivoDto> MantenimientosADtos(List<MantenimientoCorrectivoEntity> entities)
        {
            return entities.Select(MantenimientoADto).ToList();
        }
    }
}
