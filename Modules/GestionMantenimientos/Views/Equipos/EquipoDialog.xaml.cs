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
                Equipo = new EquipoDto(equipo);
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
                Equipo.Estado = GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo;
                IsEditMode = false;
            }

            DataContext = new EquipoDialogViewModel(Equipo)
            {
                EstadosEquipo = new[] { GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo, GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Inactivo },
                Sedes = System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0],
                FrecuenciasMantenimiento = (System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0])
                    .Where(f => f != FrecuenciaMantenimiento.Correctivo && f != FrecuenciaMantenimiento.Predictivo)
                    .ToArray()
            };

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
        }

        private async void EquipoDialog_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                FechaCompraPicker.DisplayDateEnd = DateTime.Today;
            }
            catch { }

            if (this.Owner != null)
            {
                this.Owner.LocationChanged += Owner_SizeOrLocationChanged;
                this.Owner.SizeChanged += Owner_SizeOrLocationChanged;
            }

            if (this.DataContext is EquipoDialogViewModel viewModel)
            {
                try
                {
                    await System.Threading.Tasks.Task.Delay(50);
                    var equipoService = ((App)System.Windows.Application.Current).ServiceProvider
                        ?.GetService(typeof(GestLog.Modules.GestionMantenimientos.Interfaces.Data.IEquipoService)) as GestLog.Modules.GestionMantenimientos.Interfaces.Data.IEquipoService;

                    if (equipoService != null)
                    {
                        await viewModel.CargarCodigosExistentesAsync(equipoService, IsEditMode);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar códigos: {ex.Message}");
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
        }

        private async void OnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var expression = FechaCompraPicker.GetBindingExpression(System.Windows.Controls.DatePicker.SelectedDateProperty);
                expression?.UpdateSource();
            }
            catch { }

            var viewModel = DataContext as EquipoDialogViewModel;
            if (viewModel != null)
            {
                Equipo.Marca = viewModel.FiltroMarca ?? string.Empty;
                Equipo.Clasificacion = viewModel.FiltroClasificacion ?? string.Empty;
                Equipo.CompradoA = viewModel.FiltroCompradoA ?? string.Empty;
                viewModel.ShowValidationErrors = true;
            }

            var errores = new List<string>();
            if (string.IsNullOrWhiteSpace(Equipo.Codigo))
                errores.Add("El código del equipo es obligatorio.");
            
            if (string.IsNullOrWhiteSpace(Equipo.Nombre))
                errores.Add("El nombre del equipo es obligatorio.");

            if (Equipo.Sede == null)
                errores.Add("La sede del equipo es obligatoria.");

            if (Equipo.Precio != null && Equipo.Precio < 0)
                errores.Add("El precio no puede ser negativo.");

            if (Equipo.Estado == GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.DadoDeBaja && !Equipo.FechaBaja.HasValue)
                errores.Add("Debe indicar la fecha de baja si el equipo está dado de baja.");

            if (!IsEditMode && !string.IsNullOrWhiteSpace(Equipo.Codigo))
            {
                var service = LoggingService.GetService<GestLog.Modules.GestionMantenimientos.Interfaces.Data.IEquipoService>();
                var equipo = await service.GetByCodigoAsync(Equipo.Codigo!);
                if (equipo != null)
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
                var interopHelper = new System.Windows.Interop.WindowInteropHelper(parentWindow);
                var screen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);
                _lastScreenOwner = screen;

                this.WindowState = WindowState.Maximized;
            }
            catch
            {
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
                    this.WindowState = WindowState.Maximized;
                    
                    var interopHelper = new System.Windows.Interop.WindowInteropHelper(this.Owner);
                    var currentScreen = System.Windows.Forms.Screen.FromHandle(interopHelper.Handle);

                    if (_lastScreenOwner == null || !_lastScreenOwner.DeviceName.Equals(currentScreen.DeviceName))
                    {
                        _lastScreenOwner = currentScreen;
                    }
                }
                catch
                {
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
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];

            private System.Collections.Generic.HashSet<string> _codigosExistentes = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            
            private string? _codigoOriginal;

            public string? Codigo 
            { 
                get => Equipo.Codigo; 
                set 
                { 
                    Equipo.Codigo = value;
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
            public DateTime? FechaCompra { get => Equipo.FechaCompra; set => Equipo.FechaCompra = value; }
            public DateTime? FechaRegistro { get => Equipo.FechaRegistro; set => Equipo.FechaRegistro = value; }
            public DateTime? FechaBaja { get => Equipo.FechaBaja; set => Equipo.FechaBaja = value; }
            public bool IsCodigoReadOnly { get => Equipo.IsCodigoReadOnly; set => Equipo.IsCodigoReadOnly = value; }
            public bool IsCodigoEnabled { get => Equipo.IsCodigoEnabled; set => Equipo.IsCodigoEnabled = value; }

            public string? Clasificacion
            {
                get => Equipo.Clasificacion;
                set
                {
                    Equipo.Clasificacion = value;
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

            public bool IsNombreVacio => _showValidationErrors && string.IsNullOrWhiteSpace(Nombre);
            public bool IsSedeVacio => _showValidationErrors && Sede == null;
            public bool IsFormularioValido => !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Codigo) && Sede != null && !IsCodigoDuplicado;

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

            public string MensajeCodigoDuplicado => IsCodigoDuplicado ? "⚠️ Este código ya existe en el sistema." : string.Empty;

            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoADisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoAFiltrados { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> MarcasFiltradas { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();

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
                        RaisePropertyChanged(nameof(FiltroClasificacion));
                        return;
                    }

                    filtroClasificacion = value ?? string.Empty;
                    RaisePropertyChanged(nameof(FiltroClasificacion));

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
                        RaisePropertyChanged(nameof(FiltroCompradoA));
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
                        RaisePropertyChanged(nameof(FiltroMarca));
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
                ClasificacionesDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                ClasificacionesFiltradas = new System.Collections.ObjectModel.ObservableCollection<string>();
                CompradoADisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                CompradoAFiltrados = new System.Collections.ObjectModel.ObservableCollection<string>();
                MarcasDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                MarcasFiltradas = new System.Collections.ObjectModel.ObservableCollection<string>();
                
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
            }

            private async Task DebounceFiltrarClasificacionesAsync(System.Threading.CancellationToken token)
            {
                try
                {
                    await Task.Delay(250, token);
                    if (token.IsCancellationRequested) return;
                    await FiltrarClasificacionesAsync(token);
                }
                catch (OperationCanceledException) { }
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
                catch (OperationCanceledException) { }
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
                catch (OperationCanceledException) { }
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
                            var textoPreservado = filtroClasificacion;
                            ClasificacionesFiltradas.Clear();
                            foreach (var it in items)
                                ClasificacionesFiltradas.Add(it);

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
                            var textoPreservado = filtroCompradoA;
                            CompradoAFiltrados.Clear();
                            foreach (var it in items)
                                CompradoAFiltrados.Add(it);

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
                    
                    var items = Task.Run(() => svc.BuscarAsync(filtroActual)).GetAwaiter().GetResult();
                    
                    if (cancellationToken.IsCancellationRequested) return Task.CompletedTask;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            var textoPreservado = filtroMarca;
                            MarcasFiltradas.Clear();
                            foreach (var it in items)
                                MarcasFiltradas.Add(it);

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
            }

            private void ValidarCodigoDuplicado()
            {
                if (string.IsNullOrWhiteSpace(Codigo))
                {
                    IsCodigoDuplicado = false;
                    return;
                }

                if (!string.IsNullOrWhiteSpace(_codigoOriginal) && 
                    Codigo.Equals(_codigoOriginal, System.StringComparison.OrdinalIgnoreCase))
                {
                    IsCodigoDuplicado = false;
                    return;
                }

                bool isDuplicate = _codigosExistentes.Contains(Codigo);
                IsCodigoDuplicado = isDuplicate;
            }

            public async Task CargarCodigosExistentesAsync(GestLog.Modules.GestionMantenimientos.Interfaces.Data.IEquipoService equipoService, bool isEditMode)
            {
                try
                {
                    _codigoOriginal = isEditMode ? Codigo : null;

                    var codigos = await equipoService.GetAllCodigosAsync();
                    var codigosList = codigos.ToList();

                    _codigosExistentes.Clear();
                    foreach (var codigo in codigosList)
                    {
                        if (!string.IsNullOrWhiteSpace(codigo))
                        {
                            _codigosExistentes.Add(codigo);
                        }
                    }

                    ValidarCodigoDuplicado();
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}



