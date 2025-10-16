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
        }

        private async void OnGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Forzar actualización del binding del DatePicker para asegurar que la propiedad FechaCompra del DTO esté actualizada
            try
            {
                var expression = FechaCompraPicker.GetBindingExpression(System.Windows.Controls.DatePicker.SelectedDateProperty);
                expression?.UpdateSource();
            }
            catch { }

            var errores = new List<string>();
            // Validaciones: solo Código es obligatorio; otras validaciones mínimas se mantienen
            if (string.IsNullOrWhiteSpace(Equipo.Codigo))
                errores.Add("El código del equipo es obligatorio.");

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

        public class EquipoDialogViewModel
        {
            public EquipoDto Equipo { get; set; }
            public IEnumerable<EstadoEquipo> EstadosEquipo { get; set; } = new EstadoEquipo[0];
            public IEnumerable<Sede> Sedes { get; set; } = new Sede[0];
            public IEnumerable<FrecuenciaMantenimiento> FrecuenciasMantenimiento { get; set; } = new FrecuenciaMantenimiento[0];
            // Proxy directo a las propiedades del EquipoDto para binding simple
            public string? Codigo { get => Equipo.Codigo; set => Equipo.Codigo = value; }
            public string? Nombre { get => Equipo.Nombre; set => Equipo.Nombre = value; }
            public string? Marca { get => Equipo.Marca; set => Equipo.Marca = value; }
            public EstadoEquipo? Estado { get => Equipo.Estado; set => Equipo.Estado = value; }
            public Sede? Sede { get => Equipo.Sede; set => Equipo.Sede = value; }
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

            public System.Collections.ObjectModel.ObservableCollection<string> ClasificacionesDisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();
            public System.Collections.ObjectModel.ObservableCollection<string> CompradoADisponibles { get; set; } = new System.Collections.ObjectModel.ObservableCollection<string>();

            // Debounce / cancelación y supresión de cambios
            private bool _suppressFiltroClasificacionChanged = false;
            private bool _suppressFiltroCompradoAChanged = false;
            private System.Threading.CancellationTokenSource? _clasificacionFilterCts;
            private System.Threading.CancellationTokenSource? _compradoAFilterCts;

            private string filtroClasificacion = string.Empty;
            public string FiltroClasificacion
            {
                get => filtroClasificacion;
                set
                {
                    if (_suppressFiltroClasificacionChanged)
                    {
                        filtroClasificacion = value ?? string.Empty;
                        return;
                    }

                    filtroClasificacion = value ?? string.Empty;

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
                        return;
                    }

                    filtroCompradoA = value ?? string.Empty;

                    _compradoAFilterCts?.Cancel();
                    _compradoAFilterCts?.Dispose();
                    _compradoAFilterCts = new System.Threading.CancellationTokenSource();
                    var token = _compradoAFilterCts.Token;

                    _ = DebounceFiltrarCompradoAAsync(token);
                }
            }

            public EquipoDialogViewModel(EquipoDto equipo)
            {
                Equipo = equipo;
                // Inicializar colecciones para autocompletado
                ClasificacionesDisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                CompradoADisponibles = new System.Collections.ObjectModel.ObservableCollection<string>();
                // Si existen valores iniciales en el DTO, asegurarlos en las listas
                if (!string.IsNullOrWhiteSpace(Equipo.Clasificacion) && !ClasificacionesDisponibles.Contains(Equipo.Clasificacion))
                    ClasificacionesDisponibles.Add(Equipo.Clasificacion);
                if (!string.IsNullOrWhiteSpace(Equipo.CompradoA) && !CompradoADisponibles.Contains(Equipo.CompradoA))
                    CompradoADisponibles.Add(Equipo.CompradoA);

                // Cargar los más usados desde servicios registrados (si están disponibles)
                try
                {
                    var clasService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(ClasificacionAutocompletadoService)) as ClasificacionAutocompletadoService;
                    var compService = ((App)System.Windows.Application.Current).ServiceProvider?.GetService(typeof(CompradoAAutocompletadoService)) as CompradoAAutocompletadoService;
                    if (clasService != null)
                    {
                        var items = Task.Run(() => clasService.ObtenerMasUtilizadasAsync(50)).GetAwaiter().GetResult();
                        foreach (var it in items)
                            if (!ClasificacionesDisponibles.Contains(it)) ClasificacionesDisponibles.Add(it);
                    }
                    if (compService != null)
                    {
                        var items2 = Task.Run(() => compService.ObtenerMasUtilizadasAsync(50)).GetAwaiter().GetResult();
                        foreach (var it in items2)
                            if (!CompradoADisponibles.Contains(it)) CompradoADisponibles.Add(it);
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
                        ClasificacionesDisponibles.Clear();
                        foreach (var it in items) ClasificacionesDisponibles.Add(it);

                        // Restaurar el texto sin disparar el handler
                        try
                        {
                            var textoPreservado = filtroActual ?? string.Empty;
                            _suppressFiltroClasificacionChanged = true;
                            FiltroClasificacion = textoPreservado;
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
                        CompradoADisponibles.Clear();
                        foreach (var it in items) CompradoADisponibles.Add(it);

                        try
                        {
                            var textoPreservado = filtroActual ?? string.Empty;
                            _suppressFiltroCompradoAChanged = true;
                            FiltroCompradoA = textoPreservado;
                            _suppressFiltroCompradoAChanged = false;
                        }
                        catch { }
                    });
                }
                catch { }
            }
        }
    }
}
