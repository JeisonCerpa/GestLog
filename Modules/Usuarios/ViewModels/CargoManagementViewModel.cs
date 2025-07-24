using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modules.Usuarios.ViewModels
{
    public class CargoManagementViewModel : INotifyPropertyChanged
    {
        private readonly ICargoService _cargoService;
        public ObservableCollection<Cargo> Cargos { get; set; } = new();
        private Cargo? _cargoSeleccionado = null;
        public Cargo? CargoSeleccionado
        {
            get => _cargoSeleccionado;
            set { _cargoSeleccionado = value; OnPropertyChanged(); }
        }
        public ICommand RegistrarCargoCommand { get; }
        public ICommand EditarCargoCommand { get; }
        public ICommand DesactivarCargoCommand { get; }
        public ICommand BuscarCargosCommand { get; }
        private string _mensajeError = string.Empty;
        public string MensajeError
        {
            get => _mensajeError;
            set { _mensajeError = value; OnPropertyChanged(); }
        }

        public CargoManagementViewModel(ICargoService cargoService)
        {
            _cargoService = cargoService;
            RegistrarCargoCommand = new RelayCommand(async _ => await RegistrarCargoAsync(), _ => true);
            EditarCargoCommand = new RelayCommand(async _ => await EditarCargoAsync(), _ => CargoSeleccionado != null);
            DesactivarCargoCommand = new RelayCommand(async _ => await DesactivarCargoAsync(), _ => CargoSeleccionado != null);
            BuscarCargosCommand = new RelayCommand(async _ => await BuscarCargosAsync(), _ => true);
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
        private async Task RegistrarCargoAsync()
        {
            MensajeError = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeError = "El nombre del cargo es obligatorio.";
                return;
            }
            // Validación de unicidad
            var existe = await _cargoService.ExisteNombreAsync(CargoSeleccionado.Nombre);
            if (existe)
            {
                MensajeError = "Ya existe un cargo con ese nombre.";
                return;
            }
            await _cargoService.CrearCargoAsync(CargoSeleccionado);
            await BuscarCargosAsync();
        }

        private async Task EditarCargoAsync()
        {
            MensajeError = string.Empty;
            if (CargoSeleccionado == null || string.IsNullOrWhiteSpace(CargoSeleccionado.Nombre))
            {
                MensajeError = "El nombre del cargo es obligatorio.";
                return;
            }
            await _cargoService.EditarCargoAsync(CargoSeleccionado);
            await BuscarCargosAsync();
        }

        private async Task DesactivarCargoAsync()
        {
            if (CargoSeleccionado == null) return;
            await _cargoService.EliminarCargoAsync(CargoSeleccionado.IdCargo);
            await BuscarCargosAsync();
        }

        private async Task BuscarCargosAsync()
        {
            Cargos.Clear();
            var cargos = await _cargoService.ObtenerTodosAsync();
            foreach (var cargo in cargos)
                Cargos.Add(cargo);
        }

        // Implementación de RelayCommand local
        public class RelayCommand : ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Predicate<object?>? _canExecute;
            public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }
            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
            public async void Execute(object? parameter) => await _execute(parameter);
            public event EventHandler? CanExecuteChanged;
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
