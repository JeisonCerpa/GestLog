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
    public class RolManagementViewModel : INotifyPropertyChanged
    {
        private readonly IRolService _rolService;
        public ObservableCollection<Rol> Roles { get; set; } = new();
        private Rol? _rolSeleccionado = null;
        public Rol? RolSeleccionado
        {
            get => _rolSeleccionado;
            set { _rolSeleccionado = value; OnPropertyChanged(); }
        }
        public ICommand RegistrarRolCommand { get; }
        public ICommand EditarRolCommand { get; }
        public ICommand DesactivarRolCommand { get; }
        public ICommand BuscarRolesCommand { get; }
        public RolManagementViewModel(IRolService rolService)
        {
            _rolService = rolService;
            RegistrarRolCommand = new RelayCommand(async _ => await RegistrarRolAsync(), _ => true);
            EditarRolCommand = new RelayCommand(async _ => await EditarRolAsync(), _ => RolSeleccionado != null);
            DesactivarRolCommand = new RelayCommand(async _ => await DesactivarRolAsync(), _ => RolSeleccionado != null);
            BuscarRolesCommand = new RelayCommand(async _ => await BuscarRolesAsync(), _ => true);
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
        private async Task RegistrarRolAsync() { await Task.CompletedTask; }
        private async Task EditarRolAsync() { await Task.CompletedTask; }
        private async Task DesactivarRolAsync() { await Task.CompletedTask; }
        private async Task BuscarRolesAsync() { await Task.CompletedTask; }

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
