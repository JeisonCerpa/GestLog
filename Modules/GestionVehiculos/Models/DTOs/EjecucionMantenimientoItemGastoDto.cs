using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    public class EjecucionMantenimientoItemGastoDto
    {
        public int Id { get; set; }
        public int EjecucionMantenimientoId { get; set; }
        public int TipoGasto { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string? Proveedor { get; set; }
        public decimal Valor { get; set; }
        public string? NumeroFactura { get; set; }
        public string? RutaFactura { get; set; }
        public DateTimeOffset? FechaDocumento { get; set; }

        public int? PlanMantenimientoDestinoId { get; set; }
        public bool EsCompartidoEntrePlanes { get; set; }

        public string TipoGastoTexto => TipoGasto switch
        {
            1 => "Repuesto",
            2 => "Mano de obra",
            3 => "Servicio",
            _ => "Otro"
        };
    }
}