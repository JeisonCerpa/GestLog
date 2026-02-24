using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    /// <summary>
    /// DTO para PlantillaMantenimiento
    /// </summary>
    public class PlantillaMantenimientoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int IntervaloKM { get; set; }
        public int IntervaloDias { get; set; }
        public int TipoIntervalo { get; set; }
        public bool Activo { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset FechaActualizacion { get; set; }
    }
}
