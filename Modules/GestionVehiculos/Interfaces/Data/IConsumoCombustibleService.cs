using GestLog.Modules.GestionVehiculos.Models.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Data
{
    public interface IConsumoCombustibleService
    {
        Task<IReadOnlyCollection<ConsumoCombustibleVehiculoDto>> GetByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default);
        Task<ConsumoCombustibleVehiculoDto> CreateAsync(ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default);
        Task<ConsumoCombustibleVehiculoDto> UpdateAsync(int id, ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<ResumenConsumoCombustibleDto> GetResumenByPlacaAsync(string placaVehiculo, CancellationToken cancellationToken = default);
    }
}
