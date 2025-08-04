using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Modules.Usuarios.Helpers;

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
        private string _mensajeEstado = string.Empty;
        public string MensajeEstado
        {
            get => _mensajeEstado;
            set { _mensajeEstado = value; OnPropertyChanged(); }
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
        private async Task RegistrarPermisoAsync()
        {
            MensajeEstado = string.Empty;
            try
            {
                var nuevoPermiso = new Permiso
                {
                    Nombre = PermisoSeleccionado?.Nombre ?? string.Empty,
                    Descripcion = PermisoSeleccionado?.Descripcion ?? string.Empty,
                    PermisoPadreId = PermisoSeleccionado?.PermisoPadreId,
                    Modulo = PermisoSeleccionado?.Modulo ?? string.Empty // Inicialización obligatoria
                };
                var permisoCreado = await _permisoService.CrearPermisoAsync(nuevoPermiso);
                Permisos.Add(permisoCreado);
                MensajeEstado = $"Permiso '{permisoCreado.Nombre}' creado exitosamente.";
            }
            catch (PermisoDuplicadoException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al crear permiso: {ex.Message}";
            }
        }
        private async Task EditarPermisoAsync()
        {
            MensajeEstado = string.Empty;
            if (PermisoSeleccionado == null)
            {
                MensajeEstado = "Debe seleccionar un permiso para editar.";
                return;
            }
            try
            {
                var permisoEditado = await _permisoService.EditarPermisoAsync(PermisoSeleccionado);
                var idx = Permisos.IndexOf(PermisoSeleccionado);
                if (idx >= 0)
                    Permisos[idx] = permisoEditado;
                MensajeEstado = $"Permiso '{permisoEditado.Nombre}' editado exitosamente.";
            }
            catch (PermisoDuplicadoException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al editar permiso: {ex.Message}";
            }
        }
        private async Task DesactivarPermisoAsync()
        {
            MensajeEstado = string.Empty;
            if (PermisoSeleccionado == null)
            {
                MensajeEstado = "Debe seleccionar un permiso para eliminar.";
                return;
            }
            try
            {
                await _permisoService.EliminarPermisoAsync(PermisoSeleccionado.IdPermiso);
                Permisos.Remove(PermisoSeleccionado);
                MensajeEstado = $"Permiso eliminado correctamente.";
                PermisoSeleccionado = null;
            }
            catch (PermisoNotFoundException ex)
            {
                MensajeEstado = ex.Message;
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al eliminar permiso: {ex.Message}";
            }
        }
        private async Task BuscarPermisosAsync()
        {
            MensajeEstado = string.Empty;
            try
            {
                Permisos.Clear();
                var permisos = await _permisoService.ObtenerTodosAsync();
                foreach (var permiso in permisos)
                    Permisos.Add(permiso);
                MensajeEstado = $"Se cargaron {Permisos.Count} permisos.";
            }
            catch (Exception ex)
            {
                MensajeEstado = $"Error al cargar permisos: {ex.Message}";
            }
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
