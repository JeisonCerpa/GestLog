using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Personas.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Modules.Usuarios.Interfaces;
using Modules.Personas.Interfaces;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;
using GestLog.ViewModels.Base;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Globalization;

namespace GestLog.Modules.Usuarios.ViewModels
{    
    public partial class PersonaRegistroViewModel : ValidatableViewModel
    {
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        
        private Persona _persona = new Persona {
            Nombres = string.Empty,
            Apellidos = string.Empty,
            NumeroDocumento = string.Empty,
            Correo = string.Empty,
            Telefono = string.Empty
        };
        public Persona Persona
        {
            get => _persona;
            set { _persona = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Cargo> Cargos { get; }
        private readonly IPersonaService _personaService;
        private readonly ICargoRepository _cargoRepository;

        public ObservableCollection<TipoDocumento> TiposDocumento { get; }
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;

        private string _documentoError = string.Empty;
        private string _correoError = string.Empty;
        private bool _validandoDocumento;        
        private bool _validandoCorreo;

        // Propiedades de permisos
        private bool _canSavePersona = false;
        public bool CanSavePersona 
        { 
            get => _canSavePersona; 
            set { _canSavePersona = value; OnPropertyChanged(); } 
        }

        public string DocumentoError
        {
            get => _documentoError;
            set { _documentoError = value; OnPropertyChanged(); }
        }
        public string CorreoError
        {
            get => _correoError;
            set { _correoError = value; OnPropertyChanged(); }
        }
        public bool ValidandoDocumento
        {
            get => _validandoDocumento;
            set { _validandoDocumento = value; OnPropertyChanged(); }
        }        
        public bool ValidandoCorreo
        {
            get => _validandoCorreo;
            set { _validandoCorreo = value; OnPropertyChanged(); }
        }

        public bool PuedeGuardar => string.IsNullOrEmpty(DocumentoError) && string.IsNullOrEmpty(CorreoError) && !ValidandoDocumento && !ValidandoCorreo && CanSavePersona;

        private bool CanGuardar() => PuedeGuardar;        
        
        // Comando para guardar
        public CommunityToolkit.Mvvm.Input.AsyncRelayCommand SaveCommand { get; }        

        public PersonaRegistroViewModel(Persona persona, IPersonaService personaService, ITipoDocumentoRepository tipoDocumentoRepository, ICargoRepository cargoRepository, ICurrentUserService currentUserService)
        {
            Persona = persona;
            _personaService = personaService;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _cargoRepository = cargoRepository;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
              
            TiposDocumento = new ObservableCollection<TipoDocumento>();
            Cargos = new ObservableCollection<Cargo>();            
            
            // Inicializar comandos
            SaveCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(GuardarAsync, CanGuardar);

            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            _ = CargarTiposDocumentoAsync();
            _ = CargarCargosAsync();
            
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DocumentoError) || e.PropertyName == nameof(CorreoError) ||
                    e.PropertyName == nameof(ValidandoDocumento) || e.PropertyName == nameof(ValidandoCorreo))
                {
                    OnPropertyChanged(nameof(PuedeGuardar));
                    SaveCommand.NotifyCanExecuteChanged();
                }
            };
        }

        private async Task CargarTiposDocumentoAsync()
        {
            var tipos = await _tipoDocumentoRepository.ObtenerTodosAsync();
            App.Current.Dispatcher.Invoke(() => {
                TiposDocumento.Clear();
                foreach (var tipo in tipos)
                    TiposDocumento.Add(tipo);
                // Preseleccionar 'Cédula de ciudadanía' exactamente si es registro nuevo
                if (Persona.TipoDocumentoId == Guid.Empty || Persona.TipoDocumento == null || !TiposDocumento.Any(td => td.IdTipoDocumento == Persona.TipoDocumentoId))
                {
                    var cedula = TiposDocumento.FirstOrDefault(t => t.Nombre != null && t.Nombre.Trim().ToLower() == "cédula de ciudadanía");
                    if (cedula != null)
                    {
                        Persona.TipoDocumento = cedula;
                        Persona.TipoDocumentoId = cedula.IdTipoDocumento;
                        OnPropertyChanged(nameof(Persona));
                        OnPropertyChanged(nameof(Persona.TipoDocumento));
                        OnPropertyChanged(nameof(Persona.TipoDocumentoId));
                    }
                }
            });
        }

