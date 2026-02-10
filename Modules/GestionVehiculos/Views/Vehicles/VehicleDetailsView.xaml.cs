using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    public partial class VehicleDetailsView : System.Windows.Controls.UserControl
    {
        public VehicleDetailsView()
        {
            InitializeComponent();
        }

        public VehicleDetailsView(VehicleDetailsViewModel vm) : this()
        {
            this.DataContext = vm;
        }

        public async Task LoadVehicleAsync(Guid vehicleId)
        {
            if (this.DataContext is VehicleDetailsViewModel vm)
            {
                await vm.LoadAsync(vehicleId);
            }
            else
            {
                // Resolver ViewModel desde DI si no está seteado
                var sp = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var resolved = sp.GetService(typeof(VehicleDetailsViewModel)) as VehicleDetailsViewModel;
                if (resolved != null)
                {
                    this.DataContext = resolved;
                    await resolved.LoadAsync(vehicleId);
                }
                else
                {
                    throw new InvalidOperationException("No se pudo resolver VehicleDetailsViewModel desde DI");
                }
            }

            // Disparar carga de documentos si el control existe
            try
            {
                var dv = this.FindName("DocumentsView") as VehicleDocumentsView;
                if (dv != null)
                {
                    await dv.LoadAsync(vehicleId);
                }
            }
            catch
            {
                // No bloquear la carga principal si falla la carga de documentos
            }
        }
    }
}
