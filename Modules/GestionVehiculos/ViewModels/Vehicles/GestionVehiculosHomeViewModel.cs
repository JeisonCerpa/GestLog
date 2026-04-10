using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionVehiculos.Interfaces.Data;
using GestLog.Modules.GestionVehiculos.ViewModels.Mantenimientos;
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
        public string Plate { get; set; } = string.Empty;
        public string PhotoPath { get; set; } = string.Empty;
        public string MileageText { get; set; } = string.Empty;
        public string DocumentSummary { get; set; } = string.Empty;
        public string BadgeText { get; set; } = string.Empty;
        public System.Windows.Media.Brush? BadgeBackground { get; set; }
        public System.Windows.Media.Brush? BadgeForeground { get; set; }
        public ICommand? VerDetallesCommand { get; set; }
        
        // NUEVAS PROPIEDADES PARA INDICADORES
        public string StateIndicator { get; set; } = "●";
        public System.Windows.Media.Brush? StateIndicatorBrush { get; set; }
        public string StatusText { get; set; } = "OK";
        public string AlertMessage { get; set; } = string.Empty;
        public bool HasAlerts { get; set; } = false;
        public bool IsDiscarded { get; set; } = false;
        public string DiscardedOverlayText { get; set; } = string.Empty;
        public VehicleState State { get; set; } = VehicleState.Activo;
    }

    /// <summary>
    /// ViewModel principal para la vista de Gestión de Vehículos
    /// </summary>
    public partial class GestionVehiculosHomeViewModel : ObservableObject
    {
        private readonly IVehicleService _vehicleService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private ObservableCollection<VehicleCardViewModel> vehicles = new();

        [ObservableProperty]
        private ObservableCollection<VehicleCardViewModel> filteredVehicles = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool isFormDialogOpen = false;

        // NUEVAS PROPIEDADES PARA ESTADÍSTICAS Y FILTROS
        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private string filterEstado = "Todos";

        [ObservableProperty]
        private string sortOption = "Nombre (A-Z)";

        [ObservableProperty]
        private int totalVehicles = 0;

        [ObservableProperty]
        private int pendingMaintenances = 0;

        [ObservableProperty]
        private int expiredDocuments = 0;

        [ObservableProperty]
        private bool hasNoVehicles = true;

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
                FilteredVehicles.Clear();

                var vehiclesDto = await _vehicleService.GetAllAsync(cancellationToken);

                foreach (var vehicle in vehiclesDto)
                {
                    var cardVm = MapToCardViewModel(vehicle);
                    Vehicles.Add(cardVm);
                }

                TotalVehicles = Vehicles.Count;
                PendingMaintenances = CalculatePendingMaintenances();
                ExpiredDocuments = CalculateExpiredDocuments();
                
                RefreshFilteredVehicles();  // Esto también actualiza HasNoVehicles

                // _logger.LogInformation($"Vehículos cargados: {Vehicles.Count}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al cargar vehículos. Intente nuevamente.";
                _logger.LogError(ex, "Error loading vehicles");
                HasNoVehicles = true; // Si hay error, mostrar empty state
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
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

                // Mostrar el diálogo en el hilo de UI para evitar bloqueos si el comando se ejecuta desde otro contexto
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null)
                {
                    // Fallback: mostrar directamente
                    if (dialog.ShowDialog() == true)
                    {
                        await LoadVehiclesAsync();
                    }
                }
                else
                {
                    var showResult = await dispatcher.InvokeAsync(() => dialog.ShowDialog());
                    if (showResult == true)
                    {
                        // Recargar la lista de vehículos después de guardar
                        await LoadVehiclesAsync();
                    }
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Error al abrir el formulario.";
                // Logging intentionally removed as requested
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
        /// Navega a la vista de configuración de plantillas de mantenimiento.
        /// </summary>
        [RelayCommand]
        public async Task AbrirConfiguracionPlantillasAsync()
        {
            try
            {
                var sp = LoggingService.GetServiceProvider();
                var plantillasVm = sp.GetService(typeof(PlantillasMantenimientoViewModel)) as PlantillasMantenimientoViewModel;
                if (plantillasVm == null)
                {
                    ErrorMessage = "No se pudo abrir configuración de plantillas.";
                    _logger.LogWarning("PlantillasMantenimientoViewModel no registrado en DI");
                    return;
                }

                await plantillasVm.LoadPlantillasAsync();

                var plantillasView = new Views.Mantenimientos.PlantillasMantenimientoView
                {
                    DataContext = plantillasVm
                };

                var mainWindow = System.Windows.Application.Current?.MainWindow as GestLog.MainWindow;
                var app = System.Windows.Application.Current;

                if (mainWindow != null && app != null)
                {
                    await app.Dispatcher.InvokeAsync(() =>
                    {
                        dynamic mw = mainWindow;
                        dynamic pv = plantillasView;
                        mw.NavigateToView(pv, "Configuración de Plantillas");
                    });
                }
                else
                {
                    ErrorMessage = "No se pudo abrir la vista de plantillas (MainWindow no disponible).";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al abrir configuración de plantillas.";
                _logger.LogError(ex, "Error opening maintenance templates view");
            }
        }

        /// <summary>
        /// Mapea un DTO de vehículo a ViewModel de tarjeta
        /// </summary>
        private VehicleCardViewModel MapToCardViewModel(VehicleDto vehicle)
        {
            var cardVm = new VehicleCardViewModel
            {
                VehicleId = vehicle.Id,
                VehicleName = $"{vehicle.Brand} {vehicle.Model} {vehicle.Year}",
                Plate = vehicle.Plate ?? "N/A",
                PhotoPath = vehicle.PhotoThumbPath ?? vehicle.PhotoPath ?? "/Assets/PlantillaSIMICS.png",
                MileageText = $"KM: {vehicle.Mileage:N0}",
                DocumentSummary = GetDocumentSummary(vehicle),
                VerDetallesCommand = new RelayCommand(async () => await VerDetallesAsync(vehicle.Id)),
                State = vehicle.State  // Asignar el estado del vehículo para ordenamiento
            };

            // Establecer badge y colores según estado
            (cardVm.BadgeText, cardVm.BadgeBackground, cardVm.BadgeForeground) = GetBadgeInfo(vehicle.State);

            // Establecer indicadores de estado
            cardVm.StatusText = vehicle.State.ToString();
            cardVm.StateIndicator = "●";
            cardVm.StateIndicatorBrush = vehicle.State switch
            {
                VehicleState.Activo => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 17, 137, 56)),
                VehicleState.EnMantenimiento => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 230, 126, 34)),
                VehicleState.DadoDeBaja => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 220, 53, 69)),
                VehicleState.Inactivo => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 140, 140, 140)),
                _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 140, 140, 140))
            };
            cardVm.HasAlerts = false;
            cardVm.AlertMessage = string.Empty;
            cardVm.IsDiscarded = vehicle.State == VehicleState.Inactivo || vehicle.State == VehicleState.DadoDeBaja;
            cardVm.DiscardedOverlayText = vehicle.State switch
            {
                VehicleState.DadoDeBaja => "DADO DE BAJA",
                VehicleState.Inactivo => "INACTIVO",
                _ => string.Empty
            };

            return cardVm;
        }

        /// <summary>
        /// Obtiene el resumen de documentos (placeholder)
        /// </summary>
        private string GetDocumentSummary(VehicleDto vehicle)
        {
            // TODO: Obtener estado real de documentos desde BD
            return "SOAT: ✓ Vigente\nTecno-Mec.: ✓ Vigente";
        }

        /// <summary>
        /// Navega a la vista de detalles del vehículo dentro de la aplicación
        /// </summary>
        private async System.Threading.Tasks.Task VerDetallesAsync(Guid vehicleId)
        {
            try
            {
                // _logger.LogInformation($"Navegando a detalles del vehículo: {vehicleId}");

                // Resolver ViewModel desde DI
                var sp = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                var detailsVm = sp.GetService(typeof(VehicleDetailsViewModel)) as VehicleDetailsViewModel;
                if (detailsVm == null)
                {
                    _logger.LogWarning("VehicleDetailsViewModel no registrado en DI");
                    ErrorMessage = "No se pudo abrir detalles del vehículo.";
                    return;
                }                // Cargar datos en background para no bloquear UI
                await System.Threading.Tasks.Task.Run(async () => await detailsVm.LoadAsync(vehicleId));

                // Crear la vista y navegar en hilo UI
                var detailsView = new Views.Vehicles.VehicleDetailsView(detailsVm);                var mainWindow = System.Windows.Application.Current?.MainWindow as GestLog.MainWindow;
                var app = System.Windows.Application.Current;
                if (mainWindow != null && app != null)
                {
                    await app.Dispatcher.InvokeAsync(async () =>
                    {
                        dynamic mw = mainWindow;
                        dynamic dv = detailsView;
                        mw.NavigateToView(dv, $"Vehículo - {detailsVm.Plate}");
                        
                        // ✅ Llamar a LoadVehicleAsync DESPUÉS de mostrar la vista para cargar documentos
                        // await detailsView.LoadVehicleAsync(vehicleId);
                        await detailsView.LoadVehicleAsync(vehicleId);
                    });
                }
                else
                {
                    ErrorMessage = "No se pudo abrir la vista de detalles (MainWindow no disponible).";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error al abrir detalles del vehículo.";
                _logger.LogError(ex, "Error viewing vehicle details");
            }
        }

        /// <summary>
        /// Obtiene información del badge - Estado General del vehículo
        /// </summary>
        private (string text, System.Windows.Media.Brush background, System.Windows.Media.Brush foreground) GetBadgeInfo(VehicleState state)
        {
            // TODO: Integrar datos reales de documentos vencidos y mantenimientos atrasados
            // Por ahora basamos en estado general del vehículo
            return state switch
            {
                VehicleState.Activo => (
                    "✓ TODO OK",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 224, 232, 226)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 54, 92, 70))
                ),
                VehicleState.EnMantenimiento => (
                    "⚠ REVISAR",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 232, 225, 212)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 130, 88, 35))
                ),
                VehicleState.DadoDeBaja => (
                    "DADO DE BAJA",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 224, 224, 224)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 95, 95, 95))
                ),
                VehicleState.Inactivo => (
                    "INACTIVO",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 224, 224, 224)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 95, 95, 95))
                ),
                _ => (
                    "DESCONOCIDO",
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 220, 220, 220)),
                    new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 100, 100, 100))
                )
            };
        }

        /// <summary>
        /// Observador de cambios en búsqueda
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            RefreshFilteredVehicles();
        }

        /// <summary>
        /// Observador de cambios en filtro de estado
        /// </summary>
        partial void OnFilterEstadoChanged(string value)
        {
            RefreshFilteredVehicles();
        }

        /// <summary>
        /// Observador de cambios en ordenamiento
        /// </summary>
        partial void OnSortOptionChanged(string value)
        {
            RefreshFilteredVehicles();
        }

        /// <summary>
        /// Obtiene la prioridad del estado para ordenar: Activos → En Mantenimiento → Inactivos → Dados de Baja
        /// </summary>
        private int GetStatePriority(VehicleState state)
        {
            return state switch
            {
                VehicleState.Activo => 1,
                VehicleState.EnMantenimiento => 2,
                VehicleState.Inactivo => 3,
                VehicleState.DadoDeBaja => 4,
                _ => 5
            };
        }

        /// <summary>
        /// Actualiza la colección de vehículos filtrados según búsqueda, filtros y orden
        /// </summary>
        private void RefreshFilteredVehicles()
        {
            if (Vehicles == null)
                return;

            // Aplicar filtro de búsqueda (por nombre o placa)
            var q = string.IsNullOrWhiteSpace(SearchText)
                ? Vehicles
                : new ObservableCollection<VehicleCardViewModel>(Vehicles.Where(v =>
                    v.VehicleName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    v.Plate.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

            // Aplicar filtro de estado
            if (!string.IsNullOrEmpty(FilterEstado) && FilterEstado != "Todos")
            {
                q = new ObservableCollection<VehicleCardViewModel>(q.Where(v =>
                    (FilterEstado == "Activos" && v.StatusText == "OK") ||
                    (FilterEstado == "Con Alertas" && v.HasAlerts)));
            }

            // Aplicar ordenamiento: primero por estado (Activos → Mantenimiento → Inactivos → Dados de Baja), 
            // luego por la opción de ordenamiento seleccionada
            IOrderedEnumerable<VehicleCardViewModel> ordenada;
            
            if (SortOption == "Alertas")
            {
                ordenada = q
                    .OrderBy(v => GetStatePriority(v.State))
                    .ThenByDescending(v => v.HasAlerts)
                    .ThenBy(v => v.VehicleName);
            }
            else if (SortOption == "Reciente")
            {
                ordenada = q
                    .OrderBy(v => GetStatePriority(v.State))
                    .ThenByDescending(v => v.VehicleId);
            }
            else // "Nombre (A-Z)" (default)
            {
                ordenada = q
                    .OrderBy(v => GetStatePriority(v.State))
                    .ThenBy(v => v.VehicleName);
            }

            FilteredVehicles.Clear();
            foreach (var item in ordenada)
                FilteredVehicles.Add(item);

            // Actualizar HasNoVehicles para el Empty State
            HasNoVehicles = FilteredVehicles.Count == 0;
        }

        /// <summary>
        /// Limpia los filtros
        /// </summary>
        [RelayCommand]
        public void ClearFilters()
        {
            SearchText = string.Empty;
            FilterEstado = "Todos";
            SortOption = "Nombre (A-Z)";
        }

        /// <summary>
        /// Calcula el número de mantenimientos pendientes (placeholder)
        /// </summary>
        private int CalculatePendingMaintenances()
        {
            // TODO: Implementar lógica real de mantenimientos pendientes
            return 0;
        }

        /// <summary>
        /// Calcula el número de documentos vencidos (placeholder)
        /// </summary>
        private int CalculateExpiredDocuments()
        {
            // TODO: Implementar lógica real de documentos vencidos
            return 0;
        }
    }
}
