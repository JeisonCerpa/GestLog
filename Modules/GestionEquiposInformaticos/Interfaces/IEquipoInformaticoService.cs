using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces
{
    public interface IEquipoInformaticoService
    {
        Task<EquipoInformaticoEntity?> GetByCodigoAsync(string codigo);
        Task<IEnumerable<EquipoInformaticoEntity>> GetAllAsync();
    }
}
