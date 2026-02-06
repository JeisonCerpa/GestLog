using CommunityToolkit.Mvvm.ComponentModel;
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
                Type = dto.Type;
                State = dto.State;
                PhotoPath = dto.PhotoPath;
                PhotoThumbPath = dto.PhotoThumbPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando detalles del vehículo");
                throw;
            }
        }
    }
}
