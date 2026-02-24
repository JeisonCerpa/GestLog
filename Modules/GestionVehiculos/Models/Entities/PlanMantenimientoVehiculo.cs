using System;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Plan de mantenimiento asignado a un vehículo específico
    /// </summary>
    public class PlanMantenimientoVehiculo
    {
        /// <summary>

    /// <summary>
    /// Último kilometraje registrado para calcular el siguiente mantenimiento
    /// </summary>
    public long? UltimoKMRegistrado { get; set; }

    /// <summary>
    /// Fecha del último mantenimiento registrado para calcular próximo vencimiento por tiempo
    /// </summary>
    public DateTimeOffset? UltimaFechaMantenimiento { get; set; }
        /// Identificador único del plan
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Placa del vehículo (identificador único, sin redundancia)
        /// </summary>
        public string PlacaVehiculo { get; set; } = string.Empty;

        /// <summary>
        /// FK a PlantillaMantenimiento
        /// </summary>
        public int PlantillaId { get; set; }

        /// <summary>
        /// Intervalo en km personalizado (si se quiere diferente al de la plantilla)
        /// </summary>
        public int? IntervaloKMPersonalizado { get; set; }

        /// <summary>
        /// Intervalo en días personalizado (si se quiere diferente al de la plantilla)
        /// </summary>
        public int? IntervaloDiasPersonalizado { get; set; }        /// <summary>
        /// Fecha cuando comenzó a regir este plan
        /// </summary>
        public DateTimeOffset FechaInicio { get; set; }

        /// <summary>
        /// Fecha cuando termina este plan (null si está vigente)
        /// </summary>
        public DateTimeOffset? FechaFin { get; set; }

        /// <summary>
        /// Indica si el plan está activo
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
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

        /// <summary>
        /// PROPIEDAD CALCULADA - No persistida en BD
        /// Próxima fecha estimada de ejecución basada en el último mantenimiento y el intervalo
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime? ProximaFechaEjecucion { get; set; }

        /// <summary>
        /// PROPIEDAD CALCULADA - No persistida en BD
        /// Próximo kilómetro estimado de ejecución basado en el último mantenimiento y el intervalo
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public long? ProximoKMEjecucion { get; set; }
    }
}
