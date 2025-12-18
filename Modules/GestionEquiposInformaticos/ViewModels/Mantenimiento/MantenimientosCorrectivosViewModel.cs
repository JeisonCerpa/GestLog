using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento
{
    /// <summary>
    /// ViewModel para la vista de Mantenimientos Correctivos
    /// Gestiona la carga, filtrado y visualización de mantenimientos correctivos (reactivos)
    /// </summary>
    public partial class MantenimientosCorrectivosViewModel : ObservableObject
    {
        private readonly IMantenimientoCorrectivoService _mantenimientoService;
        private readonly IGestLogLogger _logger;
        private readonly CurrentUserInfo _currentUserInfo;
        private CancellationTokenSource? _cancellationTokenSource;
        // Debounce token para evitar aplicar filtros en cada tecla
        private CancellationTokenSource? _debounceCts;
        private const int DebounceDelayMs = 300;

        // Lista maestra con todos los mantenimientos cargados (sin filtrar)
        private List<MantenimientoCorrectivoDto> _allMantenimientos = new();

        // Filtro único de texto para buscar por equipo, proveedor o texto libre
        [ObservableProperty]
        private string filter = string.Empty;

        partial void OnFilterChanged(string value)
        {
            // Aplicar filtros con debounce para evitar ejecuciones por cada tecla
            _ = DebounceApplyFiltersAsync();
        }

        [ObservableProperty]
        private bool filtrarPendientes = false;

        partial void OnFiltrarPendientesChanged(bool value)
        {
            ApplyFilters();
        }

        [ObservableProperty]
        private bool filtrarCompletados = false;

        partial void OnFiltrarCompletadosChanged(bool value)
        {
            ApplyFilters();
        }

        [ObservableProperty]
        private bool filtrarCancelados = false;

        partial void OnFiltrarCanceladosChanged(bool value)
        {
            ApplyFilters();
        }

        [ObservableProperty]
        private bool filtrarEnReparacion = false;

        partial void OnFiltrarEnReparacionChanged(bool value)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Debounce wrapper para ApplyFilters: espera un pequeño retraso antes de aplicar, cancelable si el usuario sigue escribiendo.
        /// </summary>
        private async Task DebounceApplyFiltersAsync()
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                // Esperar en el contexto actual (UI) y luego aplicar filtros ahí mismo
                await Task.Delay(DebounceDelayMs, token);

                ApplyFilters();
            }
            catch (OperationCanceledException)
            {
                // Ignorar
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en debounce de filtros");
            }
        }

        /// <summary>
        /// Colección de mantenimientos correctivos
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<MantenimientoCorrectivoDto> mantenimientos = new();

        /// <summary>
        /// Mantenimiento seleccionado actualmente
        /// </summary>
        [ObservableProperty]
        private MantenimientoCorrectivoDto? selectedMantenimiento;

        /// <summary>
        /// Indica si se están cargando los datos
        /// </summary>
        [ObservableProperty]
        private bool isLoading;

        /// <summary>
        /// Mensaje de error para mostrar en la UI
        /// </summary>
        [ObservableProperty]
        private string errorMessage = string.Empty;

        /// <summary>
        /// Filtro de estado para mostrar solo mantenimientos en cierto estado
        /// </summary>
        [ObservableProperty]
        private EstadoMantenimientoCorrectivo? filtroEstado;

        /// <summary>
        /// Indica si solo mostrar mantenimientos en reparación
        /// </summary>
        [ObservableProperty]
        private bool mostrarSoloEnReparacion;

        /// <summary>
        /// Indica si incluir registros dados de baja
        /// </summary>
        [ObservableProperty]
        private bool incluirDadosDeBaja;

        public MantenimientosCorrectivosViewModel(
            IMantenimientoCorrectivoService mantenimientoService,
            IGestLogLogger logger,
            CurrentUserInfo currentUserInfo)
        {
            _mantenimientoService = mantenimientoService ?? throw new ArgumentNullException(nameof(mantenimientoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserInfo = currentUserInfo ?? throw new ArgumentNullException(nameof(currentUserInfo));
        }

        /// <summary>
        /// Abre la ventana modal para crear un nuevo mantenimiento correctivo.
        /// La ventana resuelve su ViewModel desde el ServiceProvider en su code-behind.
        /// </summary>
        [RelayCommand]
        public async Task AgregarMantenimientoAsync()
        {
            try
            {
                // Crear y mostrar la ventana modal. El code-behind resolverá el VM y suscribirá OnExito.
                var window = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.CrearMantenimientoCorrectivoWindow();
                var owner = System.Windows.Application.Current?.MainWindow;
                window.ConfigurarParaVentanaPadre(owner);

                var result = window.ShowDialog();
                if (result == true)
                {
                    // Si se creó con éxito, refrescar la lista
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abriendo ventana de crear mantenimiento");
            }
        }

        /// <summary>
        /// Inicializa la carga de mantenimientos correctivos
        /// </summary>
        [RelayCommand]
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await CargarMantenimientosAsync(cancellationToken);
        }

        /// <summary>
        /// Carga todos los mantenimientos correctivos
        /// </summary>
        private async Task CargarMantenimientosAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

                IsLoading = true;
                ErrorMessage = string.Empty;

                _logger.LogDebug("Iniciando carga de mantenimientos correctivos");

                var mantenimientos = await _mantenimientoService.ObtenerTodosAsync(IncluirDadosDeBaja, cts.Token);

                // Guardar lista maestra y aplicar filtros locales
                _allMantenimientos = mantenimientos.ToList();
                ApplyFilters();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Operación de carga de mantenimientos cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar mantenimientos correctivos");
                ErrorMessage = "Error al cargar los mantenimientos. Intente nuevamente.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Aplica los filtros actuales sobre la colección maestra y actualiza la colección expuesta <see cref="Mantenimientos"/>.
        /// </summary>
        private void ApplyFilters()
        {
            try
            {
                if (_allMantenimientos == null) return;

                IEnumerable<MantenimientoCorrectivoDto> query = _allMantenimientos.AsEnumerable();

                // Filtro por estado específico (si está seleccionado)
                if (FiltroEstado.HasValue)
                {
                    query = query.Where(m => m.Estado == FiltroEstado.Value);
                }

                // Mostrar solo en reparación
                if (MostrarSoloEnReparacion)
                {
                    query = query.Where(m => m.Estado == EstadoMantenimientoCorrectivo.EnReparacion);
                }                // Filtro de texto único: buscar en NombreEntidad, Codigo y ProveedorAsignado
                if (!string.IsNullOrWhiteSpace(Filter))
                {
                    var term = Filter.Trim().ToLowerInvariant();
                    query = query.Where(m => (m.NombreEntidad ?? string.Empty).ToLowerInvariant().Contains(term)
                                              || (m.Codigo ?? string.Empty).ToLowerInvariant().Contains(term)
                                              || (m.ProveedorAsignado ?? string.Empty).ToLowerInvariant().Contains(term));
                }

                // Filtrado por checkboxes de estado: si ninguno está marcado, no restringir; si alguno marcado -> incluir sólo esos estados
                var estadosSeleccionados = new List<EstadoMantenimientoCorrectivo>();
                if (FiltrarPendientes) estadosSeleccionados.Add(EstadoMantenimientoCorrectivo.Pendiente);
                if (FiltrarCompletados) estadosSeleccionados.Add(EstadoMantenimientoCorrectivo.Completado);
                if (FiltrarCancelados) estadosSeleccionados.Add(EstadoMantenimientoCorrectivo.Cancelado);
                if (FiltrarEnReparacion) estadosSeleccionados.Add(EstadoMantenimientoCorrectivo.EnReparacion);

                if (estadosSeleccionados.Any())
                {
                    query = query.Where(m => estadosSeleccionados.Contains(m.Estado));
                }

                // Asignar colección observable
                Mantenimientos = new ObservableCollection<MantenimientoCorrectivoDto>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error aplicando filtros en MantenimientosCorrectivosViewModel");
            }
        }

        /// <summary>
        /// Recarga los mantenimientos aplicando filtros actuales
        /// </summary>
        [RelayCommand]
        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            SelectedMantenimiento = null;
            await CargarMantenimientosAsync(cancellationToken);
        }

        /// <summary>
        /// Cambia el filtro de estado y recarga
        /// </summary>
        [RelayCommand]
        public async Task CambiarFiltroEstadoAsync(EstadoMantenimientoCorrectivo? nuevoEstado, CancellationToken cancellationToken = default)
        {
            FiltroEstado = nuevoEstado;
            await RefreshAsync(cancellationToken);
        }        /// <summary>
        /// Cancela un mantenimiento correctivo seleccionado
        /// </summary>
        [RelayCommand]
        public async Task CancelarMantenimientoAsync(string razonCancelacion = "", CancellationToken cancellationToken = default)
        {
            if (SelectedMantenimiento?.Id == null)
            {
                ErrorMessage = "Debe seleccionar un mantenimiento para cancelar";
                return;
            }

            if (string.IsNullOrWhiteSpace(razonCancelacion))
            {
                ErrorMessage = "Debe proporcionar una razón para cancelar el mantenimiento";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var resultado = await _mantenimientoService.CancelarAsync(
                    SelectedMantenimiento.Id.Value,
                    razonCancelacion,
                    cancellationToken);

                if (resultado)
                {
                    _logger.LogDebug($"Mantenimiento {SelectedMantenimiento.Id} cancelado exitosamente");
                    await RefreshAsync(cancellationToken);
                }
                else
                {
                    ErrorMessage = "No fue posible cancelar el mantenimiento";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar mantenimiento");
                ErrorMessage = "Error al cancelar el mantenimiento. Intente nuevamente.";
            }
            finally
            {
                IsLoading = false;
            }
        }        /// <summary>
        /// Envía un mantenimiento a reparación con proveedor asignado
        /// </summary>
        [RelayCommand]
        public async Task EnviarAReparacionAsync(MantenimientoCorrectivoDto mantenimiento, CancellationToken cancellationToken = default)
        {
            if (mantenimiento?.Id == null)
            {
                ErrorMessage = "Debe seleccionar un mantenimiento válido";
                return;
            }

            try
            {
                // Crear y mostrar la ventana modal de envío a reparación
                var window = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.EnviarAReparacionWindow();
                var owner = System.Windows.Application.Current?.MainWindow;
                window.ConfigurarParaVentanaPadre(owner);

                // Resolver el ViewModel y pasar el mantenimiento
                try
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    if (serviceProvider != null)
                    {
                        var vm = serviceProvider.GetService(typeof(EnviarAReparacionViewModel)) as EnviarAReparacionViewModel;
                        if (vm != null)
                        {
                            vm.InitializarMantenimiento(mantenimiento);
                            window.DataContext = vm;
                        }
                    }
                }
                catch { /* No fatal */ }

                var result = window.ShowDialog();
                if (result == true)
                {
                    // Si se envió con éxito, refrescar la lista
                    await RefreshAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abriendo ventana de enviar a reparación");                ErrorMessage = "Error al abrir el formulario de envío a reparación";
            }
        }

        /// <summary>
        /// Abre la ventana modal para completar un mantenimiento en estado "En Reparación"
        /// </summary>
        [RelayCommand]
        public async Task CompletarMantenimientoAsync(MantenimientoCorrectivoDto mantenimiento, CancellationToken cancellationToken = default)
        {
            if (mantenimiento?.Id == null)
            {
                ErrorMessage = "Debe seleccionar un mantenimiento válido";
                return;
            }

            // Validar que el mantenimiento esté en estado "En Reparación"
            if (mantenimiento.Estado != EstadoMantenimientoCorrectivo.EnReparacion)
            {
                ErrorMessage = "Solo se pueden completar mantenimientos que están en reparación";
                return;
            }

            try
            {
                // Crear y mostrar la ventana modal de completar mantenimiento
                var window = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.CompletarMantenimientoWindow();
                var owner = System.Windows.Application.Current?.MainWindow;
                window.ConfigurarParaVentanaPadre(owner);

                // Resolver el ViewModel y pasar el mantenimiento
                try
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    if (serviceProvider != null)
                    {
                        var vm = serviceProvider.GetService(typeof(CompletarMantenimientoViewModel)) as CompletarMantenimientoViewModel;
                        if (vm != null)
                        {
                            vm.InitializarMantenimiento(mantenimiento);
                            window.DataContext = vm;
                        }
                    }
                }
                catch { /* No fatal */ }

                var result = window.ShowDialog();
                if (result == true)
                {
                    // Si se completó con éxito, refrescar la lista
                    await RefreshAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abriendo ventana de completar mantenimiento");
                ErrorMessage = "Error al abrir el formulario de completar mantenimiento";
            }
        }        /// <summary>
        /// Abre la ventana de detalles de un mantenimiento en modo lectura
        /// </summary>
        [RelayCommand]
        public async Task VerDetallesMantenimientoAsync(MantenimientoCorrectivoDto mantenimiento, CancellationToken cancellationToken = default)
        {
            if (mantenimiento?.Id == null)
            {
                ErrorMessage = "Debe seleccionar un mantenimiento válido";
                return;
            }

            try
            {
                // Crear y mostrar la ventana modal de detalles
                var window = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.DetallesMantenimientoWindow();
                var owner = System.Windows.Application.Current?.MainWindow;
                window.ConfigurarParaVentanaPadre(owner);

                // Resolver el ViewModel y pasar el mantenimiento
                try
                {
                    var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
                    if (serviceProvider != null)
                    {
                        var vm = serviceProvider.GetService(typeof(DetallesMantenimientoViewModel)) as DetallesMantenimientoViewModel;
                        if (vm != null)
                        {
                            vm.InitializarMantenimiento(mantenimiento);
                            window.DataContext = vm;
                        }
                    }
                }
                catch { /* No fatal */ }

                await Task.Run(() => window.ShowDialog(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error abriendo detalles del mantenimiento");
                ErrorMessage = "Error al abrir los detalles del mantenimiento";
            }
        }

        /// <summary>
        /// Ejecuta la acción apropiada basada en el estado del mantenimiento
        /// </summary>
        [RelayCommand]
        public async Task EjecutarAccionDinamicaAsync(MantenimientoCorrectivoDto mantenimiento, CancellationToken cancellationToken = default)
        {
            if (mantenimiento == null)
            {
                return;
            }            switch (mantenimiento.Estado)
            {
                case EstadoMantenimientoCorrectivo.Pendiente:
                    await EnviarAReparacionAsync(mantenimiento, cancellationToken);
                    break;

                case EstadoMantenimientoCorrectivo.EnReparacion:
                    await CompletarMantenimientoAsync(mantenimiento, cancellationToken);
                    break;

                case EstadoMantenimientoCorrectivo.Completado:
                case EstadoMantenimientoCorrectivo.Cancelado:
                    await VerDetallesMantenimientoAsync(mantenimiento, cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// Limpia los recursos al descartar el ViewModel
        /// </summary>
        public void Cleanup()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
    }
}
