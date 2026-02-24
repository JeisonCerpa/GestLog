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
    /// Servicio para operaciones CRUD de Planes de Mantenimiento por Vehículo
    /// </summary>
    public class PlanMantenimientoVehiculoService : IPlanMantenimientoVehiculoService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public PlanMantenimientoVehiculoService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var planes = await context.Set<PlanMantenimientoVehiculo>()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted)
                    .OrderBy(p => p.PlacaVehiculo)
                    .ToListAsync(cancellationToken);
                var dtos = planes.Select(MapToDto).ToList();
                await EnrichCalculatedFieldsAsync(context, dtos, cancellationToken);

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes de mantenimiento");
                throw;
            }
        }

        public async Task<PlanMantenimientoVehiculoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var plan = await context.Set<PlanMantenimientoVehiculo>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
                if (plan == null)
                {
                    return null;
                }

                var dto = MapToDto(plan);
                await EnrichCalculatedFieldsAsync(context, new List<PlanMantenimientoVehiculoDto> { dto }, cancellationToken);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plan por ID: {PlanId}", id);
                throw;
            }
        }

        public async Task<PlanMantenimientoVehiculoDto?> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var plan = await context.Set<PlanMantenimientoVehiculo>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PlacaVehiculo == placaVehiculo && !p.IsDeleted, cancellationToken);
                if (plan == null)
                {
                    return null;
                }

                var dto = MapToDto(plan);
                await EnrichCalculatedFieldsAsync(context, new List<PlanMantenimientoVehiculoDto> { dto }, cancellationToken);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plan por placa: {PlacaVehiculo}", placaVehiculo);
                throw;
            }
        }

        public async Task<PlanMantenimientoVehiculoDto> CreateAsync(PlanMantenimientoVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plan = new PlanMantenimientoVehiculo
                {
                    PlacaVehiculo = dto.PlacaVehiculo,
                    PlantillaId = dto.PlantillaId,
                    IntervaloKMPersonalizado = dto.IntervaloKMPersonalizado,
                    IntervaloDiasPersonalizado = dto.IntervaloDiasPersonalizado,
                    FechaInicio = dto.FechaInicio,
                    UltimoKMRegistrado = dto.UltimoKMRegistrado,
                    UltimaFechaMantenimiento = dto.UltimaFechaMantenimiento,
                    FechaFin = dto.FechaFin,
                    Activo = dto.Activo,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                };

                context.Add(plan);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Plan de mantenimiento creado para vehículo: {PlacaVehiculo}", dto.PlacaVehiculo);

                return MapToDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan de mantenimiento para: {PlacaVehiculo}", dto.PlacaVehiculo);
                throw;
            }
        }

        public async Task<PlanMantenimientoVehiculoDto> UpdateAsync(int id, PlanMantenimientoVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plan = await context.Set<PlanMantenimientoVehiculo>()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

                if (plan == null)
                    throw new InvalidOperationException($"Plan con ID {id} no encontrado");

                plan.PlacaVehiculo = dto.PlacaVehiculo;
                plan.PlantillaId = dto.PlantillaId;
                plan.IntervaloKMPersonalizado = dto.IntervaloKMPersonalizado;
                plan.IntervaloDiasPersonalizado = dto.IntervaloDiasPersonalizado;
                plan.FechaInicio = dto.FechaInicio;
                plan.UltimoKMRegistrado = dto.UltimoKMRegistrado;
                plan.UltimaFechaMantenimiento = dto.UltimaFechaMantenimiento;
                plan.FechaFin = dto.FechaFin;
                plan.Activo = dto.Activo;
                plan.FechaActualizacion = DateTime.UtcNow;

                context.Update(plan);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(plan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan de mantenimiento: {PlanId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var plan = await context.Set<PlanMantenimientoVehiculo>()
                    .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

                if (plan == null)
                    throw new InvalidOperationException($"Plan con ID {id} no encontrado");

                plan.IsDeleted = true;
                plan.FechaActualizacion = DateTime.UtcNow;

                context.Update(plan);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Plan de mantenimiento eliminado: {PlanId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plan de mantenimiento: {PlanId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetVigentesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ahora = DateTime.UtcNow;
                
                var planes = await context.Set<PlanMantenimientoVehiculo>()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && p.Activo && 
                               p.FechaInicio <= ahora && 
                               (p.FechaFin == null || p.FechaFin > ahora))
                    .OrderBy(p => p.PlacaVehiculo)
                    .ToListAsync(cancellationToken);
                var dtos = planes.Select(MapToDto).ToList();
                await EnrichCalculatedFieldsAsync(context, dtos, cancellationToken);

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes vigentes de mantenimiento");
                throw;
            }
        }

        public async Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetVencidosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var ahora = DateTime.UtcNow;
                
                var planes = await context.Set<PlanMantenimientoVehiculo>()
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && p.Activo && 
                               p.FechaInicio <= ahora && 
                               (p.FechaFin == null || p.FechaFin > ahora))
                    .OrderBy(p => p.PlacaVehiculo)
                    .ToListAsync(cancellationToken);
                var dtos = planes.Select(MapToDto).ToList();
                await EnrichCalculatedFieldsAsync(context, dtos, cancellationToken);

                var placas = dtos
                    .Select(p => NormalizePlate(p.PlacaVehiculo))
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var vehicles = await context.Vehicles
                    .AsNoTracking()
                    .Where(v => !v.IsDeleted && placas.Contains(v.Plate))
                    .Select(v => new { v.Plate, v.Mileage })
                    .ToListAsync(cancellationToken);

                var mileageByPlate = vehicles.ToDictionary(v => v.Plate, v => v.Mileage, StringComparer.OrdinalIgnoreCase);

                return dtos.Where(plan =>
                {
                    var placa = NormalizePlate(plan.PlacaVehiculo);
                    mileageByPlate.TryGetValue(placa, out var currentMileage);

                    var vencidoPorFecha = plan.ProximaFechaEjecucion.HasValue && plan.ProximaFechaEjecucion.Value <= DateTimeOffset.Now;
                    var vencidoPorKm = plan.ProximoKMEjecucion.HasValue && currentMileage >= plan.ProximoKMEjecucion.Value;

                    return vencidoPorFecha || vencidoPorKm;
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes vencidos de mantenimiento");
                throw;
            }
        }

        private static PlanMantenimientoVehiculoDto MapToDto(PlanMantenimientoVehiculo entity)
        {
            return new PlanMantenimientoVehiculoDto
            {
                Id = entity.Id,
                PlacaVehiculo = entity.PlacaVehiculo,
                PlantillaId = entity.PlantillaId,
                IntervaloKMPersonalizado = entity.IntervaloKMPersonalizado,
                IntervaloDiasPersonalizado = entity.IntervaloDiasPersonalizado,
                FechaInicio = entity.FechaInicio,
                UltimoKMRegistrado = entity.UltimoKMRegistrado,
                UltimaFechaMantenimiento = entity.UltimaFechaMantenimiento,
                FechaFin = entity.FechaFin,
                Activo = entity.Activo,
                FechaCreacion = entity.FechaCreacion,
                FechaActualizacion = entity.FechaActualizacion,
                ProximaFechaEjecucion = entity.ProximaFechaEjecucion,
                ProximoKMEjecucion = entity.ProximoKMEjecucion
            };
        }

        private async Task EnrichCalculatedFieldsAsync(
            GestLogDbContext context,
            List<PlanMantenimientoVehiculoDto> planes,
            CancellationToken cancellationToken)
        {
            if (planes.Count == 0)
            {
                return;
            }

            var plantillaIds = planes.Select(p => p.PlantillaId).Distinct().ToList();
            var plantillas = await context.Set<PlantillaMantenimiento>()
                .AsNoTracking()
                .Where(p => !p.IsDeleted && plantillaIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var placas = planes
                .Select(p => NormalizePlate(p.PlacaVehiculo))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var vehicles = await context.Vehicles
                .AsNoTracking()
                .Where(v => !v.IsDeleted && placas.Contains(v.Plate))
                .Select(v => new { v.Plate, v.Mileage })
                .ToListAsync(cancellationToken);

            var mileageByPlate = vehicles.ToDictionary(v => v.Plate, v => v.Mileage, StringComparer.OrdinalIgnoreCase);

            var planIds = planes.Select(p => p.Id).Distinct().ToList();
            var ejecuciones = await context.Set<EjecucionMantenimiento>()
                .AsNoTracking()
                .Where(e => !e.IsDeleted && (
                    (e.PlanMantenimientoId.HasValue && planIds.Contains(e.PlanMantenimientoId.Value)) ||
                    placas.Contains(e.PlacaVehiculo)))
                .OrderByDescending(e => e.FechaEjecucion)
                .ToListAsync(cancellationToken);

            foreach (var plan in planes)
            {
                if (!plantillas.TryGetValue(plan.PlantillaId, out var plantilla))
                {
                    continue;
                }

                var placa = NormalizePlate(plan.PlacaVehiculo);
                var ultimaEjecucion = ejecuciones
                    .FirstOrDefault(e =>
                        (e.PlanMantenimientoId.HasValue && e.PlanMantenimientoId.Value == plan.Id) ||
                        string.Equals(e.PlacaVehiculo, placa, StringComparison.OrdinalIgnoreCase));

                var intervaloKm = plan.IntervaloKMPersonalizado ?? plantilla.IntervaloKM;
                var intervaloDias = plan.IntervaloDiasPersonalizado ?? plantilla.IntervaloDias;

                var baseKm = ultimaEjecucion?.KMAlMomento
                             ?? plan.UltimoKMRegistrado
                             ?? 0L;

                var baseFecha = ultimaEjecucion?.FechaEjecucion
                                ?? plan.UltimaFechaMantenimiento
                                ?? plan.FechaInicio;

                plan.ProximoKMEjecucion = baseKm + intervaloKm;
                plan.ProximaFechaEjecucion = baseFecha.AddDays(intervaloDias);

                var nowDate = DateTimeOffset.Now.Date;
                mileageByPlate.TryGetValue(placa, out var currentMileage);

                var vencidoPorFecha = plan.ProximaFechaEjecucion.HasValue && plan.ProximaFechaEjecucion.Value.Date <= nowDate;
                var vencidoPorKm = plan.ProximoKMEjecucion.HasValue && currentMileage >= plan.ProximoKMEjecucion.Value;
                var vencido = vencidoPorFecha || vencidoPorKm;

                var diasRestantes = plan.ProximaFechaEjecucion.HasValue
                    ? (int?)(plan.ProximaFechaEjecucion.Value.Date - nowDate).TotalDays
                    : null;

                var kmsRestantes = plan.ProximoKMEjecucion.HasValue
                    ? (long?)(plan.ProximoKMEjecucion.Value - currentMileage)
                    : null;

                var proximoPorFecha = diasRestantes.HasValue && diasRestantes.Value <= 7 && diasRestantes.Value >= 0;
                var proximoPorKm = kmsRestantes.HasValue && kmsRestantes.Value <= 500 && kmsRestantes.Value >= 0;
                var proximo = !vencido && (proximoPorFecha || proximoPorKm);

                if (!plan.ProximaFechaEjecucion.HasValue && !plan.ProximoKMEjecucion.HasValue)
                {
                    plan.EstadoPlan = "Sin datos";
                    plan.EstadoPlanDetalle = "No hay fecha o km próximo calculado";
                }
                else if (vencido)
                {
                    plan.EstadoPlan = "Vencido";
                    plan.EstadoPlanDetalle = "Ya superó la fecha o el km objetivo";
                }
                else if (proximo)
                {
                    plan.EstadoPlan = "Próximo";
                    plan.EstadoPlanDetalle = "Dentro de ventana 7 días / 500 km";
                }
                else
                {
                    plan.EstadoPlan = "Vigente";
                    plan.EstadoPlanDetalle = "Fuera de ventana de vencimiento";
                }
            }
        }

        private static string NormalizePlate(string? plate)
        {
            return string.IsNullOrWhiteSpace(plate)
                ? string.Empty
                : plate.Trim().ToUpperInvariant();
        }
    }
}
