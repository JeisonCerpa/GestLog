using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using System;
using GestLog.Services.Core.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Exceptions;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;

namespace GestLog.Modules.GestionMantenimientos.Services.Data
{
    public class SeguimientoService : ISeguimientoService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly ICronogramaService _cronogramaService;

        public SeguimientoService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory, ICronogramaService cronogramaService)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _cronogramaService = cronogramaService;
        }        public async Task<IEnumerable<SeguimientoMantenimientoDto>> GetAllAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var seguimientos = await dbContext.Seguimientos.ToListAsync();
            var equipos = await dbContext.Equipos.ToDictionaryAsync(e => e.Codigo, e => e.Sede);
            
            return seguimientos.Select(s => new SeguimientoMantenimientoDto
            {
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Sede = equipos.ContainsKey(s.Codigo) ? equipos[s.Codigo] : null,
                TipoMtno = s.TipoMtno,
                Descripcion = s.Descripcion,
                Responsable = s.Responsable,
                Costo = s.Costo,
                Observaciones = s.Observaciones,
                FechaRegistro = s.FechaRegistro,
                FechaRealizacion = s.FechaRealizacion,
                Semana = s.Semana,
                Anio = s.Anio,
                Estado = s.Estado,
                Frecuencia = s.Frecuencia
            });
        }        public async Task<SeguimientoMantenimientoDto?> GetByCodigoAsync(string codigo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == codigo);
            if (entity == null) return null;
            
            var equipo = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == codigo);
            
            return new SeguimientoMantenimientoDto
            {
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Sede = equipo?.Sede,
                TipoMtno = entity.TipoMtno,
                Descripcion = entity.Descripcion,
                Responsable = entity.Responsable,
                Costo = entity.Costo,
                Observaciones = entity.Observaciones,
                FechaRegistro = entity.FechaRegistro,
                FechaRealizacion = entity.FechaRealizacion,
                Semana = entity.Semana,
                Anio = entity.Anio,
                Estado = entity.Estado,
                Frecuencia = entity.Frecuencia
            };
        }

        public async Task AddAsync(SeguimientoMantenimientoDto seguimiento)
        {
            try
            {
                // Normalización defensiva: Responsable en mayúsculas
                if (seguimiento == null)
                    throw new GestionMantenimientosDomainException("El seguimiento no puede ser nulo.");

                seguimiento.Responsable = (seguimiento.Responsable ?? string.Empty).ToUpperInvariant().Trim();

                ValidarSeguimiento(seguimiento);
                var hoy = DateTime.Now;
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                int semanaActual = cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int anioActual = hoy.Year;

                if (!(seguimiento.Anio < anioActual || (seguimiento.Anio == anioActual && seguimiento.Semana <= semanaActual)))
                    throw new GestionMantenimientosDomainException("Solo se permite registrar mantenimientos en semanas anteriores o la actual.");

                using var dbContext = _dbContextFactory.CreateDbContext();

                // NUEVA LÃ“GICA: Buscar por (Codigo, Semana, Anio, TipoMtno)
                var existentePrevio = await dbContext.Seguimientos
                    .FirstOrDefaultAsync(s => s.Codigo == seguimiento.Codigo &&
                                            s.Semana == seguimiento.Semana &&
                                            s.Anio == seguimiento.Anio &&
                                            s.TipoMtno == seguimiento.TipoMtno);

                // Calcular intervalo de la semana programada
                DateTime fechaInicioSemana = FirstDateOfWeekISO8601(seguimiento.Anio, seguimiento.Semana);
                DateTime fechaFinSemana = fechaInicioSemana.AddDays(6);
                string observacionSemana = $"Programado en la semana {seguimiento.Semana} entre {fechaInicioSemana:dd/MM/yyyy} y {fechaFinSemana:dd/MM/yyyy}";
                bool agregarObservacion = seguimiento.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || seguimiento.Estado == EstadoSeguimientoMantenimiento.NoRealizado;

                // Si ya existe un registro del MISMO tipo (preventivo o correctivo), actualizar
                if (existentePrevio != null)
                {
                    existentePrevio.Nombre = seguimiento.Nombre ?? string.Empty;
                    existentePrevio.TipoMtno = seguimiento.TipoMtno ?? TipoMantenimiento.Preventivo;
                    existentePrevio.Descripcion = seguimiento.Descripcion ?? string.Empty;
                    existentePrevio.Responsable = seguimiento.Responsable ?? string.Empty;
                    existentePrevio.Costo = seguimiento.Costo.HasValue ? seguimiento.Costo.Value : 0m;
                    existentePrevio.Observaciones = seguimiento.Observaciones ?? string.Empty;
                    if (agregarObservacion)
                        existentePrevio.Observaciones = observacionSemana;
                    existentePrevio.FechaRegistro = DateTime.Now;
                    existentePrevio.FechaRealizacion = seguimiento.Estado == EstadoSeguimientoMantenimiento.NoRealizado ? null : seguimiento.FechaRealizacion;
                    existentePrevio.Estado = seguimiento.Estado;
                    existentePrevio.Frecuencia = seguimiento.Frecuencia;

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("[SeguimientoService] Seguimiento actualizado: {Codigo} Tipo {Tipo} Semana {Semana} AÃ±o {Anio}",
                        seguimiento.Codigo ?? "", seguimiento.TipoMtno?.ToString() ?? "Preventivo", seguimiento.Semana, seguimiento.Anio);
                }
                else
                {
                    // No existe del mismo tipo. Crear uno NUEVO
                    string observacionesFinal = seguimiento.Observaciones ?? string.Empty;
                    if (agregarObservacion)
                        observacionesFinal = observacionSemana;

                    var nuevo = new SeguimientoMantenimiento
                    {
                        Codigo = seguimiento.Codigo ?? string.Empty,
                        Nombre = seguimiento.Nombre ?? string.Empty,
                        TipoMtno = seguimiento.TipoMtno ?? TipoMantenimiento.Preventivo,
                        Descripcion = seguimiento.Descripcion ?? string.Empty,
                        Responsable = seguimiento.Responsable ?? string.Empty,
                        Costo = seguimiento.Costo,
                        Observaciones = observacionesFinal,
                        FechaRegistro = DateTime.Now,
                        FechaRealizacion = seguimiento.Estado == EstadoSeguimientoMantenimiento.NoRealizado ? null : seguimiento.FechaRealizacion,
                        Semana = seguimiento.Semana,
                        Anio = seguimiento.Anio,
                        Estado = seguimiento.Estado,
                        Frecuencia = seguimiento.Frecuencia
                    };
                    dbContext.Seguimientos.Add(nuevo);

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("[SeguimientoService] Seguimiento agregado: {Codigo} Tipo {Tipo} Semana {Semana} AÃ±o {Anio}",
                        seguimiento.Codigo ?? "", seguimiento.TipoMtno?.ToString() ?? "Preventivo", seguimiento.Semana, seguimiento.Anio);
                }
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on add");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on add");
                throw new GestionMantenimientosDomainException("OcurriÃ³ un error inesperado al agregar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task UpdateAsync(SeguimientoMantenimientoDto seguimiento)
        {
            try
            {
                // Normalización defensiva: Responsable en mayúsculas
                if (seguimiento == null)
                    throw new GestionMantenimientosDomainException("El seguimiento no puede ser nulo.");

                seguimiento.Responsable = (seguimiento.Responsable ?? string.Empty).ToUpperInvariant().Trim();

                ValidarSeguimiento(seguimiento);
                using var dbContext = _dbContextFactory.CreateDbContext();
                // Buscar por clave compuesta: Codigo, Semana, Anio
                var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == seguimiento.Codigo && s.Semana == seguimiento.Semana && s.Anio == seguimiento.Anio);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontrÃ³ el seguimiento a actualizar.");
                // No permitir cambiar el cÃ³digo
                entity.Nombre = seguimiento.Nombre ?? string.Empty;
                entity.TipoMtno = seguimiento.TipoMtno ?? TipoMantenimiento.Preventivo;
                entity.Descripcion = seguimiento.Descripcion ?? string.Empty;
                entity.Responsable = seguimiento.Responsable ?? string.Empty;
                entity.Costo = seguimiento.Costo.HasValue ? seguimiento.Costo.Value : 0m;
                entity.Observaciones = seguimiento.Observaciones ?? string.Empty;
                entity.FechaRegistro = seguimiento.FechaRegistro ?? DateTime.Now;
                entity.FechaRealizacion = seguimiento.FechaRealizacion;
                entity.Estado = seguimiento.Estado;
                entity.Frecuencia = seguimiento.Frecuencia;
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimiento actualizado correctamente: {Codigo} Semana {Semana} AÃ±o {Anio}", seguimiento.Codigo ?? "", seguimiento.Semana, seguimiento.Anio);
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on update");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on update");
                throw new GestionMantenimientosDomainException("OcurriÃ³ un error inesperado al actualizar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeleteAsync(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    throw new GestionMantenimientosDomainException("El cÃ³digo del seguimiento es obligatorio para eliminar.");

                using var dbContext = _dbContextFactory.CreateDbContext();
                var seguimientos = await dbContext.Seguimientos.Where(s => s.Codigo == codigo).ToListAsync();
                if (seguimientos.Count == 0)
                    throw new GestionMantenimientosDomainException("No se encontraron seguimientos con ese cÃ³digo.");

                dbContext.Seguimientos.RemoveRange(seguimientos);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimientos eliminados para cÃ³digo: {Codigo}", codigo);
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on delete");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on delete");
                throw new GestionMantenimientosDomainException("OcurriÃ³ un error inesperado al eliminar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeletePendientesByEquipoCodigoAsync(string codigoEquipo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pendientes = await dbContext.Seguimientos
                .Where(s => s.Codigo == codigoEquipo && s.Estado == EstadoSeguimientoMantenimiento.Pendiente)
                .ToListAsync();
            if (pendientes.Any())
            {
                dbContext.Seguimientos.RemoveRange(pendientes);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"[SeguimientoService] Eliminados {pendientes.Count} seguimientos pendientes para equipo {codigoEquipo}.");
            }
        }

        public async Task ActualizarObservacionesPendientesAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var seguimientosPendientes = await dbContext.Seguimientos
                .Where(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente)
                .ToListAsync();

            int actualizados = 0;
            foreach (var s in seguimientosPendientes)
            {
                DateTime fechaInicioSemana = FirstDateOfWeekISO8601(s.Anio, s.Semana);
                DateTime fechaFinSemana = fechaInicioSemana.AddDays(6);
                s.Observaciones = $"Programado en la semana {s.Semana} entre {fechaInicioSemana:dd/MM/yyyy} y {fechaFinSemana:dd/MM/yyyy}";
                actualizados++;
            }
            if (actualizados > 0)
                await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[SeguimientoService] Observaciones actualizadas en {actualizados} seguimientos.");
        }

        private void ValidarSeguimiento(SeguimientoMantenimientoDto seguimiento)
        {
            if (seguimiento == null)
                throw new GestionMantenimientosDomainException("El seguimiento no puede ser nulo.");

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(seguimiento);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(seguimiento, context, results, true);
            if (!isValid)
            {
                var mensaje = string.Join("\n", results.Select(r => r.ErrorMessage));
                throw new GestionMantenimientosDomainException(mensaje);
            }

            // Validaciones de negocio adicionales
            if (seguimiento.Costo != null && seguimiento.Costo < 0)
                throw new GestionMantenimientosDomainException("El costo no puede ser negativo.");
        }

        // DEPRECATED: La importación desde Excel se consolidó en ISeguimientoImportService / SeguimientoImportService.
        // Método ImportarDesdeExcelAsync y sus helpers fueron eliminados para evitar duplicidad de lógica.
        // Si necesita restaurar temporalmente la lógica, recupere la implementación desde el historial de git.

        /// <summary>
        /// Crea cronogramas automÃ¡ticamente para datos histÃ³ricos importados
        /// Agrupa seguimientos por (Codigo, Anio) y marca las semanas donde hubo mantenimientos
        /// </summary>
        public async Task CrearCronogramasDesdeSeguidmientosAsync()
        {
            _logger.LogInformation("[SeguimientoService] Iniciando creaciÃ³n automÃ¡tica de cronogramas desde seguimientos importados");
            try
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                // Obtener todos los seguimientos que no sean Pendiente (los ya realizados)
                var seguimientos = await dbContext.Seguimientos
                    .Where(s => s.Estado != EstadoSeguimientoMantenimiento.Pendiente)
                    .ToListAsync();

                if (!seguimientos.Any())
                {
                    _logger.LogInformation("[SeguimientoService] No hay seguimientos para crear cronogramas");
                    return;
                }

                // Agrupar por (Codigo, Anio)
                var grupos = seguimientos
                    .GroupBy(s => new { s.Codigo, s.Anio })
                    .ToList();

                _logger.LogInformation("[SeguimientoService] Se encontraron {GrupoCount} grupos de seguimientos (Codigo, AÃ±o)", grupos.Count);

                foreach (var grupo in grupos)
                {
                    try
                    {
                        var codigo = grupo.Key.Codigo;
                        var anio = grupo.Key.Anio;

                        // Obtener nombre del equipo desde la BD
                        var equipo = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == codigo);
                        string nombreEquipo = equipo?.Nombre ?? codigo;

                        // Obtener semanas donde hubo mantenimientos
                        var semanasConMantenimiento = grupo.Select(s => s.Semana).Distinct().OrderBy(s => s).ToList();

                        // Calcular nÃºmero de semanas en el aÃ±o
                        int weeksInYear = ISOWeek.GetWeeksInYear(anio);

                        // Crear array de semanas (true donde hubo mantenimiento)
                        bool[] semanas = new bool[weeksInYear];
                        foreach (var semanaNum in semanasConMantenimiento)
                        {
                            if (semanaNum >= 1 && semanaNum <= weeksInYear)
                            {
                                semanas[semanaNum - 1] = true; // El array es 0-based, las semanas son 1-based
                            }
                        }

                        // Buscar cronograma existente
                        var cronogramaExistente = await dbContext.Cronogramas
                            .FirstOrDefaultAsync(c => c.Codigo == codigo && c.Anio == anio);                        if (cronogramaExistente == null)
                        {
                            // Crear nuevo cronograma
                            var nuevoCronograma = new CronogramaMantenimiento
                            {
                                Codigo = codigo,
                                Nombre = nombreEquipo,
                                Anio = anio,
                                Semanas = semanas,
                                FrecuenciaMtto = null // Sin frecuencia para datos histÃ³ricos
                            };

                            dbContext.Cronogramas.Add(nuevoCronograma);
                            _logger.LogInformation("[SeguimientoService] Cronograma creado: {Codigo} {Anio} con {SemanasCount} semanas marcadas",
                                codigo, anio, semanasConMantenimiento.Count);
                        }
                        else
                        {
                            // Actualizar cronograma existente: COMBINAR (OR) las semanas importadas con las existentes
                            // Esto preserva cualquier semana que ya estaba marcada
                            for (int i = 0; i < semanas.Length && i < cronogramaExistente.Semanas.Length; i++)
                            {
                                if (semanas[i])
                                {
                                    cronogramaExistente.Semanas[i] = true;
                                }
                            }
                            _logger.LogInformation("[SeguimientoService] Cronograma actualizado (combinado): {Codigo} {Anio} con {SemanasCount} semanas importadas",
                                codigo, anio, semanasConMantenimiento.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[SeguimientoService] Error procesando grupo {Codigo} {Anio}",
                            grupo.Key.Codigo, grupo.Key.Anio);
                    }
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] CreaciÃ³n de cronogramas completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error al crear cronogramas desde seguimientos");
                throw new GestionMantenimientosDomainException("Error al crear cronogramas: " + ex.Message, ex);
            }
        }

        public async Task ExportarAExcelAsync(string filePath)
        {
            _logger.LogInformation("[SeguimientoService] Iniciando exportaciÃ³n a Excel: {FilePath}", filePath);
            try
            {
                var seguimientos = (await GetAllAsync()).Where(s => s.Estado != EstadoSeguimientoMantenimiento.Pendiente).ToList();
                if (!seguimientos.Any())
                {
                    _logger.LogWarning("[SeguimientoService] No hay datos para exportar");
                    throw new InvalidOperationException("No hay datos de seguimientos para exportar.");
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Seguimientos");

                    // Encabezados
                    var headers = new[] { "Codigo", "Nombre", "TipoMtno", "Descripcion", "Responsable", "Costo", "Observaciones", "FechaRegistro", "Estado" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cell = worksheet.Cell(1, i + 1);
                        cell.Value = headers[i];
                        cell.Style.Font.Bold = true;
                    }

                    // Datos
                    int row = 2;
                    foreach (var s in seguimientos)
                    {
                        worksheet.Cell(row, 1).Value = s.Codigo;
                        worksheet.Cell(row, 2).Value = s.Nombre;
                        worksheet.Cell(row, 3).Value = s.TipoMtno?.ToString() ?? "";
                        worksheet.Cell(row, 4).Value = s.Descripcion;
                        worksheet.Cell(row, 5).Value = s.Responsable;
                        worksheet.Cell(row, 6).Value = s.Costo;
                        worksheet.Cell(row, 7).Value = s.Observaciones;
                        worksheet.Cell(row, 8).Value = s.FechaRegistro;
                        worksheet.Cell(row, 9).Value = SepararCamelCase(s.Estado.ToString());
                        row++;
                    }

                    worksheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                }

                _logger.LogInformation("[SeguimientoService] ExportaciÃ³n completada: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error al exportar a Excel");
                throw new GestionMantenimientosDomainException($"Error al exportar a Excel: {ex.Message}", ex);
            }
        }

        public Task BackupAsync()
        {
            // TODO: Implementar backup de datos
            return Task.CompletedTask;
        }

        public async Task<List<SeguimientoMantenimientoDto>> GetSeguimientosAsync()
        {
            var seguimientos = await GetAllAsync();
            return seguimientos.ToList();
        }

        // Utilidad para obtener el primer dÃ­a de la semana ISO 8601
        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }

        private string SepararCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (char.IsUpper(c) && result.Length > 0)
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }
    }
}


