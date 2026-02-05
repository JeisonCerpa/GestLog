using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    /// <summary>
    /// ViewModel para el formulario de agregar/editar vehículos
    /// </summary>
    public partial class VehicleFormViewModel : ObservableObject
    {
    private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private string tituloDialog = "Agregar Vehículo";

        [ObservableProperty]
        private string textoBotonPrincipal = "Guardar";

        [ObservableProperty]
        private string plate = string.Empty;

        [ObservableProperty]
        private string vin = string.Empty;

        [ObservableProperty]
        private string brand = string.Empty;

        [ObservableProperty]
        private string model = string.Empty;

        [ObservableProperty]
        private string? version;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        [ObservableProperty]
        private string? color;

        [ObservableProperty]
        private long mileage = 0;

        [ObservableProperty]
        private VehicleType selectedType = VehicleType.Particular;

        [ObservableProperty]
        private VehicleState selectedState = VehicleState.Activo;

        [ObservableProperty]
        private string? photoPath;

        [ObservableProperty]
        private string? photoThumbPath;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private bool isEditing = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        public ObservableCollection<VehicleType> VehicleTypes { get; }
        public ObservableCollection<VehicleState> VehicleStates { get; }

        public VehicleFormViewModel(IVehicleService vehicleService, IGestLogLogger logger)
        {
            _vehicleService = vehicleService;
            _logger = logger;

            // Cargar tipos de vehículos y estados
            VehicleTypes = new ObservableCollection<VehicleType>(
                Enum.GetValues(typeof(VehicleType)) as VehicleType[] ?? Array.Empty<VehicleType>());

            VehicleStates = new ObservableCollection<VehicleState>(
                Enum.GetValues(typeof(VehicleState)) as VehicleState[] ?? Array.Empty<VehicleState>());
        }

        [RelayCommand]
        private async Task SaveAsync(CancellationToken cancellationToken)
        {
            try
            {
                IsProcessing = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(Plate))
                {
                    ErrorMessage = "La placa del vehículo es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Vin))
                {
                    ErrorMessage = "El VIN es obligatorio";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Brand))
                {
                    ErrorMessage = "La marca del vehículo es obligatoria";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Model))
                {
                    ErrorMessage = "El modelo del vehículo es obligatorio";
                    return;
                }

                // Si es nuevo vehículo, validar que la placa no exista
                if (!IsEditing)
                {
                    var existingVehicle = await _vehicleService.ExistsByPlateAsync(Plate.Trim(), cancellationToken);
                    if (existingVehicle)
                    {
                        ErrorMessage = $"Ya existe un vehículo con la placa '{Plate}'";
                        return;
                    }
                }

                // Crear DTO
                var vehicleDto = new VehicleDto
                {
                    Id = Guid.NewGuid(),
                    Plate = Plate.Trim().ToUpper(),
                    Vin = Vin.Trim().ToUpper(),
                    Brand = Brand.Trim(),
                    Model = Model.Trim(),
                    Version = Version?.Trim(),
                    Year = Year,
                    Color = Color?.Trim(),
                    Mileage = Mileage,
                    Type = SelectedType,
                    State = SelectedState,
                    PhotoPath = PhotoPath,
                    PhotoThumbPath = PhotoThumbPath,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                // Guardar en BD
                var savedVehicle = await _vehicleService.CreateAsync(vehicleDto, cancellationToken);

                SuccessMessage = $"Vehículo '{savedVehicle.Brand} {savedVehicle.Model}' registrado exitosamente";
                _logger.LogInformation($"Vehículo creado: {savedVehicle.Plate} - {savedVehicle.Brand} {savedVehicle.Model} | Kilometraje: {savedVehicle.Mileage}");

                // Mostrar mensaje de éxito y cerrar
                await Task.Delay(1500);
                
                // Establecer DialogResult = true antes de cerrar para que HomeViewModel sepa que fue exitoso
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (System.Windows.Application.Current.Windows.Cast<Window>()
                        .FirstOrDefault(w => w.DataContext == this) is VehicleFormDialog dialog)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando vehículo");
                ErrorMessage = "Error al guardar el vehículo. Verifique los datos e intente nuevamente.";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// Configura el ViewModel para crear un nuevo vehículo
        /// </summary>
        public void ConfigureForNew()
        {
            TituloDialog = "Agregar Vehículo";
            TextoBotonPrincipal = "Guardar";
            IsEditing = false;
            ClearForm();
        }

        /// <summary>
        /// Configura el ViewModel para editar un vehículo existente
        /// </summary>
        public void ConfigureForEdit(VehicleDto vehicle)
        {
            Plate = vehicle.Plate ?? string.Empty;
            Vin = vehicle.Vin ?? string.Empty;
            Brand = vehicle.Brand ?? string.Empty;
            Model = vehicle.Model ?? string.Empty;
            Version = vehicle.Version;
            Year = vehicle.Year;
            Color = vehicle.Color;
            Mileage = vehicle.Mileage;
            SelectedType = vehicle.Type;
            SelectedState = vehicle.State;
            PhotoPath = vehicle.PhotoPath;
            PhotoThumbPath = vehicle.PhotoThumbPath;

            TituloDialog = "Editar Vehículo";
            TextoBotonPrincipal = "Actualizar";
            IsEditing = true;
        }

        private void ClearForm()
        {
            Plate = string.Empty;
            Vin = string.Empty;
            Brand = string.Empty;
            Model = string.Empty;
            Version = null;
            Year = DateTime.Now.Year;
            Color = null;
            Mileage = 0;
            SelectedType = VehicleType.Particular;
            SelectedState = VehicleState.Activo;
            PhotoPath = null;
            PhotoThumbPath = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }
    }
}
