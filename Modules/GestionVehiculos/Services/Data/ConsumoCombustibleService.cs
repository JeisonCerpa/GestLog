using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Entities;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Services.Data
{
    public class ConsumoCombustibleService : IConsumoCombustibleService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public ConsumoCombustibleService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyCollection<ConsumoCombustibleVehiculoDto>> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            var placa = (placaVehiculo ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(placa))
            {
                return Array.Empty<ConsumoCombustibleVehiculoDto>();
            }

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var items = await context.ConsumosCombustibleVehiculo
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.PlacaVehiculo == placa)
                .OrderByDescending(x => x.FechaTanqueada)
                .ThenByDescending(x => x.Id)
                .ToListAsync(cancellationToken);

            return items.Select(MapToDto).ToList();
        }

        public async Task<ConsumoCombustibleVehiculoDto> CreateAsync(ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            var placa = (dto.PlacaVehiculo ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(placa))
            {
                throw new InvalidOperationException("La placa del vehículo es obligatoria.");
            }

            if (dto.KMAlMomento < 0 || dto.Galones <= 0 || dto.ValorTotal < 0)
            {
                throw new InvalidOperationException("Los valores de kilometraje, galones y costo no son válidos.");
            }

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = new ConsumoCombustibleVehiculo
            {
                PlacaVehiculo = placa,
                FechaTanqueada = dto.FechaTanqueada,
                KMAlMomento = dto.KMAlMomento,
                Galones = dto.Galones,
                ValorTotal = dto.ValorTotal,
                Proveedor = dto.Proveedor?.Trim(),
                RutaFactura = dto.RutaFactura?.Trim(),
                Observaciones = dto.Observaciones?.Trim(),
                FechaRegistro = DateTimeOffset.UtcNow,
                FechaActualizacion = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            context.ConsumosCombustibleVehiculo.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            await SyncVehicleMileageIfHigherAsync(context, placa, cancellationToken);

            return MapToDto(entity);
        }

        public async Task<ConsumoCombustibleVehiculoDto> UpdateAsync(int id, ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await context.ConsumosCombustibleVehiculo
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException($"No existe registro de tanqueada con ID {id}.");
            }

            if (dto.KMAlMomento < 0 || dto.Galones <= 0 || dto.ValorTotal < 0)
            {
                throw new InvalidOperationException("Los valores de kilometraje, galones y costo no son válidos.");
            }

            entity.FechaTanqueada = dto.FechaTanqueada;
            entity.KMAlMomento = dto.KMAlMomento;
            entity.Galones = dto.Galones;
            entity.ValorTotal = dto.ValorTotal;
            entity.Proveedor = dto.Proveedor?.Trim();
            entity.RutaFactura = dto.RutaFactura?.Trim();
            entity.Observaciones = dto.Observaciones?.Trim();
            entity.FechaActualizacion = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            var placa = (entity.PlacaVehiculo ?? string.Empty).Trim().ToUpperInvariant();
            await SyncVehicleMileageIfHigherAsync(context, placa, cancellationToken);

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var entity = await context.ConsumosCombustibleVehiculo
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

            if (entity == null)
            {
                return;
            }

            entity.IsDeleted = true;
            entity.FechaActualizacion = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task<ResumenConsumoCombustibleDto> GetResumenByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            var items = await GetByPlacaAsync(placaVehiculo, cancellationToken);
            if (items.Count == 0)
            {
                return new ResumenConsumoCombustibleDto();
            }

            var totalGalones = items.Sum(x => x.Galones);
            var totalCosto = items.Sum(x => x.ValorTotal);

            return new ResumenConsumoCombustibleDto
            {
                TotalRegistros = items.Count,
                TotalGalones = decimal.Round(totalGalones, 2),
                TotalCosto = decimal.Round(totalCosto, 2),
                PromedioCostoPorGalon = totalGalones > 0 ? decimal.Round(totalCosto / totalGalones, 2) : 0m
            };
        }

        private static ConsumoCombustibleVehiculoDto MapToDto(ConsumoCombustibleVehiculo entity)
        {
            return new ConsumoCombustibleVehiculoDto
            {
                Id = entity.Id,
                PlacaVehiculo = entity.PlacaVehiculo,
                FechaTanqueada = entity.FechaTanqueada,
                KMAlMomento = entity.KMAlMomento,
                Galones = entity.Galones,
                ValorTotal = entity.ValorTotal,
                Proveedor = entity.Proveedor,
                RutaFactura = entity.RutaFactura,
                Observaciones = entity.Observaciones
            };
        }

        private static async Task SyncVehicleMileageIfHigherAsync(
            GestLogDbContext context,
            string placa,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(placa))
            {
                return;
            }

            var vehiculo = await context.Vehicles
                .FirstOrDefaultAsync(v => !v.IsDeleted && v.Plate == placa, cancellationToken);

            if (vehiculo == null)
            {
                return;
            }

            var maxKmCombustible = await context.ConsumosCombustibleVehiculo
                .Where(x => !x.IsDeleted && x.PlacaVehiculo == placa)
                .Select(x => (long?)x.KMAlMomento)
                .MaxAsync(cancellationToken) ?? 0;

            var maxKmMantenimiento = await context.EjecucionesMantenimiento
                .Where(x => !x.IsDeleted && x.PlacaVehiculo == placa)
                .Select(x => (long?)x.KMAlMomento)
                .MaxAsync(cancellationToken) ?? 0;

            var maxOperativo = Math.Max(maxKmCombustible, maxKmMantenimiento);
            if (maxOperativo > vehiculo.Mileage)
            {
                vehiculo.Mileage = maxOperativo;
                vehiculo.UpdatedAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
