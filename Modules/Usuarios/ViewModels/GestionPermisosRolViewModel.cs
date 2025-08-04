using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace GestLog.Modules.Usuarios.ViewModels
{
    /// <summary>
    /// ViewModel para gestionar la asignación de permisos a roles de forma visual
    /// </summary>
    public class GestionPermisosRolViewModel : INotifyPropertyChanged
    {
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IGestLogLogger _logger;

        // === PROPIEDADES PRINCIPALES ===
        private ObservableCollection<Rol> _roles = new();
        public ObservableCollection<Rol> Roles
        {
            get => _roles;
            set { _roles = value; OnPropertyChanged(); }
        }

        private Rol? _rolSeleccionado;
        public Rol? RolSeleccionado
        {
            get => _rolSeleccionado;
            set 
            { 
                _rolSeleccionado = value; 
                OnPropertyChanged();
                if (_rolSeleccionado != null)
                {
                    _ = CargarPermisosDelRolAsync();
                }
            }
        }

        private ObservableCollection<ModuloPermisos> _modulosPermisos = new();
        public ObservableCollection<ModuloPermisos> ModulosPermisos
        {
            get => _modulosPermisos;
            set { _modulosPermisos = value; OnPropertyChanged(); }
        }

        private string _mensajeEstado = "Selecciona un rol para comenzar...";
        public string MensajeEstado
        {
            get => _mensajeEstado;
            set { _mensajeEstado = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // === COMANDOS ===
        public ICommand CargarRolesCommand { get; }
        public ICommand CargarPermisosDelRolCommand { get; }
        public ICommand GuardarAsignacionesCommand { get; }

        // === CONSTRUCTOR ===
        public GestionPermisosRolViewModel(IRolService rolService, IPermisoService permisoService, IGestLogLogger logger)
        {
            _rolService = rolService ?? throw new ArgumentNullException(nameof(rolService));
            _permisoService = permisoService ?? throw new ArgumentNullException(nameof(permisoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Inicializar comandos
            CargarRolesCommand = new AsyncRelayCommand(CargarRolesAsync);
            CargarPermisosDelRolCommand = new AsyncRelayCommand(CargarPermisosDelRolAsync);
            GuardarAsignacionesCommand = new AsyncRelayCommand(GuardarAsignacionesAsync);

            // Cargar roles al inicializar
            _ = CargarRolesAsync();
        }

        // === MÉTODOS PRINCIPALES ===
        private async Task CargarRolesAsync()
        {
            try
            {
                IsLoading = true;
                MensajeEstado = "Cargando roles...";

                var roles = await _rolService.ObtenerTodosAsync();

                // Limpiar y agregar roles en el hilo de la UI
                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == true)
                {
                    Roles.Clear();
                    foreach (var rol in roles)
                        Roles.Add(rol);
                }
                else
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() => {
                        Roles.Clear();
                        foreach (var rol in roles)
                            Roles.Add(rol);
                    });
                }

                MensajeEstado = $"Se cargaron {Roles.Count} roles.";
                _logger.LogInformation($"Roles cargados: {Roles.Count}");
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar roles: {ex.Message}";
                _logger.LogError(ex, "Error cargando roles");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CargarPermisosDelRolAsync()
        {
            if (RolSeleccionado == null) return;

            try
            {
                IsLoading = true;
                MensajeEstado = $"Cargando permisos para '{RolSeleccionado.Nombre}'...";

                // Obtener todos los permisos y los permisos asignados al rol
                var todosLosPermisos = await _permisoService.ObtenerTodosAsync();
                var permisosAsignados = await _rolService.ObtenerPermisosDeRolAsync(RolSeleccionado.IdRol);

                // Agrupar permisos por módulo
                var modulosAgrupados = todosLosPermisos
                    .GroupBy(p => p.Modulo)
                    .Select(g => {
                        var modulo = new ModuloPermisos
                        {
                            NombreModulo = g.Key
                        };
                        modulo.Permisos = new ObservableCollection<PermisoViewModel>(
                            g.Select(p => {
                                var permisoVM = new PermisoViewModel
                                {
                                    Permiso = p,
                                    EstaAsignado = permisosAsignados.Any(pa => pa.IdPermiso == p.IdPermiso),
                                    ParentModulo = modulo
                                };
                                return permisoVM;
                            })
                        );
                        return modulo;
                    })
                    .OrderBy(m => m.NombreModulo)
                    .ToList();

                // Actualizar la colección de módulos
                ModulosPermisos.Clear();
                foreach (var modulo in modulosAgrupados)
                {
                    ModulosPermisos.Add(modulo);
                }

                MensajeEstado = $"Permisos cargados para '{RolSeleccionado.Nombre}' - {ModulosPermisos.Count} módulos.";
                _logger.LogInformation($"Permisos cargados para rol {RolSeleccionado.IdRol}: {ModulosPermisos.Count} módulos");
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar permisos: {ex.Message}";
                _logger.LogError(ex, $"Error cargando permisos para rol {RolSeleccionado?.IdRol}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GuardarAsignacionesAsync()
        {
            if (RolSeleccionado == null) return;

            try
            {
                IsLoading = true;
                MensajeEstado = "Guardando asignaciones de permisos...";

                // Obtener permisos seleccionados
                var permisosAsignados = ModulosPermisos
                    .SelectMany(m => m.Permisos)
                    .Where(p => p.EstaAsignado)
                    .Select(p => p.Permiso.IdPermiso)
                    .ToList();

                // Guardar en el servicio
                await _rolService.AsignarPermisosARolAsync(RolSeleccionado.IdRol, permisosAsignados);

                MensajeEstado = $"Permisos guardados exitosamente para '{RolSeleccionado.Nombre}'.";
                _logger.LogInformation($"Permisos guardados para rol {RolSeleccionado.IdRol}: {permisosAsignados.Count} permisos");
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al guardar permisos: {ex.Message}";
                _logger.LogError(ex, $"Error guardando permisos para rol {RolSeleccionado?.IdRol}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // === IMPLEMENTACIÓN DE INotifyPropertyChanged ===
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // === CLASES AUXILIARES ===
    public class ModuloPermisos : INotifyPropertyChanged
    {
        public string NombreModulo { get; set; } = string.Empty;
        public ObservableCollection<PermisoViewModel> Permisos { get; set; } = new();

        private bool _notificando;

        public ModuloPermisos()
        {
            Permisos.CollectionChanged += (s, e) => SuscribirPermisos();
            SuscribirPermisos();
        }

        private void SuscribirPermisos()
        {
            foreach (var p in Permisos)
            {
                p.PropertyChanged -= Permiso_PropertyChanged;
                p.PropertyChanged += Permiso_PropertyChanged;
            }
        }

        private void Permiso_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PermisoViewModel.EstaAsignado) && !_notificando)
            {
                OnPropertyChanged(nameof(TodosAsignados));
            }
        }

        // Propiedad para el estado visual del toggle (true, false, null)
        public bool? TodosAsignados
        {
            get
            {
                if (!Permisos.Any()) return false;
                if (Permisos.All(p => p.EstaAsignado)) return true;
                if (Permisos.All(p => !p.EstaAsignado)) return false;
                return null; // Indeterminado
            }
            set
            {
                if (_notificando) return;
                
                // Lógica simple: si todos están activos, desactivar; sino, activar
                bool nuevoEstado;
                var estadoActual = Permisos.Any() && Permisos.All(p => p.EstaAsignado);
                nuevoEstado = !estadoActual;

                _notificando = true;
                foreach (var p in Permisos)
                {
                    p.EstaAsignado = nuevoEstado;
                }
                _notificando = false;
                
                OnPropertyChanged(nameof(TodosAsignados));
            }
        }

        public void NotificarTodosAsignados()
        {
            if (!_notificando)
                OnPropertyChanged(nameof(TodosAsignados));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PermisoViewModel : INotifyPropertyChanged
    {
        public Permiso Permiso { get; set; } = new()
        {
            Nombre = string.Empty,
            Descripcion = string.Empty,
            Modulo = string.Empty
        };

        private bool _estaAsignado;
        public bool EstaAsignado
        {
            get => _estaAsignado;
            set
            {
                if (_estaAsignado != value)
                {
                    _estaAsignado = value;
                    OnPropertyChanged();
                    // Notificar al padre si existe
                    ParentModulo?.NotificarTodosAsignados();
                }
            }
        }

        // Referencia al módulo padre para notificación explícita
        public ModuloPermisos? ParentModulo { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
