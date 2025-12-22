using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export
{
    /// <summary>
    /// Interfaz para el servicio de exportación de periféricos a Excel
    /// Respeta SRP: solo responsable de exportar periféricos a archivos Excel
    /// </summary>
    public interface IPerifericoExportService
    {
        /// <summary>
        /// Exporta una lista de periféricos a un archivo Excel con formato profesional
        /// </summary>
        /// <param name="rutaArchivo">Ruta completa del archivo Excel a crear</param>
        /// <param name="perifericos">Colección de periféricos a exportar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Tarea completada cuando la exportación termina</returns>
        Task ExportarPerifericosAExcelAsync(string rutaArchivo, IEnumerable<PerifericoEquipoInformaticoDto> perifericos, CancellationToken cancellationToken = default);
    }
}
