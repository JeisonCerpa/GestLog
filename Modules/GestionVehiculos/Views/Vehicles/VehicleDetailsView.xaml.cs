using System.Windows.Controls;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
using GestLog.Modules.GestionVehiculos.Views.Mantenimientos;
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
                        var currentPlate = (this.DataContext as VehicleDetailsViewModel)?.Plate ?? string.Empty;

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

                        var planesView = this.FindName("PlanesView") as PlanesMantenimientoView;
                        var ejecucionesView = this.FindName("EjecucionesView") as EjecucionesMantenimientoView;
                        if (planesView != null)
                        {
                            if (!(planesView.DataContext is PlanesMantenimientoViewModel))
                            {
                                var planesVm = sp?.GetService(typeof(PlanesMantenimientoViewModel)) as PlanesMantenimientoViewModel;
                                if (planesVm != null)
                                {
                                    planesView.DataContext = planesVm;
                                }
                                else
                                {
                                    logger?.LogWarning("VehicleDetailsView: No se pudo resolver PlanesMantenimientoViewModel desde DI");
                                }
                            }

                                // suscribir eventos para interacciones de la vista de planes
                                planesView.HistoryRequested += async plan =>
                                {
                                    // cambiar a la pestaña de ejecuciones y cargar datos
                                    if (this.FindName("MantenimientosTabs") is System.Windows.Controls.TabControl maintTab)
                                    {
                                        maintTab.SelectedIndex = 1; // índice de Ejecuciones
                                    }

                                    var ejecucionesView = this.FindName("EjecucionesView") as EjecucionesMantenimientoView;
                                    if (ejecucionesView != null && ejecucionesView.DataContext is EjecucionesMantenimientoViewModel ejecVm)
                                    {
                                        await ejecVm.LoadByPlanAsync(plan.Id);
                                    }
                                };

                            // cargar los planes en el viewmodel
                            if (planesView.DataContext is PlanesMantenimientoViewModel planesDataContext)
                            {
                                if (string.IsNullOrWhiteSpace(currentPlate))
                                {
                                    await planesDataContext.LoadPlanesAsync();
                                }
                                else
                                {
                                    await planesDataContext.InitializeForVehicleAsync(currentPlate);
                                }
                            }

                            if (ejecucionesView != null)
                            {
                                if (!(ejecucionesView.DataContext is EjecucionesMantenimientoViewModel))
                                {
                                    var ejecucionesVm = sp?.GetService(typeof(EjecucionesMantenimientoViewModel)) as EjecucionesMantenimientoViewModel;
                                    if (ejecucionesVm != null)
                                    {
                                        ejecucionesView.DataContext = ejecucionesVm;
                                    }
                                    else
                                    {
                                        logger?.LogWarning("VehicleDetailsView: No se pudo resolver EjecucionesMantenimientoViewModel desde DI");
                                    }
                                }

                                if (ejecucionesView.DataContext is EjecucionesMantenimientoViewModel ejecucionesDataContext)
                                {
                                    ejecucionesDataContext.FilterPlaca = currentPlate;
                                    if (string.IsNullOrWhiteSpace(currentPlate))
                                    {
                                        await ejecucionesDataContext.LoadEjecucionesAsync();
                                    }
                                    else
                                    {
                                        await ejecucionesDataContext.LoadHistorialVehiculoAsync();
                                    }
                                }
                            }
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
