using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Messages;
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
    /// Servicio para gestión de mantenimientos correctivos (reactivos)
    /// </summary>
    public class MantenimientoCorrectivoService : IMantenimientoCorrectivoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _contextFactory;
        private readonly IGestLogLogger _logger;
        private readonly IEquipoInformaticoService _equipoService;
        private readonly IPerifericoService _perifericoService;
        private readonly IMessenger _messenger;

        public MantenimientoCorrectivoService(
            IDbContextFactory<GestLogDbContext> contextFactory,
            IGestLogLogger logger,
            IEquipoInformaticoService equipoService,
            IPerifericoService perifericoService,
            IMessenger messenger)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _equipoService = equipoService ?? throw new ArgumentNullException(nameof(equipoService));
            _perifericoService = perifericoService ?? throw new ArgumentNullException(nameof(perifericoService));
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        /// <inheritdoc/>
        public async Task<List<MantenimientoCorrectivoDto>> ObtenerTodosAsync(
            bool includeDadosDeBaja = false,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var query = context.MantenimientosCorrectivos.AsNoTracking().AsQueryable();
                    var mantenimientos = await query
                        .OrderByDescending(m => m.FechaRegistro)
                        .ToListAsync(cancellationToken);
                    return MantenimientosADtos(mantenimientos);
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
                    return mantenimiento == null ? null : MantenimientoADto(mantenimiento);
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimientos = await context.MantenimientosCorrectivos
                        .AsNoTracking()
                        .Where(m => m.TipoEntidad == "Equipo")
                        .OrderByDescending(m => m.FechaRegistro)
                        .ToListAsync(cancellationToken);
                    return MantenimientosADtos(mantenimientos);
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimientos = await context.MantenimientosCorrectivos
                        .AsNoTracking()
                        .Where(m => m.TipoEntidad == "Periférico")
                        .OrderByDescending(m => m.FechaRegistro)
                        .ToListAsync(cancellationToken);
                    return MantenimientosADtos(mantenimientos);
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimientos = await context.MantenimientosCorrectivos
                        .AsNoTracking()
                        .Where(m => m.Estado == EstadoMantenimientoCorrectivo.EnReparacion)
                        .OrderByDescending(m => m.FechaRegistro)
                        .ToListAsync(cancellationToken);
                    return MantenimientosADtos(mantenimientos);
                }
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

                using (var context = _contextFactory.CreateDbContext())
                {
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

                    context.MantenimientosCorrectivos.Add(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Mantenimiento correctivo creado. ID: {Id}", mantenimiento.Id);
                    return mantenimiento.Id;
                }
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

                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == dto.Id.Value, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    mantenimiento.ProveedorAsignado = dto.ProveedorAsignado;
                    mantenimiento.Observaciones = dto.Observaciones;
                    mantenimiento.FechaActualizacion = DateTime.UtcNow;

                    context.MantenimientosCorrectivos.Update(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Mantenimiento correctivo actualizado. ID: {Id}", dto.Id.Value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando mantenimiento correctivo con ID {Id}", dto?.Id?.ToString() ?? "sin ID");
                throw;
            }
        }        /// <inheritdoc/>
        public async Task<bool> EnviarAReparacionAsync(
            int id,
            string proveedorAsignado,
            DateTime fechaInicio,
            string? observaciones = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    // Solo se puede enviar a reparación si está en estado Pendiente
                    if (mantenimiento.Estado != EstadoMantenimientoCorrectivo.Pendiente)
                        return false;

                    mantenimiento.Estado = EstadoMantenimientoCorrectivo.EnReparacion;
                    mantenimiento.ProveedorAsignado = proveedorAsignado;
                    mantenimiento.FechaInicio = fechaInicio;
                    if (!string.IsNullOrWhiteSpace(observaciones))
                        mantenimiento.Observaciones = observaciones;
                    mantenimiento.FechaActualizacion = DateTime.UtcNow;                    context.MantenimientosCorrectivos.Update(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);                    // Actualizar estado del equipo o periférico a "En Reparación"
                    if (!string.IsNullOrWhiteSpace(mantenimiento.Codigo))
                    {
                        bool estadoCambiado = await ActualizarEstadoEquipoOPeriferico(
                            mantenimiento.TipoEntidad,
                            mantenimiento.Codigo,
                            cancellationToken);

                        if (estadoCambiado)
                        {
                            _logger.LogInformation("Mantenimiento correctivo enviado a reparación. ID: {Id}, Proveedor: {Proveedor}, Estado de {TipoEntidad} actualizado", 
                                id, proveedorAsignado, mantenimiento.TipoEntidad);
                            
                            // Enviar mensaje para notificar cambio de estado a otras vistas
                            _messenger.Send(new MantenimientosCorrectivosActualizadosMessage(null));
                        }
                        else
                        {
                            _logger.LogWarning("Mantenimiento correctivo enviado a reparación (ID: {Id}), pero no se pudo actualizar estado de {TipoEntidad} con código {Codigo}", 
                                id, mantenimiento.TipoEntidad, mantenimiento.Codigo);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Mantenimiento correctivo enviado a reparación (ID: {Id}), código está vacío", id);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando mantenimiento correctivo a reparación con ID {Id}", id);
                throw;
            }
        }        /// <inheritdoc/>
        public async Task<bool> CompletarAsync(
            int id,
            decimal? costoReparacion = null,
            string? observaciones = null,
            int? periodoGarantia = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    mantenimiento.Estado = EstadoMantenimientoCorrectivo.Completado;
                    mantenimiento.FechaCompletado = DateTime.UtcNow;
                    if (costoReparacion.HasValue)
                        mantenimiento.CostoReparacion = costoReparacion;
                    if (!string.IsNullOrWhiteSpace(observaciones))
                        mantenimiento.Observaciones = observaciones;
                    if (periodoGarantia.HasValue)
                        mantenimiento.PeriodoGarantia = periodoGarantia;
                    mantenimiento.FechaActualizacion = DateTime.UtcNow;                    context.MantenimientosCorrectivos.Update(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);                    // Restaurar estado del equipo o periférico a su estado normal
                    if (!string.IsNullOrWhiteSpace(mantenimiento.Codigo))
                    {
                        bool estadoRestaurado = await RestaurarEstadoEquipoOPeriferico(
                            mantenimiento.TipoEntidad,
                            mantenimiento.Codigo,
                            cancellationToken);

                        if (estadoRestaurado)
                        {
                            _logger.LogInformation("Mantenimiento correctivo completado. ID: {Id}, Costo: {Costo}, Garantía: {Periodo} días, Estado de {TipoEntidad} restaurado",
                                id, costoReparacion ?? 0, periodoGarantia ?? 0, mantenimiento.TipoEntidad);
                            
                            // Enviar mensaje para notificar cambio de estado a otras vistas
                            _messenger.Send(new MantenimientosCorrectivosActualizadosMessage(null));
                        }
                        else
                        {
                            _logger.LogWarning("Mantenimiento correctivo completado (ID: {Id}), pero no se pudo restaurar estado de {TipoEntidad} con código {Codigo}",
                                id, mantenimiento.TipoEntidad, mantenimiento.Codigo);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Mantenimiento correctivo completado (ID: {Id}), código está vacío", id);
                    }

                    return true;
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    mantenimiento.Estado = EstadoMantenimientoCorrectivo.Cancelado;
                    if (!string.IsNullOrWhiteSpace(razonCancelacion))
                        mantenimiento.Observaciones = razonCancelacion;
                    mantenimiento.FechaActualizacion = DateTime.UtcNow;

                    context.MantenimientosCorrectivos.Update(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Mantenimiento correctivo cancelado. ID: {Id}", id);
                    return true;
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    context.MantenimientosCorrectivos.Remove(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Mantenimiento correctivo eliminado. ID: {Id}", id);
                    return true;
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    var mantenimiento = await context.MantenimientosCorrectivos
                        .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

                    if (mantenimiento == null)
                        return false;

                    mantenimiento.Estado = EstadoMantenimientoCorrectivo.Cancelado;
                    mantenimiento.FechaActualizacion = DateTime.UtcNow;

                    context.MantenimientosCorrectivos.Update(mantenimiento);
                    await context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Mantenimiento correctivo dado de baja. ID: {Id}", id);
                    return true;
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    return await context.MantenimientosCorrectivos
                        .AnyAsync(m => m.TipoEntidad == "Equipo" && 
                                       m.Estado == EstadoMantenimientoCorrectivo.EnReparacion,
                                  cancellationToken);
                }
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
                using (var context = _contextFactory.CreateDbContext())
                {
                    return await context.MantenimientosCorrectivos
                        .AnyAsync(m => m.TipoEntidad == "Periférico" && 
                                       m.Estado == EstadoMantenimientoCorrectivo.EnReparacion,
                                  cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando mantenimiento periférico en progreso para periférico {PerifericoId}", perifericoId);
                throw;
            }
        }        /// <summary>
        /// Actualiza el estado del equipo o periférico cuando se envía a reparación
        /// </summary>
        private async Task<bool> ActualizarEstadoEquipoOPeriferico(
            string tipoEntidad,
            string codigo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (tipoEntidad == "Equipo")
                {
                    return await _equipoService.CambiarEstadoAsync(
                        codigo,
                        EstadoEquipoInformatico.EnReparacion,
                        cancellationToken);
                }
                else if (tipoEntidad == "Periférico")
                {
                    return await _perifericoService.CambiarEstadoAsync(
                        codigo,
                        EstadoPeriferico.EnReparacion,
                        cancellationToken);
                }

                _logger.LogWarning("TipoEntidad desconocido: {TipoEntidad}", tipoEntidad);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando estado de {TipoEntidad} con código {Codigo}", tipoEntidad, codigo);
                return false;
            }
        }        /// <summary>
        /// Restaura el estado del equipo o periférico cuando se completa el mantenimiento
        /// </summary>
        private async Task<bool> RestaurarEstadoEquipoOPeriferico(
            string tipoEntidad,
            string codigo,
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    _logger.LogWarning("Código vacío al restaurar estado de {TipoEntidad}", tipoEntidad);
                    return false;
                }

                if (tipoEntidad == "Equipo")
                {
                    // Restaurar a "Activo" después de reparación
                    return await _equipoService.CambiarEstadoAsync(
                        codigo,
                        EstadoEquipoInformatico.Activo,
                        cancellationToken);
                }
                else if (tipoEntidad == "Periférico")
                {
                    // Restaurar a "AlmacenadoFuncionando" después de reparación
                    return await _perifericoService.CambiarEstadoAsync(
                        codigo,
                        EstadoPeriferico.AlmacenadoFuncionando,
                        cancellationToken);
                }

                _logger.LogWarning("TipoEntidad desconocido: {TipoEntidad}", tipoEntidad);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restaurando estado de {TipoEntidad} con código {Codigo}", tipoEntidad, codigo);
                return false;
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
                CostoReparacion = entity.CostoReparacion,
                PeriodoGarantia = entity.PeriodoGarantia
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

