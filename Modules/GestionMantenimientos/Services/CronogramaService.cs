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

namespace GestLog.Modules.GestionMantenimientos.Services
{
    public class CronogramaService : ICronogramaService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        public CronogramaService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
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
            if (cronograma.Semanas == null || cronograma.Semanas.Length != 52)
                throw new GestionMantenimientosDomainException("El cronograma debe tener 52 semanas definidas.");
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
                                TipoMtno = TipoMantenimiento.Preventivo // Por defecto, o puedes ajustar según lógica
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
                                TipoMtno = TipoMantenimiento.Preventivo // Por defecto, o puedes ajustar según lógica
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
                    // Validar encabezados de semanas
                    for (int s = 1; s <= 52; s++)
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
                                Semanas = new bool[52]
                            };
                            for (int s = 0; s < 52; s++)
                            {
                                var val = worksheet.Cell(row, 7 + s).GetString();
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
                // S1...S52
                for (int s = 1; s <= 52; s++)
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
                    for (int s = 0; s < 52; s++)
                    {
                        worksheet.Cell(row, 6 + s).Value = c.Semanas != null && c.Semanas.Length > s && c.Semanas[s] ? "✔" : "";
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

        // Genera el array de semanas según la frecuencia y la semana de inicio
        public static bool[] GenerarSemanas(int semanaInicio, FrecuenciaMantenimiento? frecuencia)
        {
            var semanas = new bool[52];
            if (semanaInicio < 1 || semanaInicio > 52 || frecuencia == null)
                return semanas;
            int salto = frecuencia switch
            {
                FrecuenciaMantenimiento.Semanal => 1,
                FrecuenciaMantenimiento.Quincenal => 2,
                FrecuenciaMantenimiento.Mensual => 4,
                FrecuenciaMantenimiento.Bimestral => 8,
                FrecuenciaMantenimiento.Trimestral => 13,
                FrecuenciaMantenimiento.Semestral => 26,
                FrecuenciaMantenimiento.Anual => 52,
                _ => 1
            };
            int i = semanaInicio - 1;
            while (i < 52)
            {
                semanas[i] = true;
                i += salto;
            }
            return semanas;
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
                int semanaInicio = 1;                if (cronogramaActual != null)
                {
                    // Buscar última semana con mantenimiento programado
                    int lastWeek = Array.FindLastIndex(cronogramaActual.Semanas, s => s);                    if (lastWeek >= 0 && equipo.FrecuenciaMtto != null)
                    {
                        int salto = equipo.FrecuenciaMtto switch
                        {
                            Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                            Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                            Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                            Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                            Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                            Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                            Models.Enums.FrecuenciaMantenimiento.Anual => 52,
                            _ => 1
                        };                        // lastWeek es el índice (base 0), convertir a número de semana (base 1)
                        int ultimaSemana = lastWeek + 1;
                        int proximaSemana = ultimaSemana + salto;
                        
                        // LOG DEBUG: para depuración
                        _logger.LogInformation("[CronogramaService] DEBUG - Equipo: {Codigo}, lastWeek: {lastWeek}, ultimaSemana: {ultimaSemana}, salto: {salto}, proximaSemana: {proximaSemana}", 
                            equipo.Codigo, lastWeek, ultimaSemana, salto, proximaSemana);
                        
                        // Calcular la semana correspondiente del siguiente año
                        if (proximaSemana > 52)
                        {
                            semanaInicio = proximaSemana - 52;
                            _logger.LogInformation("[CronogramaService] DEBUG - Caso 1: semanaInicio calculada: {semanaInicio}", semanaInicio);
                        }
                        else
                        {
                            // Si aún no excede 52, significa que necesitamos calcular el siguiente ciclo
                            // Para el próximo año, agregamos un salto más
                            semanaInicio = proximaSemana + salto;
                            if (semanaInicio > 52)
                            {
                                semanaInicio = semanaInicio - 52;
                            }
                            _logger.LogInformation("[CronogramaService] DEBUG - Caso 2: semanaInicio calculada: {semanaInicio}", semanaInicio);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("[CronogramaService] DEBUG - Equipo: {Codigo}, lastWeek: {lastWeek}, usando semanaInicio = 1", equipo.Codigo, lastWeek);
                        semanaInicio = 1;
                    }
                }                var semanas = GenerarSemanas(semanaInicio, equipo.FrecuenciaMtto);
                
                // LOG DEBUG: mostrar las primeras semanas del cronograma generado
                var primerasSemanas = string.Join(", ", semanas.Select((s, i) => s ? (i + 1).ToString() : null).Where(s => s != null).Take(5));
                _logger.LogInformation("[CronogramaService] DEBUG - Equipo: {Codigo}, Cronograma 2025 generado. Primeras semanas: {primeras}", equipo.Codigo, primerasSemanas);
                
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
            await dbContext.SaveChangesAsync();        }

        /// <summary>
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
                                FechaRegistro = fechaLunes
                            });
                            totalAgregados++;
                        }
                    }
                }
            }
            
            await dbContext.SaveChangesAsync();
            _logger.LogInformation($"[MIGRACION] Seguimientos generados: {totalAgregados}");
        }

        /// <summary>
        /// Asegura que todos los equipos activos tengan cronogramas completos desde su año de registro hasta el actual (y el siguiente si es octubre o más).
        /// </summary>
        public async Task EnsureAllCronogramasUpToDateAsync()
        {
            var now = DateTime.Now;
            int anioActual = now.Year;
            int anioLimite = anioActual;
            if (now.Month >= 10) // Octubre o más, también crear el del siguiente año
                anioLimite = anioActual + 1;
            using var dbContext = _dbContextFactory.CreateDbContext();
            var equipos = await dbContext.Equipos.Where(e => e.Estado == Models.Enums.EstadoEquipo.Activo && e.FechaRegistro != null && e.FrecuenciaMtto != null).ToListAsync();
            foreach (var equipo in equipos)
            {
                int anioRegistro = equipo.FechaRegistro!.Value.Year;
                int semanaRegistro = CalcularSemanaISO8601(equipo.FechaRegistro.Value);
                for (int anio = anioRegistro; anio <= anioLimite; anio++)
                {
                    bool existe = await dbContext.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);
                    if (existe) continue;
                    int semanaInicio;
                    int salto = equipo.FrecuenciaMtto switch
                    {
                        Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                        Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                        Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                        Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                        Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                        Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                        Models.Enums.FrecuenciaMantenimiento.Anual => 52,
                        _ => 1
                    };
                    if (anio == anioRegistro)
                    {
                        semanaInicio = semanaRegistro + salto;
                        // Si la semana de inicio supera 52, que empiece en el siguiente ciclo
                        while (semanaInicio > 52) semanaInicio -= 52;
                        // Si la semana de inicio es menor a 1, ajusta a 1
                        if (semanaInicio < 1) semanaInicio = 1;
                        // Si la semana de inicio está muy cerca del final y no cabe el ciclo, que empiece en la primera semana del siguiente año
                        if (semanaInicio > 52 - salto + 1) semanaInicio = 1;
                    }                    else
                    {
                        var cronogramaAnterior = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == equipo.Codigo && c.Anio == (anio - 1));
                        
                        // LOG DEBUG: verificar si encuentra cronograma anterior
                        _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Equipo: {Codigo}, Año: {anio}, Buscando cronograma del año: {anioAnterior}, Encontrado: {encontrado}", 
                            equipo.Codigo, anio, anio - 1, cronogramaAnterior != null);
                        
                        if (cronogramaAnterior != null)
                        {
                            int lastWeek = Array.FindLastIndex(cronogramaAnterior.Semanas, s => s);                            if (lastWeek >= 0)
                            {                                // lastWeek es el índice (base 0), convertir a número de semana (base 1)
                                int ultimaSemana = lastWeek + 1;
                                int proximaSemana = ultimaSemana + salto;
                                
                                // LOG DEBUG: para depuración EnsureAllCronogramasUpToDateAsync
                                _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Equipo: {Codigo}, Año: {anio}, lastWeek: {lastWeek}, ultimaSemana: {ultimaSemana}, salto: {salto}, proximaSemana: {proximaSemana}", 
                                    equipo.Codigo, anio, lastWeek, ultimaSemana, salto, proximaSemana);
                                
                                // Calcular la semana correspondiente del siguiente año
                                if (proximaSemana > 52)
                                {
                                    semanaInicio = proximaSemana - 52;
                                    _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Caso 1: semanaInicio calculada: {semanaInicio}", semanaInicio);
                                }
                                else
                                {
                                    // Si aún no excede 52, significa que necesitamos calcular el siguiente ciclo
                                    // Para el próximo año, agregamos un salto más
                                    semanaInicio = proximaSemana + salto;
                                    if (semanaInicio > 52)
                                    {
                                        semanaInicio = semanaInicio - 52;
                                    }
                                    _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Caso 2: semanaInicio calculada: {semanaInicio}", semanaInicio);
                                }
                            }
                            else
                            {
                                _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Equipo: {Codigo}, Año: {anio}, lastWeek: {lastWeek}, usando semanaInicio = 1", equipo.Codigo, anio, lastWeek);
                                semanaInicio = 1;
                            }
                        }                        else
                        {
                            _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Equipo: {Codigo}, Año: {anio}, No se encontró cronograma anterior, usando semanaInicio = 1", equipo.Codigo, anio);
                            semanaInicio = 1;
                        }
                    }                    var semanas = GenerarSemanas(semanaInicio, equipo.FrecuenciaMtto);
                    
                    // LOG DEBUG: mostrar las primeras semanas del cronograma generado en EnsureAllCronogramasUpToDateAsync
                    var primerasSemanas = string.Join(", ", semanas.Select((s, i) => s ? (i + 1).ToString() : null).Where(s => s != null).Take(5));
                    _logger.LogInformation("[CronogramaService] DEBUG ENSURE - Equipo: {Codigo}, Año: {anio}, Cronograma generado. Primeras semanas: {primeras}", equipo.Codigo, anio, primerasSemanas);
                      var nuevo = new CronogramaMantenimiento
                    {
                        Codigo = equipo.Codigo!,
                        Nombre = equipo.Nombre!,
                        Marca = equipo.Marca,
                        Sede = equipo.Sede?.ToString(),
                        FrecuenciaMtto = equipo.FrecuenciaMtto,
                        Semanas = semanas,
                        Anio = anio
                    };
                    dbContext.Cronogramas.Add(nuevo);
                    
                    // Guardar inmediatamente para que esté disponible en la siguiente iteración
                    await dbContext.SaveChangesAsync();
                }
            }
        }

        private int CalcularSemanaISO8601(DateTime fecha)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(fecha, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }        /// <summary>
        /// Devuelve el estado de los mantenimientos programados para una semana y año dados.
        /// </summary>
        public async Task<List<MantenimientoSemanaEstadoDto>> GetEstadoMantenimientosSemanaAsync(int semana, int anio)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            
            _logger.LogInformation("[CronogramaService] DEBUG - Consultando estados para semana {semana}, año {anio}", semana, anio);
            
            // Obtener todos los cronogramas del año y filtrar en memoria
            // porque EF no puede traducir la indexación de arrays a SQL
            var cronogramasDelAnio = await dbContext.Cronogramas
                .Where(c => c.Anio == anio)
                .ToListAsync();
            
            _logger.LogInformation("[CronogramaService] DEBUG - Encontrados {count} cronogramas para el año {anio}", cronogramasDelAnio.Count, anio);
            
            // Filtrar en memoria los que tienen mantenimiento en la semana especificada
            var cronogramasConMantenimiento = cronogramasDelAnio
                .Where(c => c.Semanas != null && 
                           c.Semanas.Length >= semana && 
                           c.Semanas[semana - 1])
                .ToList();
            
            _logger.LogInformation("[CronogramaService] DEBUG - Encontrados {count} cronogramas con mantenimiento en semana {semana}", cronogramasConMantenimiento.Count, semana);
            
            var estados = new List<MantenimientoSemanaEstadoDto>();
            var seguimientos = await dbContext.Seguimientos
                .Where(s => s.Anio == anio && s.Semana == semana)
                .ToListAsync();

            foreach (var c in cronogramasConMantenimiento)
            {
                var seguimiento = seguimientos.FirstOrDefault(s => s.Codigo == c.Codigo);                var estado = new MantenimientoSemanaEstadoDto
                {
                    CodigoEquipo = c.Codigo,
                    NombreEquipo = c.Nombre,
                    Semana = semana,
                    Anio = anio,
                    Frecuencia = c.FrecuenciaMtto,
                    Programado = true,
                    Seguimiento = null
                };

                // Determinar el estado según la lógica de negocio
                if (seguimiento == null || seguimiento.FechaRegistro == null)
                {
                    // No realizado
                    var hoy = DateTime.Now;
                    var fechaFinSemana = System.Globalization.CultureInfo.CurrentCulture.Calendar.AddWeeks(new DateTime(anio, 1, 1), semana - 1).AddDays(6);
                    if (hoy <= fechaFinSemana)
                    {
                        estado.Realizado = false;
                        estado.Atrasado = false;
                        estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                    }
                    else
                    {
                        estado.Realizado = false;
                        estado.Atrasado = true;
                        estado.Estado = EstadoSeguimientoMantenimiento.Atrasado;
                    }
                }
                else
                {
                    // Realizado
                    DateTime? fechaRealizacion = seguimiento.FechaRealizacion;
                    if (fechaRealizacion.HasValue)
                    {
                        var fechaInicioSemana = System.Globalization.CultureInfo.CurrentCulture.Calendar.AddWeeks(new DateTime(anio, 1, 1), semana - 1);
                        var fechaFinSemana = fechaInicioSemana.AddDays(6);
                        if (fechaRealizacion.Value >= fechaInicioSemana && fechaRealizacion.Value <= fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                        }
                        else if (fechaRealizacion.Value > fechaFinSemana)
                        {
                            estado.Realizado = true;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo;
                        }
                        else
                        {
                            // Realizado antes de la semana (caso raro)
                            estado.Realizado = true;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo;
                        }
                    }
                    else
                    {
                        // No hay fecha real de realización, dejar como pendiente/atrasado según la fecha actual
                        var hoy = DateTime.Now;
                        var fechaFinSemana = System.Globalization.CultureInfo.CurrentCulture.Calendar.AddWeeks(new DateTime(anio, 1, 1), semana - 1).AddDays(6);
                        if (hoy <= fechaFinSemana)
                        {
                            estado.Realizado = false;
                            estado.Atrasado = false;
                            estado.Estado = EstadoSeguimientoMantenimiento.Pendiente;
                        }
                        else
                        {
                            estado.Realizado = false;
                            estado.Atrasado = true;
                            estado.Estado = EstadoSeguimientoMantenimiento.Atrasado;
                        }
                    }
                    estado.Seguimiento = new SeguimientoMantenimientoDto
                    {
                        Codigo = seguimiento.Codigo,
                        Nombre = seguimiento.Nombre,
                        TipoMtno = seguimiento.TipoMtno,
                        Descripcion = seguimiento.Descripcion,
                        Responsable = seguimiento.Responsable,
                        Costo = seguimiento.Costo,
                        Observaciones = seguimiento.Observaciones,
                        FechaRegistro = seguimiento.FechaRegistro,
                        FechaRealizacion = seguimiento.FechaRealizacion,
                        Semana = seguimiento.Semana,
                        Anio = seguimiento.Anio,
                        Estado = estado.Estado
                    };
                }
                estados.Add(estado);
                _logger.LogInformation("[CronogramaService] DEBUG - Agregado estado para equipo {codigo} - {nombre}", c.Codigo, c.Nombre);
            }
            
            _logger.LogInformation("[CronogramaService] DEBUG - Retornando {count} estados de mantenimiento", estados.Count);
            return estados;
        }
    }
}
