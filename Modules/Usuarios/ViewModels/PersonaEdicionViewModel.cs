using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Personas.Models;
using Modules.Personas.Interfaces;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class PersonaEdicionViewModel : ObservableObject, IDataErrorInfo
    {
        [ObservableProperty]
        private Persona persona;

        public ObservableCollection<Cargo> Cargos { get; }
        public ObservableCollection<string> Estados { get; }
        public ObservableCollection<TipoDocumento> TiposDocumento { get; }
        private readonly IPersonaService _personaService;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;
        private readonly ICargoRepository _cargoRepository;

        public PersonaEdicionViewModel(Persona persona, ObservableCollection<string> estados, IPersonaService personaService, ITipoDocumentoRepository tipoDocumentoRepository, ICargoRepository cargoRepository)
        {
            Persona = persona;
            Estados = estados;
            _personaService = personaService;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _cargoRepository = cargoRepository;
            TiposDocumento = new ObservableCollection<TipoDocumento>();
            Cargos = new ObservableCollection<Cargo>();
            _ = CargarTiposDocumentoAsync();
            _ = CargarCargosAsync();
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

        [RelayCommand]
        private async Task Guardar()
        {
            Persona.CargoId = Persona.Cargo?.IdCargo ?? Guid.Empty;
            Persona.TipoDocumentoId = Persona.TipoDocumento?.IdTipoDocumento ?? Guid.Empty;
            await _personaService.EditarPersonaAsync(Persona);
            Cerrar(true);
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

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(Persona.Nombres):
                        if (string.IsNullOrWhiteSpace(Persona.Nombres))
                            return "El nombre es obligatorio.";
                        break;
                    case nameof(Persona.Apellidos):
                        if (string.IsNullOrWhiteSpace(Persona.Apellidos))
                            return "El apellido es obligatorio.";
                        break;
                    case nameof(Persona.NumeroDocumento):
                        if (string.IsNullOrWhiteSpace(Persona.NumeroDocumento))
                            return "El número de documento es obligatorio.";
                        if (!Regex.IsMatch(Persona.NumeroDocumento, "^[0-9A-Za-z]+$"))
                            return "El número de documento no es válido.";
                        // Validación de unicidad (simulada, reemplazar por consulta real)
                        // if (await _personaService.ExisteDocumentoAsync(Persona.NumeroDocumento))
                        //     return "El número de documento ya está registrado.";
                        break;
                    case nameof(Persona.Correo):
                        if (string.IsNullOrWhiteSpace(Persona.Correo))
                            return "El correo electrónico es obligatorio.";
                        if (!Regex.IsMatch(Persona.Correo, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
                            return "El correo electrónico no tiene un formato válido.";
                        // Validación de unicidad (simulada, reemplazar por consulta real)
                        // if (await _personaService.ExisteCorreoAsync(Persona.Correo))
                        //     return "El correo electrónico ya está registrado.";
                        break;
                    case nameof(Persona.Telefono):
                        if (string.IsNullOrWhiteSpace(Persona.Telefono))
                            return "El teléfono es obligatorio.";
                        if (!Regex.IsMatch(Persona.Telefono, "^[0-9]{7,15}$"))
                            return "El teléfono debe tener entre 7 y 15 dígitos.";
                        break;
                    case nameof(Persona.Cargo):
                        if (Persona.Cargo == null)
                            return "Debe seleccionar un cargo.";
                        break;
                    case nameof(Persona.TipoDocumento):
                        if (Persona.TipoDocumento == null)
                            return "Debe seleccionar un tipo de documento.";
                        break;
                }
                return string.Empty;
            }
        }

        public string Error => string.Empty;
    }
}
