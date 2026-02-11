using System;
using GestLog.Modules.GestionVehiculos.Models.Enums;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Entidad que representa un documento de un vehículo (SOAT, Tecno-Mecánica, etc.)
    /// </summary>
    public class VehicleDocument
    {
        /// <summary>
        /// Identificador único del documento
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID del vehículo propietario del documento (Foreign Key)
        /// </summary>
        public Guid VehicleId { get; set; }

        /// <summary>
        /// Referencia a la entidad Vehicle
        /// </summary>
        public Vehicle? Vehicle { get; set; }

        /// <summary>
        /// Tipo de documento (ej: "SOAT", "Tecno-Mecánica", "Matrícula", "Revisión Técnica", etc.)
        /// </summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Número del documento (ej: número de póliza SOAT, número de certificado, etc.)
        /// </summary>
        public string? DocumentNumber { get; set; }

        /// <summary>
        /// Fecha de emisión del documento
        /// </summary>
        public DateTimeOffset IssuedDate { get; set; }

        /// <summary>
        /// Fecha de vencimiento del documento
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }

        /// <summary>
        /// Nombre del archivo almacenado (SOAT_2025.pdf, etc.)
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Ruta relativa del archivo almacenado (documentos/vehicle-id/filename)
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Observaciones o comentarios adicionales del documento
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indica si el documento está activo/vigente
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indica el estado semántico del documento (vigente, archivado, etc.)
        /// </summary>
        public DocumentStatus Status { get; set; } = DocumentStatus.Vigente;

        /// <summary>
        /// Fecha en que el documento fue archivado (si aplica)
        /// </summary>
        public DateTimeOffset? ArchivedAt { get; set; }

        /// <summary>
        /// Fecha de creación del registro en el sistema
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Fecha de última modificación
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
