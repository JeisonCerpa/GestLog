using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Data
{
    /// <summary>
    /// Servicio para gestionar mantenimientos correctivos (reactivos) de equipos e periféricos
    /// Responsabilidades:
    /// - CRUD de mantenimientos correctivos
    /// - Cambio de estado de equipos/periféricos durante reparación
    /// - Validación de bloqueos para dar de baja durante mantenimiento
    /// </summary>
    public class MantenimientoCorrectivoService : IMantenimientoCorrectivoService
    {
        private readonly GestLogDbContext _context;
        private readonly IGestLogLogger _logger;

        public MantenimientoCorrectivoService(
            GestLogDbContext context,
            IGestLogLogger logger)
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

                if (!includeDadosDeBaja)
                    query = query.Where(m => !m.DadoDeBaja);

                var mantenimientos = await query
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);

                return MapearListaADtos(mantenimientos);
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

                return mantenimiento == null ? null : MapearADto(mantenimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mantenimiento correctivo con ID {Id}", id);
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
                    .Where(m => m.EquipoInformaticoId == equipoInformaticoId && !m.DadoDeBaja)
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);

                return MapearListaADtos(mantenimientos);
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
                    .Where(m => m.PerifericoEquipoInformaticoId == perifericoId && !m.DadoDeBaja)
                    .OrderByDescending(m => m.FechaRegistro)
                    .ToListAsync(cancellationToken);

                return MapearListaADtos(mantenimientos);
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
                    .Where(m => m.Estado == EstadoMantenimientoCorrectivo.EnReparacion && !m.DadoDeBaja)
                    .OrderByDescending(m => m.FechaInicio)
                    .ToListAsync(cancellationToken);

                return MapearListaADtos(mantenimientos);
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
                    TipoEntidad = dto.TipoEntidad,
                    EquipoInformaticoId = dto.EquipoInformaticoId,
                    PerifericoEquipoInformaticoId = dto.PerifericoEquipoInformaticoId,
                    FechaFalla = dto.FechaFalla,
                    DescripcionFalla = dto.DescripcionFalla,
                    ProveedorAsignado = dto.ProveedorAsignado,
                    Estado = EstadoMantenimientoCorrectivo.EnReparacion, // Se inicia en En Reparación
                    FechaInicio = DateTime.Now,
                    Observaciones = dto.Observaciones,
                    DadoDeBaja = false,
                    UsuarioRegistroId = usuarioRegistroId,
                    FechaRegistro = DateTime.Now,
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };                _context.MantenimientosCorrectivos.Add(mantenimiento);

                // ✅ PASO 12: Cambiar estado del equipo/periférico a "En Reparación"
                if (dto.TipoEntidad == "Equipo" && !string.IsNullOrEmpty(dto.EquipoInformaticoCodigo))
                {
                    var equipo = await _context.EquiposInformaticos
                        .FirstOrDefaultAsync(e => e.Codigo == dto.EquipoInformaticoCodigo, cancellationToken);
                    
                    if (equipo != null)
                    {
                        equipo.Estado = "En Reparación";
                        equipo.FechaModificacion = DateTime.Now;
                        _context.EquiposInformaticos.Update(equipo);
                    }
                }
                else if (dto.TipoEntidad == "Periferico" && !string.IsNullOrEmpty(dto.PerifericoEquipoInformaticoCodigo))
                {
                    var periferico = await _context.PerifericosEquiposInformaticos
                        .FirstOrDefaultAsync(p => p.Codigo == dto.PerifericoEquipoInformaticoCodigo, cancellationToken);
                    
                    if (periferico != null)
                    {
                        periferico.Estado = EstadoPeriferico.EnReparacion;
                        periferico.FechaModificacion = DateTime.Now;
                        _context.PerifericosEquiposInformaticos.Update(periferico);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Mantenimiento correctivo creado. ID: {Id}, Tipo: {Tipo}, Estado: {Estado}",
                    mantenimiento.Id, dto.TipoEntidad, mantenimiento.Estado);

                return mantenimiento.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando mantenimiento correctivo");
                throw;
            }
        }        /// <inheritdoc/>
        public async Task<bool> ActualizarAsync(
            MantenimientoCorrectivoDto dto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null || !dto.Id.HasValue)
                    throw new ArgumentException("ID del mantenimiento es requerido");

                var mantenimiento = await _context.MantenimientosCorrectivos
                    .FirstOrDefaultAsync(m => m.Id == dto.Id, cancellationToken);

                if (mantenimiento == null)
                    return false;

                mantenimiento.ProveedorAsignado = dto.ProveedorAsignado;
                mantenimiento.Observaciones = dto.Observaciones;
                mantenimiento.FechaActualizacion = DateTime.Now;

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo actualizado. ID: {Id}", dto.Id.Value);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando mantenimiento correctivo con ID {Id}", dto.Id?.ToString() ?? "sin ID");
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
                    return false;                mantenimiento.Estado = EstadoMantenimientoCorrectivo.Completado;
                mantenimiento.FechaCompletado = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(observaciones))
                    mantenimiento.Observaciones = observaciones;
                mantenimiento.FechaActualizacion = DateTime.Now;

                // ✅ PASO 12: Cambiar estado del equipo/periférico a "Activo"
                if (mantenimiento.TipoEntidad == "Equipo" && mantenimiento.EquipoInformatico != null)
                {
                    mantenimiento.EquipoInformatico.Estado = "Activo";
                    mantenimiento.EquipoInformatico.FechaModificacion = DateTime.Now;
                    _context.EquiposInformaticos.Update(mantenimiento.EquipoInformatico);
                }
                else if (mantenimiento.TipoEntidad == "Periferico" && mantenimiento.PerifericoEquipoInformatico != null)
                {
                    mantenimiento.PerifericoEquipoInformatico.Estado = EstadoPeriferico.EnUso;
                    mantenimiento.PerifericoEquipoInformatico.FechaModificacion = DateTime.Now;
                    _context.PerifericosEquiposInformaticos.Update(mantenimiento.PerifericoEquipoInformatico);
                }

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
                    return false;                mantenimiento.Estado = EstadoMantenimientoCorrectivo.Cancelado;
                mantenimiento.Observaciones = $"Cancelado: {razonCancelacion}";
                mantenimiento.FechaActualizacion = DateTime.Now;

                // ✅ PASO 12: Cambiar estado del equipo/periférico a "Activo" cuando se cancela
                if (mantenimiento.TipoEntidad == "Equipo" && mantenimiento.EquipoInformatico != null)
                {
                    mantenimiento.EquipoInformatico.Estado = "Activo";
                    mantenimiento.EquipoInformatico.FechaModificacion = DateTime.Now;
                    _context.EquiposInformaticos.Update(mantenimiento.EquipoInformatico);
                }
                else if (mantenimiento.TipoEntidad == "Periferico" && mantenimiento.PerifericoEquipoInformatico != null)
                {
                    mantenimiento.PerifericoEquipoInformatico.Estado = EstadoPeriferico.EnUso;
                    mantenimiento.PerifericoEquipoInformatico.FechaModificacion = DateTime.Now;
                    _context.PerifericosEquiposInformaticos.Update(mantenimiento.PerifericoEquipoInformatico);
                }

                _context.MantenimientosCorrectivos.Update(mantenimiento);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Mantenimiento correctivo cancelado. ID: {Id}, Razón: {Razon}", id, razonCancelacion);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando mantenimiento correctivo con ID {Id}", id);
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

                mantenimiento.DadoDeBaja = true;
                mantenimiento.FechaActualizacion = DateTime.Now;

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
                    .AnyAsync(m => m.EquipoInformaticoId == equipoInformaticoId &&
                                   m.Estado == EstadoMantenimientoCorrectivo.EnReparacion &&
                                   !m.DadoDeBaja,
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
                    .AnyAsync(m => m.PerifericoEquipoInformaticoId == perifericoId &&
                                   m.Estado == EstadoMantenimientoCorrectivo.EnReparacion &&
                                   !m.DadoDeBaja,
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando mantenimiento en progreso para periférico {PerifericoId}", perifericoId);
                throw;
            }
        }

        /// <summary>
        /// Mapea una entidad a DTO
        /// </summary>
        private MantenimientoCorrectivoDto MapearADto(MantenimientoCorrectivoEntity entity)
        {
            return new MantenimientoCorrectivoDto
            {
                Id = entity.Id,
                TipoEntidad = entity.TipoEntidad,
                EquipoInformaticoId = entity.EquipoInformaticoId,
                PerifericoEquipoInformaticoId = entity.PerifericoEquipoInformaticoId,
                FechaFalla = entity.FechaFalla,
                DescripcionFalla = entity.DescripcionFalla,
                ProveedorAsignado = entity.ProveedorAsignado,
                Estado = entity.Estado,
                FechaInicio = entity.FechaInicio,
                FechaCompletado = entity.FechaCompletado,
                Observaciones = entity.Observaciones,
                DadoDeBaja = entity.DadoDeBaja,
                UsuarioRegistroId = entity.UsuarioRegistroId,
                FechaRegistro = entity.FechaRegistro,
                FechaCreacion = entity.FechaCreacion,
                FechaActualizacion = entity.FechaActualizacion
            };
        }

        /// <summary>
        /// Mapea una lista de entidades a DTOs
        /// </summary>
        private List<MantenimientoCorrectivoDto> MapearListaADtos(List<MantenimientoCorrectivoEntity> entities)
        {
            return entities.Select(MapearADto).ToList();
        }
    }
}
