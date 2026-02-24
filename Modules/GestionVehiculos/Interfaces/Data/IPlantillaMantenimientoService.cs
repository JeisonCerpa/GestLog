using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Data
{
    /// <summary>
    /// Interfaz para operaciones CRUD de Plantillas de Mantenimiento
    /// </summary>
    public interface IPlantillaMantenimientoService
    {
        Task<IEnumerable<PlantillaMantenimientoDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PlantillaMantenimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PlantillaMantenimientoDto> CreateAsync(PlantillaMantenimientoDto dto, CancellationToken cancellationToken = default);
        Task<PlantillaMantenimientoDto> UpdateAsync(int id, PlantillaMantenimientoDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
