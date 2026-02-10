using System;

namespace GestLog.Modules.GestionVehiculos.Messages.Documents
{
    /// <summary>
    /// Mensaje enviado cuando se crea un documento de vehículo.
    /// Contiene el VehicleId y el Id del documento creado.
    /// </summary>
    public sealed class VehicleDocumentCreatedMessage
    {
        public Guid VehicleId { get; }
        public Guid DocumentId { get; }

        public VehicleDocumentCreatedMessage(Guid vehicleId, Guid documentId)
        {
            VehicleId = vehicleId;
            DocumentId = documentId;
        }
    }
}
