using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using System;
using GestLog.Services.Core.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;
using System.Globalization;

namespace GestLog.Modules.GestionMantenimientos.Services
{
    public class CronogramaService : ICronogramaService
    {
        private readonly IGestLogLogger _logger;
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
                        worksheet.Cell(row, headers.Length + 1 + s).Value = c.Semanas != null && c.Semanas.Length > s && c.Semanas[s] ? "✔" : "";
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
            int salto = frecuencia switch
            {
                FrecuenciaMantenimiento.Semanal => 1,
                FrecuenciaMantenimiento.Quincenal => 2,
                FrecuenciaMantenimiento.Mensual => 4,
                FrecuenciaMantenimiento.Bimestral => 8,
                FrecuenciaMantenimiento.Trimestral => 13,
                FrecuenciaMantenimiento.Semestral => 26,
                FrecuenciaMantenimiento.Anual => weeksInYear,
                _ => 1
            };
            int i = semanaInicio - 1;
            while (i < weeksInYear)
            {
                semanas[i] = true;
                i += salto;
            }
            return semanas;
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
                if (exists) continue;
                // Buscar cronograma del año actual
                var cronogramaActual = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == now.Year);
                int semanaInicio = 1;
                if (cronogramaActual != null)
                {
                    // Buscar última semana con mantenimiento programado
                    int lastWeek = Array.FindLastIndex(cronogramaActual.Semanas, s => s);
                    if (lastWeek >= 0 && equipo.FrecuenciaMtto != null)
                    {
                        int salto = equipo.FrecuenciaMtto switch
                        {
                            Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                            Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                            Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                            Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                            Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                            Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                            Models.Enums.FrecuenciaMantenimiento.Anual => ISOWeek.GetWeeksInYear(now.Year),
                            _ => 1
                        };

                        // Calcular la siguiente semana respetando el ciclo (módulo weeksInYear)
                        int yearsWeeks = ISOWeek.GetWeeksInYear(nextYear);
                        int ultimaSemana = lastWeek + 1; // base 1
                        int proximaSemana = ((ultimaSemana - 1 + salto) % yearsWeeks) + 1;
                        semanaInicio = proximaSemana;
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
                            {
                                Codigo = cronograma.Codigo,
                                Nombre = cronograma.Nombre,
                                Semana = semana,
                                Anio = cronograma.Anio,
                                TipoMtno = Models.Enums.TipoMantenimiento.Preventivo,
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
                anioLimite = anioActual + 1;
            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipos = await dbContext.Equipos.Where(e => e.Estado == Models.Enums.EstadoEquipo.Activo && e.FechaRegistro != null && e.FrecuenciaMtto != null).ToListAsync();
            
            _logger.LogInformation($"[CRONOGRAMA] ✓ Equipos activos encontrados: {equipos.Count}, Años a procesar: {anioActual} a {anioLimite}");            
            int totalCronogramasCreados = 0;
            foreach (var equipo in equipos)
            {
                _logger.LogInformation($"[CRONOGRAMA] 📋 Procesando equipo: {equipo.Codigo}");
                int anioRegistro = equipo.FechaRegistro!.Value.Year;
                int semanaRegistro = CalcularSemanaISO8601(equipo.FechaRegistro.Value);
                _logger.LogInformation($"[CRONOGRAMA] FechaRegistro={equipo.FechaRegistro:yyyy-MM-dd}, SemanaRegistro={semanaRegistro}, Frecuencia={equipo.FrecuenciaMtto}");
                
                for (int anio = anioRegistro; anio <= anioLimite; anio++)
                {
                    bool existe = await dbContext.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);
                    if (existe) 
                    {
                        _logger.LogInformation($"[CRONOGRAMA] ⏭️ Cronograma existe para {equipo.Codigo} año {anio}, saltando");
                        continue;
                    }
                    
                    _logger.LogInformation($"[CRONOGRAMA] ✅ Creando cronograma para {equipo.Codigo} año {anio}");
                    int semanaInicio;
                    int salto = equipo.FrecuenciaMtto switch
                    {
                        Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                        Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                        Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                        Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                        Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                        Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                        Models.Enums.FrecuenciaMantenimiento.Anual => ISOWeek.GetWeeksInYear(anio),
                        _ => 1
                    };                    if (anio == anioRegistro)
                    {
                        // Calcular semana de inicio a partir de la semana de registro y la frecuencia usando módulo weeksInYear
                        // El primer mantenimiento debe ser después del registro, no en el mismo día
                        int yearsWeeks = ISOWeek.GetWeeksInYear(anio);
                        
                        // La semana de inicio debe ser: semana_registro + salto
                        // Esto asegura que el primer mantenimiento sea DESPUÉS del registro
                        int proximaSemana = semanaRegistro + salto;
                        
                        // Si se pasa del año, ajustar con módulo
                        if (proximaSemana > yearsWeeks)
                        {
                            proximaSemana = ((proximaSemana - 1) % yearsWeeks) + 1;
                        }
                        
                        semanaInicio = proximaSemana;
                        
                        _logger.LogInformation($"[CRONOGRAMA] Equipo={equipo.Codigo}, FechaRegistro={equipo.FechaRegistro:yyyy-MM-dd}, SemanaRegistro={semanaRegistro}, Salto={salto}, SemanaInicio={semanaInicio}, TotalSemanas={yearsWeeks}, Año={anio}");
                    }
                    else
                    {
                        var cronogramaAnterior = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == (anio - 1));

                        if (cronogramaAnterior != null)
                        {
                            int lastWeek = Array.FindLastIndex(cronogramaAnterior.Semanas, s => s);
                            if (lastWeek >= 0)
                            {
                                int saltoAnterior = equipo.FrecuenciaMtto switch
                                {
                                    Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                    Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                    Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                    Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                    Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                    Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                    Models.Enums.FrecuenciaMantenimiento.Anual => ISOWeek.GetWeeksInYear(anio),
                                    _ => 1
                                };
                                // Calcular la semana de inicio para el año actual a partir del último mantenimiento del año anterior
                                int ultimaSemana = lastWeek + 1;
                                int yearsWeeks = ISOWeek.GetWeeksInYear(anio);
                                semanaInicio = ((ultimaSemana - 1 + saltoAnterior) % yearsWeeks) + 1;
                            }
                            else
                            {
                                semanaInicio = 1;
                            }
                        }
                        else
                        {
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
        }
        public async Task<List<MantenimientoSemanaEstadoDto>> GetEstadoMantenimientosSemanaAsync(int semana, int anio)
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
            var estados = new List<MantenimientoSemanaEstadoDto>();
            var seguimientos = await dbContext.Seguimientos
                .Where(s => s.Anio == anio && s.Semana == semana)
                .ToListAsync();            // Calcular fechas de inicio y fin de la semana ISO 8601
            var fechaInicioSemana = FirstDateOfWeekISO8601(anio, semana);
            var fechaFinSemana = fechaInicioSemana.AddDays(6);
            // Calcular semana y año actual
            var hoy = DateTime.Now;
            int semanaActual = ISOWeek.GetWeekOfYear(hoy);
            int anioActual = hoy.Year;
            foreach (var c in cronogramasConMantenimiento)
            {
                var seguimiento = seguimientos.FirstOrDefault(s => s.Codigo == c.Codigo);
                var estado = new MantenimientoSemanaEstadoDto
                {
                    CodigoEquipo = c.Codigo,
                    NombreEquipo = c.Nombre,
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
                }
            }
            // Al final del método, después de procesar los programados:
            // Agregar estados para seguimientos manuales (no programados) de esa semana/año que no esté ya en la lista
            var codigosProgramados = cronogramasConMantenimiento.Select(c => c.Codigo).ToHashSet();
            foreach (var seguimiento in seguimientos)
            {
                if (!codigosProgramados.Contains(seguimiento.Codigo))
                {
                    var estado = new MantenimientoSemanaEstadoDto
                    {
                        CodigoEquipo = seguimiento.Codigo,
                        NombreEquipo = seguimiento.Nombre,
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
        }
    }
}
