using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionMantenimientos.Interfaces.Export
{
    public interface ICronogramaExportService
    {
        /// <summary>
        /// Exporta cronogramas y seguimientos a un archivo Excel en la ruta especificada.
        /// </summary>
        Task ExportAsync(IEnumerable<CronogramaMantenimientoDto> cronogramas, IEnumerable<SeguimientoMantenimientoDto>? seguimientos, int anio, string realizadoPor, string outputPath, CancellationToken ct);
    }
}

