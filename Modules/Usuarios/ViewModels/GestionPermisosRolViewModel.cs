using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
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
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

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
        }        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        // Propiedades de permisos
        private bool _canViewRole;
        public bool CanViewRole { get => _canViewRole; set { _canViewRole = value; OnPropertyChanged(); } }

        private bool _canEditRole;
        public bool CanEditRole { get => _canEditRole; set { _canEditRole = value; OnPropertyChanged(); } }

        private bool _canDeleteRole;
        public bool CanDeleteRole { get => _canDeleteRole; set { _canDeleteRole = value; OnPropertyChanged(); } }

        private bool _canAssignPermissions;
        public bool CanAssignPermissions { get => _canAssignPermissions; set { _canAssignPermissions = value; OnPropertyChanged(); } }

        // === COMANDOS ===
        public ICommand CargarRolesCommand { get; }
        public ICommand CargarPermisosDelRolCommand { get; }
        public ICommand GuardarAsignacionesCommand { get; }
        public ICommand AbrirVerRolCommand { get; }
        public ICommand CerrarVerRolCommand { get; }
        
        // Comandos faltantes para los botones en la vista
        public ICommand SeleccionarRolCommand { get; }
        public ICommand AbrirEditarRolCommand { get; }
        public ICommand EliminarRolCommand { get; }

        // === PROPIEDADES PARA VER DETALLES DE ROL ===
        private bool _isModalVerRolVisible = false;
        public bool IsModalVerRolVisible
        {
            get => _isModalVerRolVisible;
            set { _isModalVerRolVisible = value; OnPropertyChanged(); }
        }
        private Rol? _rolVerDetalle;
        public Rol? RolVerDetalle
        {
            get => _rolVerDetalle;
            set { _rolVerDetalle = value; OnPropertyChanged(); }
        }
        private ObservableCollection<ModuloPermisos> _modulosPermisosVer = new();
        public ObservableCollection<ModuloPermisos> ModulosPermisosVer
        {
            get => _modulosPermisosVer;
            set { _modulosPermisosVer = value; OnPropertyChanged(); }
        }        // === CONSTRUCTOR ===
        public GestionPermisosRolViewModel(IRolService rolService, IPermisoService permisoService, IGestLogLogger logger, ICurrentUserService currentUserService)
        {
            _rolService = rolService ?? throw new ArgumentNullException(nameof(rolService));
            _permisoService = permisoService ?? throw new ArgumentNullException(nameof(permisoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };            // Inicializar comandos
            CargarRolesCommand = new AsyncRelayCommand(CargarRolesAsync);
            CargarPermisosDelRolCommand = new AsyncRelayCommand(CargarPermisosDelRolAsync);
            GuardarAsignacionesCommand = new AsyncRelayCommand(GuardarAsignacionesAsync, () => CanAssignPermissions);
            AbrirVerRolCommand = new AsyncRelayCommand<Rol>(AbrirVerRolAsync, _ => CanViewRole);
            CerrarVerRolCommand = new RelayCommand(() => IsModalVerRolVisible = false);
            
            // Comandos faltantes para los botones en la vista
            SeleccionarRolCommand = new RelayCommand<Rol>(SeleccionarRol, _ => CanViewRole);
            AbrirEditarRolCommand = new RelayCommand<Rol>(AbrirEditarRol, _ => CanEditRole);
            EliminarRolCommand = new AsyncRelayCommand<Rol>(EliminarRolAsync, _ => CanDeleteRole);

            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

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

        private async Task AbrirVerRolAsync(Rol? rol)
        {
            if (rol == null) return;
            RolVerDetalle = rol;
            IsLoading = true;
            try
            {
                // Obtener permisos asignados
                var permisosAsignados = await _rolService.ObtenerPermisosDeRolAsync(rol.IdRol);
                // Agrupar por módulo
                var modulosAgrupados = permisosAsignados
                    .GroupBy(p => p.Modulo)
                    .Select(g => {
                        var modulo = new ModuloPermisos
                        {
                            NombreModulo = g.Key
                        };
                        modulo.Permisos = new ObservableCollection<PermisoViewModel>(
                            g.Select(p => new PermisoViewModel
                            {
                                Permiso = p,
                                EstaAsignado = true,
                                ParentModulo = modulo
                            })
                        );
                        return modulo;
                    })
                    .OrderBy(m => m.NombreModulo)
                    .ToList();
                ModulosPermisosVer = new ObservableCollection<ModuloPermisos>(modulosAgrupados);
                IsModalVerRolVisible = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // === MÉTODOS DE COMANDOS ADICIONALES ===
        private void SeleccionarRol(Rol? rol)
        {
            if (rol != null)
            {
                foreach (var r in Roles)
                    r.IsSelected = false;
                rol.IsSelected = true;
                RolSeleccionado = rol;
            }
        }

        private void AbrirEditarRol(Rol? rol)
        {
            if (rol == null) return;
            
            // Aquí se podría abrir una ventana de edición o cambiar a otra vista
            // Por ahora solo mostramos un mensaje de estado
            MensajeEstado = $"Función de edición para rol '{rol.Nombre}' no implementada aún.";
            _logger.LogInformation($"Solicitada edición de rol: {rol.Nombre} ({rol.IdRol})");
        }        private async Task EliminarRolAsync(Rol? rol)
        {
            if (rol == null) return;

            try
            {
                IsLoading = true;
                MensajeEstado = $"Eliminando rol '{rol.Nombre}'...";

                // Aquí se implementaría la lógica de eliminación
                // await _rolService.EliminarRolAsync(rol.IdRol);
                
                // Simular operación async mientras no esté implementada
                await Task.Delay(500);
                
                // Por ahora solo mostramos un mensaje
                MensajeEstado = $"Función de eliminación para rol '{rol.Nombre}' no implementada aún.";
                _logger.LogInformation($"Solicitada eliminación de rol: {rol.Nombre} ({rol.IdRol})");
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al eliminar rol: {ex.Message}";
                _logger.LogError(ex, $"Error eliminando rol {rol?.IdRol}");
            }
            finally
            {
                IsLoading = false;            }
        }

        // === MÉTODOS DE GESTIÓN DE PERMISOS ===
        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            CanViewRole = _currentUser.HasPermission("Roles.Ver");
            CanEditRole = _currentUser.HasPermission("Roles.Editar");
            CanDeleteRole = _currentUser.HasPermission("Roles.Eliminar");
            CanAssignPermissions = _currentUser.HasPermission("Roles.AsignarPermisos");

            // Notificar cambios en comandos que implementan IRelayCommand
            if (GuardarAsignacionesCommand is IRelayCommand guardarCmd) guardarCmd.NotifyCanExecuteChanged();
            if (AbrirVerRolCommand is IRelayCommand abrirVerCmd) abrirVerCmd.NotifyCanExecuteChanged();
            if (SeleccionarRolCommand is IRelayCommand seleccionarCmd) seleccionarCmd.NotifyCanExecuteChanged();
            if (AbrirEditarRolCommand is IRelayCommand abrirEditarCmd) abrirEditarCmd.NotifyCanExecuteChanged();
            if (EliminarRolCommand is IRelayCommand eliminarCmd) eliminarCmd.NotifyCanExecuteChanged();
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
