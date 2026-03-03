using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Entities;
using GestLog.Services.Core.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Services.Data
{
    /// <summary>
    /// Servicio para operaciones CRUD de vehículos
    /// </summary>
    public class VehicleService : IVehicleService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public VehicleService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var vehicles = await context.Vehicles
                    .AsNoTracking()
                    .Where(v => !v.IsDeleted)
                    .OrderBy(v => v.Plate)
                    .ToListAsync(cancellationToken);

                return vehicles.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de vehículos");
                throw;
            }
        }

        public async Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var vehicle = await context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

                return vehicle != null ? MapToDto(vehicle) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener vehículo por ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task<VehicleDto?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var vehicle = await context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Plate == plate && !v.IsDeleted, cancellationToken);

                return vehicle != null ? MapToDto(vehicle) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener vehículo por placa: {Plate}", plate);
                throw;
            }
        }

        public async Task<VehicleDto> CreateAsync(VehicleDto vehicleDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var normalizedPlate = NormalizePlate(vehicleDto.Plate);

                // Validar placa incluyendo eliminados lógicos (el índice UNIQUE en BD también los incluye)
                var existingByPlate = await context.Vehicles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Plate == normalizedPlate, cancellationToken);

                if (existingByPlate != null)
                {
                    if (existingByPlate.IsDeleted)
                    {
                        throw new InvalidOperationException($"La placa '{normalizedPlate}' ya existe en un vehículo eliminado. Debes restaurarlo o usar otra placa.");
                    }

                    throw new InvalidOperationException($"Ya existe un vehículo con la placa '{normalizedPlate}'.");
                }
                
                var vehicle = new Vehicle
                {
                    Id = Guid.NewGuid(),                    Plate = vehicleDto.Plate,
                    Vin = vehicleDto.Vin,
                    Brand = vehicleDto.Brand,
                    Model = vehicleDto.Model,
                    Version = vehicleDto.Version,
                    Year = vehicleDto.Year,
                    Color = vehicleDto.Color,
                    Mileage = vehicleDto.Mileage,
                    Type = vehicleDto.Type,
                    State = vehicleDto.State,
                    PhotoPath = vehicleDto.PhotoPath,
                    PhotoThumbPath = vehicleDto.PhotoThumbPath,
                    FuelType = vehicleDto.FuelType,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                context.Vehicles.Add(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(vehicle);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                var normalizedPlate = NormalizePlate(vehicleDto.Plate);
                throw new InvalidOperationException($"No se pudo guardar: la placa '{normalizedPlate}' ya está registrada (incluyendo vehículos eliminados).", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear vehículo");
                throw;
            }
        }

        public async Task<VehicleDto> UpdateAsync(Guid id, VehicleDto vehicleDto, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                var normalizedPlate = NormalizePlate(vehicleDto.Plate);
                
                var vehicle = await context.Vehicles
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehículo con ID {id} no encontrado");
                }

                // Validar cambio de placa
                if (!string.Equals(vehicle.Plate, normalizedPlate, StringComparison.OrdinalIgnoreCase))
                {
                    var duplicated = await context.Vehicles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Plate == normalizedPlate && v.Id != id, cancellationToken);

                    if (duplicated != null)
                    {
                        if (duplicated.IsDeleted)
                        {
                            throw new InvalidOperationException($"La placa '{normalizedPlate}' ya existe en un vehículo eliminado. Debes restaurarlo o usar otra placa.");
                        }

                        throw new InvalidOperationException($"Ya existe un vehículo con la placa '{normalizedPlate}'.");
                    }
                }

                vehicle.Plate = normalizedPlate;
                vehicle.Vin = vehicleDto.Vin;                vehicle.Brand = vehicleDto.Brand;
                vehicle.Model = vehicleDto.Model;
                vehicle.Version = vehicleDto.Version;
                vehicle.Year = vehicleDto.Year;
                vehicle.Color = vehicleDto.Color;
                vehicle.Mileage = vehicleDto.Mileage;
                vehicle.Type = vehicleDto.Type;
                vehicle.State = vehicleDto.State;
                vehicle.PhotoPath = vehicleDto.PhotoPath;
                vehicle.PhotoThumbPath = vehicleDto.PhotoThumbPath;
                vehicle.FuelType = vehicleDto.FuelType;
                vehicle.UpdatedAt = DateTimeOffset.UtcNow;

                context.Vehicles.Update(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                return MapToDto(vehicle);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                var normalizedPlate = NormalizePlate(vehicleDto.Plate);
                throw new InvalidOperationException($"No se pudo actualizar: la placa '{normalizedPlate}' ya está registrada (incluyendo vehículos eliminados).", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar vehículo con ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
                var vehicle = await context.Vehicles
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehículo con ID {id} no encontrado");
                }

                // Soft delete
                vehicle.IsDeleted = true;
                vehicle.UpdatedAt = DateTimeOffset.UtcNow;

                context.Vehicles.Update(vehicle);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar vehículo con ID: {VehicleId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedPlate = NormalizePlate(plate);
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.Vehicles
                    .AsNoTracking()
                    .AnyAsync(v => v.Plate == normalizedPlate && !v.IsDeleted, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de placa: {Plate}", plate);
                throw;
            }
        }

        private static string NormalizePlate(string? plate)
            => (plate ?? string.Empty).Trim().ToUpperInvariant();

        public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.Vehicles
                    .AsNoTracking()
                    .Where(v => !v.IsDeleted)
                    .CountAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener contador total de vehículos");
                throw;
            }
        }

        public async Task<List<string>> GetSuggestedBrandsAsync(string? filter = null, int limit = 30, CancellationToken cancellationToken = default)
        {
            try
            {
                var normalizedFilter = (filter ?? string.Empty).Trim();
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

                var query = context.Vehicles
                    .AsNoTracking()
                    .Where(v => !v.IsDeleted && !string.IsNullOrWhiteSpace(v.Brand));

                if (!string.IsNullOrWhiteSpace(normalizedFilter))
                {
                    var lowered = normalizedFilter.ToLower();
                    query = query.Where(v => v.Brand.ToLower().Contains(lowered));
                }

                var marcas = await query
                    .GroupBy(v => v.Brand.Trim().ToLower())
                    .Select(g => new
                    {
                        Marca = g.First().Brand.Trim(),
                        Cantidad = g.Count()
                    })
                    .OrderByDescending(x => x.Cantidad)
                    .ThenBy(x => x.Marca)
                    .Take(limit)
                    .Select(x => x.Marca)
                    .ToListAsync(cancellationToken);

                return marcas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener sugerencias de marcas de vehículos");
                throw;
            }
        }

        /// <summary>
        /// Mapea una entidad Vehicle a VehicleDto
        /// </summary>
        private static VehicleDto MapToDto(Vehicle vehicle)
        {
            return new VehicleDto
            {
                Id = vehicle.Id,
                Plate = vehicle.Plate,
                Vin = vehicle.Vin,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Version = vehicle.Version,
                Year = vehicle.Year,
                Color = vehicle.Color,
                Mileage = vehicle.Mileage,
                Type = vehicle.Type,
                State = vehicle.State,
                PhotoPath = vehicle.PhotoPath,
                PhotoThumbPath = vehicle.PhotoThumbPath,
                FuelType = vehicle.FuelType,
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt,
                IsDeleted = vehicle.IsDeleted
            };
        }
    }
}
