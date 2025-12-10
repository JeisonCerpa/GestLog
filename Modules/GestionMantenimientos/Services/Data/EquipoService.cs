using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using System;
using GestLog.Services.Core.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;

namespace GestLog.Modules.GestionMantenimientos.Services.Data
{
    public class EquipoService : IEquipoService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        public EquipoService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<EquipoDto>> GetAllAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipos = await dbContext.Equipos.ToListAsync();
            return equipos.Select(e => new EquipoDto
            {
                Codigo = e.Codigo,
                Nombre = e.Nombre,
                Marca = e.Marca,
                Estado = e.Estado,
                Sede = e.Sede,
                Precio = e.Precio,
                Observaciones = e.Observaciones,
                Clasificacion = e.Clasificacion,
                CompradoA = e.CompradoA,
                FechaRegistro = e.FechaRegistro,
                FrecuenciaMtto = e.FrecuenciaMtto,
                FechaBaja = e.FechaBaja,
                FechaCompra = e.FechaCompra
                // SemanaInicioMtto eliminado
            });
        }

        public async Task<EquipoDto?> GetByCodigoAsync(string codigo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipo = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == codigo);
            if (equipo == null) return null;
            return new EquipoDto
            {
                Codigo = equipo.Codigo,
                Nombre = equipo.Nombre,
                Marca = equipo.Marca,
                Estado = equipo.Estado,
                Sede = equipo.Sede,
                Precio = equipo.Precio,
                Observaciones = equipo.Observaciones,
                Clasificacion = equipo.Clasificacion,
                CompradoA = equipo.CompradoA,
                FechaRegistro = equipo.FechaRegistro,
                FrecuenciaMtto = equipo.FrecuenciaMtto,
                FechaBaja = equipo.FechaBaja,
                FechaCompra = equipo.FechaCompra
                // SemanaInicioMtto eliminado
            };
        }

        /// <summary>
        /// 🚀 Obtiene solo los códigos de todos los equipos (optimizado para validación)
        /// Usado en diálogos de creación/edición para validar códigos duplicados rápidamente
        /// </summary>
        public async Task<IEnumerable<string>> GetAllCodigosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return await dbContext.Equipos
                .AsNoTracking()
                .Select(e => e.Codigo)
                .ToListAsync();
        }

        public async Task AddAsync(EquipoDto equipo)
        {
            try
            {
                ValidarEquipo(equipo);
                using var dbContext = _dbContextFactory.CreateDbContext();                if (await dbContext.Equipos.AnyAsync(e => e.Codigo == equipo.Codigo))
                    throw new GestionMantenimientosDomainException($"Ya existe un equipo con el código '{equipo.Codigo}'.");
                  // Forzar la fecha de registro a la fecha actual
                var fechaRegistro = DateTime.Now;
                // Si FechaCompra no se proporciona, usar la fecha de registro
                var fechaCompra = equipo.FechaCompra ?? fechaRegistro;
                int semanaInicio = CalcularSemanaISO8601(fechaCompra);
                // Si Nombre se deja vacio, permitir null (nombre ahora nullable en entidad)
                if (string.IsNullOrWhiteSpace(equipo.Nombre))
                    equipo.Nombre = null;
                var entity = new Equipo
                {
                    Codigo = equipo.Codigo!,
                    Nombre = equipo.Nombre,
                    Marca = equipo.Marca,
                    Estado = equipo.Estado ?? Models.Enums.EstadoEquipo.Activo,
                    Sede = equipo.Sede,
                    FechaRegistro = fechaRegistro,
                    Clasificacion = equipo.Clasificacion,
                    CompradoA = equipo.CompradoA,
                    Precio = equipo.Precio,
                    Observaciones = equipo.Observaciones,
                    FrecuenciaMtto = equipo.FrecuenciaMtto,
                    FechaBaja = equipo.FechaBaja,
                    FechaCompra = fechaCompra
                    // SemanaInicioMtto eliminado
                };                dbContext.Equipos.Add(entity);
                await dbContext.SaveChangesAsync();
                
                // Actualizar el DTO con las fechas para que estén disponibles
                equipo.FechaRegistro = fechaRegistro;
                equipo.FechaCompra = fechaCompra;
                  _logger.LogInformation("[EquipoService] Equipo agregado correctamente: {Codigo}", equipo.Codigo ?? "");
                
                // Generar cronogramas desde el año de compra hasta el siguiente año (solo si es octubre o después)
                if (equipo.FrecuenciaMtto != null)
                {
                    // Primero, eliminar cronogramas anteriores si existen (para evitar duplicados)
                    using (var dbContextClean = _dbContextFactory.CreateDbContext())
                    {
                        var cronogramasAnteriores = await dbContextClean.Cronogramas.Where(c => c.Codigo == equipo.Codigo).ToListAsync();
                        if (cronogramasAnteriores.Any())
                        {
                            dbContextClean.Cronogramas.RemoveRange(cronogramasAnteriores);
                            await dbContextClean.SaveChangesAsync();
                            _logger.LogInformation("[EquipoService] Cronogramas anteriores eliminados para: {Codigo}", equipo.Codigo ?? "");
                        }
                    }                    var anioRegistro = fechaCompra.Year;
                    var anioActual = DateTime.Now.Year;
                    var mesActual = DateTime.Now.Month;
                      // Solo generar el año siguiente si estamos en octubre o después
                    var anioLimite = anioActual;
                    if (mesActual >= 10)
                    {
                        anioLimite = anioActual + 1;
                    }
                    
                    // Acumular cronogramas para guardar en batch (evita múltiples SaveChangesAsync)
                    var cronogramasAAgregar = new List<CronogramaMantenimiento>();
                    
                    for (int anio = anioRegistro; anio <= anioLimite; anio++)
                    {
                        using var dbContext2 = _dbContextFactory.CreateDbContext();
                        
                        // No duplicar cronogramas si ya existen
                        bool existe = await dbContext2.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);
                        if (existe) continue;
                        
                        int semanaIni = 1;
                        if (anio == anioRegistro && equipo.FrecuenciaMtto != null)
                        {                            // PRIMER AÑO: La primera semana de mantenimiento es la semana de FechaCompra + salto
                            int salto = equipo.FrecuenciaMtto switch
                            {
                                Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                _ => 1
                            };
                            int proximaSemana = semanaInicio + salto;
                            int yearsWeeks = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                            
                            // Si excede el año actual, la primera semana será en el siguiente año
                            // No generar cronograma en este año, se creará en el siguiente
                            if (proximaSemana > yearsWeeks)
                            {
                                _logger.LogInformation("[EquipoService] Primera semana de mantenimiento ({Original}) excede el año {Codigo}, se generará en siguiente año", 
                                    proximaSemana, equipo.Codigo ?? "");
                                continue;
                            }
                            semanaIni = proximaSemana;
                        }else if (anio > anioRegistro && equipo.FrecuenciaMtto != null)
                        {                            // AÑOS POSTERIORES: Calcular a partir del último mantenimiento del año anterior
                            var cronogramaAnterior = await dbContext2.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == (anio - 1));
                            if (cronogramaAnterior != null)
                            {
                                int lastWeek = System.Array.FindLastIndex(cronogramaAnterior.Semanas, s => s);
                                
                                if (lastWeek >= 0)
                                {
                                    int salto = equipo.FrecuenciaMtto switch
                                    {
                                        Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                        Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                        Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                        Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                        Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                        Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                        Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                        Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                        _ => 1
                                    };
                                      int ultimaSemana = lastWeek + 1; // Convertir a 1-based
                                    int proximaSemana = ultimaSemana + salto;
                                    int weeksInPreviousYear = System.Globalization.ISOWeek.GetWeeksInYear(anio - 1);
                                    int weeksInCurrentYear = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                                    
                                    // Si se pasa del año anterior, ajustar al año actual
                                    if (proximaSemana > weeksInPreviousYear)
                                    {
                                        proximaSemana = proximaSemana - weeksInPreviousYear;
                                        // Asegurar que la semana calculada sea válida en el año actual
                                        if (proximaSemana > weeksInCurrentYear)
                                        {
                                            proximaSemana = ((proximaSemana - 1) % weeksInCurrentYear) + 1;
                                        }
                                    }
                                    
                                    semanaIni = proximaSemana;
                                    _logger.LogInformation($"[EquipoService] Cronograma {anio} creado - Equipo={equipo.Codigo}, SemanaInicio={semanaIni}");
                                }
                            }                            else if (anio - 1 == anioRegistro && (equipo.FechaCompra.HasValue || equipo.FechaRegistro.HasValue))
                            {                                // Caso especial: Si el cronograma del año anterior se saltó (porque la próxima semana > semanas en año)
                                // pero el equipo se registró ese año, usar la semana de registro del equipo
                                var fechaParaCalculo = equipo.FechaCompra ?? equipo.FechaRegistro;
                                int semanaRegistroEquipo = System.Globalization.ISOWeek.GetWeekOfYear(fechaParaCalculo.Value);
                                
                                int salto = equipo.FrecuenciaMtto switch
                                {
                                    Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                    Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                    Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                    Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                    Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                    Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                    Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                    Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                    _ => 1
                                };
                                
                                int proximaSemana = semanaRegistroEquipo + salto;
                                int weeksInPreviousYear = System.Globalization.ISOWeek.GetWeeksInYear(anio - 1);
                                int weeksInCurrentYear = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                                
                                // Si se pasa del año anterior, ajustar al año actual
                                if (proximaSemana > weeksInPreviousYear)
                                {
                                    proximaSemana = proximaSemana - weeksInPreviousYear;
                                    // Asegurar que la semana calculada sea válida en el año actual
                                    if (proximaSemana > weeksInCurrentYear)
                                    {
                                        proximaSemana = ((proximaSemana - 1) % weeksInCurrentYear) + 1;
                                    }
                                }
                                
                                semanaIni = proximaSemana;
                                _logger.LogInformation($"[EquipoService] Cronograma {anio} creado desde semana registro - Equipo={equipo.Codigo}, SemanaRegistro={semanaRegistroEquipo}, SemanaInicio={semanaIni}");
                            }                            else
                            {
                                // Si no hay cronograma anterior ni el equipo se registró en el año anterior, usar semana 1 por defecto
                                semanaIni = 1;
                                _logger.LogWarning($"[EquipoService] No se encontró cronograma anterior para {equipo.Codigo} año {anio - 1}. Usando semana 1.");
                            }                        }
                        var semanas = CronogramaService.GenerarSemanas(semanaIni, equipo.FrecuenciaMtto, anio);
                        var cronograma = new CronogramaMantenimiento
                        {
                            Codigo = equipo.Codigo!,
                            Nombre = equipo.Nombre!,
                            Marca = equipo.Marca,
                            Sede = equipo.Sede?.ToString(),
                            FrecuenciaMtto = equipo.FrecuenciaMtto,
                            Semanas = semanas,
                            Anio = anio
                        };
                        
                        cronogramasAAgregar.Add(cronograma);
                    }
                    
                    // Guardar todos los cronogramas en una sola operación (mucho más eficiente)
                    if (cronogramasAAgregar.Any())
                    {
                        using var dbContextFinal = _dbContextFactory.CreateDbContext();
                        dbContextFinal.Cronogramas.AddRange(cronogramasAAgregar);
                        await dbContextFinal.SaveChangesAsync();
                        _logger.LogInformation("[EquipoService] {Count} cronogramas agregados para: {Codigo}", cronogramasAAgregar.Count, equipo.Codigo ?? "");
                    }
                }
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[EquipoService] Validation error on add");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoService] Unexpected error on add");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al agregar el equipo. Por favor, contacte al administrador.", ex);
            }
        }        public async Task UpdateAsync(EquipoDto equipo)
        {
            try
            {
                ValidarEquipo(equipo);
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == equipo.Codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el equipo a actualizar.");
                
                // 🔍 Detectar si la frecuencia de mantenimiento cambió
                bool frecuenciaChanged = entity.FrecuenciaMtto != equipo.FrecuenciaMtto;
                
                // No permitir cambiar el código
                // entity.Codigo = equipo.Codigo; // NO modificar
                // Evitar forzar Nombre a "-"; permitir null
                entity.Nombre = string.IsNullOrWhiteSpace(equipo.Nombre) ? null : equipo.Nombre;
                entity.Marca = equipo.Marca;
                entity.Estado = equipo.Estado ?? Models.Enums.EstadoEquipo.Activo;
                entity.Sede = equipo.Sede;
                entity.Precio = equipo.Precio;
                entity.Observaciones = equipo.Observaciones;
                entity.Clasificacion = equipo.Clasificacion;
                entity.CompradoA = equipo.CompradoA;
                entity.FechaRegistro = equipo.FechaRegistro ?? entity.FechaRegistro;
                entity.FechaCompra = equipo.FechaCompra; // Persistir Fecha de Compra al actualizar
                entity.FrecuenciaMtto = equipo.FrecuenciaMtto;
                entity.FechaBaja = equipo.FechaBaja;
                // SemanaInicioMtto eliminado
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[EquipoService] Equipo actualizado correctamente: {Codigo}", equipo.Codigo ?? "");
                
                // 🔄 Si la frecuencia cambió, regenerar cronogramas
                if (frecuenciaChanged && equipo.FrecuenciaMtto != null)
                {
                    _logger.LogInformation("[EquipoService] Frecuencia cambió para {Codigo}. Regenerando cronogramas...", equipo.Codigo ?? "");
                    
                    // Eliminar cronogramas antiguos
                    var cronogramasAntiguos = await dbContext.Cronogramas.Where(c => c.Codigo == equipo.Codigo).ToListAsync();
                    if (cronogramasAntiguos.Any())
                    {
                        dbContext.Cronogramas.RemoveRange(cronogramasAntiguos);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("[EquipoService] Cronogramas antiguos eliminados para: {Codigo}", equipo.Codigo ?? "");
                    }
                    
                    // Regenerar cronogramas con la nueva frecuencia
                    var anioRegistro = (equipo.FechaCompra ?? entity.FechaCompra ?? DateTime.Now).Year;
                    var anioActual = DateTime.Now.Year;
                    var mesActual = DateTime.Now.Month;
                    var anioLimite = anioActual;
                    if (mesActual >= 10)
                    {
                        anioLimite = anioActual + 1;
                    }
                      // Acumular cronogramas nuevos para guardar en batch (evita múltiples SaveChangesAsync)
                    var cronogramasNuevos = new List<CronogramaMantenimiento>();
                    
                    for (int anio = anioRegistro; anio <= anioLimite; anio++)
                    {
                        // Evitar duplicados
                        bool existe = await dbContext.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);
                        if (existe) continue;
                        
                        int semanaIni = 1;
                        if (anio == anioRegistro)
                        {
                            // PRIMER AÑO: Primera semana de mantenimiento
                            int semanaInicio = CalcularSemanaISO8601(equipo.FechaCompra ?? entity.FechaCompra ?? DateTime.Now);
                            int salto = equipo.FrecuenciaMtto switch
                            {
                                Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                _ => 1
                            };int proximaSemana = semanaInicio + salto;
                            int yearsWeeks = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                            
                            // Si excede el año actual, la primera semana será en el siguiente año
                            // No generar cronograma en este año, se creará en el siguiente
                            if (proximaSemana > yearsWeeks)
                            {
                                _logger.LogInformation("[EquipoService] Primera semana de mantenimiento ({Original}) excede el año {Codigo}, se generará en siguiente año (Update)", 
                                    proximaSemana, equipo.Codigo ?? "");
                                continue;
                            }
                            semanaIni = proximaSemana;
                        }                        else if (anio > anioRegistro)
                        {
                            // AÑOS POSTERIORES: Calcular a partir del último mantenimiento del año anterior
                            var cronogramaAnterior = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == (anio - 1));
                            if (cronogramaAnterior != null)
                            {
                                int lastWeek = System.Array.FindLastIndex(cronogramaAnterior.Semanas, s => s);
                                if (lastWeek >= 0)
                                {
                                    int salto = equipo.FrecuenciaMtto switch
                                    {
                                        Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                        Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                        Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                        Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                        Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                        Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                        Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                        Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                        _ => 1
                                    };
                                    
                                    int ultimaSemana = lastWeek + 1;
                                    int proximaSemana = ultimaSemana + salto;
                                    int weeksInPreviousYear = System.Globalization.ISOWeek.GetWeeksInYear(anio - 1);
                                    int weeksInCurrentYear = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                                    
                                    if (proximaSemana > weeksInPreviousYear)
                                    {
                                        proximaSemana = proximaSemana - weeksInPreviousYear;
                                        if (proximaSemana > weeksInCurrentYear)
                                        {
                                            proximaSemana = ((proximaSemana - 1) % weeksInCurrentYear) + 1;
                                        }
                                    }
                                      semanaIni = proximaSemana;
                                }                            }                            else if (anio - 1 == anioRegistro && (equipo.FechaCompra.HasValue || entity.FechaCompra.HasValue || entity.FechaRegistro.HasValue))
                            {
                                // Caso especial: Si el cronograma del año anterior se saltó (porque excede semanas)
                                // pero el equipo se registró ese año, usar la semana de registro del equipo
                                var fechaParaCalculo = equipo.FechaCompra ?? entity.FechaCompra ?? entity.FechaRegistro ?? DateTime.Now;
                                int semanaRegistroEquipo = CalcularSemanaISO8601(fechaParaCalculo);
                                
                                int salto = equipo.FrecuenciaMtto switch
                                {
                                    Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                    Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                    Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                    Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                    Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                    Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 17,
                                    Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                    Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                    _ => 1
                                };
                                
                                int proximaSemana = semanaRegistroEquipo + salto;
                                int weeksInPreviousYear = System.Globalization.ISOWeek.GetWeeksInYear(anio - 1);
                                int weeksInCurrentYear = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                                
                                // Si se pasa del año anterior, ajustar al año actual
                                if (proximaSemana > weeksInPreviousYear)
                                {
                                    proximaSemana = proximaSemana - weeksInPreviousYear;
                                    // Asegurar que la semana calculada sea válida en el año actual
                                    if (proximaSemana > weeksInCurrentYear)
                                    {
                                        proximaSemana = ((proximaSemana - 1) % weeksInCurrentYear) + 1;
                                    }
                                }
                                
                                semanaIni = proximaSemana;
                                _logger.LogInformation($"[EquipoService] Cronograma {anio} creado desde semana registro (Update) - Equipo={equipo.Codigo}, SemanaRegistro={semanaRegistroEquipo}, SemanaInicio={semanaIni}");
                            }
                            else
                            {
                                semanaIni = 1;
                            }
                        }
                          // Generar nuevo cronograma
                        var semanas = CronogramaService.GenerarSemanas(semanaIni, equipo.FrecuenciaMtto, anio);
                        var cronograma = new CronogramaMantenimiento
                        {
                            Codigo = equipo.Codigo!,
                            Nombre = equipo.Nombre!,
                            Marca = equipo.Marca,
                            Sede = equipo.Sede?.ToString(),
                            FrecuenciaMtto = equipo.FrecuenciaMtto,
                            Semanas = semanas,
                            Anio = anio
                        };
                        
                        cronogramasNuevos.Add(cronograma);
                    }
                    
                    // Guardar todos los cronogramas nuevos en una sola operación (mucho más eficiente)
                    if (cronogramasNuevos.Any())
                    {
                        dbContext.Cronogramas.AddRange(cronogramasNuevos);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("[EquipoService] {Count} cronogramas nuevos creados para: {Codigo}", cronogramasNuevos.Count, equipo.Codigo ?? "");
                    }
                      // 🔄 Ajustar seguimientos: eliminar pendientes de semanas que ya no están programadas
                    // Y crear nuevos seguimientos para semanas que se agregaron
                    // Mantener: Realizados, Atrasados, No Realizados (ya completados)
                    var seguimientosExistentes = await dbContext.Seguimientos
                        .Where(s => s.Codigo == equipo.Codigo)
                        .ToListAsync();
                    
                    var cronogramasRegenerados = cronogramasNuevos; // Usar los cronogramas recién creados
                    
                    int seguimientosEliminados = 0;
                    int seguimientosCreados = 0;
                    
                    // Crear un HashSet para búsqueda rápida O(1) en lugar de Any() que es O(n)
                    var seguimientosPendientesPorSemana = new HashSet<string>();
                    foreach (var seg in seguimientosExistentes.Where(s => s.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Pendiente))
                    {
                        seguimientosPendientesPorSemana.Add($"{seg.Codigo}_{seg.Semana}_{seg.Anio}");
                    }
                    
                    // 1️⃣ ELIMINAR seguimientos pendientes que NO están en el nuevo cronograma
                    var seguimientosAEliminar = new List<SeguimientoMantenimiento>();
                    foreach (var seg in seguimientosExistentes.Where(s => s.Estado == Models.Enums.EstadoSeguimientoMantenimiento.Pendiente))
                    {
                        var cronogramaAnio = cronogramasRegenerados.FirstOrDefault(c => c.Anio == seg.Anio);
                        if (cronogramaAnio != null)
                        {
                            int semanaIndex = seg.Semana - 1;
                            if (semanaIndex >= 0 && semanaIndex < cronogramaAnio.Semanas.Length)
                            {
                                if (!cronogramaAnio.Semanas[semanaIndex])
                                {
                                    seguimientosAEliminar.Add(seg);
                                    seguimientosEliminados++;
                                    _logger.LogInformation("[EquipoService] Seguimiento pendiente a eliminar: {Codigo}, Semana={Semana}, Año={Anio}", 
                                        equipo.Codigo ?? "", seg.Semana, seg.Anio);
                                }
                            }
                        }
                    }
                    
                    // Eliminar en batch
                    if (seguimientosAEliminar.Any())
                    {
                        dbContext.Seguimientos.RemoveRange(seguimientosAEliminar);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("[EquipoService] Total seguimientos pendientes eliminados: {Count} para {Codigo}", 
                            seguimientosEliminados, equipo.Codigo ?? "");
                    }
                    
                    // 2️⃣ CREAR seguimientos nuevos para semanas que se agregaron en el cronograma
                    var seguimientosACrear = new List<SeguimientoMantenimiento>();
                    foreach (var cronograma in cronogramasRegenerados)
                    {
                        for (int i = 0; i < cronograma.Semanas.Length; i++)
                        {
                            int semana = i + 1; // Convertir a 1-based
                            
                            // Si la semana está programada en el cronograma
                            if (cronograma.Semanas[i])
                            {
                                // Verificar si ya existe un seguimiento para esta semana usando HashSet (O(1))
                                var key = $"{equipo.Codigo}_{semana}_{cronograma.Anio}";
                                if (!seguimientosPendientesPorSemana.Contains(key))
                                {
                                    // Crear nuevo seguimiento pendiente
                                    var nuevoSeguimiento = new SeguimientoMantenimiento
                                    {
                                        Codigo = equipo.Codigo!,
                                        Nombre = equipo.Nombre!,
                                        Semana = semana,
                                        Anio = cronograma.Anio,
                                        TipoMtno = TipoMantenimiento.Preventivo,
                                        Descripcion = "Mantenimiento programado",
                                        Responsable = string.Empty,
                                        FechaRegistro = DateTime.Now,
                                        Estado = Models.Enums.EstadoSeguimientoMantenimiento.Pendiente,
                                        Frecuencia = equipo.FrecuenciaMtto
                                    };
                                    
                                    seguimientosACrear.Add(nuevoSeguimiento);
                                    seguimientosCreados++;
                                    _logger.LogInformation("[EquipoService] Nuevo seguimiento pendiente a crear: {Codigo}, Semana={Semana}, Año={Anio}", 
                                        equipo.Codigo ?? "", semana, cronograma.Anio);
                                }
                            }
                        }
                    }
                    
                    // Crear en batch
                    if (seguimientosACrear.Any())
                    {
                        dbContext.Seguimientos.AddRange(seguimientosACrear);
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("[EquipoService] Total nuevos seguimientos pendientes creados: {Count} para {Codigo}", 
                            seguimientosCreados, equipo.Codigo ?? "");
                    }
                    
                    // 📢 Notificar que los cronogramas y seguimientos han sido actualizados
                    WeakReferenceMessenger.Default.Send(new CronogramasActualizadosMessage());
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                    
                    _logger.LogInformation("[EquipoService] Resumen: {Codigo} - Seguimientos Eliminados={Elim}, Creados={Crea}", 
                        equipo.Codigo ?? "", seguimientosEliminados, seguimientosCreados);
                }
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[EquipoService] Validation error on update");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoService] Unexpected error on update");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al actualizar el equipo. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeleteAsync(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    throw new GestionMantenimientosDomainException("El código del equipo es obligatorio para eliminar.");
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el equipo a eliminar.");
                dbContext.Equipos.Remove(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[EquipoService] Equipo eliminado correctamente: {Codigo}", codigo ?? "");
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[EquipoService] Validation error on delete");
                throw;
            }
            catch (Exception ex)
            {            _logger.LogError(ex, "[EquipoService] Unexpected error on delete");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al eliminar el equipo. Por favor, contacte al administrador.", ex);
            }
        }

        private void ValidarEquipo(EquipoDto equipo)
        {
            if (equipo == null)
                throw new GestionMantenimientosDomainException("El equipo no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(equipo.Codigo))
                throw new GestionMantenimientosDomainException("El código es obligatorio.");
            
            // ✅ NUEVO: Nombre es obligatorio según BD (columna NOT NULL)
            if (string.IsNullOrWhiteSpace(equipo.Nombre))
                throw new GestionMantenimientosDomainException("El nombre del equipo es obligatorio.");
            
            // Marca y Sede ya no son obligatorios por requerimiento
            if (equipo.Precio != null && equipo.Precio < 0)
                throw new GestionMantenimientosDomainException("El precio no puede ser negativo.");
            if (equipo.FrecuenciaMtto != null && equipo.FrecuenciaMtto <= 0)
                throw new GestionMantenimientosDomainException("La frecuencia de mantenimiento debe ser mayor a cero.");

            // Fecha de compra no puede ser futura
            if (equipo.FechaCompra != null && equipo.FechaCompra.Value.Date > DateTime.Today)
                throw new GestionMantenimientosDomainException("La fecha de compra no puede ser futura.");

            // TODO: Validar duplicados y otras reglas de negocio si aplica
        }

        public async Task ImportarDesdeExcelAsync(string filePath)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("[EquipoService] Starting import from Excel: {FilePath}", filePath);
                try
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"El archivo no existe: {filePath}");

                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    var headers = new[] { "Codigo", "Nombre", "Marca", "Estado", "Sede", "Precio", "Observaciones", "FechaRegistro", "FrecuenciaMtto", "FechaBaja" };
                    // Validar encabezados
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cellValue = worksheet.Cell(1, i + 1).GetString();
                        if (!string.Equals(cellValue, headers[i], StringComparison.OrdinalIgnoreCase))
                            throw new GestionMantenimientosDomainException($"Columna esperada '{headers[i]}' no encontrada en la posición {i + 1}.");
                    }
                    var equipos = new List<EquipoDto>();
                    int row = 2;
                    while (!worksheet.Cell(row, 1).IsEmpty())
                    {
                        try
                        {
                            var freqInt = worksheet.Cell(row, 9).GetValue<int?>();
                            var dto = new EquipoDto
                            {
                                Codigo = worksheet.Cell(row, 1).GetString(),
                                Nombre = worksheet.Cell(row, 2).GetString(),
                                Marca = worksheet.Cell(row, 3).GetString(),
                                Estado = Enum.TryParse<EstadoEquipo>(worksheet.Cell(row, 4).GetString(), out var estado) ? estado : (EstadoEquipo?)null,
                                Sede = Enum.TryParse<Sede>(worksheet.Cell(row, 5).GetString(), out var sede) ? sede : (Sede?)null,
                                Precio = worksheet.Cell(row, 6).GetValue<decimal?>(),
                                Observaciones = worksheet.Cell(row, 7).GetString(),
                                FechaRegistro = worksheet.Cell(row, 8).GetDateTime(),
                                FrecuenciaMtto = freqInt.HasValue ? (FrecuenciaMantenimiento?)freqInt.Value : null,
                                FechaBaja = worksheet.Cell(row, 10).GetDateTime()
                                // SemanaInicioMtto eliminado
                            };
                            ValidarEquipo(dto);
                            equipos.Add(dto);
                        }
                        catch (GestionMantenimientosDomainException ex)
                        {
                            _logger.LogWarning(ex, $"[EquipoService] Validation error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error de validación en la fila {row}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"[EquipoService] Unexpected error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error inesperado en la fila {row}: {ex.Message}", ex);
                        }
                        row++;
                    }
                    // Aquí deberías guardar los equipos importados en la base de datos o colección interna
                    _logger.LogInformation("[EquipoService] Equipos importados: {Count}", equipos.Count);
                    // Notificar actualización de seguimientos
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                }
                catch (GestionMantenimientosDomainException ex)
                {
                    _logger.LogWarning(ex, "[EquipoService] Validation error on import");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EquipoService] Error importing from Excel");
                    throw new GestionMantenimientosDomainException($"Error al importar desde Excel: {ex.Message}", ex);
                }
            });
        }

        public async Task ExportarAExcelAsync(string filePath)
        {
            _logger.LogInformation("[EquipoService] Starting export to Excel: {FilePath}", filePath);
            try
            {
                var equipos = (await GetAllAsync()).ToList();
                if (!equipos.Any())
                {
                    _logger.LogWarning("[EquipoService] No data to export.");
                    throw new InvalidOperationException("No hay datos de equipos para exportar.");
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Equipos");

                // Escribir encabezados
                var headers = new[] { "Codigo", "Nombre", "Marca", "Estado", "Sede", "Precio", "Observaciones", "FechaRegistro", "FrecuenciaMtto", "FechaBaja" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Escribir datos
                int row = 2;
                foreach (var eq in equipos)
                {
                    worksheet.Cell(row, 1).Value = eq.Codigo;
                    worksheet.Cell(row, 2).Value = eq.Nombre;
                    worksheet.Cell(row, 3).Value = eq.Marca;
                    worksheet.Cell(row, 4).Value = eq.Estado?.ToString();
                    worksheet.Cell(row, 5).Value = eq.Sede?.ToString();
                    worksheet.Cell(row, 6).Value = eq.Precio;
                    worksheet.Cell(row, 7).Value = eq.Observaciones;
                    worksheet.Cell(row, 8).Value = eq.FechaRegistro;
                    worksheet.Cell(row, 9).Value = eq.FrecuenciaMtto.HasValue ? (int)eq.FrecuenciaMtto.Value : (int?)null;
                    worksheet.Cell(row, 10).Value = eq.FechaBaja;
                    // SemanaInicioMtto eliminado
                    row++;
                }

                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                _logger.LogInformation("[EquipoService] Export completed: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EquipoService] Error exporting to Excel");
                throw new Exception($"Error al exportar a Excel: {ex.Message}", ex);
            }
        }

        public Task BackupAsync()
        {
            // TODO: Implementar backup de datos
            return Task.CompletedTask;
        }

        public Task<List<EquipoDto>> GetEquiposAsync()
        {
            // TODO: Implementar lógica real de obtención de equipos
            return Task.FromResult(new List<EquipoDto>());
        }        // Calcula la semana ISO 8601 para una fecha dada
        private int CalcularSemanaISO8601(DateTime fecha)
        {
            return System.Globalization.ISOWeek.GetWeekOfYear(fecha);
        }
    }
}
