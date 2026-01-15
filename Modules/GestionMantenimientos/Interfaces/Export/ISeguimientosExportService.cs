using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionMantenimientos.Interfaces.Export
{
    public interface ISeguimientosExportService
    {
        /// <summary>
        /// Exporta seguimientos a un archivo Excel en la ruta especificada.
        /// </summary>
        Task ExportAsync(IEnumerable<SeguimientoMantenimientoDto> seguimientos, int anio, string outputPath, CancellationToken ct);
    }
}
