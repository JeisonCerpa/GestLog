using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Personas.Models;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.ViewModels.Base;           // ✅ NUEVO: Clase base auto-refresh
using GestLog.Services.Interfaces;       // ✅ NUEVO: IDatabaseConnectionService
using GestLog.Services.Core.Logging;     // ✅ NUEVO: IGestLogLogger
using Modules.Personas.Interfaces;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Data;
using System.ComponentModel;
using System.Timers;
using System.Windows.Controls;
using Modules.Usuarios.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.Usuarios.ViewModels
{    public partial class PersonaManagementViewModel : DatabaseAwareViewModel
    {
        private readonly IPersonaService _personaService;
        private readonly ICargoService _cargoService;
        private readonly ICargoRepository _cargoRepository;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;

        [ObservableProperty]
        private ObservableCollection<Persona> personas = new();

        [ObservableProperty]
        private Persona? personaSeleccionada;

        [ObservableProperty]
        private ObservableCollection<Cargo> cargos = new();

        [ObservableProperty]
        private ObservableCollection<string> estados = new() { "Activo", "Inactivo" };

        [ObservableProperty]
        private ICollectionView? personasView;

        [ObservableProperty]
        private string filtroTexto = string.Empty;

        [ObservableProperty]
        private string filtroEstado = "Todos";

        private System.Timers.Timer? _debounceTimer;

        [ObservableProperty]
        private System.Windows.Controls.UserControl? vistaActual;        [ObservableProperty]
        private string mensajeValidacion = string.Empty;

        // Propiedades de permisos
        [ObservableProperty]
        private bool canCreatePersona = false;

        [ObservableProperty]
        private bool canEditPersona = false;

        [ObservableProperty]
        private bool canDeletePersona = false;

        [ObservableProperty]
        private bool canViewPersona = false;

        [ObservableProperty]
        private bool canActivatePersona = false;

        public string TextoActivarDesactivar => PersonaSeleccionada?.Activo == true ? "Desactivar" : "Activar";        public PersonaManagementViewModel(
            IPersonaService personaService, 
            ICargoService cargoService, 
            ICargoRepository cargoRepository, 
            ITipoDocumentoRepository tipoDocumentoRepository, 
            ICurrentUserService currentUserService,
            IDatabaseConnectionService databaseService,
            IGestLogLogger logger)
            : base(databaseService, logger)
        {
            _personaService = personaService;
            _cargoService = cargoService;
            _cargoRepository = cargoRepository;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };

            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            _ = InicializarAsync();
            PersonasView = CollectionViewSource.GetDefaultView(Personas);
            if (PersonasView != null)
                PersonasView.Filter = FiltrarPersona;
        }

        public async Task InicializarAsync()
        {
            await CargarCargos();
            await CargarPersonas();
        }        [RelayCommand(CanExecute = nameof(CanCreatePersona))]
        private async Task RegistrarPersona()
        {
            var primerCargo = Cargos.FirstOrDefault();
            var nuevaPersona = new Persona {
                Nombres = string.Empty,
                Apellidos = string.Empty,
                TipoDocumento = null, // Ahora es un objeto, no string
                TipoDocumentoId = Guid.Empty, // Inicializar como vacío
                NumeroDocumento = string.Empty,
                Correo = string.Empty,
                Telefono = string.Empty,
                Cargo = primerCargo,
                CargoId = primerCargo?.IdCargo ?? Guid.Empty,
                Activo = true
            };
            var vm = new PersonaRegistroViewModel(nuevaPersona, _personaService, _tipoDocumentoRepository, _cargoRepository, _currentUserService);
            var win = new Views.Tools.GestionIdentidadCatalogos.Personas.PersonaRegistroWindow { DataContext = vm };
            if (win.ShowDialog() == true)
            {
                await CargarPersonas();
                PersonasView?.Refresh();
            }
        }        [RelayCommand(CanExecute = nameof(CanEditPersona))]
        private async Task GuardarPersona()
        {
            if (PersonaSeleccionada == null) return;
            try
            {
                PersonaSeleccionada.CargoId = PersonaSeleccionada.Cargo?.IdCargo ?? Guid.Empty;
                // Eliminar referencia a Estado, solo usar Activo
                var personaGuardada = await _personaService.RegistrarPersonaAsync(PersonaSeleccionada);
                await CargarPersonas();
                PersonaSeleccionada = personaGuardada;
                // TODO: Mostrar mensaje de éxito al usuario
            }
            catch
            {
                // TODO: Mostrar mensaje de error al usuario
            }
        }

        private async Task CargarPersonas()
        {
            var lista = await _personaService.BuscarPersonasAsync("");
            // Obtener todos los usuarios para asociar
            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var usuarioService = serviceProvider.GetService(typeof(IUsuarioService)) as IUsuarioService;
            var usuarios = usuarioService != null ? await usuarioService.BuscarUsuariosAsync("") : new List<GestLog.Modules.Usuarios.Models.Usuario>();
            var personas = lista.ToList();
            foreach (var persona in personas)
            {
                persona.TieneUsuario = usuarios.Any(u => u.PersonaId == persona.IdPersona);
            }
            Personas = new ObservableCollection<Persona>(personas);
        }

        private async Task CargarCargos()
        {
            var lista = await _cargoService.ObtenerTodosAsync();
            Cargos = new ObservableCollection<Cargo>(lista);
        }

        partial void OnFiltroTextoChanged(string value)
        {
            DebounceFiltrar();
        }

        partial void OnFiltroEstadoChanged(string value)
        {
            PersonasView?.Refresh();
        }

        partial void OnPersonaSeleccionadaChanged(Persona? value)
        {
            if (value != null)
                MostrarDetalle();
            else
                VistaActual = null;
        }

        partial void OnPersonasChanged(ObservableCollection<Persona> value)
        {
            PersonasView = CollectionViewSource.GetDefaultView(Personas);
            if (PersonasView != null)
                PersonasView.Filter = FiltrarPersona;
            PersonasView?.Refresh();
            if (PersonaSeleccionada != null)
            {
                PersonaSeleccionada = Personas.FirstOrDefault(p => p.IdPersona == PersonaSeleccionada.IdPersona);
            }
        }

        private void MostrarDetalle()
        {
            // Lógica para mostrar detalle de persona
        }

        private void MostrarEdicion()
        {
            // Lógica para mostrar vista de edición de persona
        }

        private void DebounceFiltrar()
        {
            _debounceTimer?.Stop();
            if (_debounceTimer is null)
            {
                _debounceTimer = new System.Timers.Timer(350);
                _debounceTimer.AutoReset = false;
                _debounceTimer.Elapsed += (s, e) =>
                {
                    _debounceTimer?.Stop();
                    App.Current.Dispatcher.Invoke(() => PersonasView?.Refresh());
                };
            }
            _debounceTimer.Start();
        }

        private bool FiltrarPersona(object obj)
        {
            if (obj is not Persona p) return false;
            // Filtro por texto (solo nombre completo o documento)
            if (!string.IsNullOrWhiteSpace(FiltroTexto))
            {
                var filtro = FiltroTexto.ToLowerInvariant().Trim();
                var nombreCompleto = ($"{p.Nombres} {p.Apellidos}").ToLowerInvariant();
                var documento = p.NumeroDocumento?.ToLowerInvariant() ?? string.Empty;
                if (!nombreCompleto.Contains(filtro) && !documento.Contains(filtro))
                    return false;
            }
            // Filtro por estado
            if (FiltroEstado != "Todos")
            {
                if (FiltroEstado == "Activo" && !p.Activo) return false;
                if (FiltroEstado == "Inactivo" && p.Activo) return false;
            }
            return true;
        }

        [RelayCommand]
        private void VerPersona(Persona persona)
        {
            PersonaSeleccionada = persona;
            // Lógica para mostrar el detalle en el panel/modal
        }

        [RelayCommand]
        private void EditarPersona(Persona? persona)
        {
            if (persona == null) return;
            // Clonar la persona para edición (evita cambios directos hasta guardar)
            var personaCopia = new Persona
            {
                IdPersona = persona.IdPersona,
                Nombres = persona.Nombres,
                Apellidos = persona.Apellidos,
                TipoDocumento = persona.TipoDocumento,
                TipoDocumentoId = persona.TipoDocumentoId,
                NumeroDocumento = persona.NumeroDocumento,
                Correo = persona.Correo,
                Telefono = persona.Telefono,
                Cargo = persona.Cargo,
                CargoId = persona.CargoId,
                Activo = persona.Activo
            };
            var vm = new PersonaEdicionViewModel(personaCopia, Estados, _personaService, _tipoDocumentoRepository, _cargoRepository, _currentUserService);
            var win = new Views.Tools.GestionIdentidadCatalogos.Personas.PersonaEdicionWindow { DataContext = vm, Owner = System.Windows.Application.Current.MainWindow };
            if (win.ShowDialog() == true)
            {
                // Guardar cambios en la base de datos y refrescar la lista
                _ = CargarPersonas();
                PersonasView?.Refresh();
            }
        }

        [RelayCommand]
        private void CerrarDetalle()
        {
            PersonaSeleccionada = null;
            VistaActual = null;
        }

        [RelayCommand]
        private void CancelarEdicion()
        {
            MostrarDetalle();
        }        [RelayCommand(CanExecute = nameof(CanActivatePersona))]
        private async Task ActivarDesactivarPersona(Persona persona)
        {
            persona.Activo = !persona.Activo;
            await _personaService.EditarPersonaAsync(persona);
            await CargarPersonas();
            PersonasView?.Refresh();
        }

        [RelayCommand(CanExecute = nameof(CanCreatePersona))]
        private void CrearUsuarioParaPersona(Persona persona)
        {
            if (persona == null)
            {
                MensajeValidacion = "Debe seleccionar una persona.";
                return;
            }            var serviceProvider = GestLog.Services.Core.Logging.LoggingService.GetServiceProvider();
            var usuarioVm = serviceProvider.GetService<UsuarioManagementViewModel>();
            if (usuarioVm != null)
            {
                // Solo esa persona disponible para el registro
                usuarioVm.PersonasDisponibles = new System.Collections.ObjectModel.ObservableCollection<GestLog.Modules.Personas.Models.Persona> { persona };
                usuarioVm.PersonaIdSeleccionada = persona.IdPersona;
                usuarioVm.NuevoUsuarioNombre = string.Empty;
                usuarioVm.NuevoUsuarioPassword = string.Empty;
                var win = new GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario.UsuarioRegistroWindow { DataContext = usuarioVm, Owner = System.Windows.Application.Current.MainWindow };
                if (win.ShowDialog() == true)
                {
                    _ = CargarPersonas();
                    PersonasView?.Refresh();
                }
            }
            else
            {
                MensajeValidacion = "No se pudo cargar el formulario de usuario.";
            }
        }

        // === MÉTODOS DE GESTIÓN DE PERMISOS ===
        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        private void RecalcularPermisos()
        {
            CanCreatePersona = _currentUser.HasPermission("Personas.Crear");
            CanEditPersona = _currentUser.HasPermission("Personas.Editar");
            CanDeletePersona = _currentUser.HasPermission("Personas.Editar"); // Usar Editar para eliminar/desactivar
            CanViewPersona = _currentUser.HasPermission("Personas.Ver");
            CanActivatePersona = _currentUser.HasPermission("Personas.ActivarDesactivar");
        }        /// <summary>
        /// Implementación del método abstracto para auto-refresh automático
        /// </summary>
        protected override async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogInformation("[PersonaManagementViewModel] Refrescando datos automáticamente");
                await InicializarAsync();
                _logger.LogInformation("[PersonaManagementViewModel] Datos refrescados exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PersonaManagementViewModel] Error al refrescar datos");
                throw;
            }
        }

        /// <summary>
        /// Override para manejar cuando se pierde la conexión específicamente para personas
        /// </summary>
        protected override void OnConnectionLost()
        {
            MensajeValidacion = "Sin conexión - Gestión de personas no disponible";
        }
    }
}
