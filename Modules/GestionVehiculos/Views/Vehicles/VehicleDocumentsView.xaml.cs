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
        }

        private async void BtnNewDocument_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Asegurar ViewModel (resolver desde DI si la vista heredó el DataContext y no se inicializó)
            if (Vm == null)
            {
                var sp2 = ((App)System.Windows.Application.Current).ServiceProvider;
                var resolved = sp2?.GetService(typeof(VehicleDocumentsViewModel)) as VehicleDocumentsViewModel;
                if (resolved == null)
                {
                    System.Windows.MessageBox.Show("No se pudo resolver VehicleDocumentsViewModel. Revise la configuración de DI.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }                this.DataContext = resolved;
            }

            try
            {
                var sp = ((App)System.Windows.Application.Current).ServiceProvider;

                // Resolver las dependencias del ViewModel del diálogo
                var docService = sp?.GetService(typeof(IVehicleDocumentService)) as IVehicleDocumentService;
                var photoStorage = sp?.GetService(typeof(IPhotoStorageService)) as IPhotoStorageService;
                var logger = sp?.GetService(typeof(IGestLogLogger)) as IGestLogLogger;

                if (docService == null || photoStorage == null || logger == null)
                {
                    System.Windows.MessageBox.Show("Servicios requeridos no están disponibles en DI.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                        var vehicleService = sp?.GetService(typeof(GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService)) as GestLog.Modules.GestionVehiculos.Interfaces.Data.IVehicleService;
                        if (vehicleService != null)
                        {
                            var vehicle = await vehicleService.GetByPlateAsync(plate);
                            if (vehicle != null)
                            {
                                resolvedVehicleId = vehicle.Id;
                            }
                            else
                            {
                                System.Windows.MessageBox.Show($"No se encontró vehículo con placa '{plate}'.", "Vehículo no encontrado", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                                return;
                            }
                        }
                        else
                        {
                            System.Windows.MessageBox.Show($"No se pudo resolver IVehicleService desde DI para buscar placa '{plate}'.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No se pudo determinar la placa del vehículo. Asegúrese de abrir Documentos desde la vista de Detalles del vehículo.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                }

                var dialogViewModel = new VehicleDocumentDialogModel(docService, photoStorage, logger);
                dialogViewModel.VehicleId = resolvedVehicleId;

                // Crear el diálogo y pasarle el ViewModel
                var dialog = new VehicleDocumentDialog(dialogViewModel);

                var result = dialog.ShowDialog();
                if (result == true)
                {
                    // Refresh documents después de guardar
                    if (Vm != null)
                    {
                        await Vm.InitializeAsync(dialogViewModel.VehicleId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir diálogo de documento: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                System.Windows.MessageBox.Show($"Error al descargar/abrir documento: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
