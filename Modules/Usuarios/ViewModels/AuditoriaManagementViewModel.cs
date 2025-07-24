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
    public class AuditoriaManagementViewModel : INotifyPropertyChanged
    {
        private readonly IAuditoriaService _auditoriaService;
        public ObservableCollection<Auditoria> Auditorias { get; set; } = new();
        private Auditoria? _auditoriaSeleccionada = null;
        public Auditoria? AuditoriaSeleccionada
        {
            get => _auditoriaSeleccionada;
            set { _auditoriaSeleccionada = value; OnPropertyChanged(); }
        }
        public ICommand BuscarAuditoriasCommand { get; }
        public AuditoriaManagementViewModel(IAuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
            BuscarAuditoriasCommand = new RelayCommand(async _ => await BuscarAuditoriasAsync(), _ => true);
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
        private async Task BuscarAuditoriasAsync() { await Task.CompletedTask; }

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
