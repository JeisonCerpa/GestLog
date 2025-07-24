using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Personas.Models;
using GestLog.Modules.Usuarios.Models;
using Modules.Personas.Interfaces;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Data;
using System.ComponentModel;
using System.Timers;
using System.Windows.Controls;
using PersonaDetalleView = GestLog.Views.IdentidadCatalogos.Personas.PersonaDetalleView;
using PersonaEdicionView = GestLog.Views.IdentidadCatalogos.Personas.PersonaEdicionView;
using GestLog.Views.IdentidadCatalogos.Personas;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class PersonaManagementViewModel : ObservableObject
    {
        private readonly IPersonaService _personaService;
        private readonly ICargoService _cargoService;
        private readonly ICargoRepository _cargoRepository;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;

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
        private System.Windows.Controls.UserControl? vistaActual;

        [ObservableProperty]
        private string mensajeValidacion = string.Empty;

        public string TextoActivarDesactivar => PersonaSeleccionada?.Activo == true ? "Desactivar" : "Activar";

        public PersonaManagementViewModel(IPersonaService personaService, ICargoService cargoService, ICargoRepository cargoRepository, ITipoDocumentoRepository tipoDocumentoRepository)
        {
            _personaService = personaService;
            _cargoService = cargoService;
            _cargoRepository = cargoRepository;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _ = InicializarAsync();
            PersonasView = CollectionViewSource.GetDefaultView(Personas);
            if (PersonasView != null)
                PersonasView.Filter = FiltrarPersona;
        }

        private async Task InicializarAsync()
        {
            await CargarCargos();
            await CargarPersonas();
        }

        [RelayCommand]
        private async Task RegistrarPersona()
        {
            var primerCargo = Cargos.FirstOrDefault();
            var nuevaPersona = new Persona {
                Nombres = string.Empty,
                Apellidos = string.Empty,
                TipoDocumento = string.Empty,
                NumeroDocumento = string.Empty,
                Correo = string.Empty,
                Telefono = string.Empty,
                Cargo = primerCargo,
                CargoId = primerCargo?.IdCargo ?? Guid.Empty,
                Estado = "Activo",
                Activo = true
            };
            var registroViewModel = new PersonaRegistroViewModel(nuevaPersona, _personaService, _tipoDocumentoRepository, _cargoRepository);
            var window = new PersonaRegistroWindow
            {
                DataContext = registroViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            var result = window.ShowDialog();
            if (result == true)
            {
                await CargarPersonas();
            }
        }

        [RelayCommand]
        private async Task GuardarPersona()
        {
            if (PersonaSeleccionada == null) return;
            try
            {
                PersonaSeleccionada.CargoId = PersonaSeleccionada.Cargo?.IdCargo ?? Guid.Empty;
                PersonaSeleccionada.Activo = PersonaSeleccionada.Estado == "Activo";
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
            Personas = new ObservableCollection<Persona>(lista);
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
            var detalle = new PersonaDetalleView();
            detalle.DataContext = this;
            VistaActual = detalle;
        }

        private void MostrarEdicion()
        {
            var edicion = new PersonaEdicionView();
            edicion.DataContext = this;
            VistaActual = edicion;
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
            // Filtro por texto
            if (!string.IsNullOrWhiteSpace(FiltroTexto))
            {
                var terminos = FiltroTexto.ToLowerInvariant().Split(';').Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t));
                var campos = new[]
                {
                    p.Nombres?.ToLowerInvariant() ?? string.Empty,
                    p.Apellidos?.ToLowerInvariant() ?? string.Empty,
                    p.NumeroDocumento?.ToLowerInvariant() ?? string.Empty,
                    p.Correo?.ToLowerInvariant() ?? string.Empty,
                    p.Telefono?.ToLowerInvariant() ?? string.Empty,
                    p.Cargo?.Nombre?.ToLowerInvariant() ?? string.Empty
                };
                if (!terminos.All(termino => campos.Any(campo => campo.Contains(termino))))
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
        private async Task EditarPersona()
        {
            if (PersonaSeleccionada == null) return;
            // Clonar la persona para edición (evita cambios directos hasta guardar)
            var personaCopia = new Persona
            {
                IdPersona = PersonaSeleccionada.IdPersona,
                Nombres = PersonaSeleccionada.Nombres,
                Apellidos = PersonaSeleccionada.Apellidos,
                TipoDocumento = PersonaSeleccionada.TipoDocumento,
                NumeroDocumento = PersonaSeleccionada.NumeroDocumento,
                Correo = PersonaSeleccionada.Correo,
                Telefono = PersonaSeleccionada.Telefono,
                Cargo = PersonaSeleccionada.Cargo,
                CargoId = PersonaSeleccionada.CargoId,
                Estado = PersonaSeleccionada.Estado,
                Activo = PersonaSeleccionada.Activo
            };
            var edicionViewModel = new PersonaEdicionViewModel(personaCopia, Estados, _personaService, _tipoDocumentoRepository, _cargoRepository);
            var window = new GestLog.Views.IdentidadCatalogos.Personas.PersonaEdicionWindow
            {
                DataContext = edicionViewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };
            var result = window.ShowDialog();
            if (result == true)
            {
                await CargarPersonas();
                // Selecciona la persona editada en la lista
                PersonaSeleccionada = Personas.FirstOrDefault(p => p.IdPersona == personaCopia.IdPersona);
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
        }

        [RelayCommand]
        private async Task ActivarDesactivarPersona(Persona persona)
        {
            persona.Activo = !persona.Activo;
            persona.Estado = persona.Activo ? "Activo" : "Inactivo";
            await _personaService.EditarPersonaAsync(persona);
            await CargarPersonas();
            PersonasView?.Refresh();
        }
    }
}
