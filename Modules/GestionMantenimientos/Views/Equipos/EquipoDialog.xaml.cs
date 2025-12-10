using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Linq;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Services;
using GestLog.Modules.GestionMantenimientos.Services.Autocomplete;
using GestLog.Modules.GestionMantenimientos.Interfaces;

namespace GestLog.Modules.GestionMantenimientos.Views.Equipos
{
    /// <summary>
    /// LÃ³gica de interacciÃ³n para EquipoDialog.xaml
    /// </summary>
    public partial class EquipoDialog : Window
    {
        public EquipoDto Equipo { get; private set; }
        public bool IsEditMode { get; }
        private System.Windows.Forms.Screen? _lastScreenOwner;

        public EquipoDialog(EquipoDto? equipo = null)
        {
            InitializeComponent();
            if (equipo != null)
            {
                // Modo ediciÃ³n: clonar para no modificar el original hasta guardar
                Equipo = new EquipoDto(equipo);
                // Asegurar que si no tiene estado se preseleccione Activo
                if (Equipo.Estado == null)
                    Equipo.Estado = GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo;
                Equipo.IsCodigoReadOnly = true;
                Equipo.IsCodigoEnabled = false;
                IsEditMode = true;
            }
            else
            {
                Equipo = new EquipoDto();
                Equipo.IsCodigoReadOnly = false;
                Equipo.IsCodigoEnabled = true;
                // Preseleccionar Activo por defecto
                Equipo.Estado = GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo;
                IsEditMode = false;
            }            DataContext = new EquipoDialogViewModel(Equipo)
            {
                EstadosEquipo = new[] { GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo, GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Inactivo },
                Sedes = System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0],
                FrecuenciasMantenimiento = (System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0])
                    .Where(f => f != FrecuenciaMantenimiento.Correctivo && f != FrecuenciaMantenimiento.Predictivo)
                    .ToArray()
            };

