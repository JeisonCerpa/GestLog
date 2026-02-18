using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using GestLog.Modules.GestionVehiculos.ViewModels.Vehicles;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.Views.Vehicles
{
    /// <summary>
    /// Vista para la pestaña de documentos de vehículos
    /// </summary>
    public partial class VehicleDocumentsView : System.Windows.Controls.UserControl
    {
        public VehicleDocumentsView()
        {
            InitializeComponent();
            // Cuando la vista se vuelve a mostrar, re-inicializar para asegurar que la lista esté actualizada
            this.Loaded += VehicleDocumentsView_Loaded;
        }

        private VehicleDocumentsViewModel? Vm => this.DataContext as VehicleDocumentsViewModel;
        private Guid _vehicleId = Guid.Empty;

        public async Task LoadAsync(Guid vehicleId)
        {
            _vehicleId = vehicleId;
            // Si el DataContext no es un VehicleDocumentsViewModel (por ejemplo heredado del padre), resolver desde DI
            if (!(this.DataContext is VehicleDocumentsViewModel))
            {
                var sp = ((App)System.Windows.Application.Current).ServiceProvider;
                var vm = sp?.GetService(typeof(VehicleDocumentsViewModel)) as VehicleDocumentsViewModel;
                if (vm == null) throw new InvalidOperationException("No se pudo resolver VehicleDocumentsViewModel desde DI");
                this.DataContext = vm;
            }

            if (this.DataContext is VehicleDocumentsViewModel v)
            {
                await v.InitializeAsync(vehicleId);
            }
        }

        private async void VehicleDocumentsView_Loaded(object? sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Si conocíamos el vehicleId, forzar re-inicialización para refrescar lista al volver a la vista
                if (_vehicleId != Guid.Empty)
                {
                    if (this.DataContext is VehicleDocumentsViewModel vm)
                    {
                        await vm.InitializeAsync(_vehicleId);
                    }
                    else
                    {
                        // Intentar resolver desde DI si el DataContext no es un VM
                        await LoadAsync(_vehicleId);
                    }
                }
            }
            catch { }
            finally
            {
                // No desuscribir: queremos que la vista recargue cada vez que se vuelva visible.
            }
        }        private async void BtnNewDocument_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] ========== INICIO DEL EVENTO ==========");
            
            // Asegurar ViewModel (resolver desde DI si la vista heredó el DataContext y no se inicializó)
            if (Vm == null)
            {
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Vm es null, resolviendo desde DI...");
                var sp2 = ((App)System.Windows.Application.Current).ServiceProvider;
                var resolved = sp2?.GetService(typeof(VehicleDocumentsViewModel)) as VehicleDocumentsViewModel;
                if (resolved == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] ERROR: No se pudo resolver VehicleDocumentsViewModel");
                    var appDialogFallback = sp2?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService)) as GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService;
                    appDialogFallback?.ShowError("No se pudo resolver VehicleDocumentsViewModel. Revise la configuración de DI.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] VehicleDocumentsViewModel resuelto correctamente");
                this.DataContext = resolved;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Vm ya está establecido");
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Obteniendo ServiceProvider y AppDialog...");
                var sp = ((App)System.Windows.Application.Current).ServiceProvider;
                var appDialog = sp?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService)) as GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService;
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] ServiceProvider y AppDialog obtenidos");

                // Resolver solo IVehicleService aquí (se usa para buscar vehicleId por placa si es necesario).
                var vehicleService = sp?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService)) as GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService;

                if (vehicleService == null)
                {
                    appDialog?.ShowError("Servicio IVehicleService no está disponible en DI.");
                    return;
                }

                // Intentar resolver vehicleId por si no se pasó en LoadAsync
                Guid resolvedVehicleId = _vehicleId;
                if (resolvedVehicleId == Guid.Empty)
                {
                    // Buscar placa en DataContext de ancestros (por ejemplo VehicleDetailsViewModel)
                    string? plate = null;
                    try
                    {
                        var parent = this as System.Windows.DependencyObject;
                        while (parent != null)
                        {
                            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
                            if (parent is System.Windows.FrameworkElement fe && fe.DataContext != null)
                            {
                                var dc = fe.DataContext;
                                var prop = dc.GetType().GetProperty("Plate");
                                if (prop != null)
                                {
                                    plate = prop.GetValue(dc) as string;
                                    if (!string.IsNullOrWhiteSpace(plate)) break;
                                }
                            }
                        }
                    }
                    catch { /* no bloquear si VisualTree falla */ }

                    if (!string.IsNullOrWhiteSpace(plate))
                    {
                        var vehicleServiceInstance = sp?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService)) as GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService;
                        if (vehicleServiceInstance != null)
                        {
                            var vehicle = await vehicleServiceInstance.GetByPlateAsync(plate);
                            if (vehicle != null)
                            {
                                resolvedVehicleId = vehicle.Id;
                            }
                            else
                            {
                                appDialog?.ShowError($"No se encontró vehículo con placa '{plate}'.", "Vehículo no encontrado");
                                return;
                            }
                        }
                        else
                        {
                            appDialog?.ShowError($"No se pudo resolver IVehicleService desde DI para buscar placa '{plate}'.");
                            return;
                        }
                    }
                    else
                    {
                        appDialog?.ShowError("No se pudo determinar la placa del vehículo. Asegúrese de abrir Documentos desde la vista de Detalles del vehículo.");
                        return;
                    }
                }                // Intentar mostrar diálogo via DialogService (MVVM-friendly) que resolverá el VM desde DI
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Resolviendo IVehicleDocumentDialogService desde DI...");
                var dialogService = sp?.GetService(typeof(Interfaces.Dialog.IVehicleDocumentDialogService)) as Interfaces.Dialog.IVehicleDocumentDialogService;
                if (dialogService == null)
                {
                    System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] ERROR: IVehicleDocumentDialogService no está registrado");
                    appDialog?.ShowError("Servicio de diálogo no está registrado. Revise la configuración de DI.");
                    return;
                }
                System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] IVehicleDocumentDialogService obtenido correctamente");                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] ========== LLAMANDO A TryShowVehicleDocumentDialogAsync con vehicleId: {resolvedVehicleId} ==========");
                var dlgResult = await dialogService.TryShowVehicleDocumentDialogAsync(resolvedVehicleId);
                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] ========== TryShowVehicleDocumentDialogAsync RETORNÓ: dlgResult={dlgResult} ==========");

                var shown = dlgResult == true;
                if (shown)
                {
                    System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Diálogo mostró resultado=true, refrescando lista de documentos...");
                    // Refresh documents después de guardar
                    if (Vm != null)
                    {
                        System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Llamando a Vm.InitializeAsync...");
                        await Vm.InitializeAsync(resolvedVehicleId);
                        System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Vm.InitializeAsync completado");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Vm es null, no se puede refrescar");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BtnNewDocument_Click] Diálogo fue cancelado o cerrado sin guardar");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] ========== EXCEPCIÓN ==========");
                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] Mensaje: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[BtnNewDocument_Click] ========== FIN EXCEPCIÓN ==========");
                var spErr = ((App)System.Windows.Application.Current).ServiceProvider;
                var appDialogErr = spErr?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService)) as GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService;
                appDialogErr?.ShowError($"Error al abrir diálogo de documento: {ex.Message}");
            }
        }

        private async void BtnDownload_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Vm?.SelectedDocument == null) return;

            var sp = ((App)System.Windows.Application.Current).ServiceProvider;
            var photoStorage = sp?.GetService(typeof(IPhotoStorageService)) as IPhotoStorageService;
            if (photoStorage == null) return;

            try
            {
                var uri = await photoStorage.GetUriAsync(Vm.SelectedDocument.FilePath ?? string.Empty);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                var appDialogErr = sp?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService)) as GestLog.Modules.GestionVehiculos.Interfaces.Dialog.IAppDialogService;
                appDialogErr?.ShowError($"Error al descargar/abrir documento: {ex.Message}");
            }
        }
    }
}
