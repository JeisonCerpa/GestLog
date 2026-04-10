using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Personas.Models;
using GestLog.Modules.Personas.Models.Enums;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.ViewModels.Base;
using Modules.Personas.Interfaces;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class PersonaFormViewModel : ValidatableViewModel
    {
        public record SedeOption(Sede? Value, string Display);

        private readonly IPersonaService _personaService;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;
        private readonly ICargoRepository _cargoRepository;
        private readonly ICurrentUserService _currentUserService;
        private CurrentUserInfo _currentUser;
        private readonly bool _esEdicion;
        private string _documentoOriginal = string.Empty;
        private string _correoOriginal = string.Empty;

        private Persona _persona = new Persona
        {
            Nombres = string.Empty,
            Apellidos = string.Empty,
            NumeroDocumento = string.Empty,
            Correo = string.Empty,
            Telefono = string.Empty,
            Activo = true
        };

        public Persona Persona
        {
            get => _persona;
            set { _persona = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Cargo> Cargos { get; }
        public ObservableCollection<TipoDocumento> TiposDocumento { get; }
        public ObservableCollection<SedeOption> Sedes { get; } = new();

        public string FormTitle => _esEdicion ? "Editar Persona" : "Registrar Persona";
        public string FormSubtitle => _esEdicion ? "Ajusta los datos y el estado de la persona" : "Completa los datos básicos para crear una nueva persona";
        public string GuardarText => _esEdicion ? "Guardar cambios" : "Guardar";
        public bool MostrarEstado => _esEdicion;
        public bool MostrarDesactivar => _esEdicion;

        private string _documentoError = string.Empty;
        private string _correoError = string.Empty;
        private bool _validandoDocumento;
        private bool _validandoCorreo;
        private bool _canSavePersona;

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

        public bool PuedeGuardar => string.IsNullOrEmpty(DocumentoError)
                                    && string.IsNullOrEmpty(CorreoError)
                                    && !ValidandoDocumento
                                    && !ValidandoCorreo
                                    && CanSavePersona;

        private bool CanGuardar() => PuedeGuardar;

        public PersonaFormViewModel(
            Persona persona,
            IPersonaService personaService,
            ITipoDocumentoRepository tipoDocumentoRepository,
            ICargoRepository cargoRepository,
            ICurrentUserService currentUserService,
            bool esEdicion = false)
        {
            Persona = persona;
            _personaService = personaService;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _cargoRepository = cargoRepository;
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _currentUser = _currentUserService.Current ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            _esEdicion = esEdicion;

            Cargos = new ObservableCollection<Cargo>();
            TiposDocumento = new ObservableCollection<TipoDocumento>();

            if (_esEdicion)
            {
                _documentoOriginal = persona.NumeroDocumento;
                _correoOriginal = persona.Correo;
            }

            RecalcularPermisos();
            _currentUserService.CurrentUserChanged += OnCurrentUserChanged;

            _ = CargarTiposDocumentoAsync();
            _ = CargarCargosAsync();
            CargarSedes();

            PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(DocumentoError) || e.PropertyName == nameof(CorreoError) ||
                    e.PropertyName == nameof(ValidandoDocumento) || e.PropertyName == nameof(ValidandoCorreo) ||
                    e.PropertyName == nameof(CanSavePersona))
                {
                    OnPropertyChanged(nameof(PuedeGuardar));
                    GuardarCommand.NotifyCanExecuteChanged();
                }
            };
        }

        private async Task CargarTiposDocumentoAsync()
        {
            var tipos = await _tipoDocumentoRepository.ObtenerTodosAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                TiposDocumento.Clear();
                foreach (var tipo in tipos)
                    TiposDocumento.Add(tipo);

                if (_esEdicion)
                {
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
                }
                else if (Persona.TipoDocumentoId == Guid.Empty || Persona.TipoDocumento == null || !TiposDocumento.Any(td => td.IdTipoDocumento == Persona.TipoDocumentoId))
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
            App.Current.Dispatcher.Invoke(() =>
            {
                Cargos.Clear();
                foreach (var cargo in cargos)
                    Cargos.Add(cargo);

                if (_esEdicion)
                {
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
                }
                else if (Persona.Cargo == null && Cargos.Any())
                {
                    Persona.Cargo = Cargos.First();
                    Persona.CargoId = Persona.Cargo.IdCargo;
                    OnPropertyChanged(nameof(Persona));
                    OnPropertyChanged(nameof(Persona.Cargo));
                    OnPropertyChanged(nameof(Persona.CargoId));
                }
            });
        }

        private void CargarSedes()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Sedes.Clear();
                Sedes.Add(new SedeOption(null, "Sin sede"));
                foreach (var val in Enum.GetValues(typeof(Sede)))
                {
                    var sede = (Sede)val;
                    var name = Enum.GetName(typeof(Sede), sede) ?? sede.ToString();
                    var field = typeof(Sede).GetField(name);
                    var descAttr = field?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).FirstOrDefault() as System.ComponentModel.DescriptionAttribute;
                    var display = descAttr != null ? descAttr.Description : name;
                    Sedes.Add(new SedeOption(sede, display));
                }
            });
        }

        [RelayCommand(CanExecute = nameof(CanGuardar))]
        private async Task Guardar()
        {
            Persona.CargoId = Persona.Cargo?.IdCargo ?? Guid.Empty;
            Persona.TipoDocumentoId = Persona.TipoDocumento?.IdTipoDocumento ?? Guid.Empty;

            if (!_esEdicion)
                Persona.Activo = true;

            try
            {
                if (_esEdicion)
                    await _personaService.EditarPersonaAsync(Persona);
                else
                    await _personaService.RegistrarPersonaAsync(Persona);

                Cerrar(true);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, _esEdicion ? "Error al editar persona" : "Error al registrar persona", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var debeValidarUnicidad = !_esEdicion || Persona.NumeroDocumento != _documentoOriginal || Persona.TipoDocumento.IdTipoDocumento != Persona.TipoDocumentoId;
                if (debeValidarUnicidad)
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
                var debeValidarUnicidad = !_esEdicion || Persona.Correo != _correoOriginal;
                if (debeValidarUnicidad)
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

        private void OnCurrentUserChanged(object? sender, CurrentUserInfo? user)
        {
            _currentUser = user ?? new CurrentUserInfo { Username = string.Empty, FullName = string.Empty };
            RecalcularPermisos();
        }

        private void RecalcularPermisos()
        {
            CanSavePersona = _esEdicion
                ? _currentUser.HasPermission("Personas.Editar")
                : _currentUser.HasPermission("Personas.Crear");
        }
    }
}
