using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using System.Linq;

namespace GestLog.Modules.GestionVehiculos.Services.Dialog
{
    public class VehicleDocumentDialogService : IVehicleDocumentDialogService
    {
        private readonly IServiceProvider _sp;

        public VehicleDocumentDialogService(IServiceProvider sp)
        {
            _sp = sp;
        }

        public bool TryShowVehicleDocumentDialog(VehicleDocumentDialogModel dialogModel, out bool? dialogResult)
        {
            dialogResult = null;
            var dialog = new GestLog.Modules.GestionVehiculos.Views.Vehicles.VehicleDocumentDialog(dialogModel);

            var parentWindow = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            if (parentWindow != null)
                dialog.Owner = parentWindow;

            dialogResult = dialog.ShowDialog();
            return dialogResult == true;
        }

        public bool TryShowVehicleDocumentDialog(Guid vehicleId, out bool? dialogResult)
        {
            dialogResult = null;
            // Resolver VM desde DI
            var dialogModel = _sp.GetService(typeof(VehicleDocumentDialogModel)) as VehicleDocumentDialogModel;
            if (dialogModel == null)
                return false;

            dialogModel.VehicleId = vehicleId;

            return TryShowVehicleDocumentDialog(dialogModel, out dialogResult);
        }
    }
}
