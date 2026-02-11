using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    /// <summary>
    /// Resultado de una operación de reemplazo de documento
    /// </summary>
    public class ReplaceDocumentResultDto
    {
        /// <summary>
        /// ID del nuevo documento creado
        /// </summary>
        public Guid NewDocumentId { get; set; }

        /// <summary>
        /// ID del documento antiguo que fue archivado (nulo si no había documento previo)
        /// </summary>
        public Guid? ArchivedDocumentId { get; set; }

        /// <summary>
        /// Ruta del archivo antiguo que debe ser movido (nulo si no había documento previo)
        /// </summary>
        public string? ArchivedFilePath { get; set; }
    }
}
