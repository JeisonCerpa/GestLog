using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    public partial class VehicleDetailsViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private Guid id;

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
        private int year;

        [ObservableProperty]
        private string? color;

        [ObservableProperty]
        private long mileage;

        [ObservableProperty]
        private VehicleType type;

        [ObservableProperty]
        private VehicleState state;

        [ObservableProperty]
        private string? photoPath;

        [ObservableProperty]
        private string? photoThumbPath;

        [ObservableProperty]
        private string? fuelType;

        [ObservableProperty]
        private string nuevoKilometrajeInput = string.Empty;

        [ObservableProperty]
        private string mileageUpdateMessage = string.Empty;

        [ObservableProperty]
        private bool hasMileageUpdateError = false;

        // Propiedades calculadas para bindings en UI
        public string VehicleTitle => $"{Brand} {Model} {Year}".Trim();
        public string PlateDisplay => $"Placa: {Plate}";
        public string BrandModelDisplay => $"{Brand} {Model}".Trim();
        public string YearDisplay => $"Año: {Year}";

        public VehicleDetailsViewModel(IVehicleService vehicleService, IGestLogLogger logger)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task LoadAsync(Guid vehicleId, CancellationToken cancellationToken = default)
        {
            Id = vehicleId;

            try
            {
                var dto = await _vehicleService.GetByIdAsync(vehicleId, cancellationToken);
                if (dto == null)
                {
                    throw new InvalidOperationException("Vehículo no encontrado");
                }

                Plate = dto.Plate ?? string.Empty;
                Vin = dto.Vin ?? string.Empty;
                Brand = dto.Brand ?? string.Empty;
                Model = dto.Model ?? string.Empty;
                Version = dto.Version;
                Year = dto.Year;
                Color = dto.Color;
                Mileage = dto.Mileage;
                NuevoKilometrajeInput = dto.Mileage.ToString();
                Type = dto.Type;
                State = dto.State;
                PhotoPath = dto.PhotoPath;
                PhotoThumbPath = dto.PhotoThumbPath;

                // FuelType aún no está en la DTO/BD; dejar vacío por defecto (se muestra 'No especificado' en UI)
                FuelType = string.Empty;

                // Notificar que las propiedades calculadas han cambiado
                OnPropertyChanged(nameof(VehicleTitle));
                OnPropertyChanged(nameof(PlateDisplay));
                OnPropertyChanged(nameof(BrandModelDisplay));
                OnPropertyChanged(nameof(YearDisplay));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando detalles del vehículo");
                throw;
            }
        }

        [RelayCommand]
        public async Task ActualizarKilometrajeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                MileageUpdateMessage = string.Empty;
                HasMileageUpdateError = false;

                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo para actualizar kilometraje";
                    return;
                }

                if (!long.TryParse(NuevoKilometrajeInput?.Trim(), out var nuevoKilometraje) || nuevoKilometraje < 0)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "Ingrese un kilometraje válido (0 o mayor)";
                    return;
                }

                if (nuevoKilometraje < Mileage)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = $"El nuevo kilometraje ({nuevoKilometraje:N0}) no puede ser menor al actual ({Mileage:N0})";
                    return;
                }

                if (nuevoKilometraje == Mileage)
                {
                    MileageUpdateMessage = "El kilometraje ya está actualizado";
                    return;
                }

                var vehicle = await _vehicleService.GetByIdAsync(Id, cancellationToken);
                if (vehicle == null)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "Vehículo no encontrado";
                    return;
                }

                vehicle.Mileage = nuevoKilometraje;
                await _vehicleService.UpdateAsync(Id, vehicle, cancellationToken);

                Mileage = nuevoKilometraje;
                NuevoKilometrajeInput = nuevoKilometraje.ToString();
                MileageUpdateMessage = "Kilometraje actualizado correctamente";
            }
            catch (OperationCanceledException)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Operación cancelada";
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al actualizar el kilometraje";
                _logger.LogError(ex, "Error actualizando kilometraje del vehículo en detalle");
            }
        }
    }
}
