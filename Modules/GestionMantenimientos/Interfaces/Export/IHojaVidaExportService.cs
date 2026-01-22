using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Interfaces.Export
{
    /// <summary>
    /// Interfaz para exportar la "Hoja de Vida" completa de un equipo a Excel.
    /// Incluye información general del equipo e historial de mantenimientos realizados.
    /// </summary>
    public interface IHojaVidaExportService
    {
        /// <summary>
        /// Exporta la hoja de vida del equipo a un archivo Excel con formato profesional.
        /// </summary>
        /// <param name="equipo">Datos del equipo a exportar</param>
        /// <param name="mantenimientos">Historial de mantenimientos realizados</param>
        /// <param name="filePath">Ruta donde guardar el archivo Excel</param>
        /// <returns>Tarea asincrónica</returns>
        Task ExportarHojaVidaAsync(
            EquipoDto equipo,
            List<SeguimientoMantenimientoDto> mantenimientos,
            string filePath);
    }
}
