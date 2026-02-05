using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para cada tarjeta de vehículo en el grid
    /// </summary>
    public class VehicleCardViewModel : ObservableObject
    {
        public Guid VehicleId { get; set; }
        public string VehicleName { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty;
        public string MileageText { get; set; } = string.Empty;
        public string DocumentSummary { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public System.Windows.Media.Brush? BadgeBackground { get; set; }        public System.Windows.Media.Brush? BadgeForeground { get; set; }
        public ICommand? VerDetallesCommand { get; set; }
    }

    /// <summary>
    /// ViewModel principal para la vista de Gestión de Vehículos    /// </summary>
    public partial class GestionVehiculosHomeViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<VehicleCardViewModel> vehicles = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isFormDialogOpen = false;

        public GestionVehiculosHomeViewModel(IVehicleService vehicleService, IGestLogLogger logger)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Carga los vehículos desde la base de datos
        /// </summary>
        [RelayCommand]
        public async Task LoadVehiclesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                Vehicles.Clear();

                var vehiclesDto = await _vehicleService.GetAllAsync(cancellationToken);

                foreach (var vehicle in vehiclesDto)
                {
                    var cardVm = MapToCardViewModel(vehicle);
                    Vehicles.Add(cardVm);
                }

                _logger.LogInformation($"Vehículos cargados: {Vehicles.Count}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar vehículos. Intente nuevamente.";
                _logger.LogError(ex, "Error loading vehicles");
            }            finally
            {
                IsLoading = false;
            }
        }        /// <summary>
        /// Abre el formulario para agregar un nuevo vehículo
        /// </summary>        
        [RelayCommand]
        public async Task AgregarVehiculoAsync()
        {
            try
            {
                var dbContextFactory = ((App)System.Windows.Application.Current).ServiceProvider?
                    .GetService(typeof(Microsoft.EntityFrameworkCore.IDbContextFactory<GestLog.Modules.DatabaseConnection.GestLogDbContext>))
                    as Microsoft.EntityFrameworkCore.IDbContextFactory<GestLog.Modules.DatabaseConnection.GestLogDbContext>;

                if (dbContextFactory == null)
                    throw new InvalidOperationException("IDbContextFactory no está registrado en DI");

                var dialog = new Views.Vehicles.VehicleFormDialog(dbContextFactory);
                var ownerWindow = System.Windows.Application.Current?.MainWindow;

                if (ownerWindow != null)
                {
                    dialog.Owner = ownerWindow;
                }

                if (dialog.ShowDialog() == true)
                {
                    // Recargar la lista de vehículos después de guardar
                    await LoadVehiclesAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al abrir el formulario.";
                _logger.LogError(ex, "Error opening vehicle form");
            }
        }

        /// <summary>
        /// Cierra el formulario de vehículo y recarga la lista
        /// </summary>
        [RelayCommand]
        public void CloseFormDialog()
        {
            try
            {
                IsFormDialogOpen = false;
                // Recargar vehículos si se guardó uno nuevo
                LoadVehiclesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing vehicle form");
            }
        }

        /// <summary>
        /// Mapea un DTO de vehículo a ViewModel de tarjeta
        /// </summary>
        private VehicleCardViewModel MapToCardViewModel(VehicleDto vehicle)
        {            var cardVm = new VehicleCardViewModel
            {
                VehicleId = vehicle.Id,
                VehicleName = $"{vehicle.Brand} {vehicle.Model} {vehicle.Year}",
                PhotoPath = vehicle.PhotoPath ?? "/Assets/PlantillaSIMICS.png",
                MileageText = $"KM: {vehicle.Mileage:N0}",
                DocumentSummary = GetDocumentSummary(vehicle),
                VerDetallesCommand = new RelayCommand(() => VerDetallesSync(vehicle.Id))
            };

            // Establecer badge y colores según estado
            (cardVm.BadgeText, cardVm.BadgeBackground, cardVm.BadgeForeground) = GetBadgeInfo(vehicle.State);

            return cardVm;
        }

        /// <summary>
        /// Obtiene el resumen de documentos (placeholder)
        /// </summary>
        private string GetDocumentSummary(VehicleDto vehicle)
        {            // TODO: Obtener estado real de documentos desde BD
            return "SOAT: ✓ Vigente\nTecno-Mec.: ✓ Vigente";
        }

        /// <summary>
        /// Ver detalles de un vehículo (versión sincrónica)
        /// </summary>
        private void VerDetallesSync(Guid vehicleId)
        {
            try
            {
                _logger.LogInformation($"Viendo detalles del vehículo: {vehicleId}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al abrir detalles del vehículo.";
                _logger.LogError(ex, "Error viewing vehicle details");
            }
        }

        /// <summary>
        /// Obtiene información del badge según el estado del vehículo
        /// </summary>
        private (string text, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground) GetBadgeInfo(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => (
                    "VIGENTE ✓",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 236, 252, 245)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 17, 137, 56))
                ),
                VehicleState.EnMantenimiento => (
                    "EN MANTENIMIENTO",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 243, 224)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 230, 126, 34))
                ),
                VehicleState.DadoDeBaja => (
                    "DADO DE BAJA",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 237, 237, 237)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 158, 158, 158))
                ),
                VehicleState.Inactivo => (
                    "INACTIVO",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 243, 243, 243)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 112, 112, 112))
                ),
                _ => (
                    "DESCONOCIDO",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 220, 220, 220)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 100, 100, 100))
                )
            };
        }
    }
}
