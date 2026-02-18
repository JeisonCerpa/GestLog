using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Serilog;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.Services.Dialog
{
    public class VehicleDocumentDialogService : IVehicleDocumentDialogService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger _logger;

        public VehicleDocumentDialogService(IServiceProvider sp)
        {
            _sp = sp;
            _logger = Log.ForContext<VehicleDocumentDialogService>();
        }

        public bool TryShowVehicleDocumentDialog(VehicleDocumentDialogModel dialogModel, out bool? dialogResult)
        {
            dialogResult = null;
            try
            {
                _logger.Information("[TryShowVehicleDocumentDialog] Iniciando con ViewModel...");

                var dialog = new GestLog.Modules.GestionVehiculos.Views.Vehicles.VehicleDocumentDialog(dialogModel);
                _logger.Information("[TryShowVehicleDocumentDialog] Diálogo creado exitosamente");

                var parentWindow = System.Windows.Application.Current?.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current?.MainWindow;

                if (parentWindow != null)
                {
                    dialog.Owner = parentWindow;
                    _logger.Information("[TryShowVehicleDocumentDialog] Ventana padre asignada");
                }

                _logger.Information("[TryShowVehicleDocumentDialog] Mostrando diálogo modal...");
                dialogResult = dialog.ShowDialog();
                _logger.Information("[TryShowVehicleDocumentDialog] Diálogo cerrado. Resultado: {DialogResult}", dialogResult);

                return dialogResult == true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TryShowVehicleDocumentDialog] Error al mostrar diálogo");
                dialogResult = false;
                return false;
            }
        }

        public bool TryShowVehicleDocumentDialog(Guid vehicleId, out bool? dialogResult)
        {
            dialogResult = null;
            _logger.Information("[TryShowVehicleDocumentDialog] Resolviendo ViewModel desde DI para VehicleId: {VehicleId}", vehicleId);

            try
            {
                var dialogModel = _sp.GetService(typeof(VehicleDocumentDialogModel)) as VehicleDocumentDialogModel;
                if (dialogModel == null)
                {
                    _logger.Warning("[TryShowVehicleDocumentDialog] No se pudo resolver VehicleDocumentDialogModel desde DI");
                    return false;
                }

                dialogModel.VehicleId = vehicleId;
                _logger.Information("[TryShowVehicleDocumentDialog] ViewModel resuelto y configurado");

                return TryShowVehicleDocumentDialog(dialogModel, out dialogResult);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TryShowVehicleDocumentDialog] Error al resolver ViewModel");
                return false;
            }
        }        public async Task<bool?> TryShowVehicleDocumentDialogAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        {
            _logger.Information("[TryShowVehicleDocumentDialogAsync] INICIO para VehicleId: {VehicleId}", vehicleId);

            try
            {
                // Yield control al dispatcher para que pueda procesar otros eventos
                await Task.Delay(0, cancellationToken);

                _logger.Information("[TryShowVehicleDocumentDialogAsync] Resolviendo ViewModel desde DI");
                var dialogModel = _sp.GetService(typeof(VehicleDocumentDialogModel)) as VehicleDocumentDialogModel;
                if (dialogModel == null)
                {
                    _logger.Warning("[TryShowVehicleDocumentDialogAsync] No se pudo resolver VehicleDocumentDialogModel desde DI");
                    return false;
                }

                dialogModel.VehicleId = vehicleId;
                _logger.Information("[TryShowVehicleDocumentDialogAsync] ViewModel configurado");

                _logger.Information("[TryShowVehicleDocumentDialogAsync] Creando diálogo");
                var dialog = new GestLog.Modules.GestionVehiculos.Views.Vehicles.VehicleDocumentDialog(dialogModel);

                var parentWindow = System.Windows.Application.Current?.Windows
                    .OfType<System.Windows.Window>()
                    .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current?.MainWindow;

                if (parentWindow != null)
                {
                    dialog.Owner = parentWindow;
                    _logger.Information("[TryShowVehicleDocumentDialogAsync] Ventana padre asignada");
                }

                _logger.Information("[TryShowVehicleDocumentDialogAsync] Mostrando diálogo modal");
                var result = dialog.ShowDialog();
                _logger.Information("[TryShowVehicleDocumentDialogAsync] Diálogo cerrado. Resultado: {DialogResult}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TryShowVehicleDocumentDialogAsync] Error al mostrar diálogo");
                return false;
            }
        }
    }
}
