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
using GestLog.Modules.Personas.Models.Enums;

namespace GestLog.Modules.Usuarios.ViewModels
{    
    public partial class PersonaEdicionViewModel : ValidatableViewModel
    {
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        
        private Persona _persona = null!;
        public Persona Persona
        {
            get => _persona;
            set { _persona = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Cargo> Cargos { get; }
        public ObservableCollection<string> Estados { get; }
        public ObservableCollection<TipoDocumento> TiposDocumento { get; }
        public ObservableCollection<object> Sedes { get; } = new();
        private readonly IPersonaService _personaService;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;
        private readonly ICargoRepository _cargoRepository;

        private string _documentoError = string.Empty;
        private string _correoError = string.Empty;
        private bool _validandoDocumento;        
        private bool _validandoCorreo;
        private string _documentoOriginal = string.Empty;
        private string _correoOriginal = string.Empty;

        // Propiedades de permisos
        private bool _canEditPersona = false;
        public bool CanEditPersona 
        { 
            get => _canEditPersona; 
            set { _canEditPersona = value; OnPropertyChanged(); } 
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
        public bool PuedeGuardar => string.IsNullOrEmpty(DocumentoError) && string.IsNullOrEmpty(CorreoError) && !ValidandoDocumento && !ValidandoCorreo && CanEditPersona;

        private bool CanGuardar() => PuedeGuardar;

        // Comando para guardar
        public CommunityToolkit.Mvvm.Input.AsyncRelayCommand SaveCommand { get; }

        public PersonaEdicionViewModel(Persona persona, ObservableCollection<string> estados, IPersonaService personaService, ITipoDocumentoRepository tipoDocumentoRepository, ICargoRepository cargoRepository, ICurrentUserService currentUserService)        
        {
            Persona = persona;
            Estados = estados;
            _personaService = personaService;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _cargoRepository = cargoRepository;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            
            TiposDocumento = new ObservableCollection<TipoDocumento>();
            Cargos = new ObservableCollection<Cargo>();            
            // Inicializar comandos
            SaveCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(GuardarAsync);

            // Configurar permisos reactivos
            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            _ = CargarTiposDocumentoAsync();
            _ = CargarCargosAsync();
            CargarSedes();
            _documentoOriginal = persona.NumeroDocumento;
            _correoOriginal = persona.Correo;
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
                // Asegurar que la referencia seleccionada sea la de la lista
                if (Persona.TipoDocumentoId != Guid.Empty)
                {
                    var seleccionado = TiposDocumento.FirstOrDefault(td => td.IdTipoDocumento == Persona.TipoDocumentoId);
                    if (seleccionado != null)
                    {
                        Persona.TipoDocumento = seleccionado;
                        OnPropertyChanged(nameof(Persona));
                        OnPropertyChanged(nameof(Persona.TipoDocumento));
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
                // Asegurar que la referencia seleccionada sea la de la lista
                if (Persona.CargoId != Guid.Empty)
                {
                    var seleccionado = Cargos.FirstOrDefault(c => c.IdCargo == Persona.CargoId);
                    if (seleccionado != null)
                    {
                        Persona.Cargo = seleccionado;
                        OnPropertyChanged(nameof(Persona));
                        OnPropertyChanged(nameof(Persona.Cargo));
                    }
                }
            });
        }

        private void CargarSedes()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Sedes.Clear();
                foreach (var val in Enum.GetValues(typeof(Sede)))
                    Sedes.Add(val);
            });
        }

        [RelayCommand(CanExecute = nameof(CanGuardar))]
        private async Task Guardar()
        {
            Persona.CargoId = Persona.Cargo?.IdCargo ?? Guid.Empty;
            Persona.TipoDocumentoId = Persona.TipoDocumento?.IdTipoDocumento ?? Guid.Empty;
            try
            {
                await _personaService.EditarPersonaAsync(Persona);
                Cerrar(true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error al editar persona", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Solo validar si el documento cambió
                if (Persona.NumeroDocumento != _documentoOriginal || Persona.TipoDocumento.IdTipoDocumento != Persona.TipoDocumentoId)
                {
                    var esUnico = await _personaService.ValidarDocumentoUnicoAsync(Persona.TipoDocumento.IdTipoDocumento, Persona.NumeroDocumento);
                    if (!esUnico)
                        DocumentoError = "El número de documento ya está registrado.";
                }
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
                if (Persona.Correo != _correoOriginal)
                {
                    var esUnico = await _personaService.ValidarCorreoUnicoAsync(Persona.Correo);
                    if (!esUnico)
                        CorreoError = "El correo electrónico ya está registrado.";
                }
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
            CanEditPersona = _currentUser.HasPermission("Personas.Editar");
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
                var personaGuardada = await _personaService.EditarPersonaAsync(Persona);
                
                // Si llegamos aquí, el guardado fue exitoso
                // Se puede agregar lógica adicional según sea necesario
            }
            catch (Exception)
            {
                // Manejar errores de guardado
                throw;
            }
        }
    }
}
