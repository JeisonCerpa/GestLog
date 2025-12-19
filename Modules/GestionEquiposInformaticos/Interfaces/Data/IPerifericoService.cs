using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data
{
    public interface IPerifericoService
    {
        Task<PerifericoEquipoInformaticoEntity?> GetByCodigoAsync(string codigo);
        Task<IEnumerable<PerifericoEquipoInformaticoEntity>> GetAllAsync();
        Task<bool> CambiarEstadoAsync(string codigo, EstadoPeriferico nuevoEstado, CancellationToken cancellationToken = default);
    }
}