        private async Task CargarCargosAsync()
        {
            var cargos = await _cargoRepository.ObtenerTodosAsync();
            App.Current.Dispatcher.Invoke(() => {
                Cargos.Clear();
                foreach (var cargo in cargos)
                    Cargos.Add(cargo);
            });
        }

        [RelayCommand(CanExecute = nameof(CanGuardar))]
        private async Task Guardar()
        {
            Persona.CargoId = Persona.Cargo?.IdCargo ?? Guid.Empty;
            Persona.TipoDocumentoId = Persona.TipoDocumento?.IdTipoDocumento ?? Guid.Empty;
            Persona.Activo = true;
            try
            {
                await _personaService.RegistrarPersonaAsync(Persona);
                Cerrar(true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error al registrar persona", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancelar()
        {
            Cerrar(false);
        }

        private void Cerrar(bool resultado)
        {
            if (System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this) is Window win)
                win.DialogResult = resultado;
        }

        private async Task ValidarDocumentoAsync()
        {
            DocumentoError = string.Empty;
            if (string.IsNullOrWhiteSpace(Persona.NumeroDocumento) || Persona.TipoDocumento == null)
                return;
            ValidandoDocumento = true;
            try
            {
                var esUnico = await _personaService.ValidarDocumentoUnicoAsync(Persona.TipoDocumento.IdTipoDocumento, Persona.NumeroDocumento);
                if (!esUnico)
                    DocumentoError = "El número de documento ya está registrado.";
            }
            finally { ValidandoDocumento = false; }
            SetValidationError(nameof(Persona.NumeroDocumento), DocumentoError);
        }

        private async Task ValidarCorreoAsync()
        {
            CorreoError = string.Empty;
            if (string.IsNullOrWhiteSpace(Persona.Correo))
                return;
            ValidandoCorreo = true;
            try
            {
                var esUnico = await _personaService.ValidarCorreoUnicoAsync(Persona.Correo);
                if (!esUnico)
                    CorreoError = "El correo electrónico ya está registrado.";
            }
            finally { ValidandoCorreo = false; }
            SetValidationError(nameof(Persona.Correo), CorreoError);
        }

        private void SetValidationError(string property, string error)
        {
            var errors = string.IsNullOrEmpty(error) ? new List<string>() : new List<string> { error };
            typeof(ValidatableViewModel).GetMethod("UpdateErrors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(this, new object[] { property, errors });
        }

        // Sobrescribir SetProperty para disparar validación asíncrona
        protected override bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            var changed = base.SetProperty(ref field, value, propertyName);
            if (changed)
            {
                if (propertyName == nameof(Persona.NumeroDocumento) || propertyName == nameof(Persona.TipoDocumento))
                    _ = ValidarDocumentoAsync();
                if (propertyName == nameof(Persona.Correo))
                    _ = ValidarCorreoAsync();
            }            
            return changed;
        }

        // === MÉTODOS DE GESTIÓN DE PERMISOS ===
        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }        
        
        private void RecalcularPermisos()
        {
            CanSavePersona = _currentUser.HasPermission("Personas.Crear");
        }

        // Método para guardar persona
        private async Task GuardarAsync()
        {
            if (Persona == null) return;

            try
            {
                // Validar datos antes de guardar
                if (string.IsNullOrWhiteSpace(Persona.Nombres) || string.IsNullOrWhiteSpace(Persona.Apellidos))
                {
                    // Manejar error de validación
                    return;
                }

                Persona.CargoId = Persona.Cargo?.IdCargo ?? Guid.Empty;
                var personaGuardada = await _personaService.RegistrarPersonaAsync(Persona);
                
                // Si llegamos aquí, el guardado fue exitoso
                // Se puede agregar lógica adicional según sea necesario
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
