using System.Windows;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Services;

namespace GestLog.Views.Tools.GestionMantenimientos
{
    /// <summary>
    /// Lógica de interacción para EquipoDialog.xaml
    /// </summary>
    public partial class EquipoDialog : Window
    {
        public EquipoDto Equipo { get; private set; }
        public bool IsEditMode { get; }

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
            }
            DataContext = new EquipoDialogViewModel(Equipo)
            {
                EstadosEquipo = new[] { GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Activo, GestLog.Modules.GestionMantenimientos.Models.Enums.EstadoEquipo.Inactivo },
                Sedes = System.Enum.GetValues(typeof(Sede)) as Sede[] ?? new Sede[0],
                FrecuenciasMantenimiento = System.Enum.GetValues(typeof(FrecuenciaMantenimiento)) as FrecuenciaMantenimiento[] ?? new FrecuenciaMantenimiento[0]
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Establecer fecha máxima permitida en DatePicker para evitar fechas futuras
            try
            {
                FechaCompraPicker.DisplayDateEnd = DateTime.Today;
            }
            catch { }
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
            }            var errores = new List<string>();
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
        }        public class EquipoDialogViewModel : System.ComponentModel.INotifyPropertyChanged
        {
            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            protected void RaisePropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }

            public EquipoDto Equipo { get; set; }
            public IEnumerable<EstadoEquipo> EstadosEquipo { get; set; } = new EstadoEquipo[0];
            public IEnumerable<Sede> Sedes { get; set; } = new Sede[0];
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];            // Proxy directo a las propiedades del EquipoDto para binding simple
            public string? Codigo { get => Equipo.Codigo; set => Equipo.Codigo = value; }
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
            /// Indica si el campo Nombre está vacío (para mostrar error visual)
            /// </summary>
            public bool IsNombreVacio => string.IsNullOrWhiteSpace(Nombre);

            /// <summary>
            /// Indica si el campo Sede está vacío (para mostrar error visual)
            /// </summary>
            public bool IsSedeVacio => Sede == null;

            /// <summary>
            /// Indica si el formulario es válido para guardar (Nombre y Sede no pueden estar vacíos)
            /// </summary>
            public bool IsFormularioValido => !string.IsNullOrWhiteSpace(Nombre) && !string.IsNullOrWhiteSpace(Codigo) && Sede != null;

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
            private System.Threading.CancellationTokenSource? _marcaFilterCts;private string filtroClasificacion = string.Empty;
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
            }            private string filtroCompradoA = string.Empty;
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
            }            public EquipoDialogViewModel(EquipoDto equipo)
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
            }            private async Task DebounceFiltrarCompradoAAsync(System.Threading.CancellationToken token)
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
            }private async Task FiltrarClasificacionesAsync(System.Threading.CancellationToken cancellationToken)
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
            }            private async Task FiltrarCompradoAAsync(System.Threading.CancellationToken cancellationToken)
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
            }            private Task FiltrarMarcasAsync(System.Threading.CancellationToken cancellationToken)
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
            }
        }
    }
}
