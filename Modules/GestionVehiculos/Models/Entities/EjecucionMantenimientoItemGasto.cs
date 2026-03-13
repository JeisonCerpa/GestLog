using System;
using GestLog.Modules.GestionVehiculos.Models.Enums;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    public class EjecucionMantenimientoItemGasto
    {
        public int Id { get; set; }
        public int EjecucionMantenimientoId { get; set; }
        public TipoGastoMantenimientoVehiculo TipoGasto { get; set; } = TipoGastoMantenimientoVehiculo.Otro;
        public string Descripcion { get; set; } = string.Empty;
        public string? Proveedor { get; set; }
        public decimal Valor { get; set; }
        public string? NumeroFactura { get; set; }
        public string? RutaFactura { get; set; }
        public DateTimeOffset? FechaDocumento { get; set; }
        public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; }

        public EjecucionMantenimiento? EjecucionMantenimiento { get; set; }
    }
}