using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using Modules.Usuarios.Interfaces;
using Modules.Usuarios.Helpers;

namespace Modules.Usuarios.ViewModels
{
    public class RolManagementViewModel : DatabaseAwareViewModel
    {
        private readonly IRolService _rolService;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        private readonly ObservableCollection<Rol> _roles = new();
        public ObservableCollection<Rol> Roles => _roles;

        private Rol? _rolSeleccionado = null;
        public Rol? RolSeleccionado
        {
            get => _rolSeleccionado;
            set
            {
                _rolSeleccionado = value;
                OnPropertyChanged();
                _ = CargarDetalleRolAsync(value);
            }
        }

        // Vista filtrada para la lista maestro-detalle
        private System.ComponentModel.ICollectionView? _rolesFiltrados;
        public System.ComponentModel.ICollectionView RolesFiltrados
        {
            get
            {
                if (_rolesFiltrados == null)
                {
                    _rolesFiltrados = System.Windows.Data.CollectionViewSource.GetDefaultView(_roles);
                    _rolesFiltrados.Filter = FiltrarRol;
                }
                return _rolesFiltrados;
            }
        }

        private string _filtroRoles = string.Empty;
        public string FiltroRoles
        {
            get => _filtroRoles;
            set { _filtroRoles = value; OnPropertyChanged(); _rolesFiltrados?.Refresh(); }
        }

        private bool FiltrarRol(object obj)
        {
            if (string.IsNullOrWhiteSpace(_filtroRoles)) return true;
            return obj is Rol r &&
                ((r.Nombre?.Contains(_filtroRoles, StringComparison.OrdinalIgnoreCase) ?? false) ||
                 (r.Descripcion?.Contains(_filtroRoles, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        private int _totalPermisosSeleccionado;
        public int TotalPermisosSeleccionado
        {
            get => _totalPermisosSeleccionado;
            set { _totalPermisosSeleccionado = value; OnPropertyChanged(); }
        }

        private ObservableCollection<PermisosModuloGroup> _permisosPorModuloVer = new();
        public ObservableCollection<PermisosModuloGroup> PermisosPorModuloVer
        {
            get => _permisosPorModuloVer;
            set { _permisosPorModuloVer = value; OnPropertyChanged(); }
        }

        private string _mensajeEstado = string.Empty;
        public string MensajeEstado
        {
            get => _mensajeEstado;
            set { _mensajeEstado = value; OnPropertyChanged(); }
        }

        // Propiedades de permisos del usuario actual
        private bool _canCreateRole;
        public bool CanCreateRole { get => _canCreateRole; set { _canCreateRole = value; OnPropertyChanged(); } }

        private bool _canEditRole;
        public bool CanEditRole { get => _canEditRole; set { _canEditRole = value; OnPropertyChanged(); } }

        private bool _canDeleteRole;
        public bool CanDeleteRole { get => _canDeleteRole; set { _canDeleteRole = value; OnPropertyChanged(); } }

        private bool _canViewRole;
        public bool CanViewRole { get => _canViewRole; set { _canViewRole = value; OnPropertyChanged(); } }

        public ICommand BuscarRolesCommand { get; }
        public ICommand EliminarRolCommand { get; }

        public RolManagementViewModel(
            IRolService rolService,
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _rolService = rolService ?? throw new ArgumentNullException(nameof(rolService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            BuscarRolesCommand = new RelayCommand(async _ => await BuscarRolesAsync(), _ => CanViewRole);
            EliminarRolCommand = new RelayCommand(async param => { if (param is Rol rol) await EliminarRolAsync(rol); }, _ => CanDeleteRole);

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        }

        private async Task CargarDetalleRolAsync(Rol? rol)
        {
            if (rol == null)
            {
                PermisosPorModuloVer = new ObservableCollection<PermisosModuloGroup>();
                TotalPermisosSeleccionado = 0;
                return;
            }
            try
            {
                var permisos = await _rolService.ObtenerPermisosDeRolAsync(rol.IdRol);
                // Descartar si la selección cambió mientras cargaba
                if (_rolSeleccionado?.IdRol != rol.IdRol) return;
                var grupos = permisos
                    .GroupBy(p => p.Modulo)
                    .OrderBy(g => g.Key)
                    .Select(g => new PermisosModuloGroup
                    {
                        Modulo = g.Key,
                        Permisos = new ObservableCollection<Permiso>(g)
                    });
                PermisosPorModuloVer = new ObservableCollection<PermisosModuloGroup>(grupos);
                TotalPermisosSeleccionado = permisos.Count();
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar permisos del rol: {ex.Message}";
            }
        }

        private async Task BuscarRolesAsync()
        {
            MensajeEstado = string.Empty;
            try
            {
                var roles = await _rolService.ObtenerTodosAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var seleccionAnterior = RolSeleccionado?.IdRol;
                    Roles.Clear();
                    foreach (var rol in roles)
                        Roles.Add(rol);
                    // Restaurar la selección previa o auto-seleccionar el primero
                    RolSeleccionado = Roles.FirstOrDefault(r => r.IdRol == seleccionAnterior) ?? Roles.FirstOrDefault();
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    MensajeEstado = $"Error al cargar roles: {ex.Message}";
                });
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

        // Clase auxiliar para agrupar permisos por módulo
        public class PermisosModuloGroup
        {
            public string Modulo { get; set; } = string.Empty;
            public ObservableCollection<Permiso> Permisos { get; set; } = new();
        }

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanCreateRole = _currentUser.HasPermission("Roles.Crear");
            CanEditRole = _currentUser.HasPermission("Roles.Editar");
            CanDeleteRole = _currentUser.HasPermission("Roles.Eliminar");
            CanViewRole = _currentUser.HasPermission("Roles.Ver");

            if (BuscarRolesCommand is RelayCommand buscarCmd) buscarCmd.RaiseCanExecuteChanged();
            if (EliminarRolCommand is RelayCommand eliminarCmd) eliminarCmd.RaiseCanExecuteChanged();
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
                    await _execute(parameter);
                }
                catch (Exception ex)
                {
                    // No re-lanzar para evitar crashear la UI
                    System.Diagnostics.Debug.WriteLine($"Error en RelayCommand.Execute: {ex.Message}");
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
