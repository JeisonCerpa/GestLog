using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using System;
using GestLog.Services.Core.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Modules.DatabaseConnection;
using GestLog.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Exceptions;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using System.Globalization;

namespace GestLog.Modules.GestionMantenimientos.Services.Data
{
    public class CronogramaService : ICronogramaService
    {        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        public CronogramaService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        }

        public async Task<IEnumerable<CronogramaMantenimientoDto>> GetAllAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var cronos = await dbContext.Cronogramas.ToListAsync();
            return cronos.Select(c => new CronogramaMantenimientoDto
            {
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Marca = c.Marca,
                Sede = c.Sede,
                FrecuenciaMtto = c.FrecuenciaMtto,
                Semanas = c.Semanas.ToArray(),
                Anio = c.Anio > 0 ? c.Anio : DateTime.Now.Year
            });
        }

        public async Task<CronogramaMantenimientoDto?> GetByCodigoAsync(string codigo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == codigo);
            if (entity == null) return null;
            return new CronogramaMantenimientoDto
            {
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Marca = entity.Marca,
                Sede = entity.Sede,
                FrecuenciaMtto = entity.FrecuenciaMtto,
                Semanas = entity.Semanas.ToArray(),
                Anio = entity.Anio > 0 ? entity.Anio : DateTime.Now.Year
            };
        }

        private void ValidarCronograma(CronogramaMantenimientoDto cronograma)
        {
            if (cronograma == null)
                throw new GestionMantenimientosDomainException("El cronograma no puede ser nulo.");

            var context = new System.ComponentModel.DataAnnotations.ValidationContext(cronograma);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(cronograma, context, results, true);
            if (!isValid)
            {
                var mensaje = string.Join("\n", results.Select(r => r.ErrorMessage));
                throw new GestionMantenimientosDomainException(mensaje);
            }

            // Validaciones de negocio adicionales
            if (string.IsNullOrWhiteSpace(cronograma.Codigo))
                throw new GestionMantenimientosDomainException("El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(cronograma.Nombre))
                throw new GestionMantenimientosDomainException("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(cronograma.Marca))
                throw new GestionMantenimientosDomainException("La marca es obligatoria.");
            if (string.IsNullOrWhiteSpace(cronograma.Sede))
                throw new GestionMantenimientosDomainException("La sede es obligatoria.");
            if (cronograma.FrecuenciaMtto != null && (int)cronograma.FrecuenciaMtto <= 0)
                throw new GestionMantenimientosDomainException("La frecuencia de mantenimiento debe ser mayor a cero.");
            // Validar que la longitud de 'Semanas' coincida con el número de semanas del año correspondiente (ISO)
            int targetYear = cronograma.Anio > 0 ? cronograma.Anio : DateTime.Now.Year;
            int weeksInYear = ISOWeek.GetWeeksInYear(targetYear);
            if (cronograma.Semanas == null || cronograma.Semanas.Length != weeksInYear)
                throw new GestionMantenimientosDomainException($"El cronograma debe tener {weeksInYear} semanas definidas para el año {targetYear}.");
            // Validar duplicados solo en alta
        }

        public async Task AddAsync(CronogramaMantenimientoDto cronograma)
        {
            try
            {
                ValidarCronograma(cronograma);
                using var dbContext = _dbContextFactory.CreateDbContext();
                if (await dbContext.Cronogramas.AnyAsync(c => c.Codigo == cronograma.Codigo && c.Anio == cronograma.Anio))
                    throw new GestionMantenimientosDomainException($"Ya existe un cronograma con el código '{cronograma.Codigo}' para el año {cronograma.Anio}.");
                var entity = new CronogramaMantenimiento
                {
                    Codigo = cronograma.Codigo!,
                    Nombre = cronograma.Nombre!,
                    Marca = cronograma.Marca,
                    Sede = cronograma.Sede,
                    FrecuenciaMtto = cronograma.FrecuenciaMtto,
                    Semanas = cronograma.Semanas.ToArray(),
                    Anio = cronograma.Anio > 0 ? cronograma.Anio : DateTime.Now.Year
                };
                dbContext.Cronogramas.Add(entity);
                // Generar seguimientos pendientes para cada semana programada
                for (int i = 0; i < entity.Semanas.Length; i++)
                {
                    if (entity.Semanas[i])
                    {
                        int semana = i + 1;
                        // Verificar si ya existe seguimiento para este equipo, semana y año
                        bool existe = dbContext.Seguimientos.Any(s => s.Codigo == entity.Codigo && s.Semana == semana && s.Anio == entity.Anio);
                        if (!existe)
                        {
                            dbContext.Seguimientos.Add(new SeguimientoMantenimiento
                            {
                                Codigo = entity.Codigo,
                                Nombre = entity.Nombre,
                                Semana = semana,
                                Anio = entity.Anio,
                                TipoMtno = TipoMantenimiento.Preventivo, // Por defecto, o puedes ajustar según lógica
                                Descripcion = "Mantenimiento programado",
                                Responsable = string.Empty,
                                FechaRegistro = DateTime.Now
                            });
                        }
                    }
                }
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[CronogramaService] Cronograma agregado correctamente: {Codigo} - {Anio}", cronograma?.Codigo ?? "", entity.Anio);
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[CronogramaService] Validation error on add");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaService] Unexpected error on add");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al agregar el cronograma. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task UpdateAsync(CronogramaMantenimientoDto cronograma)
        {
            try
            {
                ValidarCronograma(cronograma);
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == cronograma.Codigo && c.Anio == cronograma.Anio);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el cronograma a actualizar.");
                // No permitir cambiar el código
                entity.Nombre = cronograma.Nombre!;
                entity.Marca = cronograma.Marca;
                entity.Sede = cronograma.Sede;
                entity.FrecuenciaMtto = cronograma.FrecuenciaMtto;
                entity.Semanas = cronograma.Semanas.ToArray();
                entity.Anio = cronograma.Anio > 0 ? cronograma.Anio : DateTime.Now.Year;
                await dbContext.SaveChangesAsync();
                // Actualizar seguimientos: crear los que falten para semanas programadas
                for (int i = 0; i < entity.Semanas.Length; i++)
                {
                    if (entity.Semanas[i])
                    {
                        int semana = i + 1;
                        bool existe = dbContext.Seguimientos.Any(s => s.Codigo == entity.Codigo && s.Semana == semana && s.Anio == entity.Anio);
                        if (!existe)
                        {
                            dbContext.Seguimientos.Add(new SeguimientoMantenimiento
                            {
                                Codigo = entity.Codigo,
                                Nombre = entity.Nombre,
                                Semana = semana,
                                Anio = entity.Anio,
                                TipoMtno = TipoMantenimiento.Preventivo, // Por defecto, o puedes ajustar según lógica
                                Descripcion = "Mantenimiento programado",
                                Responsable = string.Empty,
                                FechaRegistro = DateTime.Now
                            });
                        }
                    }
                }
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[CronogramaService] Cronograma actualizado correctamente: {Codigo} - {Anio}", cronograma?.Codigo ?? "", entity.Anio);
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[CronogramaService] Validation error on update");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaService] Unexpected error on update");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al actualizar el cronograma. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeleteAsync(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    throw new GestionMantenimientosDomainException("El código del cronograma es obligatorio para eliminar.");
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el cronograma a eliminar.");
                dbContext.Cronogramas.Remove(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[CronogramaService] Cronograma eliminado correctamente: {Codigo}", codigo ?? "");
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[CronogramaService] Validation error on delete");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaService] Unexpected error on delete");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al eliminar el cronograma. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeleteByEquipoCodigoAsync(string codigoEquipo)
        {
            if (string.IsNullOrWhiteSpace(codigoEquipo))
                throw new GestionMantenimientosDomainException("El código del equipo es obligatorio para eliminar cronogramas.");
            using var dbContext = _dbContextFactory.CreateDbContext();
            var cronogramas = dbContext.Cronogramas.Where(c => c.Codigo == codigoEquipo);
            dbContext.Cronogramas.RemoveRange(cronogramas);
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("[CronogramaService] Cronogramas eliminados para equipo: {Codigo}", codigoEquipo);
        }

        public async Task ImportarDesdeExcelAsync(string filePath)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("[CronogramaService] Starting import from Excel: {FilePath}", filePath);
                try
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"El archivo no existe: {filePath}");

                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    var headers = new[] { "Codigo", "Nombre", "Marca", "Sede", "FrecuenciaMtto" };
                    // Validar encabezados fijos
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cellValue = worksheet.Cell(1, i + 1).GetString();
                        if (!string.Equals(cellValue, headers[i], StringComparison.OrdinalIgnoreCase))
                            throw new GestionMantenimientosDomainException($"Columna esperada '{headers[i]}' no encontrada en la posición {i + 1}.");
                    }
                    // Determinar número de semanas en el año objetivo (si se provee en el archivo, buscar columna 'Anio' o usar año actual)
                    int fileYear = DateTime.Now.Year;
                    // Determinar de forma segura la última columna con contenido en la fila de encabezados
                    int lastColumn = worksheet.Row(1).LastCellUsed()?.Address.ColumnNumber
                                     ?? worksheet.LastColumnUsed()?.ColumnNumber()
                                     ?? (headers.Length + System.Globalization.ISOWeek.GetWeeksInYear(DateTime.Now.Year));
                    for (int col = 1; col <= lastColumn; col++)
                    {
                        var h = worksheet.Cell(1, col).GetString();
                        if (string.Equals(h, "Anio", StringComparison.OrdinalIgnoreCase))
                        {
                            // Tomamos el año de la primera fila de datos (si existe)
                            var val = worksheet.Cell(2, col).GetValue<int?>();
                            if (val.HasValue) fileYear = val.Value;
                            break;
                        }
                    }
                    int weeksInYear = System.Globalization.ISOWeek.GetWeeksInYear(fileYear);
                    // Validar encabezados de semanas dinámicamente
                    for (int s = 1; s <= weeksInYear; s++)
                    {
                        var cellValue = worksheet.Cell(1, headers.Length + s).GetString();
                        if (!string.Equals(cellValue, $"S{s}", StringComparison.OrdinalIgnoreCase))
                            throw new GestionMantenimientosDomainException($"Columna esperada 'S{s}' no encontrada en la posición {headers.Length + s}.");
                    }
                    var cronogramas = new List<CronogramaMantenimientoDto>();
                    int row = 2;
                    while (!worksheet.Cell(row, 1).IsEmpty())
                    {
                        try
                        {
                            var freqInt = worksheet.Cell(row, 6).GetValue<int?>();
                            var dto = new CronogramaMantenimientoDto
                            {
                                Codigo = worksheet.Cell(row, 1).GetString(),
                                Nombre = worksheet.Cell(row, 2).GetString(),
                                Marca = worksheet.Cell(row, 3).GetString(),
                                Sede = worksheet.Cell(row, 4).GetString(),
                                FrecuenciaMtto = freqInt.HasValue ? (FrecuenciaMantenimiento?)freqInt.Value : null,
                                Semanas = new bool[weeksInYear],
                                Anio = fileYear
                            };
                            for (int s = 0; s < weeksInYear; s++)
                            {
                                var val = worksheet.Cell(row, headers.Length + 1 + s).GetString();
                                dto.Semanas[s] = !string.IsNullOrWhiteSpace(val);
                            }
                            ValidarCronograma(dto);
                            cronogramas.Add(dto);
                        }
                        catch (GestionMantenimientosDomainException ex)
                        {
                            _logger.LogWarning(ex, $"[CronogramaService] Validation error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error de validación en la fila {row}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"[CronogramaService] Unexpected error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error inesperado en la fila {row}: {ex.Message}", ex);
                        }
                        row++;
                    }
                    // Aquí deberías guardar los cronogramas importados en la base de datos o colección interna
                    _logger.LogInformation("[CronogramaService] Cronogramas importados: {Count}", cronogramas.Count);
                    // Notificar actualización de seguimientos
                    WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
                }
                catch (GestionMantenimientosDomainException ex)
                {
                    _logger.LogWarning(ex, "[CronogramaService] Validation error on import");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CronogramaService] Error importing from Excel");
                    throw new GestionMantenimientosDomainException($"Error al importar desde Excel: {ex.Message}", ex);
                }
            });
        }

        public async Task ExportarAExcelAsync(string filePath)
        {
            _logger.LogInformation("[CronogramaService] Starting export to Excel: {FilePath}", filePath);
            try
            {
                var cronogramas = (await GetAllAsync()).ToList();
                if (!cronogramas.Any())
                {
                    _logger.LogWarning("[CronogramaService] No data to export.");
                    throw new InvalidOperationException("No hay datos de cronogramas para exportar.");
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Cronogramas");

                // Encabezados
                var headers = new[] { "Codigo", "Nombre", "Marca", "Sede", "FrecuenciaMtto" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }
                // Determinar semanas del año (por defecto año actual aunque la lista puede contener años distintos)
                int exportYear = DateTime.Now.Year;
                if (cronogramas.Any()) exportYear = cronogramas.First().Anio > 0 ? cronogramas.First().Anio : exportYear;
                int weeksInYearExport = ISOWeek.GetWeeksInYear(exportYear);
                // S1...S{N}
                for (int s = 1; s <= weeksInYearExport; s++)
                {
                    worksheet.Cell(1, headers.Length + s).Value = $"S{s}";
                    worksheet.Cell(1, headers.Length + s).Style.Font.Bold = true;
                }

                // Datos
                int row = 2;
                foreach (var c in cronogramas)
                {
                    worksheet.Cell(row, 1).Value = c.Codigo;
                    worksheet.Cell(row, 2).Value = c.Nombre;
                    worksheet.Cell(row, 3).Value = c.Marca;
                    worksheet.Cell(row, 4).Value = c.Sede;
                    // SemanaInicioMtto eliminado de la exportación
                    worksheet.Cell(row, 6).Value = c.FrecuenciaMtto.HasValue ? (int)c.FrecuenciaMtto.Value : (int?)null;
                    for (int s = 0; s < weeksInYearExport; s++)
                    {
                        worksheet.Cell(row, headers.Length + 1 + s).Value = c.Semanas != null && c.Semanas.Length > s && c.Semanas[s] ? "✓" : "";
                    }
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                _logger.LogInformation("[CronogramaService] Export completed: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CronogramaService] Error exporting to Excel");
                throw new Exception($"Error al exportar a Excel: {ex.Message}", ex);
            }
        }

        public Task BackupAsync()
        {
            // TODO: Implementar backup de datos
            return Task.CompletedTask;
        }

        public async Task<List<CronogramaMantenimientoDto>> GetCronogramasAsync()
        {
            var cronos = await _dbContextFactory.CreateDbContext().Cronogramas.ToListAsync();
            return cronos.Select(c => new CronogramaMantenimientoDto
            {
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Marca = c.Marca,
                Sede = c.Sede,
                FrecuenciaMtto = c.FrecuenciaMtto,
                Semanas = c.Semanas.ToArray(),
                Anio = c.Anio > 0 ? c.Anio : DateTime.Now.Year
            }).ToList();
        }

        // Genera el array de semanas según la frecuencia y el año (soporta 52 o 53 semanas según ISO)
        public static bool[] GenerarSemanas(int semanaInicio, FrecuenciaMantenimiento? frecuencia, int year)
        {
            int weeksInYear = ISOWeek.GetWeeksInYear(year);
            var semanas = new bool[weeksInYear];
            if (frecuencia == null) return semanas;
            if (semanaInicio < 1 || semanaInicio > weeksInYear) return semanas;

            // Usar el nuevo método mejorado que calcula basado en fechas reales
            return GenerarSemanasBasadoEnFechas(semanaInicio, frecuencia, year);
        }        /// <summary>
        /// Genera un array de semanas basado en cálculos de fechas reales.
        /// Esto es más preciso que usar saltos fijos de semanas, especialmente para 
        /// frecuencias como cuatrimestral que dependen de meses específicos.
        /// </summary>
        private static bool[] GenerarSemanasBasadoEnFechas(int semanaInicio, FrecuenciaMantenimiento? frecuencia, int year)
        {
            int weeksInYear = ISOWeek.GetWeeksInYear(year);
            var semanas = new bool[weeksInYear];
            
            if (frecuencia == null || semanaInicio < 1 || semanaInicio > weeksInYear)
                return semanas;

            // Calcular la fecha del lunes de la semana de inicio
            var fechaInicio = GetLunesDeISOMeek(year, semanaInicio);
            
            // Determinar cuántos meses se deben sumar según la frecuencia
            int mesesAsumar = frecuencia switch
            {
                FrecuenciaMantenimiento.Semanal => 0, // Especial: 1 semana
                FrecuenciaMantenimiento.Quincenal => 0, // Especial: 2 semanas
                FrecuenciaMantenimiento.Mensual => 1,
                FrecuenciaMantenimiento.Bimestral => 2,
                FrecuenciaMantenimiento.Trimestral => 3,
                FrecuenciaMantenimiento.Cuatrimestral => 4, // ✅ AQUÍ ESTÁ LA SOLUCIÓN
                FrecuenciaMantenimiento.Semestral => 6,
                FrecuenciaMantenimiento.Anual => 12,
                _ => 1
            };

            // Marcar semanas según la frecuencia
            var fechaActual = fechaInicio;
            
            while (fechaActual.Year <= year)
            {
                // Si estamos fuera del año, detener
                if (fechaActual.Year > year)
                    break;

                // Obtener la semana ISO de esta fecha
                int isoWeek = ISOWeek.GetWeekOfYear(fechaActual);
                int isoYear = fechaActual.Year; // La semana obtenida es del año de la fecha
                
                // Nota: ISOWeek.GetWeekOfYear solo retorna el número de semana, 
                // pero la fecha puede estar en una semana que pertenece a otro año
                // Necesitamos verificar esto manualmente
                if (fechaActual.Month == 1 && isoWeek > 30)
                {
                    // Esa semana pertenece al año anterior
                    isoYear = fechaActual.Year - 1;
                }
                else if (fechaActual.Month == 12 && isoWeek < 10)
                {
                    // Esa semana pertenece al siguiente año
                    isoYear = fechaActual.Year + 1;
                }
                
                // Si la semana pertenece al año actual, marcarla
                if (isoYear == year && isoWeek >= 1 && isoWeek <= weeksInYear)
                {
                    semanas[isoWeek - 1] = true; // Convertir a índice 0-based
                }

                // Calcular próxima fecha según frecuencia
                if (frecuencia == FrecuenciaMantenimiento.Semanal)
                {
                    fechaActual = fechaActual.AddDays(7); // 1 semana
                }
                else if (frecuencia == FrecuenciaMantenimiento.Quincenal)
                {
                    fechaActual = fechaActual.AddDays(14); // 2 semanas
                }
                else
                {
                    fechaActual = fechaActual.AddMonths(mesesAsumar);
                }
            }            return semanas;
        }

        /// <summary>
        /// Obtiene la fecha del lunes correspondiente a una semana ISO específica.
        /// </summary>
        private static DateTime GetLunesDeISOMeek(int year, int week)
        {
            // El 4 de enero siempre está en la semana 1 ISO
            var ref4Enero = new DateTime(year, 1, 4);
            
            // Calcular el lunes de la semana 1:
            // DayOfWeek: Monday=1, Tuesday=2, ..., Sunday=0
            // Necesitamos retroceder al lunes anterior (o quedarnos en lunes si ya lo es)
            int daysFromMonday = ((int)ref4Enero.DayOfWeek - 1 + 7) % 7;
            var lunesSemana1 = ref4Enero.AddDays(-daysFromMonday);
            
            // El lunes de la semana requerida
            int diasDiferencia = (week - 1) * 7;
            return lunesSemana1.AddDays(diasDiferencia);
        }

        // Sobrecarga para compatibilidad (usa el año actual)
        public static bool[] GenerarSemanas(int semanaInicio, FrecuenciaMantenimiento? frecuencia)
        {
            return GenerarSemanas(semanaInicio, frecuencia, DateTime.Now.Year);
        }

        /// <summary>
        /// Genera automáticamente los cronogramas del siguiente año para todos los equipos activos si faltan 3 meses para acabar el año y aún no existen.
        /// </summary>
        public async Task GenerateNextYearCronogramasIfNeeded()
        {
            var now = DateTime.Now;
            if (now.Month < 10) // Solo ejecutar a partir de octubre
                return;
            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipos = await dbContext.Equipos.Where(e => e.Estado == Models.Enums.EstadoEquipo.Activo).ToListAsync();
            int nextYear = now.Year + 1;
            foreach (var equipo in equipos)
            {
                // Si ya existe cronograma para el siguiente año, omitir
                bool exists = await dbContext.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == nextYear);
                if (exists) continue;                // Buscar cronograma del año actual
                var cronogramaActual = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == now.Year);
                int semanaInicio = 1;
                
                if (cronogramaActual != null)
                {
                    // Buscar última semana con mantenimiento programado
                    int lastWeek = Array.FindLastIndex(cronogramaActual.Semanas, s => s);                    if (lastWeek >= 0 && equipo.FrecuenciaMtto != null)
                    {
                        // ✅ NUEVO: Usar el DÍA DEL MES específico, no solo el offset de día de semana
                        // Obtener la fecha del lunes de la última semana del año actual
                        var lunesUltimaSemana = GetLunesDeISOMeek(now.Year, lastWeek + 1);
                        
                        // Buscar el día específico del mes dentro de la última semana
                        int diaDelMes = equipo.FechaCompra!.Value.Day; // Ej: 15
                        DateTime? fechaUltimaSemana = null;
                        
                        for (int d = 0; d < 7; d++)
                        {
                            var fechaEnSemana = lunesUltimaSemana.AddDays(d);
                            if (fechaEnSemana.Day == diaDelMes)
                            {
                                fechaUltimaSemana = fechaEnSemana;
                                break;
                            }
                        }
                        
                        // Si no existe ese día en la última semana, usar el domingo de la semana
                        if (!fechaUltimaSemana.HasValue)
                        {
                            fechaUltimaSemana = lunesUltimaSemana.AddDays(6);
                        }
                        
                        // Calcular cuántos meses según frecuencia
                        int mesesAsumar = equipo.FrecuenciaMtto switch
                        {
                            Models.Enums.FrecuenciaMantenimiento.Semanal => 0,
                            Models.Enums.FrecuenciaMantenimiento.Quincenal => 0,
                            Models.Enums.FrecuenciaMantenimiento.Mensual => 1,
                            Models.Enums.FrecuenciaMantenimiento.Bimestral => 2,
                            Models.Enums.FrecuenciaMantenimiento.Trimestral => 3,
                            Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 4, // ✅ 4 MESES EXACTOS
                            Models.Enums.FrecuenciaMantenimiento.Semestral => 6,
                            Models.Enums.FrecuenciaMantenimiento.Anual => 12,
                            _ => 1
                        };
                        
                        // Calcular próxima fecha de mantenimiento
                        DateTime fechaProxima;
                        if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Semanal)
                            fechaProxima = fechaUltimaSemana.Value.AddDays(7);
                        else if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Quincenal)
                            fechaProxima = fechaUltimaSemana.Value.AddDays(14);
                        else
                            fechaProxima = fechaUltimaSemana.Value.AddMonths(mesesAsumar);
                        
                        // Obtener la semana ISO de la próxima fecha
                        int isoWeek = ISOWeek.GetWeekOfYear(fechaProxima);
                        int isoYear = fechaProxima.Year;
                        
                        // Ajustar para semanas que cruzan años
                        if (fechaProxima.Month == 1 && isoWeek > 30)
                        {
                            isoYear = fechaProxima.Year - 1;
                        }
                        else if (fechaProxima.Month == 12 && isoWeek < 10)
                        {
                            isoYear = fechaProxima.Year + 1;
                        }
                        
                        // Si cae en el siguiente año, usar esa semana
                        if (isoYear == nextYear)
                        {
                            semanaInicio = isoWeek;
                        }
                        else
                        {
                            semanaInicio = 1; // Fallback
                        }
                    }
                    else
                    {
                        semanaInicio = 1;
                    }
                }
                var semanas = GenerarSemanas(semanaInicio, equipo.FrecuenciaMtto, nextYear);

                var nuevo = new Models.Entities.CronogramaMantenimiento
                {
                    // SemanaInicioMtto eliminado de la generación de cronogramas para el próximo año
                    Codigo = equipo.Codigo!,
                    Nombre = equipo.Nombre!,
                    Marca = equipo.Marca,
                    Sede = equipo.Sede?.ToString(),
                    FrecuenciaMtto = equipo.FrecuenciaMtto,
                    Semanas = semanas,
                    Anio = nextYear
                };
                dbContext.Cronogramas.Add(nuevo);
            }
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Genera los seguimientos faltantes para todos los cronogramas existentes.
        /// </summary>
        public async Task GenerarSeguimientosFaltantesAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var cronogramas = dbContext.Cronogramas.ToList();
            int totalAgregados = 0;

            foreach (var cronograma in cronogramas)
            {
                for (int i = 0; i < cronograma.Semanas.Length; i++)
                {
                    if (cronograma.Semanas[i])
                    {
                        int semana = i + 1;
                        bool existe = dbContext.Seguimientos.Any(s => s.Codigo == cronograma.Codigo && s.Semana == semana && s.Anio == cronograma.Anio);
                        if (!existe)
                        {
                            // Calcular el lunes de la semana correspondiente
                            var fechaLunes = System.Globalization.CultureInfo.CurrentCulture.Calendar.AddWeeks(new DateTime(cronograma.Anio, 1, 1), semana - 1);
                            dbContext.Seguimientos.Add(new Models.Entities.SeguimientoMantenimiento
                            {                                Codigo = cronograma.Codigo,
                                Nombre = cronograma.Nombre,
                                Semana = semana,
                                Anio = cronograma.Anio,
                                TipoMtno = TipoMantenimiento.Preventivo,
                                Descripcion = "Mantenimiento programado",
                                Responsable = string.Empty,
                                FechaRegistro = fechaLunes
                            });
                            totalAgregados++;
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[MIGRACION] Seguimientos generados: {totalAgregados}");
            // Notificar actualización de seguimientos
            WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
        }        /// <summary>
        /// Asegura que todos los equipos activos tengan cronogramas completos desde su año de registro hasta el actual (y el siguiente si es octubre o más).
        /// </summary>
        public async Task EnsureAllCronogramasUpToDateAsync()
        {
            _logger.LogInformation("[CRONOGRAMA] 🔄 EnsureAllCronogramasUpToDateAsync INICIANDO...");
            var now = DateTime.Now;
            int anioActual = now.Year;
            int anioLimite = anioActual;
            if (now.Month >= 10) // Octubre o más, también crear el del siguiente año
                anioLimite = anioActual + 1;            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipos = await dbContext.Equipos.Where(e => e.Estado == Models.Enums.EstadoEquipo.Activo && e.FechaCompra != null && e.FrecuenciaMtto != null).ToListAsync();
            
            _logger.LogInformation($"[CRONOGRAMA] ✓ Equipos activos encontrados: {equipos.Count}, Años a procesar: {anioActual} a {anioLimite}");            
            int totalCronogramasCreados = 0;            foreach (var equipo in equipos)
            {
                _logger.LogDebug($"[CRONOGRAMA] 📋 Procesando equipo: {equipo.Codigo}");
                int anioRegistro = equipo.FechaCompra!.Value.Year;
                int semanaRegistro = CalcularSemanaISO8601(equipo.FechaCompra.Value);
                _logger.LogDebug($"[CRONOGRAMA] FechaCompra={equipo.FechaCompra:yyyy-MM-dd}, SemanaCompra={semanaRegistro}, Frecuencia={equipo.FrecuenciaMtto}");
                
                for (int anio = anioRegistro; anio <= anioLimite; anio++)
                {
                    bool existe = await dbContext.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);                    if (existe)
                    {
                        _logger.LogDebug($"[CRONOGRAMA] ↩️ Cronograma existe para {equipo.Codigo} año {anio}, saltando");
                        continue;
                    }                    _logger.LogDebug($"[CRONOGRAMA] ✅ Creando cronograma para {equipo.Codigo} año {anio}");
                    int semanaInicio;
                    if (anio == anioRegistro)
                    {
                        // ✅ CORREGIDO: Usar cálculo basado en fechas reales, NO saltos de semanas fijas
                        int yearsWeeks = ISOWeek.GetWeeksInYear(anio);
                        
                        // Obtener fecha del lunes de la semana de compra
                        var fechaCompra = GetLunesDeISOMeek(anio, semanaRegistro);
                        
                        // Calcular cuántos meses según frecuencia
                        int mesesAsumar = equipo.FrecuenciaMtto switch
                        {
                            Models.Enums.FrecuenciaMantenimiento.Semanal => 0,
                            Models.Enums.FrecuenciaMantenimiento.Quincenal => 0,
                            Models.Enums.FrecuenciaMantenimiento.Mensual => 1,
                            Models.Enums.FrecuenciaMantenimiento.Bimestral => 2,
                            Models.Enums.FrecuenciaMantenimiento.Trimestral => 3,
                            Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 4, // ✅ 4 MESES EXACTOS
                            Models.Enums.FrecuenciaMantenimiento.Semestral => 6,
                            Models.Enums.FrecuenciaMantenimiento.Anual => 12,
                            _ => 1
                        };
                        
                        // Calcular próxima fecha de mantenimiento
                        DateTime fechaProxima;
                        if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Semanal)
                            fechaProxima = fechaCompra.AddDays(7);
                        else if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Quincenal)
                            fechaProxima = fechaCompra.AddDays(14);
                        else
                            fechaProxima = fechaCompra.AddMonths(mesesAsumar);
                        
                        // Obtener la semana ISO de la próxima fecha
                        int isoWeek = ISOWeek.GetWeekOfYear(fechaProxima);
                        int isoYear = fechaProxima.Year;
                        
                        // Ajustar para semanas que cruzan años
                        if (fechaProxima.Month == 1 && isoWeek > 30)
                        {
                            isoYear = fechaProxima.Year - 1;
                        }
                        else if (fechaProxima.Month == 12 && isoWeek < 10)
                        {
                            isoYear = fechaProxima.Year + 1;
                        }
                        
                        // Si cae en el siguiente año, NO generar cronograma en este año
                        if (isoYear > anio)
                        {
                            _logger.LogDebug($"[CRONOGRAMA] Equipo={equipo.Codigo}, FechaCompra={equipo.FechaCompra:yyyy-MM-dd}, Próximo mtto={fechaProxima:yyyy-MM-dd} (semana {isoWeek}), está en año {isoYear}, saltando {anio}");
                            continue;  // Saltar este año, el cronograma se creará en el siguiente
                        }
                        
                        semanaInicio = isoWeek;
                        _logger.LogDebug($"[CRONOGRAMA] Equipo={equipo.Codigo}, FechaCompra={equipo.FechaCompra:yyyy-MM-dd}, Próximo mtto={fechaProxima:yyyy-MM-dd}, SemanaInicio={semanaInicio}, Año={anio}");
                    }                    else
                    {
                        var cronogramaAnterior = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == (anio - 1));
                          if (cronogramaAnterior != null)
                        {
                            // ✅ CORREGIDO: Usar el DÍA DEL MES específico, no solo el offset de día de semana
                            int lastWeek = Array.FindLastIndex(cronogramaAnterior.Semanas, s => s);
                            if (lastWeek >= 0)
                            {
                                // Obtener la fecha del lunes de la última semana del año anterior
                                var lunesUltimaSemana = GetLunesDeISOMeek(anio - 1, lastWeek + 1);
                                
                                // Buscar el día específico del mes dentro de la última semana
                                int diaDelMes = equipo.FechaCompra!.Value.Day; // Ej: 15
                                DateTime? fechaUltimaSemana = null;
                                
                                for (int d = 0; d < 7; d++)
                                {
                                    var fechaEnSemana = lunesUltimaSemana.AddDays(d);
                                    if (fechaEnSemana.Day == diaDelMes)
                                    {
                                        fechaUltimaSemana = fechaEnSemana;
                                        break;
                                    }
                                }
                                
                                // Si no existe ese día en la última semana, usar el domingo de la semana
                                if (!fechaUltimaSemana.HasValue)
                                {
                                    fechaUltimaSemana = lunesUltimaSemana.AddDays(6);
                                }
                                
                                // Calcular cuántos meses según frecuencia
                                int mesesAsumar = equipo.FrecuenciaMtto switch
                                {
                                    Models.Enums.FrecuenciaMantenimiento.Semanal => 0,
                                    Models.Enums.FrecuenciaMantenimiento.Quincenal => 0,
                                    Models.Enums.FrecuenciaMantenimiento.Mensual => 1,
                                    Models.Enums.FrecuenciaMantenimiento.Bimestral => 2,
                                    Models.Enums.FrecuenciaMantenimiento.Trimestral => 3,
                                    Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 4,
                                    Models.Enums.FrecuenciaMantenimiento.Semestral => 6,
                                    Models.Enums.FrecuenciaMantenimiento.Anual => 12,
                                    _ => 1
                                };                                
                                // Calcular próxima fecha de mantenimiento
                                DateTime fechaProxima;
                                if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Semanal)
                                    fechaProxima = fechaUltimaSemana.Value.AddDays(7);
                                else if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Quincenal)
                                    fechaProxima = fechaUltimaSemana.Value.AddDays(14);
                                else
                                    fechaProxima = fechaUltimaSemana.Value.AddMonths(mesesAsumar);
                                
                                // Obtener la semana ISO de la próxima fecha
                                int isoWeek = ISOWeek.GetWeekOfYear(fechaProxima);
                                int isoYear = fechaProxima.Year;
                                
                                // Ajustar para semanas que cruzan años
                                if (fechaProxima.Month == 1 && isoWeek > 30)
                                {
                                    isoYear = fechaProxima.Year - 1;
                                }
                                else if (fechaProxima.Month == 12 && isoWeek < 10)
                                {
                                    isoYear = fechaProxima.Year + 1;
                                }
                                
                                // Si cae en el año actual, usar esa semana
                                if (isoYear == anio)
                                {
                                    semanaInicio = isoWeek;
                                }
                                else
                                {
                                    semanaInicio = 1; // Fallback
                                }
                                
                                _logger.LogInformation($"[CRONOGRAMA] Año siguiente - Equipo={equipo.Codigo}, UltimaMttoAñoAnterior={fechaUltimaSemana:yyyy-MM-dd} (sem {lastWeek + 1}), ProximoMtto={fechaProxima:yyyy-MM-dd} (sem {isoWeek}/{isoYear}), SemanaInicio={semanaInicio}, Año={anio}");
                            }
                            else
                            {
                                _logger.LogWarning($"[CRONOGRAMA] Año siguiente - Equipo={equipo.Codigo} NO tiene mantenimientos en {anio - 1}, iniciando en semana 1");
                                semanaInicio = 1;
                            }
                        }
                        else if (anio - 1 == anioRegistro && equipo.FechaCompra.HasValue)
                        {
                            // ✅ CORREGIDO: Caso especial cuando cronograma del año de compra se saltó
                            int semanaRegistroEquipo = ISOWeek.GetWeekOfYear(equipo.FechaCompra.Value);
                            
                            // Obtener fecha del lunes de la semana de compra
                            var fechaCompra = GetLunesDeISOMeek(anioRegistro, semanaRegistroEquipo);
                            
                            // Calcular cuántos meses según frecuencia
                            int mesesAsumar = equipo.FrecuenciaMtto switch
                            {
                                Models.Enums.FrecuenciaMantenimiento.Semanal => 0,
                                Models.Enums.FrecuenciaMantenimiento.Quincenal => 0,
                                Models.Enums.FrecuenciaMantenimiento.Mensual => 1,
                                Models.Enums.FrecuenciaMantenimiento.Bimestral => 2,
                                Models.Enums.FrecuenciaMantenimiento.Trimestral => 3,
                                Models.Enums.FrecuenciaMantenimiento.Cuatrimestral => 4,
                                Models.Enums.FrecuenciaMantenimiento.Semestral => 6,
                                Models.Enums.FrecuenciaMantenimiento.Anual => 12,
                                _ => 1
                            };
                            
                            // Calcular próxima fecha de mantenimiento
                            DateTime fechaProxima;
                            if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Semanal)
                                fechaProxima = fechaCompra.AddDays(7);
                            else if (equipo.FrecuenciaMtto == Models.Enums.FrecuenciaMantenimiento.Quincenal)
                                fechaProxima = fechaCompra.AddDays(14);
                            else
                                fechaProxima = fechaCompra.AddMonths(mesesAsumar);
                            
                            // Obtener la semana ISO de la próxima fecha
                            int isoWeek = ISOWeek.GetWeekOfYear(fechaProxima);
                            int isoYear = fechaProxima.Year;
                            
                            // Ajustar para semanas que cruzan años
                            if (fechaProxima.Month == 1 && isoWeek > 30)
                            {
                                isoYear = fechaProxima.Year - 1;
                            }
                            else if (fechaProxima.Month == 12 && isoWeek < 10)
                            {
                                isoYear = fechaProxima.Year + 1;
                            }
                            
                            // Si cae en el año actual, usar esa semana
                            if (isoYear == anio)
                            {
                                semanaInicio = isoWeek;
                            }
                            else
                            {
                                semanaInicio = 1;
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"[CRONOGRAMA] Año siguiente - Equipo={equipo.Codigo} NO tiene cronograma en {anio - 1}, iniciando en semana 1");
                            semanaInicio = 1;
                        }
                    }
                    var semanas = GenerarSemanas(semanaInicio, equipo.FrecuenciaMtto, anio);
                    var nuevo = new CronogramaMantenimiento
                    {
                        Codigo = equipo.Codigo!,
                        Nombre = equipo.Nombre!,
                        Marca = equipo.Marca,
                        Sede = equipo.Sede?.ToString(),
                        FrecuenciaMtto = equipo.FrecuenciaMtto,
                        Semanas = semanas,
                        Anio = anio
                    };                    dbContext.Cronogramas.Add(nuevo);

                    // Guardar inmediatamente para que esté disponible en la siguiente iteración
                    await dbContext.SaveChangesAsync();
                    totalCronogramasCreados++;
                }
            }
            
            _logger.LogInformation($"[CRONOGRAMA] ✅ EnsureAllCronogramasUpToDateAsync FINALIZADO - Total cronogramas creados: {totalCronogramasCreados}");
            
            // Al final del método, notificar actualización de seguimientos
            WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());
        }        private int CalcularSemanaISO8601(DateTime fecha)
        {
            // Usar ISOWeek para cálculo verdadero de semanas ISO 8601
            return ISOWeek.GetWeekOfYear(fecha);
        }        // Utilidad para obtener el primer día de la semana ISO 8601
        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
            var firstThursday = jan1.AddDays(daysOffset);
            int firstWeek = ISOWeek.GetWeekOfYear(firstThursday);
            var weekNum = weekOfYear;
            if (firstWeek <= 1)
                weekNum -= 1;
            var result = firstThursday.AddDays(weekNum * 7);
            return result.AddDays(-3);
        }

        private SeguimientoMantenimientoDto? MapToDto(SeguimientoMantenimiento? s)
        {
            if (s == null) return null;
            return new SeguimientoMantenimientoDto
            {
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                TipoMtno = s.TipoMtno,
                Descripcion = s.Descripcion,
                Responsable = s.Responsable,
                Costo = s.Costo,
                Observaciones = s.Observaciones,
                FechaRegistro = s.FechaRegistro,
                FechaRealizacion = s.FechaRealizacion,
                Semana = s.Semana,
                Anio = s.Anio,
                Estado = s.Estado
            };
        }        public async Task<List<MantenimientoSemanaEstadoDto>> GetEstadoMantenimientosSemanaAsync(int semana, int anio)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var cronogramasDelAnio = await dbContext.Cronogramas
                .Where(c => c.Anio == anio)
                .ToListAsync();

            var cronogramasConMantenimiento = cronogramasDelAnio
                .Where(c => c.Semanas != null &&
                           c.Semanas.Length >= semana &&
                           c.Semanas[semana - 1])
                .ToList();
            var estados = new List<MantenimientoSemanaEstadoDto>();            var seguimientos = await dbContext.Seguimientos
                .Where(s => s.Anio == anio && s.Semana == semana)
                .ToListAsync();
            
            // Cargar datos de equipos para obtener Sede
            var equiposDict = await dbContext.Equipos
                .ToDictionaryAsync(e => e.Codigo, e => e.Sede);
            
            // Calcular fechas de inicio y fin de la semana ISO 8601
            var fechaInicioSemana = FirstDateOfWeekISO8601(anio, semana);
            var fechaFinSemana = fechaInicioSemana.AddDays(6);
            // Calcular semana y año actual
            var hoy = DateTime.Now;
            int semanaActual = ISOWeek.GetWeekOfYear(hoy);
            int anioActual = hoy.Year;            foreach (var c in cronogramasConMantenimiento)
            {
                var seguimiento = seguimientos.FirstOrDefault(s => s.Codigo == c.Codigo);
                var sede = ParseSede(c.Sede);
                var estado = new MantenimientoSemanaEstadoDto
                {
                    CodigoEquipo = c.Codigo,
                    NombreEquipo = c.Nombre,
                    Sede = sede,
                    Semana = semana,
                    Anio = anio,
                    Frecuencia = c.FrecuenciaMtto,
                    Programado = true,
                    Seguimiento = MapToDto(seguimiento)
                };
                int diff = semana - semanaActual;
                bool puedeRegistrar = (anio == anioActual && (diff == 0 || diff == -1));
                // Lógica reforzada para el estado visual
                if (anio < anioActual || (anio == anioActual && diff < -1))
                {
                    // Semanas previas a la anterior
                    if (seguimiento == null || seguimiento.FechaRegistro == null)
                    {
                        estado.Realizado = false;
                        estado.Atrasado = false;
                        estado.Estado = EstadoSeguimientoMantenimiento.NoRealizado;
                    }
                    else
                    {
                        DateTime? fechaRealizacion = seguimiento.FechaRealizacion;
                        if (fechaRealizacion.HasValue && fechaRealizacion.Value >= fechaInicioSemana && fechaRealizacion.Value <= fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                        }
                        else if (fechaRealizacion.HasValue && fechaRealizacion.Value > fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                        }
                        else
                        {
                            // Si existe seguimiento pero no tiene fecha válida, se considera no realizado
                            estado.Realizado = false;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.NoRealizado;
                        }
                    }
                    estados.Add(estado);
                    continue;
                }
                if (anio == anioActual && diff == -1)
                {
                    // Semana anterior
                    if (seguimiento == null || seguimiento.FechaRegistro == null)
                    {
                        estado.Realizado = false;
                        estado.Atrasado = true;
                        estado.Estado = EstadoSeguimientoMantenimiento.Atrasado;
                    }
                    else
                    {
                        DateTime? fechaRealizacion = seguimiento.FechaRealizacion;
                        if (fechaRealizacion.HasValue && fechaRealizacion.Value >= fechaInicioSemana && fechaRealizacion.Value <= fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                        }
                        else if (fechaRealizacion.HasValue && fechaRealizacion.Value > fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                        }
                        else
                        {
                            estado.Realizado = false;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.Atrasado;
                        }
                    }
                    estados.Add(estado);
                    continue;
                }
                if (anio == anioActual && diff == 0)
                {
                    // Semana actual
                    if (seguimiento == null || seguimiento.FechaRegistro == null)
                    {
                        estado.Realizado = false;
                        estado.Atrasado = false;
                        estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                    }
                    else
                    {
                        DateTime? fechaRealizacion = seguimiento.FechaRealizacion;
                        if (fechaRealizacion.HasValue && fechaRealizacion.Value >= fechaInicioSemana && fechaRealizacion.Value <= fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                        }
                        else if (fechaRealizacion.HasValue && fechaRealizacion.Value > fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                        }
                        else
                        {
                            estado.Realizado = false;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                        }
                    }
                    estados.Add(estado);
                    continue;
                }
                // Semana posterior: pendiente, pero no registrable
                if (anio == anioActual && diff > 0)
                {
                    estado.Realizado = false;
                    estado.Atrasado = false;
                    estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                    estados.Add(estado);
                    continue;
                }

                // Años futuros: marcar como pendiente (no registrable todavía) para que se muestren en la UI
                if (anio > anioActual)
                {
                    estado.Realizado = false;
                    estado.Atrasado = false;
                    estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                    estados.Add(estado);
                    continue;
                }            }
            // Al final del método, después de procesar los programados:
            // Agregar estados para seguimientos manuales (no programados) de esa semana/año que no esté ya en la lista
            var codigosProgramados = cronogramasConMantenimiento.Select(c => c.Codigo).ToHashSet();            foreach (var seguimiento in seguimientos)
            {
                if (!codigosProgramados.Contains(seguimiento.Codigo))
                {
                    var sede = equiposDict.ContainsKey(seguimiento.Codigo) ? equiposDict[seguimiento.Codigo] : null;
                    var estado = new MantenimientoSemanaEstadoDto
                    {
                        CodigoEquipo = seguimiento.Codigo,
                        NombreEquipo = seguimiento.Nombre,
                        Sede = sede,
                        Semana = semana,
                        Anio = anio,
                        Frecuencia = seguimiento.Frecuencia,
                        Programado = false,
                        Seguimiento = MapToDto(seguimiento),
                        Realizado = seguimiento.FechaRealizacion.HasValue,
                        Estado = seguimiento.Estado
                    };
                    estados.Add(estado);
                }
            }

            return estados;
        }        private Sede? ParseSede(string? sedeString)
        {
            if (string.IsNullOrWhiteSpace(sedeString))
                return null;
            
            if (Enum.TryParse<Sede>(sedeString, ignoreCase: true, out var sede))
                return sede;
            
            return null;
        }

        /// <summary>
        /// Sincroniza los datos desnormalizados (Nombre, Marca, Sede) del equipo con todos sus cronogramas.
        /// Se ejecuta cuando se edita un equipo para reflejar los cambios en los cronogramas existentes.
        /// </summary>
        public async Task SyncEquipoCronogramasAsync(string codigoEquipo)
        {
            try
            {
                _logger.LogInformation($"[CRONOGRAMA] Sincronizando datos del equipo {codigoEquipo} con sus cronogramas...");

                using var dbContext = _dbContextFactory.CreateDbContext();
                
                // Obtener el equipo actualizado
                var equipo = await dbContext.Equipos
                    .FirstOrDefaultAsync(e => e.Codigo == codigoEquipo);
                
                if (equipo == null)
                {
                    _logger.LogWarning($"[CRONOGRAMA] Equipo {codigoEquipo} no encontrado para sincronizar cronogramas");
                    return;
                }

                // Obtener todos los cronogramas del equipo
                var cronogramas = await dbContext.Cronogramas
                    .Where(c => c.Codigo == codigoEquipo)
                    .ToListAsync();

                if (!cronogramas.Any())
                {
                    _logger.LogDebug($"[CRONOGRAMA] No hay cronogramas para el equipo {codigoEquipo}");
                    return;
                }

                // Actualizar los datos desnormalizados en cada cronograma
                int actualizados = 0;
                foreach (var cronograma in cronogramas)
                {
                    bool cambio = false;

                    // Actualizar Nombre (Equipo.Nombre es string?)
                    if (cronograma.Nombre != equipo.Nombre)
                    {
                        cronograma.Nombre = equipo.Nombre ?? "";
                        cambio = true;
                    }

                    // Actualizar Marca
                    if (cronograma.Marca != equipo.Marca)
                    {
                        cronograma.Marca = equipo.Marca;
                        cambio = true;
                    }

                    // Actualizar Sede (convertir enum a string)
                    string? sedeCronograma = equipo.Sede.HasValue ? equipo.Sede.ToString() : null;
                    if (cronograma.Sede != sedeCronograma)
                    {
                        cronograma.Sede = sedeCronograma;
                        cambio = true;
                    }

                    if (cambio)
                    {
                        actualizados++;
                        _logger.LogDebug($"[CRONOGRAMA] Actualizado cronograma {cronograma.Codigo} año {cronograma.Anio}");
                    }
                }

                // Guardar cambios solo si hubo actualizaciones
                if (actualizados > 0)
                {
                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation($"[CRONOGRAMA] ✓ Sincronización completada: {actualizados} cronogramas actualizados para equipo {codigoEquipo}");
                }
                else
                {
                    _logger.LogDebug($"[CRONOGRAMA] No había cambios para sincronizar en el equipo {codigoEquipo}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[CRONOGRAMA] Error sincronizando cronogramas del equipo {codigoEquipo}");
                throw;
            }
        }
    }
}


