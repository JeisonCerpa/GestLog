using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Modules.GestionVehiculos.Models.Enums;
using GestLog.Modules.GestionVehiculos.Views.Vehicles;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Vehicles
{
    public partial class VehicleDetailsViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IPlanMantenimientoVehiculoService _planMantenimientoVehiculoService;
        private readonly IAppDialogService _dialogService;
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

        [ObservableProperty]
        private string proximoMantenimientoDisplay = "Sin programar";

        // Propiedades calculadas para bindings en UI
        public string VehicleTitle => $"{Brand} {Model} {Year}".Trim();
        public string PlateDisplay => $"Placa: {Plate}";
        public string BrandModelDisplay => $"{Brand} {Model}".Trim();
        public string YearDisplay => $"Año: {Year}";

        public VehicleDetailsViewModel(
            IVehicleService vehicleService,
            IPlanMantenimientoVehiculoService planMantenimientoVehiculoService,
            IAppDialogService dialogService,
            IGestLogLogger logger)
        {
            _vehicleService = vehicleService ?? throw new ArgumentNullException(nameof(vehicleService));
            _planMantenimientoVehiculoService = planMantenimientoVehiculoService ?? throw new ArgumentNullException(nameof(planMantenimientoVehiculoService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
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

                await CargarProximoMantenimientoAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando detalles del vehículo");
                throw;
            }
        }

        private async Task CargarProximoMantenimientoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ProximoMantenimientoDisplay = "Sin programar";

                if (string.IsNullOrWhiteSpace(Plate))
                {
                    return;
                }

                var planes = await _planMantenimientoVehiculoService.GetByPlacaListAsync(Plate, cancellationToken);
                var activos = planes?.Where(p => p.Activo).ToList();

                if (activos == null || activos.Count == 0)
                {
                    return;
                }

                var porFecha = activos
                    .Where(p => p.ProximaFechaEjecucion.HasValue)
                    .OrderBy(p => p.ProximaFechaEjecucion)
                    .FirstOrDefault();

                if (porFecha?.ProximaFechaEjecucion != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(porFecha.PlantillaNombre)
                        ? "Mantenimiento"
                        : porFecha.PlantillaNombre;

                    ProximoMantenimientoDisplay = $"{porFecha.ProximaFechaEjecucion.Value:dd/MM/yyyy} · {nombre}";
                    return;
                }

                var porKm = activos
                    .Where(p => p.ProximoKMEjecucion.HasValue)
                    .OrderBy(p => p.ProximoKMEjecucion)
                    .FirstOrDefault();

                if (porKm?.ProximoKMEjecucion != null)
                {
                    var nombre = string.IsNullOrWhiteSpace(porKm.PlantillaNombre)
                        ? "Mantenimiento"
                        : porKm.PlantillaNombre;

                    ProximoMantenimientoDisplay = $"{porKm.ProximoKMEjecucion.Value:N0} km · {nombre}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo calcular el próximo mantenimiento para la placa {Plate}", Plate);
                ProximoMantenimientoDisplay = "Sin programar";
            }
        }

        [RelayCommand]
        private async Task EditarVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo para editar.";
                    return;
                }

                var current = await _vehicleService.GetByIdAsync(Id, cancellationToken);
                if (current == null)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "El vehículo ya no existe o no está disponible.";
                    return;
                }

                var dbContextFactory = ((App)System.Windows.Application.Current).ServiceProvider?
                    .GetService(typeof(IDbContextFactory<GestLogDbContext>)) as IDbContextFactory<GestLogDbContext>;

                if (dbContextFactory == null)
                {
                    throw new InvalidOperationException("IDbContextFactory no está registrado en DI");
                }

                bool? result = null;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var dialog = new VehicleFormDialog(current, dbContextFactory);
                    var owner = System.Windows.Application.Current?.MainWindow;
                    if (owner != null)
                    {
                        dialog.Owner = owner;
                    }

                    result = dialog.ShowDialog();
                });

                if (result == true)
                {
                    await LoadAsync(Id, cancellationToken);
                    HasMileageUpdateError = false;
                    MileageUpdateMessage = "Vehículo actualizado correctamente.";
                }
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al abrir el formulario de edición.";
                _logger.LogError(ex, "Error abriendo edición de vehículo desde detalle");
            }
        }

        [RelayCommand]
        private async Task EliminarVehiculoAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Id == Guid.Empty)
                {
                    HasMileageUpdateError = true;
                    MileageUpdateMessage = "No se pudo identificar el vehículo a eliminar.";
                    return;
                }

                var confirmacion1 = _dialogService.ConfirmWarning(
                    "Esta acción eliminará el vehículo (borrado lógico). ¿Deseas continuar?",
                    "Confirmar eliminación");

                if (!confirmacion1)
                {
                    return;
                }

                var confirmacion2 = _dialogService.Confirm(
                    $"Confirmación final: se eliminará el vehículo con placa '{Plate}'. ¿Eliminar ahora?",
                    "Confirmación final");

                if (!confirmacion2)
                {
                    return;
                }

                await _vehicleService.DeleteAsync(Id, cancellationToken);
                HasMileageUpdateError = false;
                MileageUpdateMessage = "Vehículo eliminado correctamente.";
            }
            catch (Exception ex)
            {
                HasMileageUpdateError = true;
                MileageUpdateMessage = "Error al eliminar el vehículo.";
                _logger.LogError(ex, "Error eliminando vehículo desde detalle");
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
