using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;     // ✅ NUEVO: IGestLogLogger
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Modules.Usuarios.Helpers;

namespace Modules.Usuarios.ViewModels
{    public class RolManagementViewModel : DatabaseAwareViewModel
    {        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        private ObservableCollection<Rol> _roles;        public ObservableCollection<Rol> Roles 
        { 
            get 
            {
                System.Diagnostics.Debug.WriteLine($"Roles getter called: Count = {_roles?.Count ?? -1}");
                return _roles ?? new ObservableCollection<Rol>(); 
            }
            set 
            { 
                System.Diagnostics.Debug.WriteLine($"Roles setter called: Old Count = {_roles?.Count ?? -1}, New Count = {value?.Count ?? -1}");
                _roles = value ?? new ObservableCollection<Rol>(); 
                OnPropertyChanged(); 
            } 
        }
        private Rol? _rolSeleccionado = null;
        public Rol? RolSeleccionado
        {
            get => _rolSeleccionado;
            set { _rolSeleccionado = value; OnPropertyChanged(); }
        }        private string _mensajeEstado = string.Empty;
        public string MensajeEstado
        {
            get => _mensajeEstado;
            set { _mensajeEstado = value; OnPropertyChanged(); }
        }

        // Propiedades de permisos
        private bool _canCreateRole;
        public bool CanCreateRole { get => _canCreateRole; set { _canCreateRole = value; OnPropertyChanged(); } }

        private bool _canEditRole;
        public bool CanEditRole { get => _canEditRole; set { _canEditRole = value; OnPropertyChanged(); } }

        private bool _canDeleteRole;
        public bool CanDeleteRole { get => _canDeleteRole; set { _canDeleteRole = value; OnPropertyChanged(); } }

        private bool _canViewRole;
        public bool CanViewRole { get => _canViewRole; set { _canViewRole = value; OnPropertyChanged(); } }

        private bool _canAssignPermissions;
        public bool CanAssignPermissions { get => _canAssignPermissions; set { _canAssignPermissions = value; OnPropertyChanged(); } }
        public ICommand RegistrarRolCommand { get; }
        public ICommand EditarRolCommand { get; }
        public ICommand DesactivarRolCommand { get; }
        public ICommand BuscarRolesCommand { get; }
        public ICommand AbrirNuevoRolCommand { get; }
        public ICommand AbrirEditarRolCommand { get; }
        public ICommand EliminarRolCommand { get; }
        public ICommand AbrirVerRolCommand { get; }        public RolManagementViewModel(
            IRolService rolService, 
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            System.Diagnostics.Debug.WriteLine("RolManagementViewModel: Constructor iniciado");
            _rolService = rolService ?? throw new ArgumentNullException(nameof(rolService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            _roles = new ObservableCollection<Rol>();
            _mensajeEstado = string.Empty;
            
            // Subscribirse a las notificaciones de la colección para debugging
            _roles.CollectionChanged += (sender, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"ObservableCollection CollectionChanged: Action = {e.Action}, NewItems = {e.NewItems?.Count ?? 0}");
            };
            
            System.Diagnostics.Debug.WriteLine($"RolManagementViewModel: ObservableCollection inicializada. Count = {_roles.Count}");            
            System.Diagnostics.Debug.WriteLine("RolManagementViewModel: Creando comandos");
            RegistrarRolCommand = new RelayCommand(async _ => await RegistrarRolAsync(), _ => CanCreateRole);
            EditarRolCommand = new RelayCommand(async _ => await EditarRolAsync(), _ => RolSeleccionado != null && CanEditRole);
            DesactivarRolCommand = new RelayCommand(async _ => await DesactivarRolAsync(), _ => RolSeleccionado != null && CanDeleteRole);
            BuscarRolesCommand = new RelayCommand(async _ => await BuscarRolesAsync(), _ => CanViewRole);
            
            AbrirNuevoRolCommand = new RelayCommand(_ => { AbrirNuevoRol(); return Task.CompletedTask; }, _ => CanCreateRole);
            AbrirEditarRolCommand = new RelayCommand(param => { if (param is Rol rol) AbrirEditarRol(rol); return Task.CompletedTask; }, _ => CanEditRole);
            EliminarRolCommand = new RelayCommand(async param => { if (param is Rol rol) await EliminarRolAsync(rol); }, _ => CanDeleteRole);
            AbrirVerRolCommand = new RelayCommand(async param => { if (param is Rol rol) await AbrirVerRolAsync(rol); }, _ => CanViewRole);
              GuardarModalRolCommand = new RelayCommand(async _ => await GuardarModalRolAsync(), _ => CanCreateRole || CanEditRole);
            CancelarModalRolCommand = new RelayCommand(_ => { IsModalRolVisible = false; MensajeValidacion = string.Empty; return Task.CompletedTask; }, _ => true);
            
            // Configurar permisos reactivos
            RecalcularPermisos();            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
            
            System.Diagnostics.Debug.WriteLine("RolManagementViewModel: Constructor completado");
            System.Diagnostics.Debug.WriteLine($"RolManagementViewModel: Roles collection inicializada con {Roles.Count} elementos");
        }

        private async Task RegistrarRolAsync()
        {
            MensajeEstado = string.Empty;
            try
            {
                var nuevoRol = new Rol
                {
                    Nombre = RolSeleccionado?.Nombre ?? string.Empty,
                    Descripcion = RolSeleccionado?.Descripcion ?? string.Empty
                };
                var rolCreado = await _rolService.CrearRolAsync(nuevoRol);
                Roles.Add(rolCreado);
                MensajeEstado = $"Rol '{rolCreado.Nombre}' creado exitosamente.";
            }
            catch (RolDuplicadoException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al crear rol: {ex.Message}";
            }
        }
        private async Task EditarRolAsync()
        {
            MensajeEstado = string.Empty;
            if (RolSeleccionado == null)
            {
                MensajeEstado = "Debe seleccionar un rol para editar.";
                return;
            }
            try
            {
                var rolEditado = await _rolService.EditarRolAsync(RolSeleccionado);
                var idx = Roles.IndexOf(RolSeleccionado);
                if (idx >= 0)
                    Roles[idx] = rolEditado;
                MensajeEstado = $"Rol '{rolEditado.Nombre}' editado exitosamente.";
            }
            catch (RolDuplicadoException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al editar rol: {ex.Message}";
            }
        }
        private async Task DesactivarRolAsync()
        {
            MensajeEstado = string.Empty;
            if (RolSeleccionado == null)
            {
                MensajeEstado = "Debe seleccionar un rol para eliminar.";
                return;
            }
            try
            {
                await _rolService.EliminarRolAsync(RolSeleccionado.IdRol);
                Roles.Remove(RolSeleccionado);
                MensajeEstado = $"Rol eliminado correctamente.";
                RolSeleccionado = null;
            }
            catch (RolNotFoundException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al eliminar rol: {ex.Message}";
            }
        }        private async Task BuscarRolesAsync()
        {
            MensajeEstado = string.Empty;
            try
            {
                var roles = await _rolService.ObtenerTodosAsync();
                System.Diagnostics.Debug.WriteLine($"Roles obtenidos del servicio: {roles.Count()}");
                
                // Asegurar que la actualización de la UI se haga en el hilo principal
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Roles.Clear();
                    int count = 0;
                    foreach (var rol in roles)
                    {
                        Roles.Add(rol);
                        System.Diagnostics.Debug.WriteLine($"Rol agregado a ObservableCollection: {rol.IdRol} - {rol.Nombre}");
                        count++;
                    }
                    System.Diagnostics.Debug.WriteLine($"Total roles en ObservableCollection: {Roles.Count}");
                    // Forzar notificación de PropertyChanged para la propiedad Roles
                    OnPropertyChanged(nameof(Roles));
                    MensajeEstado = $"Se cargaron {Roles.Count} roles.";
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MensajeEstado = $"Error al cargar roles: {ex.Message}";
                });
                System.Diagnostics.Debug.WriteLine($"Error al cargar roles: {ex.Message}");
            }
        }        // --- CRUD Modal State ---
        private bool _isModalRolVisible = false;
        public bool IsModalRolVisible
        {
            get => _isModalRolVisible;
            set { _isModalRolVisible = value; OnPropertyChanged(); }
        }
        private string _modalTitulo = "Nuevo Rol";
        public string ModalTitulo
        {
            get => _modalTitulo;
            set { _modalTitulo = value; OnPropertyChanged(); }
        }
        private Rol _rolEditado = new Rol { Nombre = string.Empty, Descripcion = string.Empty };
        public Rol RolEditado
        {
            get => _rolEditado;
            set { _rolEditado = value; OnPropertyChanged(); }
        }
        private string _mensajeValidacion = string.Empty;
        public string MensajeValidacion
        {
            get => _mensajeValidacion;
            set { _mensajeValidacion = value; OnPropertyChanged(); }
        }
        public ICommand GuardarModalRolCommand { get; }
        public ICommand CancelarModalRolCommand { get; }
        // --- CRUD Modal Logic ---
        public void AbrirNuevoRol()
        {
            ModalTitulo = "Nuevo Rol";
            RolEditado = new Rol { Nombre = string.Empty, Descripcion = string.Empty };
            MensajeValidacion = string.Empty;
            IsModalRolVisible = true;
        }
        public void AbrirEditarRol(Rol rol)
        {
            ModalTitulo = "Editar Rol";
            RolEditado = new Rol { IdRol = rol.IdRol, Nombre = rol.Nombre, Descripcion = rol.Descripcion };
            MensajeValidacion = string.Empty;
            IsModalRolVisible = true;
        }
        public async Task AbrirNuevoRolAsync(IPermisoService permisoService)
        {
            ModalTitulo = "Nuevo Rol";
            RolEditado = new Rol { Nombre = string.Empty, Descripcion = string.Empty };
            MensajeValidacion = string.Empty;
            PermisosSeleccionados.Clear();
            var permisos = await permisoService.ObtenerTodosAsync();
            CargarPermisosParaAsignacion(permisos);
            IsModalRolVisible = true;
        }
        public async Task AbrirEditarRolAsync(Rol rol, IPermisoService permisoService)
        {
            ModalTitulo = "Editar Rol";
            RolEditado = new Rol { IdRol = rol.IdRol, Nombre = rol.Nombre, Descripcion = rol.Descripcion };
            MensajeValidacion = string.Empty;
            var permisos = await permisoService.ObtenerTodosAsync();
            CargarPermisosParaAsignacion(permisos);
            PermisosSeleccionados.Clear();
            // Cargar permisos asignados al rol
            var asignados = await _rolService.ObtenerPermisosDeRolAsync(rol.IdRol);
            foreach (var p in asignados)
                PermisosSeleccionados.Add(p.IdPermiso);
            IsModalRolVisible = true;
        }
        // Permisos disponibles agrupados por módulo para la UI
        private ObservableCollection<PermisosModuloGroup> _permisosPorModulo = new();
        public ObservableCollection<PermisosModuloGroup> PermisosPorModulo
        {
            get => _permisosPorModulo;
            set { _permisosPorModulo = value; OnPropertyChanged(); }
        }
        // Permisos seleccionados para el rol
        private ObservableCollection<Guid> _permisosSeleccionados = new();
        public ObservableCollection<Guid> PermisosSeleccionados
        {
            get => _permisosSeleccionados;
            set { _permisosSeleccionados = value; OnPropertyChanged(); }
        }
        // Clase auxiliar para agrupar permisos por módulo
        public class PermisosModuloGroup
        {
            public string Modulo { get; set; } = string.Empty;
            public ObservableCollection<Permiso> Permisos { get; set; } = new();
        }
        public void CargarPermisosParaAsignacion(IEnumerable<Permiso> permisos)
        {
            var grupos = permisos
                .GroupBy(p => p.Modulo)
                .Select(g => new PermisosModuloGroup
                {
                    Modulo = g.Key,
                    Permisos = new ObservableCollection<Permiso>(g)
                });
            PermisosPorModulo = new ObservableCollection<PermisosModuloGroup>(grupos);
        }
        private async Task GuardarModalRolAsync()
        {
            MensajeValidacion = string.Empty;
            if (string.IsNullOrWhiteSpace(RolEditado.Nombre))
            {
                MensajeValidacion = "El nombre del rol es obligatorio.";
                return;
            }
            try
            {
                if (RolEditado.IdRol == Guid.Empty)
                {
                    // Crear nuevo rol
                    var rolCreado = await _rolService.CrearRolAsync(RolEditado);
                    await _rolService.AsignarPermisosARolAsync(rolCreado.IdRol, PermisosSeleccionados);
                    Roles.Add(rolCreado);
                    MensajeEstado = $"Rol '{rolCreado.Nombre}' creado exitosamente.";
                }
                else
                {
                    // Editar rol existente
                    var rolEditado = await _rolService.EditarRolAsync(RolEditado);
                    await _rolService.AsignarPermisosARolAsync(rolEditado.IdRol, PermisosSeleccionados);
                    var rolExistente = Roles.FirstOrDefault(r => r.IdRol == rolEditado.IdRol);
                    if (rolExistente != null)
                    {
                        var idx = Roles.IndexOf(rolExistente);
                        Roles[idx] = rolEditado;
                    }
                    MensajeEstado = $"Rol '{rolEditado.Nombre}' editado exitosamente.";
                }
                IsModalRolVisible = false;
            }
            catch (RolDuplicadoException ex)
            {
                MensajeValidacion = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeValidacion = $"Error: {ex.Message}";
            }
        }
        private async Task EliminarRolAsync(Rol rol)
        {
            MensajeEstado = string.Empty;
            if (rol == null)
            {
                MensajeEstado = "Debe seleccionar un rol para eliminar.";
                return;
            }
            var result = System.Windows.MessageBox.Show($"¿Está seguro que desea eliminar el rol '{rol.Nombre}'? Esta acción no se puede deshacer.", "Confirmar eliminación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes)
                return;
            try
            {
                await _rolService.EliminarRolAsync(rol.IdRol);
                Roles.Remove(rol);
                MensajeEstado = $"Rol eliminado correctamente.";
                if (RolSeleccionado?.IdRol == rol.IdRol)
                    RolSeleccionado = null;
            }
            catch (RolNotFoundException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al eliminar rol: {ex.Message}";
            }
        }
        // Modal de solo lectura para ver detalles
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
        private ObservableCollection<PermisosModuloGroup> _permisosPorModuloVer = new();
        public ObservableCollection<PermisosModuloGroup> PermisosPorModuloVer
        {
            get => _permisosPorModuloVer;
            set { _permisosPorModuloVer = value; OnPropertyChanged(); }
        }
        private async Task AbrirVerRolAsync(Rol rol)
        {
            if (rol == null) return;
            RolVerDetalle = rol;
            // Obtener permisos asignados
            var permisos = await _rolService.ObtenerPermisosDeRolAsync(rol.IdRol);
            // Agrupar por módulo
            var grupos = permisos
                .GroupBy(p => p.Modulo)
                .Select(g => new PermisosModuloGroup
                {
                    Modulo = g.Key,
                    Permisos = new ObservableCollection<Permiso>(g)
                });
            PermisosPorModuloVer = new ObservableCollection<PermisosModuloGroup>(grupos);            IsModalVerRolVisible = true;
        }

        // Métodos de gestión de permisos
        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            CanCreateRole = _currentUser.HasPermission("Roles.Crear");
            CanEditRole = _currentUser.HasPermission("Roles.Editar");
            CanDeleteRole = _currentUser.HasPermission("Roles.Eliminar");
            CanViewRole = _currentUser.HasPermission("Roles.Ver");
            CanAssignPermissions = _currentUser.HasPermission("Roles.AsignarPermisos");

            // Notificar cambios en comandos
            if (RegistrarRolCommand is RelayCommand registrarCmd) registrarCmd.RaiseCanExecuteChanged();
            if (EditarRolCommand is RelayCommand editarCmd) editarCmd.RaiseCanExecuteChanged();
            if (DesactivarRolCommand is RelayCommand desactivarCmd) desactivarCmd.RaiseCanExecuteChanged();
            if (BuscarRolesCommand is RelayCommand buscarCmd) buscarCmd.RaiseCanExecuteChanged();
            if (AbrirNuevoRolCommand is RelayCommand abrirNuevoCmd) abrirNuevoCmd.RaiseCanExecuteChanged();
            if (AbrirEditarRolCommand is RelayCommand abrirEditarCmd) abrirEditarCmd.RaiseCanExecuteChanged();
            if (EliminarRolCommand is RelayCommand eliminarCmd) eliminarCmd.RaiseCanExecuteChanged();            if (GuardarModalRolCommand is RelayCommand guardarCmd) guardarCmd.RaiseCanExecuteChanged();
            if (AbrirVerRolCommand is RelayCommand abrirVerCmd) abrirVerCmd.RaiseCanExecuteChanged();
        }

        // Implementación de RelayCommand local
        public class RelayCommand : ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Predicate<object?>? _canExecute;
            
            public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }
            
            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
            
            public async void Execute(object? parameter)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"RelayCommand.Execute iniciado para parámetro: {parameter}");
                    await _execute(parameter);
                    System.Diagnostics.Debug.WriteLine("RelayCommand.Execute completado exitosamente");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en RelayCommand.Execute: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    // No re-lanzar la excepción aquí para evitar crashear la UI
                }
            }
              public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogDebug("[RolManagementViewModel] Refrescando datos automáticamente");
                await BuscarRolesAsync();
                _logger.LogDebug("[RolManagementViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RolManagementViewModel] Error al refrescar datos");
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para roles
        /// </summary>
        protected override void OnConnectionLost()
        {
            MensajeEstado = "Sin conexión - Gestión de roles no disponible";
        }
    }
}
