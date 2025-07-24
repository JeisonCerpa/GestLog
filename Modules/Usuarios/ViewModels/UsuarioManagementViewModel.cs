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
    public class UsuarioManagementViewModel : INotifyPropertyChanged
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ICargoService _cargoService;
        private readonly IRolService _rolService;
        private readonly IPermisoService _permisoService;
        private readonly IAuditoriaService _auditoriaService;

        public ObservableCollection<Usuario> Usuarios { get; set; } = new();
        public ObservableCollection<Cargo> Cargos { get; set; } = new();
        public ObservableCollection<Rol> Roles { get; set; } = new();
        public ObservableCollection<Permiso> Permisos { get; set; } = new();
        public ObservableCollection<Auditoria> Auditorias { get; set; } = new();

        private Usuario? _usuarioSeleccionado = null;
        public Usuario? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set { _usuarioSeleccionado = value; OnPropertyChanged(); }
        }

        public ICommand RegistrarUsuarioCommand { get; }
        public ICommand EditarUsuarioCommand { get; }
        public ICommand DesactivarUsuarioCommand { get; }
        public ICommand BuscarUsuariosCommand { get; }
        public ICommand CargarAuditoriaCommand { get; }

        public UsuarioManagementViewModel(
            IUsuarioService usuarioService,
            ICargoService cargoService,
            IRolService rolService,
            IPermisoService permisoService,
            IAuditoriaService auditoriaService)
        {
            _usuarioService = usuarioService;
            _cargoService = cargoService;
            _rolService = rolService;
            _permisoService = permisoService;
            _auditoriaService = auditoriaService;

            RegistrarUsuarioCommand = new RelayCommand(async _ => await RegistrarUsuarioAsync(), _ => true);
            EditarUsuarioCommand = new RelayCommand(async _ => await EditarUsuarioAsync(), _ => UsuarioSeleccionado != null);
            DesactivarUsuarioCommand = new RelayCommand(async _ => await DesactivarUsuarioAsync(), _ => UsuarioSeleccionado != null);
            BuscarUsuariosCommand = new RelayCommand(async _ => await BuscarUsuariosAsync(), _ => true);
            CargarAuditoriaCommand = new RelayCommand(async _ => await CargarAuditoriaAsync(), _ => UsuarioSeleccionado != null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        private async Task RegistrarUsuarioAsync() { await Task.CompletedTask; }
        private async Task EditarUsuarioAsync() { await Task.CompletedTask; }
        private async Task DesactivarUsuarioAsync() { await Task.CompletedTask; }
        private async Task BuscarUsuariosAsync() { await Task.CompletedTask; }
        private async Task CargarAuditoriaAsync() { await Task.CompletedTask; }

        // Implementación simple de RelayCommand para MVVM
        public class RelayCommand : ICommand
        {
            private readonly Func<object?, Task> _execute;
            private readonly Predicate<object?>? _canExecute;
            public event EventHandler? CanExecuteChanged;

            public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
            public async void Execute(object? parameter) => await _execute(parameter);
            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
