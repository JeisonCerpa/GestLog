using System;

namespace GestLog.Modules.GestionVehiculos.Messages.Documents
{
    public sealed class VehicleDocumentUploadProgressMessage
    {
        public Guid VehicleId { get; }
        public double Progress { get; }
        public bool IsUploading { get; }

        public VehicleDocumentUploadProgressMessage(Guid vehicleId, double progress, bool isUploading)
        {
            VehicleId = vehicleId;
            Progress = progress;
            IsUploading = isUploading;
        }
    }
}