using GestLog.Modules.GestionVehiculos.Models.Enums;
using System;

namespace GestLog.Modules.GestionVehiculos.Models.DTOs
{
    /// <summary>
    /// DTO para transferencia de datos de vehículos
    /// </summary>
    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string Plate { get; set; } = string.Empty;
        public string Vin { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? Version { get; set; }
        public int Year { get; set; }
        public string? Color { get; set; }
        public long Mileage { get; set; } = 0;
        public VehicleType Type { get; set; } = VehicleType.Particular;
        public VehicleState State { get; set; } = VehicleState.Activo;
        public string? PhotoPath { get; set; }
        public string? PhotoThumbPath { get; set; }
        public string? FuelType { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
