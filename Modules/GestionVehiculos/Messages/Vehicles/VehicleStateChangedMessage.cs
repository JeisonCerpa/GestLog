using CommunityToolkit.Mvvm.Messaging.Messages;
using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Messages.Vehicles
{
    /// <summary>
    /// Mensaje enviado cuando el estado de un vehículo cambia
    /// </summary>
    public class VehicleStateChangedMessage : ValueChangedMessage<VehicleDto>
    {
        public VehicleStateChangedMessage(VehicleDto vehicleDto) : base(vehicleDto) { }
    }
}
