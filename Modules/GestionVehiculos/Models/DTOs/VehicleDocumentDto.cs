using System;
using System.IO;

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
        public string? DocumentNumber { get; set; }

        /// <summary>
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
        public bool IsActive { get; set; }

        /// <summary>
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

        /// <summary>
        /// Ruta/URI para previsualización (llenada por la UI/VM usando el storage service)
        /// </summary>
        public string? PreviewUri { get; set; }

        /// <summary>
        /// Indica si el archivo es una imagen soportada para previsualización inline
        /// </summary>
        public bool IsImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FileName)) return false;
                var ext = Path.GetExtension(FileName)?.ToLowerInvariant();
                return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
            }
        }

        /// <summary>
        /// Texto corto de estado para la UI (emoji + texto). Calculado en base a ExpirationDate.
        /// Ej.: "✅ Vigente", "🚫 Vencido", "⚪ Sin vencimiento".
        /// </summary>
        public string StatusText
        {
            get
            {
                // Tratar fecha por defecto (no establecida) como "sin vencimiento"
                if (ExpirationDate == default(DateTimeOffset))
                    return "⚪ Sin vencimiento";

                var today = DateTimeOffset.UtcNow.Date;
                if (ExpirationDate.Date < today)
                    return "🚫 Vencido";

                return "✅ Vigente";
            }
        }

        /// <summary>
        /// Color de fondo (hex) para el badge de estado. La UI enlaza esta cadena y WPF la convierte a Brush.
        /// </summary>
        public string StatusBackground
        {
            get
            {
                if (ExpirationDate == default(DateTimeOffset))
                    return "#F3F4F6"; // gris claro

                var today = DateTimeOffset.UtcNow.Date;
                if (ExpirationDate.Date < today)
                    return "#FFEBEE"; // rojo muy claro (vencido)

                return "#E8F5E9"; // verde claro (vigente)
            }
        }

        /// <summary>
        /// Color de primer plano (hex) para el texto/emoji del badge.
        /// </summary>
        public string StatusForeground
        {
            get
            {
                if (ExpirationDate == default(DateTimeOffset))
                    return "#6B7280"; // gris oscuro

                var today = DateTimeOffset.UtcNow.Date;
                if (ExpirationDate.Date < today)
                    return "#C0392B"; // rojo

                return "#10B981"; // verde
            }
        }

    }
}
