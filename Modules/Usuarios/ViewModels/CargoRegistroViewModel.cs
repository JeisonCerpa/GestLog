using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System;
using System.Threading.Tasks;

namespace Modules.Usuarios.ViewModels
{
    public partial class CargoRegistroViewModel : ObservableObject
    {
        private readonly ICargoService _cargoService;
        private const int MaxNombreLength = 50;
        [ObservableProperty]
        private string nombre = string.Empty;
        [ObservableProperty]
        private string descripcion = string.Empty;
        [ObservableProperty]
        private string mensajeError = string.Empty;

        public IRelayCommand RegistrarCommand { get; }
        public IRelayCommand CancelarCommand { get; }

        public event Action? SolicitarCerrar;

        public CargoRegistroViewModel(ICargoService cargoService)
        {
            _cargoService = cargoService;
            RegistrarCommand = new RelayCommand(async () => await RegistrarAsync());
            CancelarCommand = new RelayCommand(() => SolicitarCerrar?.Invoke());
        }

        private async Task RegistrarAsync()
        {
            MensajeError = string.Empty;
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                MensajeError = "El nombre del cargo es obligatorio.";
                return;
            }
            if (Nombre.Length > MaxNombreLength)
            {
                MensajeError = $"El nombre no puede superar los {MaxNombreLength} caracteres.";
                return;
            }
            if (string.IsNullOrWhiteSpace(Descripcion))
            {
                MensajeError = "La descripción es obligatoria.";
                return;
            }
            var existe = await _cargoService.ExisteNombreAsync(Nombre);
            if (existe)
            {
                MensajeError = "Ya existe un cargo con ese nombre.";
                return;
            }
            var cargo = new Cargo { IdCargo = Guid.NewGuid(), Nombre = Nombre, Descripcion = Descripcion };
            await _cargoService.CrearCargoAsync(cargo);
            SolicitarCerrar?.Invoke();
        }
    }
}
