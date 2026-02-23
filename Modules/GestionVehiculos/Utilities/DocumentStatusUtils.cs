using GestLog.Modules.GestionVehiculos.Models.Enums;
using System.Windows.Media;

namespace GestLog.Modules.GestionVehiculos.Utilities
{
    /// <summary>
    /// Utilidades para conversiones de estado de documentos a colores y textos
    /// Sigue la paleta corporativa de GestLog
    /// </summary>
    public static class DocumentStatusUtils
    {
        // Colores corporativos
        private const string GreenColor = "#10B981";              // Vigente
        private const string RedColor = "#C0392B";                // Vencido
        private const string GrayColor = "#9E9E9E";               // Sin vencimiento / Archivado

        /// <summary>
        /// Obtiene el color correspondiente al estado del documento
        /// </summary>
        public static System.Windows.Media.Brush GetColorForStatus(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Vigente => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(GreenColor)),
                DocumentStatus.Vencido => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(RedColor)),
                DocumentStatus.SinVencimiento => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(GreenColor)),
                DocumentStatus.Archivado => new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(GrayColor)),
                _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }

        /// <summary>
        /// Obtiene el color en formato string hex
        /// </summary>
        public static string GetColorHexForStatus(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Vigente => GreenColor,
                DocumentStatus.Vencido => RedColor,
                DocumentStatus.SinVencimiento => GreenColor,
                DocumentStatus.Archivado => GrayColor,
                _ => "#999999"
            };
        }

        /// <summary>
        /// Obtiene el texto legible del estado
        /// </summary>
        public static string GetDisplayText(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Vigente => "Vigente",
                DocumentStatus.Vencido => "Vencido",
                DocumentStatus.SinVencimiento => "Sin vencimiento",
                DocumentStatus.Archivado => "Archivado",
                _ => "Desconocido"
            };
        }

        /// <summary>
        /// Obtiene un icono/emoji representativo del estado
        /// </summary>
        public static string GetIconForStatus(DocumentStatus status)
        {
            return status switch
            {
                DocumentStatus.Vigente => "✓",
                DocumentStatus.Vencido => "✗",
                DocumentStatus.SinVencimiento => "∞",
                DocumentStatus.Archivado => "📦",
                _ => "?"
            };
        }

        /// <summary>
        /// Indica si el documento está en situación crítica (vencido)
        /// </summary>
        public static bool IsCritical(DocumentStatus status)
        {
            return status == DocumentStatus.Vencido;
        }
    }
}
