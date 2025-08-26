using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    public interface ISeguimientoService
    {
        Task<IEnumerable<SeguimientoMantenimientoDto>> GetAllAsync();
        Task<SeguimientoMantenimientoDto?> GetByCodigoAsync(string codigo);
        Task AddAsync(SeguimientoMantenimientoDto seguimiento);
        Task UpdateAsync(SeguimientoMantenimientoDto seguimiento);
        Task DeleteAsync(string codigo);
        Task ImportarDesdeExcelAsync(string filePath);
        Task ExportarAExcelAsync(string filePath);
        Task BackupAsync();
        Task<List<SeguimientoMantenimientoDto>> GetSeguimientosAsync();
        Task DeletePendientesByEquipoCodigoAsync(string codigoEquipo);
        Task ActualizarObservacionesPendientesAsync();
    }
}
