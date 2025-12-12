using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;

namespace GestLog.Modules.GestionMantenimientos.Services.Data
{
    public class MaintenanceService : IMantenimientoService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbFactory;

        public MaintenanceService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbFactory)
        {
            _logger = logger;
            _dbFactory = dbFactory;
        }

        public async Task AddPlantillaAsync(MantenimientoPlantillaTarea plantilla, CancellationToken cancellationToken = default)
        {
            using var db = _dbFactory.CreateDbContext();
            db.MantenimientoPlantillas.Add(plantilla);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[MaintenanceService] Plantilla agregada: {Id}", plantilla.Id);
            WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
        }

        public async Task<IEnumerable<MantenimientoPlantillaTarea>> GetPlantillasAsync(CancellationToken cancellationToken = default)
        {
            using var db = _dbFactory.CreateDbContext();
            return await db.MantenimientoPlantillas.OrderBy(p => p.Orden).ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<SeguimientoMantenimiento>> GetPlannedForDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            using var db = _dbFactory.CreateDbContext();
            int semana = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            int anio = date.Year;
            var seguimientos = await db.Seguimientos.Where(s => s.Anio == anio && s.Semana == semana).ToListAsync(cancellationToken);
            return seguimientos;
        }

        public async Task AddLogAsync(SeguimientoMantenimiento seguimiento, IEnumerable<SeguimientoMantenimientoTarea>? tareas = null, CancellationToken cancellationToken = default)
        {
            using var db = _dbFactory.CreateDbContext();
            // Si existe el seguimiento pendiente: actualizarlo; sino insertarlo
            var existing = await db.Seguimientos.FirstOrDefaultAsync(s => s.Id == seguimiento.Id, cancellationToken);
            if (existing != null)
            {
                existing.FechaRealizacion = seguimiento.FechaRealizacion ?? DateTime.Now;
                existing.Estado = seguimiento.Estado;
                existing.Observaciones = seguimiento.Observaciones;
                existing.Costo = seguimiento.Costo;
                existing.Responsable = seguimiento.Responsable;
            }
            else
            {
                db.Seguimientos.Add(seguimiento);
            }

            if (tareas != null && tareas.Any())
            {
                foreach (var t in tareas)
                {
                    t.SeguimientoMantenimientoId = seguimiento.Id;
                    db.SeguimientoTareas.Add(t);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[MaintenanceService] Seguimiento guardado: {Id}", seguimiento.Id);
            WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
        }

        public async Task<IEnumerable<SeguimientoMantenimiento>> GetHistoryForEquipoAsync(string codigoEquipo, CancellationToken cancellationToken = default)
        {
            using var db = _dbFactory.CreateDbContext();
            var list = await db.Seguimientos.Where(s => s.Codigo == codigoEquipo).OrderByDescending(s => s.FechaRegistro).ToListAsync(cancellationToken);
            return list;
        }
    }
}
