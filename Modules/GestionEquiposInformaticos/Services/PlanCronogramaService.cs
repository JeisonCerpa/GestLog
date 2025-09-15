using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Services
{
    public class PlanCronogramaService : IPlanCronogramaService
    {
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly IGestLogLogger _logger;

        public PlanCronogramaService(IDbContextFactory<GestLogDbContext> dbContextFactory, IGestLogLogger logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<PlanCronogramaEquipo>> GetAllAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.PlanesCronogramaEquipos
                .Include(p => p.Equipo)
                .Include(p => p.Ejecuciones)
                .ToListAsync();
        }

        public async Task<PlanCronogramaEquipo?> GetByIdAsync(Guid planId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.PlanesCronogramaEquipos
                .Include(p => p.Equipo)
                .Include(p => p.Ejecuciones)
                .FirstOrDefaultAsync(p => p.PlanId == planId);
        }

        public async Task<List<PlanCronogramaEquipo>> GetByCodigoEquipoAsync(string codigoEquipo)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.PlanesCronogramaEquipos
                .Include(p => p.Equipo)
                .Include(p => p.Ejecuciones)
                .Where(p => p.CodigoEquipo == codigoEquipo)
                .ToListAsync();
        }

        public async Task<PlanCronogramaEquipo> CreateAsync(PlanCronogramaEquipo plan)
        {
            try
            {
                ValidarPlan(plan);
                
                using var context = _dbContextFactory.CreateDbContext();
                
                // Verificar que el equipo existe
                var equipoExiste = await context.EquiposInformaticos
                    .AnyAsync(e => e.Codigo == plan.CodigoEquipo);
                    
                if (!equipoExiste)
                    throw new ArgumentException($"No se encontró el equipo con código: {plan.CodigoEquipo}");

                // Garantizar que campos opcionales no sean null para evitar errores en BD
                plan.Descripcion = plan.Descripcion?.Trim() ?? string.Empty;
                plan.Responsable = plan.Responsable?.Trim() ?? string.Empty;

                context.PlanesCronogramaEquipos.Add(plan);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Plan de cronograma creado para equipo: {CodigoEquipo}, Día: {Dia}", 
                    plan.CodigoEquipo, plan.DiaProgramado);
                
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan de cronograma para equipo: {CodigoEquipo}", plan.CodigoEquipo);
                throw;
            }
        }

        public async Task UpdateAsync(PlanCronogramaEquipo plan)
        {
            try
            {
                ValidarPlan(plan);
                
                using var context = _dbContextFactory.CreateDbContext();
                
                var existing = await context.PlanesCronogramaEquipos
                    .FirstOrDefaultAsync(p => p.PlanId == plan.PlanId);
                    
                if (existing == null)
                    throw new ArgumentException($"No se encontró el plan con ID: {plan.PlanId}");

                existing.DiaProgramado = plan.DiaProgramado;
                existing.ChecklistJson = plan.ChecklistJson;
                
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Plan de cronograma actualizado: {PlanId}", plan.PlanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan de cronograma: {PlanId}", plan.PlanId);
                throw;
            }
        }

        public async Task DeleteAsync(Guid planId)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                
                var plan = await context.PlanesCronogramaEquipos
                    .FirstOrDefaultAsync(p => p.PlanId == planId);
                    
                if (plan == null)
                    throw new ArgumentException($"No se encontró el plan con ID: {planId}");

                context.PlanesCronogramaEquipos.Remove(plan);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Plan de cronograma eliminado: {PlanId}", planId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plan de cronograma: {PlanId}", planId);
                throw;
            }
        }

        public async Task<List<EjecucionSemanal>> GetEjecucionesByPlanAsync(Guid planId, int anio)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.EjecucionesSemanales
                .Where(e => e.PlanId == planId && e.AnioISO == anio)
                .OrderBy(e => e.SemanaISO)
                .ToListAsync();
        }

        public async Task<EjecucionSemanal> RegistrarEjecucionAsync(Guid planId, int anioISO, int semanaISO, DateTime fechaEjecucion, string usuarioEjecuta, string? resultadoJson = null)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                
                var plan = await context.PlanesCronogramaEquipos
                    .FirstOrDefaultAsync(p => p.PlanId == planId);
                    
                if (plan == null)
                    throw new ArgumentException($"No se encontró el plan con ID: {planId}");

                // Buscar ejecución existente o crear nueva
                var ejecucion = await context.EjecucionesSemanales
                    .FirstOrDefaultAsync(e => e.PlanId == planId && e.AnioISO == anioISO && e.SemanaISO == semanaISO);

                if (ejecucion == null)
                {
                    // Calcular fecha objetivo basada en el día programado de la semana
                    var fechaObjetivo = CalcularFechaObjetivo(anioISO, semanaISO, plan.DiaProgramado);
                    
                    ejecucion = new EjecucionSemanal
                    {
                        PlanId = planId,
                        AnioISO = (short)anioISO,
                        SemanaISO = (byte)semanaISO,
                        FechaObjetivo = fechaObjetivo,
                        Estado = 2 // Completada
                    };
                    context.EjecucionesSemanales.Add(ejecucion);
                }
                else
                {
                    ejecucion.Estado = 2; // Completada
                }

                ejecucion.FechaEjecucion = fechaEjecucion;
                ejecucion.UsuarioEjecuta = usuarioEjecuta;
                ejecucion.ResultadoJson = resultadoJson;

                await context.SaveChangesAsync();
                
                _logger.LogInformation("Ejecución registrada para plan: {PlanId}, Semana: {Semana}/{Anio}", 
                    planId, semanaISO, anioISO);
                
                return ejecucion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar ejecución para plan: {PlanId}", planId);
                throw;
            }
        }

        public async Task<List<PlanCronogramaEquipo>> GetPlanesParaSemanaAsync(int anioISO, int semanaISO)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Obtener todos los planes activos y filtrar por día de la semana
            var planes = await context.PlanesCronogramaEquipos
                .Include(p => p.Equipo)
                .Include(p => p.Ejecuciones.Where(e => e.AnioISO == anioISO && e.SemanaISO == semanaISO))
                .ToListAsync();

            // Filtrar planes que corresponden al día de la semana actual
            var fechaInicioSemana = CalcularFechaInicioSemana(anioISO, semanaISO);
            var planesParaSemana = new List<PlanCronogramaEquipo>();

            foreach (var plan in planes)
            {
                var fechaObjetivo = fechaInicioSemana.AddDays(plan.DiaProgramado - 1);
                var semanaObjetivo = GetSemanaISO(fechaObjetivo);
                
                if (semanaObjetivo.semana == semanaISO && semanaObjetivo.anio == anioISO)
                {
                    planesParaSemana.Add(plan);
                }
            }

            return planesParaSemana;
        }

        public async Task<List<EjecucionSemanal>> GetEjecucionesByAnioAsync(int anioISO)
        {
            using var context = _dbContextFactory.CreateDbContext();
#pragma warning disable CS8602 // Desreferencia de una referencia posiblemente NULL
            return await context.EjecucionesSemanales
                .Include(e => e.Plan).ThenInclude(p => p.Equipo)
                .Where(e => e.AnioISO == anioISO)
                .ToListAsync();
#pragma warning restore CS8602
        }

        private void ValidarPlan(PlanCronogramaEquipo plan)
        {
            if (string.IsNullOrWhiteSpace(plan.CodigoEquipo))
                throw new ArgumentException("El código del equipo es requerido");

            if (plan.DiaProgramado < 1 || plan.DiaProgramado > 7)
                throw new ArgumentException("El día programado debe estar entre 1 (Lunes) y 7 (Domingo)");
        }

        private DateTime CalcularFechaObjetivo(int anioISO, int semanaISO, byte diaProgramado)
        {
            var fechaInicioSemana = CalcularFechaInicioSemana(anioISO, semanaISO);
            return fechaInicioSemana.AddDays(diaProgramado - 1);
        }

        private DateTime CalcularFechaInicioSemana(int anio, int semana)
        {
            // Calcular el lunes de la primera semana del año ISO
            var jan1 = new DateTime(anio, 1, 1);
            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var firstMondayOfFirstWeek = firstThursday.AddDays(-3);
            
            // Ajustar si la primera semana pertenece al año anterior
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            
            if (firstWeek > 1)
                firstMondayOfFirstWeek = firstMondayOfFirstWeek.AddDays(7);
            
            return firstMondayOfFirstWeek.AddDays((semana - 1) * 7);
        }

        private (int anio, int semana) GetSemanaISO(DateTime fecha)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            var anio = fecha.Year;
            var semana = cal.GetWeekOfYear(fecha, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            
            // Ajustar año si la semana pertenece al siguiente año
            if (fecha.Month == 12 && semana == 1)
                anio++;
            else if (fecha.Month == 1 && semana > 50)
                anio--;
                
            return (anio, semana);
        }
    }
}