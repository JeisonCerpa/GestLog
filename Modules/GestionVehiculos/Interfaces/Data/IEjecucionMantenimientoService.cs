using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Data
{
    /// <summary>
    /// Interfaz para operaciones CRUD de Ejecuciones de Mantenimiento
    /// </summary>
    public interface IEjecucionMantenimientoService
    {
        Task<IEnumerable<EjecucionMantenimientoDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<EjecucionMantenimientoDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<EjecucionMantenimientoDto>> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default);
        Task<IEnumerable<EjecucionMantenimientoDto>> GetByPlanAsync(int planId, CancellationToken cancellationToken = default);
        Task<EjecucionMantenimientoDto> CreateAsync(EjecucionMantenimientoDto dto, CancellationToken cancellationToken = default);
        Task<EjecucionMantenimientoDto> UpdateAsync(int id, EjecucionMantenimientoDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la última ejecución de un plan específico
        /// </summary>
        Task<EjecucionMantenimientoDto?> GetUltimaEjecucionAsync(int planId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene el historial de ejecuciones para un vehículo
        /// </summary>
        Task<IEnumerable<EjecucionMantenimientoDto>> GetHistorialVehiculoAsync(string placaVehiculo, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene ejecuciones por placa y tipo de mantenimiento.
        /// </summary>
        Task<IEnumerable<EjecucionMantenimientoDto>> GetByPlacaAndTipoAsync(string placaVehiculo, int tipoMantenimiento, CancellationToken cancellationToken = default);
    }
}
