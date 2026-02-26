using System;
using GestLog.Modules.GestionVehiculos.Models.Enums;

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
        public int TipoMantenimiento { get; set; } = (int)Enums.TipoMantenimientoVehiculo.Preventivo;
        public string? TituloActividad { get; set; }
        public bool EsExtraordinario { get; set; }
        public int? EstadoCorrectivo { get; set; }
        /// <summary>
        /// Nombre descriptivo del plan asociado (si aplica)
        /// </summary>
        public string? PlanNombre { get; set; }
        public int Estado { get; set; }

        /// <summary>
        /// Versión fuertemente tipada del estado que facilita el enlace a controles.
        /// Cambiar esta propiedad actualiza el campo numérico subyacente.
        /// </summary>
        public EstadoEjecucion EstadoEnum
        {
            get => (EstadoEjecucion)Estado;
            set => Estado = (int)value;
        }

        public Enums.TipoMantenimientoVehiculo TipoMantenimientoEnum
        {
            get => (Enums.TipoMantenimientoVehiculo)TipoMantenimiento;
            set => TipoMantenimiento = (int)value;
        }

        public Enums.EstadoMantenimientoCorrectivoVehiculo? EstadoCorrectivoEnum
        {
            get => EstadoCorrectivo.HasValue ? (Enums.EstadoMantenimientoCorrectivoVehiculo)EstadoCorrectivo.Value : null;
            set => EstadoCorrectivo = value.HasValue ? (int)value.Value : null;
        }

        public string EstadoCorrectivoTexto => EstadoCorrectivoEnum?.ToString() ?? string.Empty;
        public string TipoMantenimientoTexto => TipoMantenimientoEnum.ToString();
        public string ExtraordinarioTexto => EsExtraordinario ? "Sí" : "No";
        public string AccionCorrectivoTexto => EstadoCorrectivoEnum switch
        {
            Enums.EstadoMantenimientoCorrectivoVehiculo.FallaReportada => "Enviar a Reparación",
            Enums.EstadoMantenimientoCorrectivoVehiculo.EnTaller => "Completar",
            Enums.EstadoMantenimientoCorrectivoVehiculo.Completado => "Detalles",
            Enums.EstadoMantenimientoCorrectivoVehiculo.Cancelado => "Detalles",
            _ => "Detalles"
        };
        public DateTimeOffset FechaRegistro { get; set; }
        public DateTimeOffset FechaActualizacion { get; set; }
    }
}
