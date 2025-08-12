using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario;
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Modules.Usuarios.Helpers;
using Modules.Personas.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using MessageBox = System.Windows.MessageBox; // Usar siempre System.Windows.MessageBox para evitar ambigüedad

namespace Modules.Usuarios.ViewModels
{
    public class UsuarioManagementViewModel : INotifyPropertyChanged
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ICargoService _cargoService;
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IPersonaService _personaService;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;        public ObservableCollection<Usuario> Usuarios { get; set; } = new();
        public ObservableCollection<Cargo> Cargos { get; set; } = new();
        public ObservableCollection<Rol> Roles { get; set; } = new();
        public ObservableCollection<Permiso> Permisos { get; set; } = new();
        public ObservableCollection<Auditoria> Auditorias { get; set; } = new();

        // Propiedades de permisos para el módulo Usuarios
        private bool _canCreateUser;
        public bool CanCreateUser
        {
            get => _canCreateUser;
            set { _canCreateUser = value; OnPropertyChanged(); }
        }
        
        private bool _canEditUser;
        public bool CanEditUser
        {
            get => _canEditUser;
            set { _canEditUser = value; OnPropertyChanged(); }
        }
        
        private bool _canDeleteUser;
        public bool CanDeleteUser
        {
            get => _canDeleteUser;
            set { _canDeleteUser = value; OnPropertyChanged(); }
        }
        
        private bool _canResetPassword;
        public bool CanResetPassword
        {
            get => _canResetPassword;
            set { _canResetPassword = value; OnPropertyChanged(); }
        }
        
        private bool _canViewAudit;
        public bool CanViewAudit
        {
            get => _canViewAudit;
            set { _canViewAudit = value; OnPropertyChanged(); }
        }

