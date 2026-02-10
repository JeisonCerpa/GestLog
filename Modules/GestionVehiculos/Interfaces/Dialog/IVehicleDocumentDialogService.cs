using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;

namespace GestLog.Modules.GestionVehiculos.Interfaces.Dialog
{
    public interface IVehicleDocumentDialogService
    {
        /// <summary>
        /// Muestra el diálogo de documento de vehículo con el ViewModel proporcionado.
        /// Retorna en out el resultado de ShowDialog() (true = aceptado, false/null = cancelado).
        /// </summary>
        bool TryShowVehicleDocumentDialog(VehicleDocumentDialogModel dialogModel, out bool? dialogResult);

        /// <summary>
        /// Crea el ViewModel vía DI, inicializa VehicleId y muestra el diálogo. Simplifica la llamada desde VMs.
        /// </summary>
        bool TryShowVehicleDocumentDialog(Guid vehicleId, out bool? dialogResult);
    }
}
