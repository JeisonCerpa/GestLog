using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Linq;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Services;
using GestLog.Modules.GestionMantenimientos.Services.Autocomplete;
using GestLog.Modules.GestionMantenimientos.Interfaces;

namespace GestLog.Modules.GestionMantenimientos.Views.Equipos
{
    /// <summary>
    /// Lógica de interacción para EquipoDialog.xaml
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
                // Modo edición: clonar para no modificar el original hasta guardar
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
            // Establecer fecha máxima permitida en DatePicker para evitar fechas futuras
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

            // ✅ IMPORTANTE: No reasignar Owner aquí - ya se estableció en el constructor
            // ConfigurarParaVentanaPadre(this.Owner);
            if (this.DataContext is EquipoDialogViewModel viewModel)
            {
                try
                {
                    // Pequeño delay para asegurar que todo esté inicializado
                    await System.Threading.Tasks.Task.Delay(50);

                    // Obtener el servicio de equipos inyectado
                    var equipoService = ((App)System.Windows.Application.Current).ServiceProvider
                        ?.GetService(typeof(IEquipoService)) as IEquipoService;

                    if (equipoService != null)
                    {
                        // Cargar códigos de forma asincrónica sin bloquear la UI
                        await viewModel.CargarCodigosExistentesAsync(equipoService, IsEditMode);
                    }
                }
                catch (System.Exception ex)
                {
                    // Log del error (sin mostrar al usuario)
                    System.Diagnostics.Debug.WriteLine($"Error al cargar códigos existentes: {ex.Message}");
                }
            }
        }



        private void OnFechaCompra_SelectedDateChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (FechaCompraPicker.SelectedDate.HasValue && FechaCompraPicker.SelectedDate.Value.Date > DateTime.Today)
                {
                    System.Windows.MessageBox.Show("La fecha de compra no puede ser futura.", "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FechaCompraPicker.SelectedDate = null;
                }
            }
            catch { }
        }        private async void OnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Forzar actualización del binding del DatePicker para asegurar que la propiedad FechaCompra del DTO esté actualizada
            try
            {
                var expression = FechaCompraPicker.GetBindingExpression(System.Windows.Controls.DatePicker.SelectedDateProperty);
                expression?.UpdateSource();
            }
            catch { }

            // ✅ CRÍTICO: Sincronizar los valores de los ComboBox editables con el DTO
            // Los ComboBox editables están vinculados solo a FiltroMarca, FiltroClasificacion, FiltroCompradoA
            // Pero necesitamos actualizar Marca, Clasificacion, CompradoA con los valores del filtro
            var viewModel = DataContext as EquipoDialogViewModel;
            if (viewModel != null)
            {
                // Actualizar el EquipoDto con los valores actuales de los filtros
                Equipo.Marca = viewModel.FiltroMarca ?? string.Empty;
                Equipo.Clasificacion = viewModel.FiltroClasificacion ?? string.Empty;
                Equipo.CompradoA = viewModel.FiltroCompradoA ?? string.Empty;
                
                // ✅ NUEVO: Activar flag para mostrar errores de validación
                viewModel.ShowValidationErrors = true;
            }

            var errores = new List<string>();
            // Validaciones: Código y Nombre son obligatorios
            if (string.IsNullOrWhiteSpace(Equipo.Codigo))
                errores.Add("El código del equipo es obligatorio.");
            
            // ✅ NUEVA: Validar que el Nombre sea obligatorio
            if (string.IsNullOrWhiteSpace(Equipo.Nombre))
                errores.Add("El nombre del equipo es obligatorio.");

            // ✅ NUEVA: Validar que la Sede sea obligatoria
            if (Equipo.Sede == null)
                errores.Add("La sede del equipo es obligatoria.");

            if (Equipo.Precio != null && Equipo.Precio < 0)
                errores.Add("El precio no puede ser negativo.");

            if (Equipo.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.DadoDeBaja && !Equipo.FechaBaja.HasValue)
                errores.Add("Debe indicar la fecha de baja si el equipo está dado de baja.");

            // Validación de unicidad de código solo en alta
            if (!IsEditMode && !string.IsNullOrWhiteSpace(Equipo.Codigo))
            {
                var service = LoggingService.GetService<GestLog.Modules.GestionMantenimientos.Interfaces.IEquipoService>();
                var existente = await service.GetByCodigoAsync(Equipo.Codigo);
                if (existente != null)
                    errores.Add($"Ya existe un equipo con el código '{Equipo.Codigo}'.");
            }

            if (errores.Count > 0)
            {
                System.Windows.MessageBox.Show(string.Join("\n", errores), "Errores de validación", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                // Esto evita problemas de DPI, pantallas múltiples y posicionamiento
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
                    
                    // Detectar si el Owner cambió de pantalla
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    // Si cambió de pantalla, actualizar la referencia
                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
                    // En caso de error, asegurar que la ventana está maximizada
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
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];            // ✅ PROPIEDADES PARA VALIDACIÓN DE CÓDIGOS DUPLICADOS
            /// <summary>
            /// Conjunto de códigos existentes cargados para validación rápida en memoria
            /// </summary>
            private System.Collections.Generic.HashSet<string> _codigosExistentes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            
            /// <summary>
            /// El código original del equipo en modo edición (para ignorarlo en validación)
            /// </summary>
            private string? _codigoOriginal;

            /// <summary>
            /// Proxy directo a las propiedades del EquipoDto para binding simple
            /// Con validación de duplicados en tiempo real
            /// </summary>
            public string? Codigo 
            { 
                get => Equipo.Codigo; 
                set 
                { 
                    Equipo.Codigo = value;
                    // Validar códigos duplicados
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

            // Clasificación / CompradoA proxies: al modificar el texto también actualizamos el filtro para activar autocompletado
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

            // ✅ NUEVAS PROPIEDADES PARA VALIDACIÓN EN TIEMPO REAL
            /// <summary>
            /// Flag que controla si se deben mostrar los errores de validación.
            /// Se activa solo después de que el usuario intenta guardar.
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
            /// Indica si el campo Nombre está vacío Y se deben mostrar errores de validación
            /// </summary>
            public bool IsNombreVacio => _showValidationErrors && string.IsNullOrWhiteSpace(Nombre);

            /// <summary>
            /// Indica si el campo Sede está vacío Y se deben mostrar errores de validación
            /// </summary>
            public bool IsSedeVacio => _showValidationErrors && Sede == null;            /// <summary>
            /// Indica si el formulario es válido para guardar (Nombre y Sede no pueden estar vacíos, Codigo no duplicado)
            /// </summary>
            public bool IsFormularioValido => !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Codigo) && Sede != null && !IsCodigoDuplicado;

            /// <summary>
            /// 🚀 Indica si el código actual ya existe en la base de datos
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
            /// Mensaje de error cuando hay código duplicado
            /// </summary>
            public string MensajeCodigoDuplicado => IsCodigoDuplicado ? "⚠️ Este código ya existe en el sistema." : string.Empty;

            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoADisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoAFiltrados { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();

            // Debounce / cancelación y supresión de cambios
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
                        RaisePropertyChanged(nameof(FiltroClasificacion)); // ← CRÍTICO: Forzar PropertyChanged incluso cuando está suprimido
                        return;
                    }

                    filtroClasificacion = value ?? string.Empty;
                    RaisePropertyChanged(nameof(FiltroClasificacion));

                    // Cancelar búsqueda anterior y crear nueva CTS para debounce
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
                        RaisePropertyChanged(nameof(FiltroCompradoA)); // ← CRÍTICO: Forzar PropertyChanged incluso cuando está suprimido
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
                        RaisePropertyChanged(nameof(FiltroMarca)); // ← CRÍTICO: Forzar PropertyChanged incluso cuando está suprimido
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

                // ✅ INICIALIZAR LOS FILTROS CON LOS VALORES ACTUALES
                // Esto prellenará los ComboBox editables cuando se edita un equipo
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
                
                // Cargar los más usados desde servicios registrados (si están disponibles)
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

                            // PASO 2: Limpiar la colección filtrada
                            ClasificacionesFiltradas.Clear();

                            // PASO 3: Añadir nuevos items
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

                            // PASO 2: Limpiar la colección filtrada
                            CompradoAFiltrados.Clear();

                            // PASO 3: Añadir nuevos items
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
                    
                    // Ejecutar búsqueda de forma sincrónica para este método Task
                    var items = Task.Run(() => svc.BuscarAsync(filtroActual)).GetAwaiter().GetResult();
                    
                    if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // PASO 1: Guardar el texto ANTES de cambiar ItemsSource
                            var textoPreservado = filtroMarca;

                            // PASO 2: Limpiar la colección filtrada
                            MarcasFiltradas.Clear();

                            // PASO 3: Añadir nuevos items
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
            /// 🚀 Valida si el código actual es duplicado (comparando contra los códigos cargados en memoria)
            /// </summary>
            private void ValidarCodigoDuplicado()
            {
                // Si el código es nulo o vacío, no hay duplicado
                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    IsCodigoDuplicado = false;
                    System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] Código vacío, no hay duplicado");
                    return;
                }

                // Si estamos en modo edición y el código es el mismo que el original, no es duplicado
                if (!string.IsNullOrWhiteSpace(_codigoOriginal) && 
                    Codigo.Equals(_codigoOriginal, System.StringComparison.OrdinalIgnoreCase))
                {
                    IsCodigoDuplicado = false;
                    System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] Código original en edición, no hay duplicado");
                    return;
                }

                // Verificar en la lista de códigos existentes (O(1) con HashSet)
                bool isDuplicate = _codigosExistentes.Contains(Codigo);
                IsCodigoDuplicado = isDuplicate;
                System.Diagnostics.Debug.WriteLine($"[ValidarCodigoDuplicado] '{Codigo}' - Total códigos en BD: {_codigosExistentes.Count}, Es duplicado: {isDuplicate}");
            }            /// <summary>
            /// 🚀 Carga todos los códigos existentes de forma asincrónica
            /// Se llama al abrir el diálogo para cargar la lista de validación
            /// </summary>
            public async Task CargarCodigosExistentesAsync(IEquipoService equipoService, bool isEditMode)
            {
                try
                {
                    // Guardar el código original en modo edición
                    _codigoOriginal = isEditMode ? Codigo : null;
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Modo edición: {isEditMode}, Código original: {_codigoOriginal}");

                    // Obtener todos los códigos de forma eficiente (solo SELECT Codigo)
                    var codigos = await equipoService.GetAllCodigosAsync();
                    var codigosList = codigos.ToList();
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Total de códigos obtenidos: {codigosList.Count}");

                    // Llenar el HashSet (case-insensitive)
                    _codigosExistentes.Clear();
                    foreach (var codigo in codigosList)
                    {
                        if (!string.IsNullOrWhiteSpace(codigo))
                        {
                            _codigosExistentes.Add(codigo);
                            System.Diagnostics.Debug.WriteLine($"  - Código cargado: '{codigo}'");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] HashSet finalizado con {_codigosExistentes.Count} códigos");

                    // Validar el código actual
                    ValidarCodigoDuplicado();
                }
                catch (System.Exception ex)
                {
                    // Log detallado del error
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] ❌ Error: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[CargarCodigosExistentesAsync] Stack: {ex.StackTrace}");
                }
            }
        }
    }
}


