using System;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Plantilla de mantenimiento reutilizable para múltiples vehículos
    /// </summary>
    public class PlantillaMantenimiento
    {
        /// <summary>
        /// Identificador único de la plantilla
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nombre descriptivo (ej: "Cambio de aceite", "Inspección general")
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción detallada del mantenimiento
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Intervalo en kilómetros (0 si no aplica)
        /// </summary>
        public int IntervaloKM { get; set; } = 0;

        /// <summary>
        /// Intervalo en días (0 si no aplica)
        /// </summary>
        public int IntervaloDias { get; set; } = 0;

        /// <summary>
        /// Tipo de intervalo: Primero (lo que ocurra antes), Ambos, Ultimo (lo que ocurra después)
        /// </summary>
        public int TipoIntervalo { get; set; } = 1; // Primero por defecto

        /// <summary>
        /// Indica si la plantilla está activa
        /// </summary>
        public bool Activo { get; set; } = true;        /// <summary>
        /// Fecha de creación del registro
        /// </summary>
        public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Fecha de última actualización
        /// </summary>
        public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Indicador de borrado lógico
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
