using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces
{
    public interface IEquipoInformaticoService
    {
        Task<EquipoInformaticoEntity?> GetByCodigoAsync(string codigo);
    }
}
