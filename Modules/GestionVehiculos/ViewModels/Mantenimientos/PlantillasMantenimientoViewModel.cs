using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.Models.DTOs;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos
{
    /// <summary>
    /// ViewModel para gestionar plantillas de mantenimiento
    /// </summary>
    public partial class PlantillasMantenimientoViewModel : ObservableObject
    {
        private readonly IPlantillaMantenimientoService _plantillaService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<PlantillaMantenimientoDto> plantillas = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private string successMessage = string.Empty;

        [ObservableProperty]
        private PlantillaMantenimientoDto? selectedPlantilla;

        [ObservableProperty]
        private string nuevaPlantillaNombre = string.Empty;

        [ObservableProperty]
        private string nuevaPlantillaDescripcion = string.Empty;

        [ObservableProperty]
        private int nuevoIntervaloKm = 5000;

        [ObservableProperty]
        private int nuevoIntervaloDias = 180;

        [ObservableProperty]
        private int nuevoTipoIntervalo = 1;

        public PlantillasMantenimientoViewModel(
            IPlantillaMantenimientoService plantillaService,
            IGestLogLogger logger)
        {
            _plantillaService = plantillaService;
            _logger = logger;
        }

        /// <summary>
        /// Carga todas las plantillas de mantenimiento
        /// </summary>
        [RelayCommand]
        public async Task LoadPlantillasAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _plantillaService.GetAllAsync(cancellationToken);
                
                Plantillas.Clear();
                foreach (var plantilla in result)
                {
                    Plantillas.Add(plantilla);
                }

                _logger.LogInformation("Plantillas de mantenimiento cargadas exitosamente");
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Carga de plantillas cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar las plantillas";
                _logger.LogError(ex, "Error cargando plantillas de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Selecciona una plantilla para ver/editar detalles
        /// </summary>
        [RelayCommand]
        public void SelectPlantilla(PlantillaMantenimientoDto plantilla)
        {
            SelectedPlantilla = plantilla;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Limpia la selección actual
        /// </summary>
        [RelayCommand]
        public void ClearSelection()
        {
            SelectedPlantilla = null;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
        }

        [RelayCommand]
        public async Task CrearPlantillaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(NuevaPlantillaNombre))
                {
                    ErrorMessage = "Debe ingresar el nombre de la plantilla";
                    return;
                }

                if (NuevoIntervaloKm <= 0 && NuevoIntervaloDias <= 0)
                {
                    ErrorMessage = "Debe definir al menos un intervalo (KM o días) mayor que 0";
                    return;
                }

                IsLoading = true;

                var nuevaPlantilla = new PlantillaMantenimientoDto
                {
                    Nombre = NuevaPlantillaNombre.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(NuevaPlantillaDescripcion) ? null : NuevaPlantillaDescripcion.Trim(),
                    IntervaloKM = NuevoIntervaloKm,
                    IntervaloDias = NuevoIntervaloDias,
                    TipoIntervalo = NuevoTipoIntervalo,
                    Activo = true
                };

                var creada = await _plantillaService.CreateAsync(nuevaPlantilla, cancellationToken);

                Plantillas.Insert(0, creada);
                SelectedPlantilla = creada;
                SuccessMessage = "Plantilla creada correctamente";

                NuevaPlantillaNombre = string.Empty;
                NuevaPlantillaDescripcion = string.Empty;
                NuevoIntervaloKm = 5000;
                NuevoIntervaloDias = 180;
                NuevoTipoIntervalo = 1;
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Creación de plantilla cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al crear plantilla";
                _logger.LogError(ex, "Error creando plantilla de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

    }
}
