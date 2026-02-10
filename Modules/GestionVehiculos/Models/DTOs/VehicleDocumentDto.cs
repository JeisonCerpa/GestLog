using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    /// <summary>
    /// DTO para transferencia de datos de documentos de vehículos
    /// </summary>
    public class VehicleDocumentDto
    {
        /// <summary>
        /// Identificador único del documento
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID del vehículo propietario
        /// </summary>
        public Guid VehicleId { get; set; }

        /// <summary>
        /// Tipo de documento (SOAT, Tecno-Mecánica, etc.)
        /// </summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Número del documento
        /// </summary>
        public string? DocumentNumber { get; set; }        /// <summary>
        /// Fecha de emisión
        /// </summary>
        public DateTimeOffset IssuedDate { get; set; }

        /// <summary>
        /// Fecha de vencimiento
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }

        /// <summary>
        /// Nombre del archivo
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Ruta del archivo
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Observaciones
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Indica si el documento está activo
        /// </summary>
        public bool IsActive { get; set; }        /// <summary>
        /// Indica si el documento está vencido (calculado)
        /// </summary>
        public bool IsExpired => DateTimeOffset.UtcNow > ExpirationDate;

        /// <summary>
        /// Días restantes para vencer (negativo si ya está vencido)
        /// </summary>
        public int DaysUntilExpiration => (int)(ExpirationDate - DateTimeOffset.UtcNow).TotalDays;

        /// <summary>
        /// Fecha de creación
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Fecha de última modificación
        /// </summary>
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
