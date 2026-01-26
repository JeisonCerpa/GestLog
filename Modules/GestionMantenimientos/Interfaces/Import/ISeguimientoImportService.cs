namespace GestLog.Modules.GestionMantenimientos.Interfaces.Import
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using GestLog.Modules.GestionMantenimientos.Models.Import;

    public interface ISeguimientoImportService
    {
        /// <summary>
        /// Importa seguimientos desde un archivo Excel.
        /// </summary>
        Task<SeguimientoImportResult> ImportAsync(string filePath, CancellationToken cancellationToken = default, IProgress<int>? progress = null);
    }
}