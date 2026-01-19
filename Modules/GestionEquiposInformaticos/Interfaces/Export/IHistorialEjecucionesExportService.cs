using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export
{
    /// <summary>
    /// Servicio para exportar el historial de ejecuciones de planes de mantenimiento a archivos Excel.
    /// Respeta SRP: responsable únicamente de la exportación a Excel.
    /// </summary>
    public interface IHistorialEjecucionesExportService
    {        /// <summary>
        /// Exporta una colección de ejecuciones de historial a un archivo Excel.
        /// </summary>
        /// <param name="filePath">Ruta completa del archivo a guardar (incluido nombre y extensión .xlsx)</param>
        /// <param name="items">Colección de ejecuciones históricas a exportar</param>
        /// <param name="cancellationToken">Token para cancelar la operación</param>
        /// <returns>Task completada cuando la exportación finalice</returns>
        /// <exception cref="ArgumentException">Si la ruta está vacía</exception>
        /// <exception cref="ArgumentNullException">Si la colección es nula</exception>
        /// <exception cref="OperationCanceledException">Si se cancela la operación</exception>
        /// <exception cref="Exception">Para otros errores de exportación</exception>
        Task ExportarHistorialAExcelAsync(
            string filePath,
            IEnumerable<EjecucionHistorialItem> items,
            CancellationToken cancellationToken = default);
    }
}
