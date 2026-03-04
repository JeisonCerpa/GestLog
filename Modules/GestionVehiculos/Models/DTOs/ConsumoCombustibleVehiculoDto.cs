using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    public class ConsumoCombustibleVehiculoDto
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

        public decimal CostoPorGalon => Galones > 0 ? decimal.Round(ValorTotal / Galones, 2) : 0m;
    }
}
