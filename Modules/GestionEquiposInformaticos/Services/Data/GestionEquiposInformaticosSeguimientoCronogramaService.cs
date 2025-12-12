using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.DatabaseConnection;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using CommunityToolkit.Mvvm.Messaging;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Data
{
    public class GestionEquiposInformaticosSeguimientoCronogramaService : IGestionEquiposInformaticosSeguimientoCronogramaService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public GestionEquiposInformaticosSeguimientoCronogramaService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DeletePendientesFuturasByEquipoCodigoAsync(string codigoEquipo, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(codigoEquipo))
                throw new ArgumentException("El código del equipo es obligatorio.", nameof(codigoEquipo));

            try
            {
                await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);                // 1) Desactivar (soft-disable) TODOS los planes activos del equipo: Activo = false
                // Las ejecuciones históricas se preservan automáticamente (no se tocan)
                var planes = await context.PlanesCronogramaEquipos
                    .Where(p => p.CodigoEquipo == codigoEquipo && p.Activo == true)
                    .ToListAsync(cancellationToken);

                if (planes.Any())
                {
                    foreach (var p in planes)
                    {
                        p.Activo = false;
                    }
                }

                // 2) Eliminar seguimientos PENDIENTES cuya semana/fecha sea futura
                // Nota: modelo de Seguimiento está en el módulo GestionMantenimientos
                var now = DateTime.Now;

                // Solo considerar seguimientos que estén en estado PENDIENTE (no ejecutados)
                var pendientes = context.Seguimientos
                    .Where(s => s.Codigo == codigoEquipo && s.Estado == EstadoSeguimientoMantenimiento.Pendiente);

                // Calcular semana ISO actual y filtrar pendientes FUTUROS (FechaRealizacion == null y semana/anio > actual)
                var (anioActual, semanaActual) = Utilities.DateTimeWeekHelper.GetIsoYearWeek(now);

                var pendientesFuturas = pendientes
                    .Where(s => s.FechaRealizacion == null && (s.Anio > anioActual || (s.Anio == anioActual && s.Semana > semanaActual)));

                var pendientesList = await pendientesFuturas.ToListAsync(cancellationToken);
                if (pendientesList.Any())
                {
                    context.Seguimientos.RemoveRange(pendientesList);
                }

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("[GestionEquiposInformaticosSeguimientoCronogramaService] Desactivados {CountPlanes} planes y eliminados {CountSeguimientos} seguimientos futuros para equipo {Codigo}", planes.Count, pendientesList.Count, codigoEquipo);                // Notificar mediante mensajería del módulo de mantenimientos — usar WeakReferenceMessenger
                try
                {
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                    WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
                }
                catch (Exception mex)
                {
                    _logger.LogWarning(mex, "Error enviando mensajes de actualización tras eliminación de seguimientos/planes para {Codigo}", codigoEquipo);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar seguimientos futuros o desactivar planes para el equipo {Codigo}", codigoEquipo);
                throw;
            }
        }
    }
}
