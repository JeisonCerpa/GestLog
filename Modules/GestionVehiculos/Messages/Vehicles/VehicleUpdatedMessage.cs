using CommunityToolkit.Mvvm.Messaging.Messages;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Messages.Vehicles
{
    /// <summary>
    /// Mensaje enviado cuando un vehículo es actualizado
    /// </summary>
    public class VehicleUpdatedMessage : ValueChangedMessage<VehicleDto>
    {
        public VehicleUpdatedMessage(VehicleDto vehicleDto) : base(vehicleDto) { }
    }
}
