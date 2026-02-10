using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System;
using GestLog.Services.Core.Logging;

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
        }        public async Task LoadVehicleAsync(Guid vehicleId)
        {
            var logger = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider()?.GetService(typeof(IGestLogLogger)) as IGestLogLogger;
            var sp = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();

            try
            {
                // 1. Cargar VehicleDetailsViewModel
                if (this.DataContext is VehicleDetailsViewModel vm)
                {
                    await vm.LoadAsync(vehicleId);
                }
                else
                {
                    var resolved = sp?.GetService(typeof(VehicleDetailsViewModel)) as VehicleDetailsViewModel;
                    if (resolved != null)
                    {
                        this.DataContext = resolved;
                        await resolved.LoadAsync(vehicleId);
                    }
                    else
                    {
                        throw new InvalidOperationException("No se pudo resolver VehicleDetailsViewModel desde DI");
                    }
                }                // 2. Cargar VehicleDocumentsView: usar Dispatcher.BeginInvoke para asegurar que el layout se ha completado
                System.Windows.Application.Current?.Dispatcher?.BeginInvoke(new System.Func<System.Threading.Tasks.Task>(async () =>
                {
                    try
                    {
                        var dv = this.FindName("DocumentsView") as VehicleDocumentsView;
                        if (dv != null)
                        {
                            // Asignar DataContext si no lo tiene
                            if (!(dv.DataContext is VehicleDocumentsViewModel))
                            {
                                var docVm = sp?.GetService(typeof(VehicleDocumentsViewModel)) as VehicleDocumentsViewModel;
                                if (docVm != null)
                                {
                                    dv.DataContext = docVm;
                                }
                                else
                                {
                                    logger?.LogWarning("VehicleDetailsView: [Dispatcher] No se pudo resolver VehicleDocumentsViewModel desde DI");
                                    return;
                                }
                            }

                            // Ahora cargar documentos
                            await dv.LoadAsync(vehicleId);
                        }
                        else
                        {
                            logger?.LogWarning("VehicleDetailsView: [Dispatcher] DocumentsView NO encontrada por FindName. Buscando en VisualTree...");
                        }
                    }
                    catch (System.Exception dispatchEx)
                    {
                        logger?.LogError(dispatchEx, "VehicleDetailsView: [Dispatcher] Error en carga de DocumentsView");
                    }                }), System.Windows.Threading.DispatcherPriority.Background);
                
                // logger?.LogInformation("VehicleDetailsView: Dispatcher.BeginInvoke encolado para cargar DocumentsView");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "VehicleDetailsView: Error al cargar vehículo o documentos");
            }
        }
    }
}
