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
                // Validar que la placa no exista
                var exists = await ExistsByPlateAsync(vehicleDto.Plate, cancellationToken);
                if (exists)
                {
                    throw new InvalidOperationException($"Ya existe un vehículo con la placa {vehicleDto.Plate}");
                }

                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                
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
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                context.Vehicles.Add(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Vehículo creado exitosamente: {Plate}", vehicle.Plate);
                return MapToDto(vehicle);
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
                
                var vehicle = await context.Vehicles
                    .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);

                if (vehicle == null)
                {
                    throw new InvalidOperationException($"Vehículo con ID {id} no encontrado");
                }

                // Validar cambio de placa
                if (vehicle.Plate != vehicleDto.Plate)
                {
                    var exists = await ExistsByPlateAsync(vehicleDto.Plate, cancellationToken);
                    if (exists)
                    {
                        throw new InvalidOperationException($"Ya existe un vehículo con la placa {vehicleDto.Plate}");
                    }
                }

                vehicle.Plate = vehicleDto.Plate;
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
                vehicle.UpdatedAt = DateTimeOffset.UtcNow;

                context.Vehicles.Update(vehicle);
                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Vehículo actualizado exitosamente: {Plate}", vehicle.Plate);
                return MapToDto(vehicle);
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

                _logger.LogInformation("Vehículo eliminado exitosamente: {Plate}", vehicle.Plate);
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
                using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
                return await context.Vehicles
                    .AsNoTracking()
                    .AnyAsync(v => v.Plate == plate && !v.IsDeleted, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de placa: {Plate}", plate);
                throw;
            }
        }

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
        }        /// <summary>
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
                CreatedAt = vehicle.CreatedAt,
                UpdatedAt = vehicle.UpdatedAt,
                IsDeleted = vehicle.IsDeleted
            };
        }
    }
}
