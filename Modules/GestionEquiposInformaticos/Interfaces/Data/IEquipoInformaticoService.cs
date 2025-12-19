using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data
{
    public interface IEquipoInformaticoService
    {
        Task<EquipoInformaticoEntity?> GetByCodigoAsync(string codigo);
        Task<IEnumerable<EquipoInformaticoEntity>> GetAllAsync();
        Task<bool> CambiarEstadoAsync(string codigo, EstadoEquipoInformatico nuevoEstado, CancellationToken cancellationToken = default);
    }
}
