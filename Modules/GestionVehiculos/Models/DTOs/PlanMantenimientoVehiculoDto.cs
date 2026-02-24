using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{    /// <summary>
    /// DTO para PlanMantenimientoVehiculo
    /// </summary>
    public class PlanMantenimientoVehiculoDto
    {
        public int Id { get; set; }
        public string PlacaVehiculo { get; set; } = string.Empty;
        public int PlantillaId { get; set; }
        public int? IntervaloKMPersonalizado { get; set; }
        public int? IntervaloDiasPersonalizado { get; set; }
        public DateTimeOffset FechaInicio { get; set; }
        public long? UltimoKMRegistrado { get; set; }
        public DateTimeOffset? UltimaFechaMantenimiento { get; set; }
        public DateTimeOffset? FechaFin { get; set; }
        public bool Activo { get; set; }
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset FechaActualizacion { get; set; }

        /// <summary>
        /// Propiedades calculadas (calculadas en Service o ViewModel)
        /// </summary>
        public DateTimeOffset? ProximaFechaEjecucion { get; set; }
        public long? ProximoKMEjecucion { get; set; }

        /// <summary>
        /// Estado calculado del plan (Vigente, Próximo, Vencido, Sin datos)
        /// </summary>
        public string? EstadoPlan { get; set; }

        /// <summary>
        /// Detalle del estado calculado (ventana por fecha/km)
        /// </summary>
        public string? EstadoPlanDetalle { get; set; }
    }
}
