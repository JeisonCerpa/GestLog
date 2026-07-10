using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Roles
{
    /// <summary>
    /// Ventana modal unificada para crear y editar roles (rol null = crear).
    /// </summary>
    public partial class RolFormWindow : Window
    {
        private readonly RolFormViewModel _viewModel;

        public RolFormWindow(Rol? rol = null)
        {
            InitializeComponent();
            _viewModel = new RolFormViewModel(rol);
            DataContext = _viewModel;

            try
            {
                Owner = System.Windows.Application.Current?.MainWindow;
                WindowState = WindowState.Maximized;
            }
            catch
            {
                // No crítico
            }

            Loaded += async (s, e) => await _viewModel.CargarPermisosAsync();
        }

        private async void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (await _viewModel.GuardarAsync())
            {
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Overlay_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Panel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }

    public class RolFormViewModel : INotifyPropertyChanged
    {
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IGestLogLogger _logger;
        private readonly Rol? _rolOriginal; // null = crear

        public bool EsEdicion => _rolOriginal != null;
        public string Titulo => EsEdicion ? "Editar Rol" : "Nuevo Rol";
        public string Subtitulo => EsEdicion
            ? "Modifique la información del rol y sus permisos asignados"
            : "Defina el nombre, la descripción y los permisos del nuevo rol";
        public string TextoGuardar => EsEdicion ? "Guardar Cambios" : "Crear Rol";

        private string _nombre = string.Empty;
        public string Nombre
        {
            get => _nombre;
            set { _nombre = value; OnPropertyChanged(); }
        }

        private string _descripcion = string.Empty;
        public string Descripcion
        {
            get => _descripcion;
            set { _descripcion = value; OnPropertyChanged(); }
        }

        private string _mensajeValidacion = string.Empty;
        public string MensajeValidacion
        {
            get => _mensajeValidacion;
            set { _mensajeValidacion = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ModuloPermisosInfo> _permisosPorModulo = new();
        public ObservableCollection<ModuloPermisosInfo> PermisosPorModulo
        {
            get => _permisosPorModulo;
            set { _permisosPorModulo = value; OnPropertyChanged(); }
        }

        // Lista completa sin filtrar; los checkboxes comparten instancias, así que el estado se conserva al filtrar
        private List<ModuloPermisosInfo> _gruposCompletos = new();

        private string _filtroPermisos = string.Empty;
        public string FiltroPermisos
        {
            get => _filtroPermisos;
            set { _filtroPermisos = value; OnPropertyChanged(); AplicarFiltroPermisos(); }
        }

        private void AplicarFiltroPermisos()
        {
            if (string.IsNullOrWhiteSpace(_filtroPermisos))
            {
                PermisosPorModulo = new ObservableCollection<ModuloPermisosInfo>(_gruposCompletos);
                return;
            }
            var filtrados = _gruposCompletos
                .Select(g => new ModuloPermisosInfo
                {
                    Modulo = g.Modulo,
                    Permisos = new ObservableCollection<PermisoSeleccionInfo>(
                        g.Modulo.Contains(_filtroPermisos, StringComparison.OrdinalIgnoreCase)
                            ? g.Permisos
                            : g.Permisos.Where(p =>
                                p.Nombre.Contains(_filtroPermisos, StringComparison.OrdinalIgnoreCase) ||
                                p.Descripcion.Contains(_filtroPermisos, StringComparison.OrdinalIgnoreCase)))
                })
                .Where(g => g.Permisos.Count > 0);
            PermisosPorModulo = new ObservableCollection<ModuloPermisosInfo>(filtrados);
        }

        public RolFormViewModel(Rol? rol)
        {
            _rolOriginal = rol;
            Nombre = rol?.Nombre ?? string.Empty;
            Descripcion = rol?.Descripcion ?? string.Empty;

            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                _rolService = serviceProvider.GetRequiredService<IRolService>();
                _permisoService = serviceProvider.GetRequiredService<IPermisoService>();
                _logger = serviceProvider.GetRequiredService<IGestLogLogger>();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al inicializar servicios: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        public async Task CargarPermisosAsync()
        {
            try
            {
                var todosLosPermisos = await _permisoService.ObtenerTodosAsync();
                var idsAsignados = _rolOriginal == null
                    ? new HashSet<Guid>()
                    : (await _rolService.ObtenerPermisosDeRolAsync(_rolOriginal.IdRol)).Select(p => p.IdPermiso).ToHashSet();

                var grupos = todosLosPermisos
                    .GroupBy(p => p.Modulo)
                    .Select(g => new ModuloPermisosInfo
                    {
                        Modulo = g.Key,
                        Permisos = new ObservableCollection<PermisoSeleccionInfo>(
                            g.Select(p => new PermisoSeleccionInfo
                            {
                                IdPermiso = p.IdPermiso,
                                Nombre = p.Nombre,
                                Descripcion = p.Descripcion,
                                EstaSeleccionado = idsAsignados.Contains(p.IdPermiso)
                            })
                        )
                    })
                    .OrderBy(m => m.Modulo);

                _gruposCompletos = grupos.ToList();
                AplicarFiltroPermisos();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando permisos para el formulario de rol");
                MensajeValidacion = "Error al cargar permisos disponibles";
            }
        }

        public async Task<bool> GuardarAsync()
        {
            MensajeValidacion = string.Empty;

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeValidacion = "El nombre del rol es obligatorio";
                return false;
            }

            try
            {
                var rol = new Rol
                {
                    IdRol = _rolOriginal?.IdRol ?? Guid.Empty,
                    Nombre = Nombre.Trim(),
                    Descripcion = Descripcion?.Trim() ?? string.Empty
                };

                Guid idRol;
                if (EsEdicion)
                {
                    await _rolService.EditarRolAsync(rol);
                    idRol = rol.IdRol;
                }
                else
                {
                    var rolCreado = await _rolService.CrearRolAsync(rol);
                    idRol = rolCreado.IdRol;
                }

                // Leer de la lista completa: con un filtro activo, PermisosPorModulo solo contiene los visibles
                var permisosSeleccionados = _gruposCompletos
                    .SelectMany(m => m.Permisos)
                    .Where(p => p.EstaSeleccionado)
                    .Select(p => p.IdPermiso)
                    .ToList();

                // En edición siempre se sincroniza (permite quitar todos los permisos)
                if (EsEdicion || permisosSeleccionados.Any())
                    await _rolService.AsignarPermisosARolAsync(idRol, permisosSeleccionados);

                _logger.LogInformation($"Rol {(EsEdicion ? "editado" : "creado")} exitosamente: {rol.Nombre} con {permisosSeleccionados.Count} permisos");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar rol");
                MensajeValidacion = $"Error al guardar rol: {ex.Message}";
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ModuloPermisosInfo
    {
        public string Modulo { get; set; } = string.Empty;
        public ObservableCollection<PermisoSeleccionInfo> Permisos { get; set; } = new();
    }

    public class PermisoSeleccionInfo : INotifyPropertyChanged
    {
        public Guid IdPermiso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;

        private bool _estaSeleccionado;
        public bool EstaSeleccionado
        {
            get => _estaSeleccionado;
            set { _estaSeleccionado = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
