using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using UserControl = System.Windows.Controls.UserControl;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    public partial class VehicleDetailsView : UserControl
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
        }
    }
}
