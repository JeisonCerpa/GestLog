// MIGRACIÓN A DatabaseAwareViewModel - AUTO-REFRESH AUTOMÁTICO
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

// ✅ NUEVAS DEPENDENCIAS PARA AUTO-REFRESH
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;    // ✅ NUEVO: IGestLogLogger
using GestLog.Modules.DatabaseConnection; // ✅ NUEVO: IDbContextFactory
using System.Threading;                  // ✅ NUEVO: CancellationTokenSource

namespace Modules.Usuarios.ViewModels
{
    public class UsuarioManagementViewModel : DatabaseAwareViewModel
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IUsuarioService _usuarioService;
        private readonly ICargoService _cargoService;
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IAuditoriaService _auditoriaService;
        private readonly IPersonaService _personaService;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        public ObservableCollection<Usuario> Usuarios { get; set; } = new();
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
                CorreoPersonaSeleccionada = ObtenerCorreoDePersona(_usuarioSeleccionado);
                
                // Actualizar información de persona en tiempo real
                _ = ActualizarInformacionUsuarioSeleccionadoAsync();
                
                // Cargar roles del usuario seleccionado
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
            return usuario?.Correo ?? string.Empty;
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

        // Lista de roles disponibles para asignar
        private ObservableCollection<Rol> _rolesDisponibles = new();
        public ObservableCollection<Rol> RolesDisponibles
        {
            get => _rolesDisponibles;
            set { _rolesDisponibles = value; OnPropertyChanged(); }
        }

        // Lista de roles seleccionados al registrar o editar usuario
        private ObservableCollection<Rol> _rolesSeleccionados = new();
        public ObservableCollection<Rol> RolesSeleccionados
        {
            get => _rolesSeleccionados;
            set { _rolesSeleccionados = value; OnPropertyChanged(); }
        }

        // Propiedad computada para mostrar el nombre completo de la persona seleccionada
        public string NombreCompletoPersonaSeleccionada
        {
            get
            {
                var persona = PersonasDisponibles.FirstOrDefault(p => p.IdPersona == PersonaIdSeleccionada);
                return persona != null ? $"{persona.Nombres} {persona.Apellidos}" : string.Empty;
            }
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
        }

        // Propiedad para mostrar los roles del usuario seleccionado
        private ObservableCollection<Rol> _rolesDeUsuario = new();
        public ObservableCollection<Rol> RolesDeUsuario
        {
            get => _rolesDeUsuario;
            set { _rolesDeUsuario = value; OnPropertyChanged(); }
        }

        public ICommand RegistrarUsuarioCommand { get; }
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

        public UsuarioManagementViewModel(
            IDbContextFactory<GestLogDbContext> dbContextFactory,
            IUsuarioService usuarioService,
            ICargoService cargoService,
            IRolService rolService,
            IPermisoService permisoService,
            IAuditoriaService auditoriaService,
            IPersonaService personaService,
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _usuarioService = usuarioService;
            _cargoService = cargoService;
            _rolService = rolService;
            _permisoService = permisoService;
            _auditoriaService = auditoriaService;
            _personaService = personaService;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            // Configurar permisos reactivos
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

            // Inicialización asíncrona
            _ = InicializarAsync();
            
            // Conectar el event handler para la colección de roles seleccionados
            RolesSeleccionados.CollectionChanged += RolesSeleccionados_CollectionChanged;
        }

        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogInformation("[UsuarioManagementViewModel] Refrescando datos automáticamente");
                
                // Recargar todas las listas principales sin mostrar UI
                await CargarUsuariosAsync();
                await CargarPersonasDisponiblesAsync();
                await CargarRolesDisponiblesAsync();
                
                // Si hay usuario seleccionado, recargar sus roles
                if (UsuarioSeleccionado != null)
                {
                    await CargarRolesDeUsuarioAsync();
                }
                
                _logger.LogInformation("[UsuarioManagementViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al refrescar datos");
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para usuarios
        /// </summary>
        protected override void OnConnectionLost()
        {
            StatusMessage = "Sin conexión - Gestión de usuarios no disponible";
        }

