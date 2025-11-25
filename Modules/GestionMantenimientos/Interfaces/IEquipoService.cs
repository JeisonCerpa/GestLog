using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    public interface IEquipoService
    {
        Task<IEnumerable<EquipoDto>> GetAllAsync();
        Task<EquipoDto?> GetByCodigoAsync(string codigo);
        /// <summary>
        /// 🚀 Obtiene solo los códigos de todos los equipos (optimizado para validación)
        /// </summary>
        Task<IEnumerable<string>> GetAllCodigosAsync();
        Task AddAsync(EquipoDto equipo);
        Task UpdateAsync(EquipoDto equipo);
        Task DeleteAsync(string codigo);
        Task ImportarDesdeExcelAsync(string filePath);
        Task ExportarAExcelAsync(string filePath);
        Task BackupAsync();
        Task<List<EquipoDto>> GetEquiposAsync();
    }
}