            // Manejar tecla Escape
            this.KeyDown += EquipoDialog_KeyDown;
        }

        private void EquipoDialog_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }        private async void EquipoDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            // Establecer fecha mÃ¡xima permitida en DatePicker para evitar fechas futuras
            try
            {
                FechaCompraPicker.DisplayDateEnd = DateTime.Today;
            }
            catch { }

            if (this.Owner != null)
            {
                // Si el Owner se mueve/redimensiona, mantener sincronizado
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }

            // âœ… IMPORTANTE: No reasignar Owner aquÃ­ - ya se estableciÃ³ en el constructor
            // ConfigurarParaVentanaPadre(this.Owner);
            if (this.DataContext is EquipoDialogViewModel viewModel)
            {
                try
                {
                    // PequeÃ±o delay para asegurar que todo estÃ© inicializado
                    await System.Threading.Tasks.Task.Delay(50);

                    // Obtener el servicio de equipos inyectado
                    var equipoService = ((App)System.Windows.Application.Current).ServiceProvider
                        ?.GetService(typeof(IEquipoService)) as IEquipoService;

                    if (equipoService != null)
                    {
                        // Cargar cÃ³digos de forma asincrÃ³nica sin bloquear la UI
                        await viewModel.CargarCodigosExistentesAsync(equipoService, IsEditMode);
                    }
                }
                catch (System.Exception ex)
                {
                    // Log del error (sin mostrar al usuario)
                    System.Diagnostics.Debug.WriteLine($"Error al cargar cÃ³digos existentes: {ex.Message}");
                }
            }
        }



        private void OnFechaCompra_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (FechaCompraPicker.SelectedDate.HasValue && FechaCompraPicker.SelectedDate.Value.Date > DateTime.Today)
                {
                    System.Windows.MessageBox.Show("La fecha de compra no puede ser futura.", "Fecha invÃ¡lida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FechaCompraPicker.SelectedDate = null;
                }
            }
            catch { }
        }        private async void OnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Forzar actualizaciÃ³n del binding del DatePicker para asegurar que la propiedad FechaCompra del DTO estÃ© actualizada
            try
            {
                var expression = FechaCompraPicker.GetBindingExpression(System.Windows.Controls.DatePicker.SelectedDateProperty);
                expression?.UpdateSource();
            }
            catch { }

            // âœ… CRÃTICO: Sincronizar los valores de los ComboBox editables con el DTO
            // Los ComboBox editables estÃ¡n vinculados solo a FiltroMarca, FiltroClasificacion, FiltroCompradoA
            // Pero necesitamos actualizar Marca, Clasificacion, CompradoA con los valores del filtro
            var viewModel = DataContext as EquipoDialogViewModel;
            if (viewModel != null)
            {
                // Actualizar el EquipoDto con los valores actuales de los filtros
                Equipo.Marca = viewModel.FiltroMarca ?? string.Empty;
                Equipo.Clasificacion = viewModel.FiltroClasificacion ?? string.Empty;
                Equipo.CompradoA = viewModel.FiltroCompradoA ?? string.Empty;
                
                // âœ… NUEVO: Activar flag para mostrar errores de validaciÃ³n
                viewModel.ShowValidationErrors = true;
            }

            var errores = new List<string>();
            // Validaciones: CÃ³digo y Nombre son obligatorios
            if (string.IsNullOrWhiteSpace(Equipo.Codigo))
                errores.Add("El cÃ³digo del equipo es obligatorio.");
            
            // âœ… NUEVA: Validar que el Nombre sea obligatorio
            if (string.IsNullOrWhiteSpace(Equipo.Nombre))
                errores.Add("El nombre del equipo es obligatorio.");

            // âœ… NUEVA: Validar que la Sede sea obligatoria
            if (Equipo.Sede == null)
                errores.Add("La sede del equipo es obligatoria.");

            if (Equipo.Precio != null && Equipo.Precio < 0)
                errores.Add("El precio no puede ser negativo.");

            if (Equipo.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.DadoDeBaja && !Equipo.FechaBaja.HasValue)
                errores.Add("Debe indicar la fecha de baja si el equipo estÃ¡ dado de baja.");

            // ValidaciÃ³n de unicidad de cÃ³digo solo en alta
            if (!IsEditMode && !string.IsNullOrWhiteSpace(Equipo.Codigo))
            {
                var service = LoggingService.GetService<GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService>();
                var existente = await service.GetByCodigoAsync(Equipo.Codigo);
                if (existente != null)
                    errores.Add($"Ya existe un equipo con el cÃ³digo '{Equipo.Codigo}'.");
            }

            if (errores.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", errores), "Errores de validaciÃ³n", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void OnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void ConfigurarParaVentanaPadre(System.Windows.Window? parentWindow)
        {
            if (parentWindow == null) return;
            
            this.Owner = parentWindow;
            this.ShowInTaskbar = false;

            try
            {
                // Guardar referencia a la pantalla actual del owner
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                // Para un overlay modal, siempre maximizar para cubrir toda la pantalla
                // Esto evita problemas de DPI, pantallas mÃºltiples y posicionamiento
                this.WindowState = WindowState.Maximized;
            }
            catch
            {
                // Fallback: maximizar en pantalla principal
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Owner_SizeOrLocationChanged(object? sender, System.EventArgs e)
        {
            if (this.Owner == null) return;

            this.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Siempre maximizar para mantener el overlay cubriendo toda la pantalla
                    this.WindowState = WindowState.Maximized;
                    
                    // Detectar si el Owner cambiÃ³ de pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    // Si cambiÃ³ de pantalla, actualizar la referencia
                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
                    // En caso de error, asegurar que la ventana estÃ¡ maximizada
                    this.WindowState = WindowState.Maximized;
                }
            });
        }

        public class EquipoDialogViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            protected void RaisePropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            public EquipoDto Equipo { get; set; }
            public IEnumerable<EstadoEquipo> EstadosEquipo { get; set; } = new EstadoEquipo[0];
            public IEnumerable<Sede> Sedes { get; set; } = new Sede[0];
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];            // âœ… PROPIEDADES PARA VALIDACIÃ“N DE CÃ“DIGOS DUPLICADOS
            /// <summary>
            /// Conjunto de cÃ³digos existentes cargados para validaciÃ³n rÃ¡pida en memoria
            /// </summary>
            private System.Collections.Generic.HashSet<string> _codigosExistentes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            
            /// <summary>
            /// El cÃ³digo original del equipo en modo ediciÃ³n (para ignorarlo en validaciÃ³n)
            /// </summary>
            private string? _codigoOriginal;

            /// <summary>
            /// Proxy directo a las propiedades del EquipoDto para binding simple
            /// Con validaciÃ³n de duplicados en tiempo real
            /// </summary>
            public string? Codigo 
            { 
                get => Equipo.Codigo; 
                set 
                { 
                    Equipo.Codigo = value;
                    // Validar cÃ³digos duplicados
                    ValidarCodigoDuplicado();
                    RaisePropertyChanged(nameof(Codigo));
                    RaisePropertyChanged(nameof(IsCodigoDuplicado));
                    RaisePropertyChanged(nameof(MensajeCodigoDuplicado));
                    RaisePropertyChanged(nameof(IsFormularioValido));
                } 
            }
            public string? Nombre 
            { 
                get => Equipo.Nombre; 
                set 
                { 
                    Equipo.Nombre = value;
                    RaisePropertyChanged(nameof(Nombre));
                    RaisePropertyChanged(nameof(IsNombreVacio));
                    RaisePropertyChanged(nameof(IsFormularioValido));
                } 
            }
            public string? Marca 
            { 
                get => Equipo.Marca; 
                set
                {
                    Equipo.Marca = value;
                    // actualizar filtro (dispara debounce)
                    FiltroMarca = value ?? string.Empty;
                }
            }
            public EstadoEquipo? Estado { get => Equipo.Estado; set => Equipo.Estado = value; }
            public Sede? Sede 
            { 
                get => Equipo.Sede; 
                set 
                { 
                    Equipo.Sede = value; 
                    RaisePropertyChanged(nameof(Sede));
                    RaisePropertyChanged(nameof(IsSedeVacio));
                    RaisePropertyChanged(nameof(IsFormularioValido));
                } 
            }
            public FrecuenciaMantenimiento? FrecuenciaMtto { get => Equipo.FrecuenciaMtto; set => Equipo.FrecuenciaMtto = value; }
            public decimal? Precio { get => Equipo.Precio; set => Equipo.Precio = value; }
            public string? Observaciones { get => Equipo.Observaciones; set => Equipo.Observaciones = value; }
            // Proxy para Fecha de Compra (necesario para que el DatePicker vinculado actualice el DTO)
            public DateTime? FechaCompra { get => Equipo.FechaCompra; set => Equipo.FechaCompra = value; }
            public DateTime? FechaRegistro { get => Equipo.FechaRegistro; set => Equipo.FechaRegistro = value; }
            public DateTime? FechaBaja { get => Equipo.FechaBaja; set => Equipo.FechaBaja = value; }
            public bool IsCodigoReadOnly { get => Equipo.IsCodigoReadOnly; set => Equipo.IsCodigoReadOnly = value; }
            public bool IsCodigoEnabled { get => Equipo.IsCodigoEnabled; set => Equipo.IsCodigoEnabled = value; }

            // ClasificaciÃ³n / CompradoA proxies: al modificar el texto tambiÃ©n actualizamos el filtro para activar autocompletado
            public string? Clasificacion
            {
                get => Equipo.Clasificacion;
                set
                {
                    Equipo.Clasificacion = value;
                    // actualizar filtro (dispara debounce)
                    FiltroClasificacion = value ?? string.Empty;
                }
            }
            public string? CompradoA
            {
                get => Equipo.CompradoA;
                set
                {
                    Equipo.CompradoA = value;
                    FiltroCompradoA = value ?? string.Empty;
                }
            }

            // âœ… NUEVAS PROPIEDADES PARA VALIDACIÃ“N EN TIEMPO REAL
            /// <summary>
            /// Flag que controla si se deben mostrar los errores de validaciÃ³n.
            /// Se activa solo despuÃ©s de que el usuario intenta guardar.
            /// </summary>
            private bool _showValidationErrors = false;
            public bool ShowValidationErrors 
            { 
                get => _showValidationErrors; 
                set 
                { 
                    _showValidationErrors = value; 
                    RaisePropertyChanged(nameof(ShowValidationErrors));
                } 
            }

            /// <summary>
            /// Indica si el campo Nombre estÃ¡ vacÃ­o Y se deben mostrar errores de validaciÃ³n
            /// </summary>
            public bool IsNombreVacio => _showValidationErrors && string.IsNullOrWhiteSpace(Nombre);

            /// <summary>
            /// Indica si el campo Sede estÃ¡ vacÃ­o Y se deben mostrar errores de validaciÃ³n
            /// </summary>
            public bool IsSedeVacio => _showValidationErrors && Sede == null;            /// <summary>
            /// Indica si el formulario es vÃ¡lido para guardar (Nombre y Sede no pueden estar vacÃ­os, Codigo no duplicado)
            /// </summary>
            public bool IsFormularioValido => !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Codigo) && Sede != null && !IsCodigoDuplicado;

            /// <summary>
            /// ðŸš€ Indica si el cÃ³digo actual ya existe en la base de datos
            /// </summary>
            private bool _isCodigoDuplicado = false;
            public bool IsCodigoDuplicado
            {
                get => _isCodigoDuplicado;
                private set
                {
                    if (_isCodigoDuplicado != value)
                    {
                        _isCodigoDuplicado = value;
                        RaisePropertyChanged(nameof(IsCodigoDuplicado));
                        RaisePropertyChanged(nameof(IsFormularioValido));
                    }
                }
            }

            /// <summary>
            /// Mensaje de error cuando hay cÃ³digo duplicado
            /// </summary>
            public string MensajeCodigoDuplicado => IsCodigoDuplicado ? "âš ï¸ Este cÃ³digo ya existe en el sistema." : string.Empty;

            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoADisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoAFiltrados { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();

            // Debounce / cancelaciÃ³n y supresiÃ³n de cambios
            private bool _suppressFiltroClasificacionChanged = false;
            private bool _suppressFiltroCompradoAChanged = false;
            private bool _suppressFiltroMarcaChanged = false;
            private System.Threading.CancellationTokenSource? _clasificacionFilterCts;
            private System.Threading.CancellationTokenSource? _compradoAFilterCts;
            private System.Threading.CancellationTokenSource? _marcaFilterCts;

            private string filtroClasificacion = string.Empty;
            public string FiltroClasificacion
            {
                get => filtroClasificacion;
                set
                {
                    if (_suppressFiltroClasificacionChanged)
                    {
                        filtroClasificacion = value ?? string.Empty;
                        RaisePropertyChanged(nameof(FiltroClasificacion)); // â† CRÃTICO: Forzar PropertyChanged incluso cuando estÃ¡ suprimido
                        return;
                    }

                    filtroClasificacion = value ?? string.Empty;
                    RaisePropertyChanged(nameof(FiltroClasificacion));

                    // Cancelar bÃºsqueda anterior y crear nueva CTS para debounce
                    _clasificacionFilterCts?.Cancel();
                    _clasificacionFilterCts?.Dispose();
                    _clasificacionFilterCts = new System.Threading.CancellationTokenSource();
                    var token = _clasificacionFilterCts.Token;

                    _ = DebounceFiltrarClasificacionesAsync(token);
                }
            }

            private string filtroCompradoA = string.Empty;
            public string FiltroCompradoA
            {
                get => filtroCompradoA;
                set
                {
                    if (_suppressFiltroCompradoAChanged)
                    {
                        filtroCompradoA = value ?? string.Empty;
                        RaisePropertyChanged(nameof(FiltroCompradoA)); // â† CRÃTICO: Forzar PropertyChanged incluso cuando estÃ¡ suprimido
                        return;
                    }

                    filtroCompradoA = value ?? string.Empty;
                    RaisePropertyChanged(nameof(FiltroCompradoA));

                    _compradoAFilterCts?.Cancel();
                    _compradoAFilterCts?.Dispose();
                    _compradoAFilterCts = new System.Threading.CancellationTokenSource();
                    var token = _compradoAFilterCts.Token;

                    _ = DebounceFiltrarCompradoAAsync(token);
                }
            }

            private string filtroMarca = string.Empty;
            public string FiltroMarca
            {
                get => filtroMarca;
                set
                {
                    if (_suppressFiltroMarcaChanged)
                    {
                        filtroMarca = value ?? string.Empty;
                        RaisePropertyChanged(nameof(FiltroMarca)); // â† CRÃTICO: Forzar PropertyChanged incluso cuando estÃ¡ suprimido
                        return;
                    }

                    filtroMarca = value ?? string.Empty;
                    RaisePropertyChanged(nameof(FiltroMarca));

                    _marcaFilterCts?.Cancel();
                    _marcaFilterCts?.Dispose();
                    _marcaFilterCts = new System.Threading.CancellationTokenSource();
                    var token = _marcaFilterCts.Token;

                    _ = DebounceFiltrarMarcasAsync(token);
                }
            }

            public EquipoDialogViewModel(EquipoDto equipo)
            {
                Equipo = equipo;
                // Inicializar colecciones para autocompletado
                ClasificacionesDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                ClasificacionesFiltradas = new System.Collections.ObjectModel.ObservableCollection<string>();
                CompradoADisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                CompradoAFiltrados = new System.Collections.ObjectModel.ObservableCollection<string>();
                MarcasDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                MarcasFiltradas = new System.Collections.ObjectModel.ObservableCollection<string>();
                
                // Si existen valores iniciales en el DTO, asegurarlos en las listas
                if (!string.IsNullOrWhiteSpace(Equipo.Clasificacion) && !ClasificacionesDisponibles.Contains(Equipo.Clasificacion))
                    ClasificacionesDisponibles.Add(Equipo.Clasificacion);
                if (!string.IsNullOrWhiteSpace(Equipo.Clasificacion) && !ClasificacionesFiltradas.Contains(Equipo.Clasificacion))
                    ClasificacionesFiltradas.Add(Equipo.Clasificacion);
                if (!string.IsNullOrWhiteSpace(Equipo.CompradoA) && !CompradoADisponibles.Contains(Equipo.CompradoA))
                    CompradoADisponibles.Add(Equipo.CompradoA);
                if (!string.IsNullOrWhiteSpace(Equipo.CompradoA) && !CompradoAFiltrados.Contains(Equipo.CompradoA))
                    CompradoAFiltrados.Add(Equipo.CompradoA);
                if (!string.IsNullOrWhiteSpace(Equipo.Marca) && !MarcasDisponibles.Contains(Equipo.Marca))
                    MarcasDisponibles.Add(Equipo.Marca);
                if (!string.IsNullOrWhiteSpace(Equipo.Marca) && !MarcasFiltradas.Contains(Equipo.Marca))
                    MarcasFiltradas.Add(Equipo.Marca);

                // âœ… INICIALIZAR LOS FILTROS CON LOS VALORES ACTUALES
                // Esto prellenarÃ¡ los ComboBox editables cuando se edita un equipo
                _suppressFiltroMarcaChanged = true;
                filtroMarca = Equipo.Marca ?? string.Empty;
                RaisePropertyChanged(nameof(FiltroMarca));
                _suppressFiltroMarcaChanged = false;

                _suppressFiltroClasificacionChanged = true;
                filtroClasificacion = Equipo.Clasificacion ?? string.Empty;
                RaisePropertyChanged(nameof(FiltroClasificacion));
                _suppressFiltroClasificacionChanged = false;

                _suppressFiltroCompradoAChanged = true;
                filtroCompradoA = Equipo.CompradoA ?? string.Empty;
                RaisePropertyChanged(nameof(FiltroCompradoA));
                _suppressFiltroCompradoAChanged = false;
                
                // Cargar los mÃ¡s usados desde servicios registrados (si estÃ¡n disponibles)
                try
                {
                    var clasService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(ClasificacionAutocompletadoService)) as ClasificacionAutocompletadoService;
                    var compService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(CompradoAAutocompletadoService)) as CompradoAAutocompletadoService;
                    var marcaService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(MarcaAutocompletadoService)) as MarcaAutocompletadoService;
                    if (clasService != null)
                    {
                        var items = Task.Run(() => clasService.ObtenerMasUtilizadasAsync(50)).GetAwaiter().GetResult();
                        foreach (var it in items)
                        {
                            if (!ClasificacionesDisponibles.Contains(it)) ClasificacionesDisponibles.Add(it);
                            if (!ClasificacionesFiltradas.Contains(it)) ClasificacionesFiltradas.Add(it);
                        }
                    }
                    if (compService != null)
                    {
                        var items2 = Task.Run(() => compService.ObtenerMasUtilizadasAsync(50)).GetAwaiter().GetResult();
                        foreach (var it in items2)
                        {
                            if (!CompradoADisponibles.Contains(it)) CompradoADisponibles.Add(it);
                            if (!CompradoAFiltrados.Contains(it)) CompradoAFiltrados.Add(it);
                        }
                    }
                    if (marcaService != null)
                    {
                        var items3 = Task.Run(() => marcaService.ObtenerMasUtilizadasAsync(50)).GetAwaiter().GetResult();
                        foreach (var it in items3)
                        {
                            if (!MarcasDisponibles.Contains(it)) MarcasDisponibles.Add(it);
                            if (!MarcasFiltradas.Contains(it)) MarcasFiltradas.Add(it);
                        }
                    }
                }
                catch
                {
                    // ignorar fallos de carga de autocompletado
                }
            }

            private async Task DebounceFiltrarClasificacionesAsync(System.Threading.CancellationToken token)
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    await FiltrarClasificacionesAsync(token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch { }
            }

            private async Task DebounceFiltrarCompradoAAsync(System.Threading.CancellationToken token)
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    await FiltrarCompradoAAsync(token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch { }
            }

            private async Task DebounceFiltrarMarcasAsync(System.Threading.CancellationToken token)
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    await FiltrarMarcasAsync(token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch { }
            }

            private async Task FiltrarClasificacionesAsync(System.Threading.CancellationToken cancellationToken)
            {
                try
                {
                    var svc = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(ClasificacionAutocompletadoService)) as ClasificacionAutocompletadoService;
                    if (svc == null) return;
                    var filtroActual = FiltroClasificacion ?? string.Empty;
                    var items = await svc.BuscarAsync(filtroActual);
                    if (cancellationToken.IsCancellationRequested) return;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // PASO 1: Guardar el texto ANTES de cambiar ItemsSource
                            var textoPreservado = filtroClasificacion;

                            // PASO 2: Limpiar la colecciÃ³n filtrada
                            ClasificacionesFiltradas.Clear();

                            // PASO 3: AÃ±adir nuevos items
                            foreach (var it in items)
                                ClasificacionesFiltradas.Add(it);

                            // PASO 4: Forzar que el binding se actualice con el texto original
                            _suppressFiltroClasificacionChanged = true;
                            filtroClasificacion = textoPreservado;
                            RaisePropertyChanged(nameof(FiltroClasificacion));
                            _suppressFiltroClasificacionChanged = false;
                        }
                        catch { }
                    });
                }
                catch { }
            }

            private async Task FiltrarCompradoAAsync(System.Threading.CancellationToken cancellationToken)
            {
                try
                {
                    var svc = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(CompradoAAutocompletadoService)) as CompradoAAutocompletadoService;
                    if (svc == null) return;
                    var filtroActual = FiltroCompradoA ?? string.Empty;
                    var items = await svc.BuscarAsync(filtroActual);
                    if (cancellationToken.IsCancellationRequested) return;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // PASO 1: Guardar el texto ANTES de cambiar ItemsSource
                            var textoPreservado = filtroCompradoA;

                            // PASO 2: Limpiar la colecciÃ³n filtrada
                            CompradoAFiltrados.Clear();

                            // PASO 3: AÃ±adir nuevos items
                            foreach (var it in items)
                                CompradoAFiltrados.Add(it);

                            // PASO 4: Forzar que el binding se actualice con el texto original
                            _suppressFiltroCompradoAChanged = true;
                            filtroCompradoA = textoPreservado;
                            RaisePropertyChanged(nameof(FiltroCompradoA));
                            _suppressFiltroCompradoAChanged = false;
                        }
                        catch { }
                    });
                }
                catch { }
            }

            private Task FiltrarMarcasAsync(System.Threading.CancellationToken cancellationToken)
            {
                try
                {
                    var svc = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(MarcaAutocompletadoService)) as MarcaAutocompletadoService;
                    if (svc == null) return Task.CompletedTask;
                    var filtroActual = FiltroMarca ?? string.Empty;
                    
                    // Ejecutar bÃºsqueda de forma sincrÃ³nica para este mÃ©todo Task
                    var items = Task.Run(() => svc.BuscarAsync(filtroActual)).GetAwaiter().GetResult();
                    
                    if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // PASO 1: Guardar el texto ANTES de cambiar ItemsSource
                            var textoPreservado = filtroMarca;

                            // PASO 2: Limpiar la colecciÃ³n filtrada
                            MarcasFiltradas.Clear();

                            // PASO 3: AÃ±adir nuevos items
                            foreach (var it in items)
                                MarcasFiltradas.Add(it);

                            // PASO 4: Forzar que el binding se actualice con el texto original
                            _suppressFiltroMarcaChanged = true;
                            filtroMarca = textoPreservado;
                            RaisePropertyChanged(nameof(FiltroMarca));
                            _suppressFiltroMarcaChanged = false;
                        }
                        catch { }
                    });
                    return Task.CompletedTask;
                }
                catch { }
                return Task.CompletedTask;
            }            /// <summary>
            /// ðŸš€ Valida si el cÃ³digo actual es duplicado (comparando contra los cÃ³digos cargados en memoria)
            /// </summary>
            private void ValidarCodigoDuplicado()
            {
                // Si el cÃ³digo es nulo o vacÃ­o, no hay duplicado
                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    IsCodigoDuplicado = false;
                    System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] CÃ³digo vacÃ­o, no hay duplicado");
                    return;
                }

                // Si estamos en modo ediciÃ³n y el cÃ³digo es el mismo que el original, no es duplicado
                if (!string.IsNullOrWhiteSpace(_codigoOriginal) && 
                    Codigo.Equals(_codigoOriginal, System.StringComparison.OrdinalIgnoreCase))
                {
                    IsCodigoDuplicado = false;
                    System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] CÃ³digo original en ediciÃ³n, no hay duplicado");
                    return;
                }

                // Verificar en la lista de cÃ³digos existentes (O(1) con HashSet)
                bool isDuplicate = _codigosExistentes.Contains(Codigo);
                IsCodigoDuplicado = isDuplicate;
                System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] '{Codigo}' - Total cÃ³digos en BD: {_codigosExistentes.Count}, Es duplicado: {isDuplicate}");
            }            /// <summary>
            /// ðŸš€ Carga todos los cÃ³digos existentes de forma asincrÃ³nica
            /// Se llama al abrir el diÃ¡logo para cargar la lista de validaciÃ³n
            /// </summary>
            public async Task CargarCodigosExistentesAsync(IEquipoService equipoService, bool isEditMode)
            {
                try
                {
                    // Guardar el cÃ³digo original en modo ediciÃ³n
                    _codigoOriginal = isEditMode ? Codigo : null;
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Modo ediciÃ³n: {isEditMode}, CÃ³digo original: {_codigoOriginal}");

                    // Obtener todos los cÃ³digos de forma eficiente (solo SELECT Codigo)
                    var codigos = await equipoService.GetAllCodigosAsync();
                    var codigosList = codigos.ToList();
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Total de cÃ³digos obtenidos: {codigosList.Count}");

                    // Llenar el HashSet (case-insensitive)
                    _codigosExistentes.Clear();
                    foreach (var codigo in codigosList)
                    {
                        if (!string.IsNullOrWhiteSpace(codigo))
                        {
                            _codigosExistentes.Add(codigo);
                            System.Diagnostics.Debug.WriteLine($"  - CÃ³digo cargado: '{codigo}'");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] HashSet finalizado con {_codigosExistentes.Count} cÃ³digos");

                    // Validar el cÃ³digo actual
                    ValidarCodigoDuplicado();
                }
                catch (System.Exception ex)
                {
                    // Log detallado del error
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] âŒ Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Stack: {ex.StackTrace}");
                }
            }
        }
    }
}