        /// <summary>
        /// Método de inicialización asíncrona con timeout ultrarrápido
        /// </summary>
        public async Task InicializarAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Cargando usuarios...";
                
                await CargarUsuariosAsync();
                await CargarPersonasDisponiblesAsync();
                await CargarRolesDisponiblesAsync();
                await CargarRolesDeUsuarioAsync();
                
                StatusMessage = $"Cargados {Usuarios.Count} usuarios";
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[UsuarioManagementViewModel] Timeout - sin conexión BD");
                StatusMessage = "Sin conexión - Módulo no disponible";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al inicializar");
                StatusMessage = "Error al cargar usuarios";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Método para cargar usuarios desde la base de datos
        public async Task CargarUsuariosAsync()
        {
            try
            {
                var lista = await _usuarioService.BuscarUsuariosAsync("");
                
                // Cargar información de personas para cada usuario
                await CargarInformacionPersonasAsync(lista);
                
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    Usuarios.Clear();
                    foreach (var usuario in lista)
                        Usuarios.Add(usuario);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al cargar usuarios");
                // No mostrar UI error en auto-refresh
            }
        }        // Método para enriquecer usuarios con información de personas
        private async Task CargarInformacionPersonasAsync(IEnumerable<Usuario> usuarios)
        {
            try
            {
                foreach (var usuario in usuarios)
                {
                    if (usuario.PersonaId != Guid.Empty)
                    {
                        var personas = await _personaService.BuscarPersonasAsync("");
                        var persona = personas.FirstOrDefault(p => p.IdPersona == usuario.PersonaId);
                        if (persona != null)
                        {
                            usuario.Correo = persona.Correo ?? string.Empty;
                            usuario.NombrePersona = $"{persona.Nombres} {persona.Apellidos}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al cargar información de personas");
            }
        }

        // Método para cargar personas activas
        public async Task CargarPersonasDisponiblesAsync()
        {
            try
            {
                if (_personaService != null)
                {
                    var personas = await _personaService.BuscarPersonasAsync("");
                    
                    System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                    {
                        PersonasDisponibles = new ObservableCollection<GestLog.Modules.Personas.Models.Persona>(personas.Where(p => p.Activo));
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al cargar personas disponibles");
            }
        }

        public async Task CargarRolesDisponiblesAsync()
        {
            try
            {
                var roles = await _rolService.ObtenerTodosAsync();
                
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RolesDisponibles = new ObservableCollection<Rol>(roles);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al cargar roles disponibles");
            }
        }

        /// <summary>
        /// Cargar roles de usuario con timeout ultrarrápido usando IDbContextFactory
        /// </summary>
        public async Task CargarRolesDeUsuarioAsync()
        {
            if (UsuarioSeleccionado == null) 
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RolesDeUsuario.Clear();
                });
                return;
            }
            
            try
            {                // ✅ TIMEOUT BALANCEADO CON FACTORY
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var db = _dbContextFactory.CreateDbContext();
                db.Database.SetCommandTimeout(15);
                
                var rolesIds = await db.UsuarioRoles
                    .AsNoTracking()
                    .Where(ur => ur.IdUsuario == UsuarioSeleccionado.IdUsuario)
                    .Select(ur => ur.IdRol)
                    .ToListAsync(timeoutCts.Token);
                
                var roles = await db.Roles
                    .AsNoTracking()
                    .Where(r => rolesIds.Contains(r.IdRol))
                    .ToListAsync(timeoutCts.Token);
                
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    RolesDeUsuario.Clear();
                    foreach (var rol in roles)
                        RolesDeUsuario.Add(rol);
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == -1 || ex.Number == 26 || ex.Number == 10060)
            {
                _logger.LogInformation("[UsuarioManagementViewModel] Sin conexión BD al cargar roles (Error {Number})", ex.Number);
                // No lanzar excepción - manejar silenciosamente
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[UsuarioManagementViewModel] Timeout al cargar roles de usuario");
                // No lanzar excepción - manejar silenciosamente  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al cargar roles del usuario");
            }
        }        // Método para actualizar información del usuario seleccionado con datos de persona
        private async Task ActualizarInformacionUsuarioSeleccionadoAsync()
        {
            if (UsuarioSeleccionado?.PersonaId == null || UsuarioSeleccionado.PersonaId == Guid.Empty) return;
            
            try
            {
                var personas = await _personaService.BuscarPersonasAsync("");
                var persona = personas?.FirstOrDefault(p => p.IdPersona == UsuarioSeleccionado.PersonaId);
                if (persona != null)
                {
                    UsuarioSeleccionado.Correo = persona.Correo ?? string.Empty;
                    UsuarioSeleccionado.NombrePersona = $"{persona.Nombres} {persona.Apellidos}";
                    OnPropertyChanged(nameof(UsuarioSeleccionado));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UsuarioManagementViewModel] Error al actualizar información de persona");
            }
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
                return;
            
            try
            {
                // Abrir ventana para solicitar nueva contraseña
                var window = new GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario.RestablecerContrasenaWindow
                {
                    NombreUsuario = UsuarioSeleccionado.NombreUsuario,
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                if (window.ShowDialog() == true && !string.IsNullOrEmpty(window.NuevaContrasena))
                {
                    await _usuarioService.RestablecerContraseñaAsync(UsuarioSeleccionado.IdUsuario, window.NuevaContrasena);
                    System.Windows.MessageBox.Show($"Contraseña del usuario '{UsuarioSeleccionado.NombreUsuario}' restablecida correctamente.", "Restablecimiento exitoso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al restablecer contraseña: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            
            // Cargar información de personas para cada usuario encontrado
            await CargarInformacionPersonasAsync(lista);
            
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Usuarios.Clear();
                foreach (var usuario in lista)
                    Usuarios.Add(usuario);
            });
        }
        
        private async Task CargarAuditoriaAsync() { await Task.CompletedTask; }

        private void LimpiarCamposNuevoUsuario()
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
        }

        private async Task RegistrarNuevoUsuarioAsync()
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
            IsModalNuevoUsuarioVisible = true;
        }
        
        private void CerrarRegistroUsuarioWindow()
        {
            IsModalNuevoUsuarioVisible = false;
            LimpiarCamposNuevoUsuario();
        }

        private async Task EliminarUsuarioAsync()
        {
            if (UsuarioSeleccionado == null) return;

            var result = System.Windows.MessageBox.Show(
                $"¿Está seguro de que desea eliminar el usuario '{UsuarioSeleccionado.NombreUsuario}'?",
                "Confirmar eliminación",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                try
                {
                    await _usuarioService.EliminarUsuarioAsync(UsuarioSeleccionado.IdUsuario);
                    System.Windows.MessageBox.Show($"Usuario '{UsuarioSeleccionado.NombreUsuario}' eliminado correctamente.", "Eliminación exitosa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await CargarUsuariosAsync();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al eliminar usuario: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void RolesSeleccionados_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            (RegistrarNuevoUsuarioCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #region Métodos de Permisos

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanCreateUser = _currentUser.HasPermission("Usuarios.Crear");
            CanEditUser = _currentUser.HasPermission("Usuarios.Editar");
            CanDeleteUser = _currentUser.HasPermission("Usuarios.Eliminar");
            CanResetPassword = _currentUser.HasPermission("Usuarios.RestablecerContrasena");
            CanViewAudit = _currentUser.HasPermission("Usuarios.VerAuditoria");
        }

        #endregion

        /// <summary>
        /// Override Dispose si hay recursos adicionales que limpiar
        /// </summary>
        public override void Dispose()
        {
            try
            {
                // Desuscribirse de eventos específicos
                if (_currentUserService != null)
                {
                    _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
                }

                if (RolesSeleccionados != null)
                {
                    RolesSeleccionados.CollectionChanged -= RolesSeleccionados_CollectionChanged;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[UsuarioManagementViewModel] Error durante dispose específico");
            }
            finally
            {
                // Llamar al dispose de la clase base
                base.Dispose();
            }
        }

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
