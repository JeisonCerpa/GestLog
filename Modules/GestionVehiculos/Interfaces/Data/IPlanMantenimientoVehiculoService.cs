using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Data
{
    /// <summary>
    /// Interfaz para operaciones CRUD de Planes de Mantenimiento por Vehículo
    /// </summary>
    public interface IPlanMantenimientoVehiculoService
    {
        Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PlanMantenimientoVehiculoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<PlanMantenimientoVehiculoDto?> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default);
        Task<PlanMantenimientoVehiculoDto> CreateAsync(PlanMantenimientoVehiculoDto dto, CancellationToken cancellationToken = default);
        Task<PlanMantenimientoVehiculoDto> UpdateAsync(int id, PlanMantenimientoVehiculoDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Obtiene todos los planes vigentes (Activo=true, FechaFin=null o FechaFin > ahora)
        /// </summary>
        Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetVigentesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene planes vencidos o próximos a vencer
        /// </summary>
        Task<IEnumerable<PlanMantenimientoVehiculoDto>> GetVencidosAsync(CancellationToken cancellationToken = default);
    }
}
