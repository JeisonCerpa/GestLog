using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Combustible
{
    public partial class ConsumoCombustibleViewModel : ObservableObject
    {
        private readonly IConsumoCombustibleService _consumoService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private string filterPlaca = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ConsumoCombustibleVehiculoDto> registros = new();

        [ObservableProperty]
        private ConsumoCombustibleVehiculoDto? selectedRegistro;

        [ObservableProperty]
        private decimal totalGalones;

        [ObservableProperty]
        private decimal totalCosto;

        [ObservableProperty]
        private decimal promedioCostoPorGalon;

        [ObservableProperty]
        private int totalRegistros;

        public ConsumoCombustibleViewModel(
            IConsumoCombustibleService consumoService,
            IGestLogLogger logger)
        {
            _consumoService = consumoService ?? throw new ArgumentNullException(nameof(consumoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeForVehicleAsync(string placaVehiculo, CancellationToken cancellationToken = default)
        {
            FilterPlaca = placaVehiculo?.Trim().ToUpperInvariant() ?? string.Empty;
            await LoadAsync(cancellationToken);
        }

        [RelayCommand]
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(FilterPlaca))
            {
                Registros.Clear();
                TotalRegistros = 0;
                TotalGalones = 0;
                TotalCosto = 0;
                PromedioCostoPorGalon = 0;
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                var items = await _consumoService.GetByPlacaAsync(FilterPlaca, cancellationToken);
                Registros = new ObservableCollection<ConsumoCombustibleVehiculoDto>(items);

                var resumen = await _consumoService.GetResumenByPlacaAsync(FilterPlaca, cancellationToken);
                TotalRegistros = resumen.TotalRegistros;
                TotalGalones = resumen.TotalGalones;
                TotalCosto = resumen.TotalCosto;
                PromedioCostoPorGalon = resumen.PromedioCostoPorGalon;
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo cargar el consumo de combustible.";
                _logger.LogError(ex, "Error cargando consumo de combustible para placa {Placa}", FilterPlaca);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task RegistrarAsync(ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                dto.PlacaVehiculo = FilterPlaca;
                await _consumoService.CreateAsync(dto, cancellationToken);
                await LoadAsync(cancellationToken);
                SuccessMessage = "Tanqueada registrada correctamente.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error registrando tanqueada para placa {Placa}", FilterPlaca);
            }
        }

        public async Task EditarAsync(ConsumoCombustibleVehiculoDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                dto.PlacaVehiculo = FilterPlaca;
                await _consumoService.UpdateAsync(dto.Id, dto, cancellationToken);
                await LoadAsync(cancellationToken);
                SuccessMessage = "Tanqueada actualizada correctamente.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error actualizando tanqueada {Id}", dto.Id);
            }
        }

        public async Task EliminarAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                await _consumoService.DeleteAsync(id, cancellationToken);
                await LoadAsync(cancellationToken);
                SuccessMessage = "Registro de tanqueada eliminado.";
            }
            catch (Exception ex)
            {
                ErrorMessage = "No se pudo eliminar el registro de tanqueada.";
                _logger.LogError(ex, "Error eliminando tanqueada {Id}", id);
            }
        }
    }
}
