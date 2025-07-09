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

namespace GestLog.Modules.GestionMantenimientos.Services
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
                FechaRegistro = e.FechaRegistro,
                FrecuenciaMtto = e.FrecuenciaMtto,
                FechaBaja = e.FechaBaja
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
                FechaRegistro = equipo.FechaRegistro,
                FrecuenciaMtto = equipo.FrecuenciaMtto,
                FechaBaja = equipo.FechaBaja
                // SemanaInicioMtto eliminado
            };
        }

        public async Task AddAsync(EquipoDto equipo)
        {
            try
            {
                ValidarEquipo(equipo);
                using var dbContext = _dbContextFactory.CreateDbContext();
                if (await dbContext.Equipos.AnyAsync(e => e.Codigo == equipo.Codigo))
                    throw new GestionMantenimientosDomainException($"Ya existe un equipo con el código '{equipo.Codigo}'.");
                var now = DateTime.Now;
                int semanaInicio = CalcularSemanaISO8601(now);
                var entity = new Equipo
                {
                    Codigo = equipo.Codigo!,
                    Nombre = equipo.Nombre!,
                    Marca = equipo.Marca,
                    Estado = equipo.Estado ?? Models.Enums.EstadoEquipo.Activo,
                    Sede = equipo.Sede,
                    FechaRegistro = now,
                    Precio = equipo.Precio,
                    Observaciones = equipo.Observaciones,
                    FrecuenciaMtto = equipo.FrecuenciaMtto,
                    FechaBaja = equipo.FechaBaja
                    // SemanaInicioMtto eliminado
                };
                dbContext.Equipos.Add(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[EquipoService] Equipo agregado correctamente: {Codigo}", equipo.Codigo ?? "");

                // Generar cronograma automáticamente
                if (equipo.FrecuenciaMtto != null)
                {
                    var semanas = CronogramaService.GenerarSemanas(semanaInicio, equipo.FrecuenciaMtto);
                    var cronograma = new CronogramaMantenimientoDto
                    {
                        Codigo = equipo.Codigo,
                        Nombre = equipo.Nombre,
                        Marca = equipo.Marca,
                        Sede = equipo.Sede?.ToString(),
                        SemanaInicioMtto = semanaInicio,
                        FrecuenciaMtto = equipo.FrecuenciaMtto,
                        Semanas = semanas
                    };
                    using var dbContext2 = _dbContextFactory.CreateDbContext();
                    dbContext2.Cronogramas.Add(new CronogramaMantenimiento
                    {
                        Codigo = cronograma.Codigo!,
                        Nombre = cronograma.Nombre!,
                        Marca = cronograma.Marca,
                        Sede = cronograma.Sede,
                        SemanaInicioMtto = cronograma.SemanaInicioMtto,
                        FrecuenciaMtto = cronograma.FrecuenciaMtto,
                        Semanas = cronograma.Semanas
                    });
                    await dbContext2.SaveChangesAsync();
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
        }

        public async Task UpdateAsync(EquipoDto equipo)
        {
            try
            {
                ValidarEquipo(equipo);
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == equipo.Codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el equipo a actualizar.");
                // No permitir cambiar el código
                // entity.Codigo = equipo.Codigo; // NO modificar
                entity.Nombre = equipo.Nombre!;
                entity.Marca = equipo.Marca;
                entity.Estado = equipo.Estado ?? Models.Enums.EstadoEquipo.Activo;
                entity.Sede = equipo.Sede;
                entity.Precio = equipo.Precio;
                entity.Observaciones = equipo.Observaciones;
                entity.FechaRegistro = equipo.FechaRegistro ?? entity.FechaRegistro;
                entity.FrecuenciaMtto = equipo.FrecuenciaMtto;
                entity.FechaBaja = equipo.FechaBaja;
                // SemanaInicioMtto eliminado
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[EquipoService] Equipo actualizado correctamente: {Codigo}", equipo.Codigo ?? "");
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
            {
                _logger.LogError(ex, "[EquipoService] Unexpected error on delete");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al eliminar el equipo. Por favor, contacte al administrador.", ex);
            }
        }

        private void ValidarEquipo(EquipoDto equipo)
        {
            if (equipo == null)
                throw new GestionMantenimientosDomainException("El equipo no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(equipo.Codigo))
                throw new GestionMantenimientosDomainException("El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(equipo.Nombre))
                throw new GestionMantenimientosDomainException("El nombre es obligatorio.");
            if (string.IsNullOrWhiteSpace(equipo.Marca))
                throw new GestionMantenimientosDomainException("La marca es obligatoria.");
            if (equipo.Sede == null)
                throw new GestionMantenimientosDomainException("La sede es obligatoria.");
            if (equipo.Precio != null && equipo.Precio < 0)
                throw new GestionMantenimientosDomainException("El precio no puede ser negativo.");
            if (equipo.FrecuenciaMtto != null && equipo.FrecuenciaMtto <= 0)
                throw new GestionMantenimientosDomainException("La frecuencia de mantenimiento debe ser mayor a cero.");
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
        }

        // Calcula la semana ISO 8601 para una fecha dada
        private int CalcularSemanaISO8601(DateTime fecha)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(fecha, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
