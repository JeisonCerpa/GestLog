using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionEquiposInformaticos.Models.Entities;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Services.Core.Logging;
using GestLog.Utilities; // añadido para helper ISO

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Data
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
                
                var equipoExiste = await context.EquiposInformaticos
                    .AnyAsync(e => e.Codigo == plan.CodigoEquipo);
                    
                if (!equipoExiste)
                    throw new ArgumentException($"No se encontró el equipo con código: {plan.CodigoEquipo}");

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
                // Persistir también Responsable y Descripción si se modificaron desde la UI
                existing.Responsable = plan.Responsable?.Trim() ?? string.Empty;
                existing.Descripcion = plan.Descripcion?.Trim() ?? string.Empty;
                existing.ChecklistJson = plan.ChecklistJson;
                
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Plan de cronograma actualizado: {PlanId}", plan.PlanId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan de cronograma: {PlanId}", plan.PlanId);
                throw;
            }
        }        public async Task DeleteAsync(Guid planId)
        {
            try
            {
                using var context = _dbContextFactory.CreateDbContext();
                
                var plan = await context.PlanesCronogramaEquipos
                    .FirstOrDefaultAsync(p => p.PlanId == planId);
                    
                if (plan == null)
                    throw new ArgumentException($"No se encontró el plan con ID: {planId}");

                // ✅ Las ejecuciones se desvincularán automáticamente (PlanId = NULL)
                // El historial se preserva gracias a CodigoEquipo y snapshots
                context.PlanesCronogramaEquipos.Remove(plan);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Plan eliminado (historial preservado): {PlanId}", planId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plan de cronograma: {PlanId}", planId);
                throw;
            }
        }

        /// <summary>
        /// ✅ NUEVO: Consultar ejecuciones por equipo (no por plan)
        /// Permite acceder al historial aunque el plan haya sido eliminado
        /// </summary>
        public async Task<List<EjecucionSemanal>> GetEjecucionesByEquipoAsync(string codigoEquipo, int anio)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.EjecucionesSemanales
                .Where(e => e.CodigoEquipo == codigoEquipo && e.AnioISO == anio)
                .OrderByDescending(e => e.AnioISO)
                .ThenByDescending(e => e.SemanaISO)
                .ToListAsync();
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

                var ejecucion = await context.EjecucionesSemanales
                    .FirstOrDefaultAsync(e => e.PlanId == planId && e.AnioISO == anioISO && e.SemanaISO == semanaISO);

                if (ejecucion == null)
                {
                    var fechaObjetivo = DateTimeWeekHelper.GetFechaObjetivoSemana(anioISO, semanaISO, plan.DiaProgramado);
                    
                    ejecucion = new EjecucionSemanal
                    {
                        PlanId = planId,
                        CodigoEquipo = plan.CodigoEquipo,  // ✅ Guardar código del equipo
                        AnioISO = (short)anioISO,
                        SemanaISO = (byte)semanaISO,
                        FechaObjetivo = fechaObjetivo,
                        Estado = 2, // Completada
                        DescripcionPlanSnapshot = plan.Descripcion,  // ✅ Snapshot
                        ResponsablePlanSnapshot = plan.Responsable    // ✅ Snapshot
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
                
                _logger.LogInformation("Ejecución registrada para plan: {PlanId}, Equipo: {Equipo}, Semana: {Semana}/{Anio}", 
                    planId, plan.CodigoEquipo, semanaISO, anioISO);
                
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
            
            var planes = await context.PlanesCronogramaEquipos
                .Include(p => p.Equipo)
                .Include(p => p.Ejecuciones.Where(e => e.AnioISO == anioISO && e.SemanaISO == semanaISO))
                .ToListAsync();

            var monday = DateTimeWeekHelper.FirstDateOfWeekISO8601(anioISO, semanaISO);
            var planesParaSemana = new List<PlanCronogramaEquipo>();

            foreach (var plan in planes)
            {
                var fechaObjetivo = monday.AddDays(plan.DiaProgramado - 1);
                var (anio, semana) = DateTimeWeekHelper.GetIsoYearWeek(fechaObjetivo);
                if (anio == anioISO && semana == semanaISO)
                {
                    planesParaSemana.Add(plan);
                }
            }
            return planesParaSemana;
        }        public async Task<List<EjecucionSemanal>> GetEjecucionesByAnioAsync(int anioISO)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.EjecucionesSemanales
                .Include(e => e.Plan)
                .Include(e => e.Equipo)  // ✅ NUEVO: Incluir equipo para trazabilidad
                .Where(e => e.AnioISO == anioISO)
                .OrderByDescending(e => e.AnioISO)
                .ThenByDescending(e => e.SemanaISO)
                .ToListAsync();
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var anosEjecuciones = await context.EjecucionesSemanales
                .Select(e => (int)e.AnioISO)
                .Distinct()
                .ToListAsync();

            var anosFechaObjetivo = await context.EjecucionesSemanales
                .Select(e => e.FechaObjetivo.Year)
                .Distinct()
                .ToListAsync();

            var anosPlanesActivos = await context.PlanesCronogramaEquipos
                .Where(p => p.Activo)
                .Select(p => p.FechaCreacion.Year)
                .Distinct()
                .ToListAsync();

            var anoMinimoPlan = anosPlanesActivos.Any() ? anosPlanesActivos.Min() : (int?)null;

            var todosLosAnos = anosEjecuciones
                .Concat(anosFechaObjetivo)
                .Concat(anosPlanesActivos)
                .Distinct()
                .ToList();

            if (anoMinimoPlan.HasValue)
            {
                todosLosAnos = todosLosAnos
                    .Where(y => y >= anoMinimoPlan.Value)
                    .ToList();
            }

            return todosLosAnos
                .OrderByDescending(y => y)
                .ToList();
        }        /// <summary>
        /// ✅ Genera y obtiene ejecuciones CON TRAZABILIDAD COMPLETA para un año.
        /// Crea registros "No Realizado" para semanas completamente pasadas sin ejecución.
        /// </summary>
        public async Task<List<EjecucionSemanal>> GenerarYObtenerEjecucionesConTrazabilidadAsync(int anioISO)
        {
            var timestampInicio = DateTime.UtcNow;
            _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] ⏰ ENTRADA: GenerarYObtenerEjecucionesConTrazabilidadAsync para año {anio} - ThreadId: {threadId}", anioISO, System.Threading.Thread.CurrentThread.ManagedThreadId);
            
            try
            {
                using var context = _dbContextFactory.CreateDbContext();

                // 1. Validar que el año es válido
                var anoMinimoPlan = await context.PlanesCronogramaEquipos
                    .Where(p => p.Activo)
                    .Select(p => p.FechaCreacion.Year)
                    .ToListAsync();
                
                var anoMin = anoMinimoPlan.DefaultIfEmpty(int.MaxValue).Min();
                if (anioISO < anoMin)
                {
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] Año {anio} anterior al año mínimo de planes ({anoMinimo}), retornando lista vacía", anioISO, anoMin);
                    return new List<EjecucionSemanal>();
                }

                // 2. Obtener todos los planes activos
                var planesActivos = await context.PlanesCronogramaEquipos
                    .Where(p => p.Activo)
                    .Include(p => p.Equipo)
                    .Include(p => p.Ejecuciones.Where(e => e.AnioISO == anioISO))
                    .ToListAsync();

                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] Planes activos cargados: {count}", planesActivos.Count);

                if (planesActivos.Count == 0)
                {
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] No hay planes activos, retornando lista vacía");
                    return new List<EjecucionSemanal>();
                }

                // 3. Para cada plan, generar registros faltantes
                int registrosGenerados = 0;
                var hoy = DateTime.Today;
                var (anioHoy, semanaHoy) = DateTimeWeekHelper.GetIsoYearWeek(hoy);

                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] Hoy es {hoy} (Año ISO: {anioHoy}, Semana ISO: {semanaHoy})", hoy, anioHoy, semanaHoy);

                foreach (var plan in planesActivos)
                {
                    var fechaInicioTrazabilidad = plan.FechaCreacion;
                    var (anioCreacion, semanaCreacion) = DateTimeWeekHelper.GetIsoYearWeek(fechaInicioTrazabilidad);

                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] Plan {planId} ({codigo}): FechaCreación={fecha} (Año ISO: {anioCreacion}, Semana: {semanaCreacion})", 
                        plan.PlanId, plan.CodigoEquipo, fechaInicioTrazabilidad, anioCreacion, semanaCreacion);

                    // Solo procesar si el plan se creó en el año solicitado o anterior
                    if (anioCreacion > anioISO)
                    {
                        _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS]   → Saltando: Plan creado en año {anioCreacion} > {anioISO}", anioCreacion, anioISO);
                        continue;
                    }

                    int semanaInicio = anioCreacion == anioISO ? semanaCreacion : 1;
                    int semanaFin = anioHoy == anioISO ? semanaHoy : 52;

                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS]   → Semanas a procesar: {semanaInicio} a {semanaFin}", semanaInicio, semanaFin);

                    // Obtener semanas ya registradas en BD para este plan (CRÍTICO PARA DEDUPLICACIÓN)
                    var semanasEnBd = await context.EjecucionesSemanales
                        .Where(e => e.PlanId == plan.PlanId && e.AnioISO == anioISO)
                        .Select(e => e.SemanaISO)
                        .ToListAsync();

                    var semanasRegistradas = new HashSet<byte>(semanasEnBd);
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS]   → Semanas YA EN BD: {semanas}", semanasEnBd.Count > 0 ? string.Join(", ", semanasEnBd) : "ninguna");                    // Procesar cada semana
                    int registrosEsteplan = 0;
                    for (int semana = semanaInicio; semana <= semanaFin; semana++)
                    {
                        // Verificar si ya existe
                        if (semanasRegistradas.Contains((byte)semana))
                        {
                            continue;
                        }

                        // Verificar que la semana ya pasó completamente
                        var domingoDelaSemana = DateTimeWeekHelper.GetFechaObjetivoSemana(anioISO, semana, 7);
                        
                        if (domingoDelaSemana >= hoy)
                        {
                            continue;
                        }

                        // Generar registro "No Realizado"
                        var fechaObjetivo = DateTimeWeekHelper.GetFechaObjetivoSemana(anioISO, semana, plan.DiaProgramado);
                        
                        var ejecucionFaltante = new EjecucionSemanal
                        {
                            PlanId = plan.PlanId,
                            CodigoEquipo = plan.CodigoEquipo,
                            AnioISO = (short)anioISO,
                            SemanaISO = (byte)semana,
                            FechaObjetivo = fechaObjetivo,
                            Estado = 3, // NoRealizada
                            DescripcionPlanSnapshot = plan.Descripcion,
                            ResponsablePlanSnapshot = plan.Responsable,
                            UsuarioEjecuta = null,
                            FechaEjecucion = null
                        };

                        context.EjecucionesSemanales.Add(ejecucionFaltante);
                        semanasRegistradas.Add((byte)semana);
                        registrosGenerados++;
                        registrosEsteplan++;
                    }_logger.LogInformation("[TRAZABILIDAD_DUPLICADOS]   → Subtotal para este plan: {registros} registros generados", registrosEsteplan);
                }                // 4. Guardar cambios
                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] Total registros a guardar: {total}", registrosGenerados);
                if (registrosGenerados > 0)
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] ✅ SaveChangesAsync() completado - {registros} registros guardados", registrosGenerados);
                }
                else
                {
                    _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] No había registros para generar");
                }

                // 5. Retornar todas las ejecuciones
                var result = await GetEjecucionesByAnioAsync(anioISO);
                var timestampFinal = DateTime.UtcNow;
                _logger.LogInformation("[TRAZABILIDAD_DUPLICADOS] ⏰ SALIDA: Retornando {count} ejecuciones - Tiempo total: {ms}ms", result.Count, (timestampFinal - timestampInicio).TotalMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TRAZABILIDAD_DUPLICADOS] ❌ ERROR en GenerarYObtenerEjecucionesConTrazabilidadAsync para año {anio}", anioISO);
                throw;
            }
        }

        private void ValidarPlan(PlanCronogramaEquipo plan)
        {
            if (string.IsNullOrWhiteSpace(plan.CodigoEquipo))
                throw new ArgumentException("El código del equipo es requerido");

            if (plan.DiaProgramado < 1 || plan.DiaProgramado > 7)
                throw new ArgumentException("El día programado debe estar entre 1 (Lunes) y 7 (Domingo)");
        }

        // Métodos de cálculo de fechas eliminados: se usa DateTimeWeekHelper centralizado.
    }
}
