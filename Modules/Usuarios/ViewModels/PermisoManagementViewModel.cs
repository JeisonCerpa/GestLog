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
    public class PermisoManagementViewModel : INotifyPropertyChanged
    {
        private readonly IPermisoService _permisoService;
        public ObservableCollection<Permiso> Permisos { get; set; } = new();
        private Permiso? _permisoSeleccionado = null;
        public Permiso? PermisoSeleccionado
        {
            get => _permisoSeleccionado;
            set { _permisoSeleccionado = value; OnPropertyChanged(); }
        }
        public ICommand RegistrarPermisoCommand { get; }
        public ICommand EditarPermisoCommand { get; }
        public ICommand DesactivarPermisoCommand { get; }
        public ICommand BuscarPermisosCommand { get; }
        public PermisoManagementViewModel(IPermisoService permisoService)
        {
            _permisoService = permisoService;
            RegistrarPermisoCommand = new RelayCommand(async _ => await RegistrarPermisoAsync(), _ => true);
            EditarPermisoCommand = new RelayCommand(async _ => await EditarPermisoAsync(), _ => PermisoSeleccionado != null);
            DesactivarPermisoCommand = new RelayCommand(async _ => await DesactivarPermisoAsync(), _ => PermisoSeleccionado != null);
            BuscarPermisosCommand = new RelayCommand(async _ => await BuscarPermisosAsync(), _ => true);
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
        private async Task RegistrarPermisoAsync() { await Task.CompletedTask; }
        private async Task EditarPermisoAsync() { await Task.CompletedTask; }
        private async Task DesactivarPermisoAsync() { await Task.CompletedTask; }
        private async Task BuscarPermisosAsync() { await Task.CompletedTask; }

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
