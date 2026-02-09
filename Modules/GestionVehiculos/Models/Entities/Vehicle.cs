using System;
using GestLog.Modules.GestionVehiculos.Models.Enums;

namespace GestLog.Modules.GestionVehiculos.Models.Entities
{
    /// <summary>
    /// Entidad que representa un vehículo en el sistema de gestión
    /// </summary>
    public class Vehicle
    {
        /// <summary>
        /// Identificador único del vehículo
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Placa del vehículo (UNIQUE)
        /// </summary>
        public string Plate { get; set; } = string.Empty;

        /// <summary>
        /// VIN (Identificador único del vehículo, 17 caracteres)
        /// </summary>
        public string Vin { get; set; } = string.Empty;

        /// <summary>
        /// Marca del vehículo (ej: Toyota, Ford, etc.)
        /// </summary>
        public string Brand { get; set; } = string.Empty;

        /// <summary>
        /// Modelo del vehículo (ej: Hilux, Ranger, etc.)
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Versión del vehículo (ej: 2.7 4x2, SRV, etc.) - opcional
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Año de fabricación del vehículo
        /// </summary>
        public int Year { get; set; }        /// <summary>
        /// Color del vehículo - opcional
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Kilometraje actual del vehículo en kilómetros
        /// </summary>
        public long Mileage { get; set; } = 0;

        /// <summary>
        /// Tipo de vehículo (Particular, Camión, Motocicleta, Van, Servicio)
        /// </summary>
        public VehicleType Type { get; set; } = VehicleType.Particular;

        /// <summary>
        /// Estado del vehículo (Activo, EnMantenimiento, DadoDeBaja, Inactivo)
        /// </summary>
        public VehicleState State { get; set; } = VehicleState.Activo;

        /// <summary>
        /// Ruta a la foto del vehículo - opcional
        /// </summary>
        public string? PhotoPath { get; set; }

        /// <summary>
        /// Ruta al thumbnail de la foto - opcional
        /// </summary>
        public string? PhotoThumbPath { get; set; }

        /// <summary>
        /// Tipo de combustible (Gasolina, Diésel, Eléctrico, Híbrido) - opcional
        /// </summary>
        public string? FuelType { get; set; }

        /// <summary>
        /// Fecha de creación del registro (UTC)
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Fecha de última actualización (UTC)
        /// </summary>
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Indicador de borrado lógico (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}
