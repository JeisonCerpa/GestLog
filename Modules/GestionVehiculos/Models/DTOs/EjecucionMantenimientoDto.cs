using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{    /// <summary>
    /// DTO para EjecucionMantenimiento
    /// </summary>
    public class EjecucionMantenimientoDto
    {
        public int Id { get; set; }
        public string PlacaVehiculo { get; set; } = string.Empty;
        public int? PlanMantenimientoId { get; set; }
        public DateTimeOffset FechaEjecucion { get; set; }
        public long KMAlMomento { get; set; }
        public string? ObservacionesTecnico { get; set; }
        public decimal? Costo { get; set; }
        public string? RutaFactura { get; set; }
        public string? ResponsableEjecucion { get; set; }
        public string? Proveedor { get; set; }
        public int Estado { get; set; }
        public DateTimeOffset FechaRegistro { get; set; }
        public DateTimeOffset FechaActualizacion { get; set; }
    }
}
