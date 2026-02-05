using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Data
{
    /// <summary>
    /// Servicio para operaciones CRUD de vehículos
    /// </summary>
    public interface IVehicleService
    {
        /// <summary>
        /// Obtiene todos los vehículos activos (no eliminados lógicamente)
        /// </summary>
        Task<IEnumerable<VehicleDto>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un vehículo por su identificador
        /// </summary>
        Task<VehicleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene un vehículo por su placa
        /// </summary>
        Task<VehicleDto?> GetByPlateAsync(string plate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crea un nuevo vehículo
        /// </summary>
        Task<VehicleDto> CreateAsync(VehicleDto vehicleDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Actualiza un vehículo existente
        /// </summary>
        Task<VehicleDto> UpdateAsync(Guid id, VehicleDto vehicleDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elimina lógicamente un vehículo (marca como eliminado)
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifica si existe un vehículo con la placa especificada
        /// </summary>
        Task<bool> ExistsByPlateAsync(string plate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el contador total de vehículos activos
        /// </summary>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    }
}
