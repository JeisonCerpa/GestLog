using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    public interface ICronogramaService
    {
        Task<IEnumerable<CronogramaMantenimientoDto>> GetAllAsync();
        Task<CronogramaMantenimientoDto?> GetByCodigoAsync(string codigo);
        Task AddAsync(CronogramaMantenimientoDto cronograma);
        Task UpdateAsync(CronogramaMantenimientoDto cronograma);
        Task DeleteAsync(string codigo);
        Task ImportarDesdeExcelAsync(string filePath);
        Task ExportarAExcelAsync(string filePath);
        Task BackupAsync();        Task<List<CronogramaMantenimientoDto>> GetCronogramasAsync();
        Task EnsureAllCronogramasUpToDateAsync();
        Task GenerarSeguimientosFaltantesAsync();
        Task<List<MantenimientoSemanaEstadoDto>> GetEstadoMantenimientosSemanaAsync(int semana, int anio);
        Task DeleteByEquipoCodigoAsync(string codigoEquipo);
    }
}
