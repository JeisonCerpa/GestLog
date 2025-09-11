using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionMantenimientos.ViewModels
{
    public partial class RegistrarMantenimientoViewModel : ObservableObject
    {
        private readonly IMantenimientoService _mantenimientoService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private SeguimientoMantenimiento _seguimiento = new SeguimientoMantenimiento();

        public ObservableCollection<SeguimientoMantenimientoTarea> Tareas { get; } = new ObservableCollection<SeguimientoMantenimientoTarea>();

        public Action? RequestClose { get; set; }

        public RegistrarMantenimientoViewModel(IMantenimientoService mantenimientoService, IGestLogLogger logger)
        {
            _mantenimientoService = mantenimientoService;
            _logger = logger;
        }

        [RelayCommand]
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Seguimiento.FechaRealizacion = DateTime.Now;
                await _mantenimientoService.AddLogAsync(Seguimiento, Tareas, cancellationToken);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando mantenimiento");
            }
        }

        [RelayCommand]
        public void AddTarea()
        {
            Tareas.Add(new SeguimientoMantenimientoTarea { NombreTarea = "Nueva tarea", Completada = false });
        }

        [RelayCommand]
        public void RemoveTarea(SeguimientoMantenimientoTarea tarea)
        {
            Tareas.Remove(tarea);
        }

        // Nuevo: comando para cancelar / cerrar el modal desde la vista (genera CancelCommand)
        [RelayCommand]
        public void Cancel()
        {
            RequestClose?.Invoke();
        }
    }
}