        private Usuario? _usuarioSeleccionado = null;
        public Usuario? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                _usuarioSeleccionado = value;
                OnPropertyChanged();
                // Cargar correo de la persona asociada si está disponible
                CorreoPersonaSeleccionada = ObtenerCorreoDePersona(_usuarioSeleccionado);                // Cargar roles del usuario seleccionado
                _ = CargarRolesDeUsuarioAsync();
                // Notificar cambios en los comandos
                (EditarUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RestablecerContrasenaCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (EliminarUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (CargarAuditoriaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
        private string ObtenerCorreoDePersona(Usuario? usuario)
        {
            // Aquí deberías obtener el correo de la persona asociada, si está disponible
            // Si el modelo Usuario no lo expone, puedes extenderlo o usar un DTO
            return string.Empty;
        }

        // Propiedades para el modal de registro
        private bool _isModalNuevoUsuarioVisible;
        public bool IsModalNuevoUsuarioVisible
        {
            get => _isModalNuevoUsuarioVisible;
            set { _isModalNuevoUsuarioVisible = value; OnPropertyChanged(); }
        }
        private string _nuevoUsuarioNombre = string.Empty;
        public string NuevoUsuarioNombre
        {
            get => _nuevoUsuarioNombre;
            set
            {
                if (_nuevoUsuarioNombre != value)
                {
                    _nuevoUsuarioNombre = value;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NuevoUsuarioNombre changed to: '{value}'");
                    OnPropertyChanged();
                    (RegistrarNuevoUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private string _nuevoUsuarioPassword = string.Empty;
        public string NuevoUsuarioPassword
        {
            get => _nuevoUsuarioPassword;
            set
            {
                if (_nuevoUsuarioPassword != value)
                {
                    _nuevoUsuarioPassword = value;
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] NuevoUsuarioPassword changed to: '{value}'");
                    OnPropertyChanged();
                    (RegistrarNuevoUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        private string _correoPersonaSeleccionada = string.Empty;
        public string CorreoPersonaSeleccionada
        {
            get => _correoPersonaSeleccionada;
            set { _correoPersonaSeleccionada = value; OnPropertyChanged(); }
        }
        // Propiedad para la persona seleccionada al registrar usuario
        private Guid _personaIdSeleccionada = Guid.Empty;
        public Guid PersonaIdSeleccionada
        {
            get => _personaIdSeleccionada;
            set
            {
                if (_personaIdSeleccionada != value)
                {
                    _personaIdSeleccionada = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NombreCompletoPersonaSeleccionada));
                    (RegistrarNuevoUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }
        // Propiedad calculada para mostrar el nombre de la persona seleccionada
        public string NombreCompletoPersonaSeleccionada
        {
            get
            {
                var persona = PersonasDisponibles?.FirstOrDefault(p => p.IdPersona == PersonaIdSeleccionada);
                return persona?.NombreCompleto ?? string.Empty;
            }
        }

        // --- NUEVO: Selección de roles en registro ---
        private ObservableCollection<Rol> _rolesDisponibles = new();
        public ObservableCollection<Rol> RolesDisponibles
        {
            get => _rolesDisponibles;
            set { _rolesDisponibles = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Rol> _rolesSeleccionados = new();
        public ObservableCollection<Rol> RolesSeleccionados
        {
            get => _rolesSeleccionados;
            set
            {
                if (_rolesSeleccionados != null)
                    _rolesSeleccionados.CollectionChanged -= RolesSeleccionados_CollectionChanged;
                _rolesSeleccionados = value;
                if (_rolesSeleccionados != null)
                    _rolesSeleccionados.CollectionChanged += RolesSeleccionados_CollectionChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RolesSeleccionadosCount));
            }
        }

        public int RolesSeleccionadosCount => RolesSeleccionados?.Count ?? 0;

        private void RolesSeleccionados_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] RolesSeleccionados_CollectionChanged: Count = {RolesSeleccionados.Count}");
            OnPropertyChanged(nameof(RolesSeleccionadosCount));
            // También notificar la propiedad original por si acaso
            OnPropertyChanged(nameof(RolesSeleccionados));
        }

        private string _filtroTexto = string.Empty;
        public string FiltroTexto
        {
            get => _filtroTexto;
            set
            {
                if (_filtroTexto != value)
                {
                    _filtroTexto = value;
                    OnPropertyChanged();
                    _ = BuscarUsuariosAsync();
                }
            }
        }        public ICommand RegistrarUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }
        public ICommand RestablecerContrasenaCommand { get; }
        public ICommand BuscarUsuariosCommand { get; }
        public ICommand CargarAuditoriaCommand { get; }
        public ICommand AbrirRegistroUsuarioWindowCommand { get; }
        public ICommand CancelarRegistroUsuarioCommand { get; }
        public ICommand RegistrarNuevoUsuarioCommand { get; }
        public ICommand EliminarUsuarioCommand { get; }

        // Lista de personas disponibles para asociar
        private ObservableCollection<GestLog.Modules.Personas.Models.Persona> _personasDisponibles = new();
        public ObservableCollection<GestLog.Modules.Personas.Models.Persona> PersonasDisponibles
        {
            get => _personasDisponibles;
            set { _personasDisponibles = value; OnPropertyChanged(); OnPropertyChanged(nameof(NombreCompletoPersonaSeleccionada)); }
        }

        // Propiedad para mostrar los roles del usuario seleccionado
        private ObservableCollection<Rol> _rolesDeUsuario = new();
        public ObservableCollection<Rol> RolesDeUsuario
        {
            get => _rolesDeUsuario;
            set { _rolesDeUsuario = value; OnPropertyChanged(); }
        }        public UsuarioManagementViewModel(
            IUsuarioService usuarioService,
            ICargoService cargoService,
            IRolService rolService,
            IPermisoService permisoService,
            IAuditoriaService auditoriaService,
            IPersonaService personaService,
            ICurrentUserService currentUserService)
        {
            _usuarioService = usuarioService;
            _cargoService = cargoService;
            _rolService = rolService;
            _permisoService = permisoService;
            _auditoriaService = auditoriaService;
            _personaService = personaService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            // Comandos con validación de permisos
            RegistrarUsuarioCommand = new RelayCommand(async _ => await RegistrarUsuarioAsync(), _ => true);
            EditarUsuarioCommand = new RelayCommand(async _ => await EditarUsuarioAsync(), _ => UsuarioSeleccionado != null && CanEditUser);
            RestablecerContrasenaCommand = new RelayCommand(async _ => await RestablecerContrasenaAsync(), _ => UsuarioSeleccionado != null && CanResetPassword);
            BuscarUsuariosCommand = new RelayCommand(async _ => await BuscarUsuariosAsync(), _ => true);
            CargarAuditoriaCommand = new RelayCommand(async _ => await CargarAuditoriaAsync(), _ => UsuarioSeleccionado != null && CanViewAudit);
            AbrirRegistroUsuarioWindowCommand = new RelayCommand(_ => { AbrirRegistroUsuarioWindow(); return Task.CompletedTask; }, _ => CanCreateUser);
            CancelarRegistroUsuarioCommand = new RelayCommand(_ => { CerrarRegistroUsuarioWindow(); return Task.CompletedTask; }, _ => true);
            RegistrarNuevoUsuarioCommand = new RelayCommand(async _ => await RegistrarNuevoUsuarioAsync(), _ => PuedeRegistrarNuevoUsuario() && CanCreateUser);
            EliminarUsuarioCommand = new RelayCommand(async _ => await EliminarUsuarioAsync(), _ => UsuarioSeleccionado != null && CanDeleteUser);

            // Cargar usuarios desde la base de datos al inicializar
            _ = CargarUsuariosAsync();
            _ = CargarPersonasDisponiblesAsync();
            _ = CargarRolesDisponiblesAsync();
            _ = CargarRolesDeUsuarioAsync();
            
            // Conectar el event handler para la colección de roles seleccionados
            RolesSeleccionados.CollectionChanged += RolesSeleccionados_CollectionChanged;
        }

        // Método para cargar usuarios desde la base de datos
        public async Task CargarUsuariosAsync()
        {
            var lista = await _usuarioService.BuscarUsuariosAsync("");
            Usuarios.Clear();
            foreach (var usuario in lista)
                Usuarios.Add(usuario);
        }

        // Método para cargar personas activas
        public async Task CargarPersonasDisponiblesAsync()
        {
            // Suponiendo que tienes un servicio de personas inyectado, por ejemplo _personaService
            if (_personaService != null)
            {
                var personas = await _personaService.BuscarPersonasAsync("");
                PersonasDisponibles = new ObservableCollection<GestLog.Modules.Personas.Models.Persona>(personas.Where(p => p.Activo));
            }
        }

        public async Task CargarRolesDisponiblesAsync()
        {
            var roles = await _rolService.ObtenerTodosAsync();
            RolesDisponibles = new ObservableCollection<Rol>(roles);
        }

        public async Task CargarRolesDeUsuarioAsync()
        {
            if (UsuarioSeleccionado == null) 
            {
                RolesDeUsuario.Clear();
                return;
            }
            try
            {
                // Obtener roles del usuario desde la base de datos
                using var db = new GestLog.Modules.DatabaseConnection.GestLogDbContextFactory().CreateDbContext(Array.Empty<string>());
                var rolesIds = await db.UsuarioRoles
                    .Where(ur => ur.IdUsuario == UsuarioSeleccionado.IdUsuario)
                    .Select(ur => ur.IdRol)
                    .ToListAsync();
                var roles = await db.Roles
                    .Where(r => rolesIds.Contains(r.IdRol))
                    .ToListAsync();
                RolesDeUsuario.Clear();
                foreach (var rol in roles)
                    RolesDeUsuario.Add(rol);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando roles del usuario: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private async Task RegistrarUsuarioAsync() { await Task.CompletedTask; }
        private async Task EditarUsuarioAsync()
        {
            if (UsuarioSeleccionado == null)
                return;
            try
            {
                await _usuarioService.EditarUsuarioAsync(UsuarioSeleccionado);
                System.Windows.MessageBox.Show($"Usuario '{UsuarioSeleccionado.NombreUsuario}' editado correctamente.", "Edición exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                await BuscarUsuariosAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al editar usuario: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }        private async Task RestablecerContrasenaAsync()
        {
            if (UsuarioSeleccionado == null)
                return;            // Solicitar nueva contraseña usando InputBox
            string nuevaContrasena = Interaction.InputBox(
                $"Ingrese la nueva contraseña para el usuario '{UsuarioSeleccionado.NombreUsuario}':\n\n" +
                "Recomendaciones:\n" +
                "• Mínimo 6 caracteres\n" +
                "• Combine letras, números y símbolos\n" +
                "• Evite información personal",
                "Restablecer Contraseña",
                "");

            if (string.IsNullOrWhiteSpace(nuevaContrasena))
                return;

            // Validar longitud mínima
            if (nuevaContrasena.Length < 6)
            {
                MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Contraseña inválida", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmar contraseña
            string confirmContrasena = Interaction.InputBox(
                "Confirme la nueva contraseña:",
                "Confirmar Contraseña",
                "");

            if (nuevaContrasena != confirmContrasena)
            {
                MessageBox.Show("Las contraseñas no coinciden. Intente nuevamente.", "Error de confirmación", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirmacion = MessageBox.Show(
                $"¿Está seguro que desea restablecer la contraseña para el usuario '{UsuarioSeleccionado.NombreUsuario}'?", 
                "Confirmar restablecimiento", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            
            if (confirmacion != MessageBoxResult.Yes)
                return;

            try
            {
                await _usuarioService.RestablecerContraseñaAsync(UsuarioSeleccionado.IdUsuario, nuevaContrasena);
                
                MessageBox.Show(
                    $"✅ Contraseña restablecida correctamente para el usuario '{UsuarioSeleccionado.NombreUsuario}'.\n\n" +
                    "La nueva contraseña ha sido establecida exitosamente.", 
                    "Restablecimiento exitoso", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al restablecer contraseña: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerarContrasenaTemporal()
        {
            // Este método ya no se usa, pero lo mantengo por si se necesita en el futuro
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        private async Task BuscarUsuariosAsync()
        {
            var lista = await _usuarioService.BuscarUsuariosAsync(FiltroTexto);
            Usuarios.Clear();
            foreach (var usuario in lista)
                Usuarios.Add(usuario);
        }
        private async Task CargarAuditoriaAsync() { await Task.CompletedTask; }        private void LimpiarCamposNuevoUsuario()
        {
            NuevoUsuarioNombre = string.Empty;
            NuevoUsuarioPassword = string.Empty;
            RolesSeleccionados.Clear();
        }
        private bool PuedeRegistrarNuevoUsuario()
        {
            var canRegister = !string.IsNullOrWhiteSpace(NuevoUsuarioNombre) &&
                   !string.IsNullOrWhiteSpace(NuevoUsuarioPassword);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] PuedeRegistrarNuevoUsuario: {canRegister} (Nombre: '{NuevoUsuarioNombre}', Password: '{NuevoUsuarioPassword}')");
            return canRegister;
        }        private async Task RegistrarNuevoUsuarioAsync()
        {
            if (!PuedeRegistrarNuevoUsuario() || PersonaIdSeleccionada == Guid.Empty)
            {
                System.Windows.MessageBox.Show("Debe seleccionar una persona para asociar el usuario.", "Registro de usuario", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Validar que se haya seleccionado al menos un rol
            if (RolesSeleccionados.Count == 0)
            {
                System.Windows.MessageBox.Show("Debe seleccionar al menos un rol para el usuario.", "Registro de usuario", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generar salt y hash seguro
                var salt = PasswordHelper.GenerateSalt();
                var hash = PasswordHelper.HashPassword(NuevoUsuarioPassword, salt);
                var nuevoUsuario = new Usuario
                {
                    IdUsuario = Guid.NewGuid(),
                    PersonaId = PersonaIdSeleccionada,
                    NombreUsuario = NuevoUsuarioNombre,
                    HashContrasena = hash,
                    Salt = salt,
                    Activo = true,
                    Desactivado = false,
                    FechaCreacion = DateTime.Now,
                    FechaModificacion = DateTime.Now
                };

                // Registrar el usuario
                await _usuarioService.RegistrarUsuarioAsync(nuevoUsuario);

                // Asignar los roles seleccionados directamente
                var rolesIds = RolesSeleccionados.Select(r => r.IdRol).ToArray();
                await _usuarioService.AsignarRolesAsync(nuevoUsuario.IdUsuario, rolesIds);

                // Mostrar mensaje de éxito
                System.Windows.MessageBox.Show($"Usuario '{nuevoUsuario.NombreUsuario}' registrado exitosamente con {RolesSeleccionados.Count} rol(es).", "Registro Exitoso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                // Cerrar ventana de registro
                if (global::System.Windows.Application.Current.Windows.OfType<global::System.Windows.Window>().FirstOrDefault(w => w is GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario.UsuarioRegistroWindow && w.DataContext == this) is global::System.Windows.Window win)
                    win.DialogResult = true;

                // Recargar lista de usuarios
                await CargarUsuariosAsync();
                LimpiarCamposNuevoUsuario();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al registrar usuario: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void AbrirRegistroUsuarioWindow()
        {
            LimpiarCamposNuevoUsuario(); // Asegura que todo esté limpio ANTES de abrir la ventana
            var win = new UsuarioRegistroWindow();
            
            // Limpiar la colección de roles seleccionados explícitamente
            RolesSeleccionados.Clear();
            
            win.DataContext = this;
            win.Owner = global::System.Windows.Application.Current.MainWindow;
            
            var result = win.ShowDialog();
            if (result == true)
            {
                // El usuario fue registrado, recargar lista
                _ = CargarUsuariosAsync();
            }
            LimpiarCamposNuevoUsuario();
        }
        private void CerrarRegistroUsuarioWindow()
        {
            if (global::System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is UsuarioRegistroWindow && w.DataContext == this) is Window win)
                win.DialogResult = false;
        }

        private async Task EliminarUsuarioAsync()
        {
            if (UsuarioSeleccionado == null)
                return;
            var result = System.Windows.MessageBox.Show($"¿Está seguro que desea eliminar el usuario '{UsuarioSeleccionado.NombreUsuario}'?\nEsta acción eliminará todas sus relaciones y no se puede deshacer.",
                "Confirmar eliminación", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
                return;
            try
            {
                await _usuarioService.EliminarUsuarioAsync(UsuarioSeleccionado.IdUsuario);
                System.Windows.MessageBox.Show($"Usuario '{UsuarioSeleccionado.NombreUsuario}' eliminado correctamente.", "Eliminación exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                await CargarUsuariosAsync();
                UsuarioSeleccionado = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al eliminar usuario: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }        }

        #region Métodos de Permisos

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            CanCreateUser = _currentUser.HasPermission("Usuarios.Crear");
            CanEditUser = _currentUser.HasPermission("Usuarios.Editar");
            CanDeleteUser = _currentUser.HasPermission("Usuarios.Eliminar");
            CanResetPassword = _currentUser.HasPermission("Usuarios.RestablecerContrasena");
            CanViewAudit = _currentUser.HasPermission("Usuarios.VerAuditoria");

            // Notificar cambios en comandos que dependen de permisos
            (RegistrarNuevoUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditarUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EliminarUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RestablecerContrasenaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CargarAuditoriaCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        // Implementación simple de RelayCommand para MVVM
        public class RelayCommand : ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Predicate<object?>? _canExecute;
            public event EventHandler? CanExecuteChanged;

            public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
            public async void Execute(object? parameter) => await _execute(parameter);
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
