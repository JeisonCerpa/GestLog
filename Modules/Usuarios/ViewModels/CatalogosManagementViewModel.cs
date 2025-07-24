using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using GestLog.Views.Usuarios;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;

namespace GestLog.Modules.Usuarios.ViewModels
{
    public partial class CatalogosManagementViewModel : ObservableObject
    {
        private readonly ICargoService _cargoService;
        private readonly ITipoDocumentoRepository _tipoDocumentoRepository;
        private readonly IModalService _modalService;

        [ObservableProperty]
        private ObservableCollection<Cargo> cargos;
        [ObservableProperty]
        private Cargo? cargoSeleccionado;
        [ObservableProperty]
        private string mensajeErrorCargo = string.Empty;

        [ObservableProperty]
        private ObservableCollection<TipoDocumento> tiposDocumento;
        [ObservableProperty]
        private TipoDocumento? tipoDocumentoSeleccionado;
        [ObservableProperty]
        private string mensajeErrorTipoDocumento = string.Empty;

        [ObservableProperty]
        private Cargo? cargoEnEdicion;
        [ObservableProperty]
        private bool isModalCargoVisible;

        public CatalogosManagementViewModel(ICargoService cargoService, ITipoDocumentoRepository tipoDocumentoRepository, IModalService modalService)
        {
            _cargoService = cargoService;
            _tipoDocumentoRepository = tipoDocumentoRepository;
            _modalService = modalService;
            Cargos = new ObservableCollection<Cargo>();
            TiposDocumento = new ObservableCollection<TipoDocumento>();
        }

        public async Task InitializeAsync()
        {
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task RegistrarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }
            var existe = await _cargoService.ExisteNombreAsync(CargoSeleccionado.Nombre);
            if (existe)
            {
                MensajeErrorCargo = "Ya existe un cargo con ese nombre.";
                return;
            }
            await _cargoService.CrearCargoAsync(CargoSeleccionado);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task EditarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }
            await _cargoService.EditarCargoAsync(CargoSeleccionado);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task DesactivarCargo()
        {
            if (CargoSeleccionado == null) return;
            await _cargoService.EliminarCargoAsync(CargoSeleccionado.IdCargo);
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task RegistrarTipoDocumento()
        {
            MensajeErrorTipoDocumento = string.Empty;
            if (TipoDocumentoSeleccionado == null || string.IsNullOrWhiteSpace(TipoDocumentoSeleccionado.Nombre))
            {
                MensajeErrorTipoDocumento = "El nombre del tipo de documento es obligatorio.";
                return;
            }
            await _tipoDocumentoRepository.AgregarAsync(TipoDocumentoSeleccionado);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task EditarTipoDocumento()
        {
            MensajeErrorTipoDocumento = string.Empty;
            if (TipoDocumentoSeleccionado == null || string.IsNullOrWhiteSpace(TipoDocumentoSeleccionado.Nombre))
            {
                MensajeErrorTipoDocumento = "El nombre del tipo de documento es obligatorio.";
                return;
            }
            await _tipoDocumentoRepository.ActualizarAsync(TipoDocumentoSeleccionado);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public async Task DesactivarTipoDocumento()
        {
            if (TipoDocumentoSeleccionado == null) return;
            await _tipoDocumentoRepository.EliminarAsync(TipoDocumentoSeleccionado.IdTipoDocumento);
            TiposDocumento = new ObservableCollection<TipoDocumento>(await _tipoDocumentoRepository.ObtenerTodosAsync());
        }

        [RelayCommand]
        public void AbrirModalNuevoCargo()
        {
            CargoEnEdicion = new Cargo
            {
                IdCargo = Guid.NewGuid(),
                Nombre = string.Empty,
                Descripcion = string.Empty
            };
            MensajeErrorCargo = string.Empty;
            _modalService.MostrarCargoModal(this);
        }

        [RelayCommand]
        public void AbrirModalEditarCargo()
        {
            if (CargoSeleccionado == null) return;
            CargoEnEdicion = new Cargo
            {
                IdCargo = CargoSeleccionado.IdCargo,
                Nombre = CargoSeleccionado.Nombre,
                Descripcion = CargoSeleccionado.Descripcion
            };
            MensajeErrorCargo = string.Empty;
            _modalService.MostrarCargoModal(this);
        }

        [RelayCommand]
        public void CerrarModalCargo()
        {
            IsModalCargoVisible = false;
            CargoEnEdicion = null;
            MensajeErrorCargo = string.Empty;
        }

        public event Action? SolicitarCerrarModal;

        [RelayCommand]
        public async Task GuardarCargo()
        {
            MensajeErrorCargo = string.Empty;
            if (CargoEnEdicion == null || string.IsNullOrWhiteSpace(CargoEnEdicion.Nombre))
            {
                MensajeErrorCargo = "El nombre del cargo es obligatorio.";
                return;
            }
            // Si el cargo ya existe en la lista, es edición
            var esEdicion = Cargos.Any(c => c.IdCargo == CargoEnEdicion.IdCargo);
            if (!esEdicion)
            {
                var existe = await _cargoService.ExisteNombreAsync(CargoEnEdicion.Nombre);
                if (existe)
                {
                    MensajeErrorCargo = "Ya existe un cargo con ese nombre.";
                    return;
                }
                await _cargoService.CrearCargoAsync(CargoEnEdicion);
            }
            else
            {
                await _cargoService.EditarCargoAsync(CargoEnEdicion);
            }
            Cargos = new ObservableCollection<Cargo>(await _cargoService.ObtenerTodosAsync());
            CargoEnEdicion = null;
            SolicitarCerrarModal?.Invoke();
        }
    }
}
