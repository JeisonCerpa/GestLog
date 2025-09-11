using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models.Entities;

namespace GestLog.Modules.GestionMantenimientos.Interfaces
{
    public interface IMantenimientoService
    {
        Task<IEnumerable<SeguimientoMantenimiento>> GetPlannedForDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task AddLogAsync(SeguimientoMantenimiento seguimiento, IEnumerable<SeguimientoMantenimientoTarea>? tareas = null, CancellationToken cancellationToken = default);
        Task<IEnumerable<SeguimientoMantenimiento>> GetHistoryForEquipoAsync(string codigoEquipo, CancellationToken cancellationToken = default);
        Task<IEnumerable<MantenimientoPlantillaTarea>> GetPlantillasAsync(CancellationToken cancellationToken = default);
        Task AddPlantillaAsync(MantenimientoPlantillaTarea plantilla, CancellationToken cancellationToken = default);
    }
}
