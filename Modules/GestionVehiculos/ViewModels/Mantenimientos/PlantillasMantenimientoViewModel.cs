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
        private ObservableCollection<PlantillaMantenimientoDto> filteredPlantillas = new();

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
        private string searchText = string.Empty;

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

                RefreshFilteredPlantillas();

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

        partial void OnSearchTextChanged(string value)
        {
            RefreshFilteredPlantillas();
        }

        private void RefreshFilteredPlantillas()
        {
            if (Plantillas == null)
                return;

            var q = string.IsNullOrWhiteSpace(SearchText)
                ? Plantillas
                : new ObservableCollection<PlantillaMantenimientoDto>(Plantillas.Where(p =>
                    (!string.IsNullOrEmpty(p.Nombre) && p.Nombre.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(p.Descripcion) && p.Descripcion.Contains(SearchText, StringComparison.OrdinalIgnoreCase))));

            FilteredPlantillas.Clear();
            foreach (var item in q)
                FilteredPlantillas.Add(item);
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
                RefreshFilteredPlantillas();
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

        [RelayCommand]
        public async Task GuardarCambiosPlantillaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (SelectedPlantilla == null)
                {
                    ErrorMessage = "Debe seleccionar una plantilla";
                    return;
                }

                if (string.IsNullOrWhiteSpace(SelectedPlantilla.Nombre))
                {
                    ErrorMessage = "El nombre de la plantilla es obligatorio";
                    return;
                }

                if (SelectedPlantilla.IntervaloKM <= 0 && SelectedPlantilla.IntervaloDias <= 0)
                {
                    ErrorMessage = "Debe definir al menos un intervalo (KM o días) mayor que 0";
                    return;
                }

                IsLoading = true;

                var actualizada = await _plantillaService.UpdateAsync(SelectedPlantilla.Id, SelectedPlantilla, cancellationToken);

                var index = Plantillas.IndexOf(SelectedPlantilla);
                if (index >= 0)
                {
                    Plantillas[index] = actualizada;
                }

                SelectedPlantilla = actualizada;
                RefreshFilteredPlantillas();
                SuccessMessage = "Plantilla actualizada correctamente";
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Actualización de plantilla cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al actualizar plantilla";
                _logger.LogError(ex, "Error actualizando plantilla de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task EliminarPlantillaAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (SelectedPlantilla == null)
                {
                    ErrorMessage = "Debe seleccionar una plantilla";
                    return;
                }

                var confirmacion = System.Windows.MessageBox.Show(
                    $"¿Desea eliminar la plantilla '{SelectedPlantilla.Nombre}'?\n\nEsta acción no se puede deshacer.",
                    "Confirmar eliminación",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirmacion != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }

                IsLoading = true;

                var plantillaAEliminar = SelectedPlantilla;
                await _plantillaService.DeleteAsync(plantillaAEliminar.Id, cancellationToken);

                Plantillas.Remove(plantillaAEliminar);
                SelectedPlantilla = null;
                RefreshFilteredPlantillas();
                SuccessMessage = "Plantilla eliminada correctamente";
            }
            catch (OperationCanceledException)
            {
                ErrorMessage = "Operación cancelada";
                _logger.LogWarning("Eliminación de plantilla cancelada");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al eliminar plantilla";
                _logger.LogError(ex, "Error eliminando plantilla de mantenimiento");
            }
            finally
            {
                IsLoading = false;
            }
        }

    }
}
