using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Interfaces.Export
{
    /// <summary>
    /// Interfaz para exportar equipos a Excel.
    /// Maneja la exportación de todos los equipos o equipos filtrados.
    /// </summary>
    public interface IEquiposExportService
    {
        /// <summary>
        /// Exporta todos los equipos a un archivo Excel.
        /// </summary>
        /// <param name="equipos">Colección de equipos a exportar</param>
        /// <param name="filePath">Ruta donde guardar el archivo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Tarea asincrónica</returns>
        Task ExportarEquiposAsync(
            IEnumerable<EquipoDto> equipos,
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exporta equipos filtrados a un archivo Excel.
        /// </summary>
        /// <param name="equipos">Colección de equipos filtrados a exportar</param>
        /// <param name="filePath">Ruta donde guardar el archivo</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Tarea asincrónica</returns>
        Task ExportarEquiposFiltradosAsync(
            IEnumerable<EquipoDto> equipos,
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
