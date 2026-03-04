using System;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Registro de tanqueada/consumo de combustible por vehículo.
    /// </summary>
    public class ConsumoCombustibleVehiculo
    {
        public int Id { get; set; }
        public string PlacaVehiculo { get; set; } = string.Empty;
        public DateTimeOffset FechaTanqueada { get; set; }
        public long KMAlMomento { get; set; }
        public decimal Galones { get; set; }
        public decimal ValorTotal { get; set; }
        public string? Proveedor { get; set; }
        public string? RutaFactura { get; set; }
        public string? Observaciones { get; set; }
        public DateTimeOffset FechaRegistro { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset FechaActualizacion { get; set; } = DateTimeOffset.UtcNow;
        public bool IsDeleted { get; set; }
    }
}
