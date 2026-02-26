using System;
using GestLog.Modules.GestionVehiculos.Models.Enums;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Registro de ejecución de un mantenimiento realizado en un vehículo
    /// </summary>
    public class EjecucionMantenimiento
    {
        /// <summary>
        /// Identificador único de la ejecución
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Placa del vehículo donde se realizó el mantenimiento
        /// </summary>
        public string PlacaVehiculo { get; set; } = string.Empty;

        /// <summary>
        /// FK a PlanMantenimientoVehiculo (null si es mantenimiento correctivo)
        /// </summary>
        public int? PlanMantenimientoId { get; set; }

        /// <summary>
        /// Fecha en que se realizó el mantenimiento
        /// </summary>
        public DateTimeOffset FechaEjecucion { get; set; }

        /// <summary>
        /// Kilometraje del vehículo al momento de la ejecución
        /// </summary>
        public long KMAlMomento { get; set; }

        /// <summary>
        /// Observaciones técnicas del mantenimiento realizado
        /// </summary>
        public string? ObservacionesTecnico { get; set; }

        /// <summary>
        /// Costo total de la intervención
        /// </summary>
        public decimal? Costo { get; set; }

        /// <summary>
        /// Ruta del archivo de factura o comprobante
        /// </summary>
        public string? RutaFactura { get; set; }

        /// <summary>
        /// Nombre del técnico o persona responsable que ejecutó el mantenimiento
        /// </summary>
        public string? ResponsableEjecucion { get; set; }

        /// <summary>
        /// Taller o proveedor que realizó el mantenimiento
        /// </summary>
        public string? Proveedor { get; set; }

        /// <summary>
        /// Tipo de mantenimiento: preventivo o correctivo.
        /// </summary>
        public TipoMantenimientoVehiculo TipoMantenimiento { get; set; } = TipoMantenimientoVehiculo.Preventivo;

        /// <summary>
        /// Título libre de la actividad (útil para correctivos no planificados).
        /// </summary>
        public string? TituloActividad { get; set; }

        /// <summary>
        /// Marca si una ejecución preventiva fue extraordinaria.
        /// </summary>
        public bool EsExtraordinario { get; set; }

        /// <summary>
        /// Estado del flujo correctivo (aplica cuando TipoMantenimiento = Correctivo).
        /// </summary>
        public EstadoMantenimientoCorrectivoVehiculo? EstadoCorrectivo { get; set; }

        /// <summary>
        /// Estado de la ejecución: Pendiente, Completado, Cancelado
        /// </summary>
        public EstadoEjecucion Estado { get; set; } = EstadoEjecucion.Pendiente;

        /// <summary>
        /// Fecha de registro en el sistema (auditoría)
        /// </summary>
        public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;

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
