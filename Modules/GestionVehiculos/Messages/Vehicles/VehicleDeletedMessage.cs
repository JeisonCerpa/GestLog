using CommunityToolkit.Mvvm.Messaging.Messages;
using System;

namespace GestLog.Modules.GestionVehiculos.Messages.Vehicles
{
    /// <summary>
    /// Mensaje enviado cuando un vehículo es eliminado
    /// </summary>
    public class VehicleDeletedMessage : ValueChangedMessage<Guid>
    {
        public VehicleDeletedMessage(Guid vehicleId) : base(vehicleId) { }
    }
}
