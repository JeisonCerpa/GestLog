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

namespace GestLog.Modules.GestionMantenimientos.Services
{
    public class CronogramaService : ICronogramaService
    {
        private readonly IGestLogLogger _logger;
        private readonly GestLogDbContext _dbContext;
        public CronogramaService(IGestLogLogger logger, GestLogDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<CronogramaMantenimientoDto>> GetAllAsync()
        {
            var cronos = await _dbContext.Cronogramas.ToListAsync();
            return cronos.Select(c => new CronogramaMantenimientoDto
            {
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Marca = c.Marca,
                Sede = c.Sede,
                SemanaInicioMtto = c.SemanaInicioMtto,
                FrecuenciaMtto = c.FrecuenciaMtto,
                Semanas = c.Semanas.ToArray()
            });
        }

        public async Task<CronogramaMantenimientoDto?> GetByCodigoAsync(string codigo)
        {
            var entity = await _dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == codigo);
            if (entity == null) return null;
            return new CronogramaMantenimientoDto
            {
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Marca = entity.Marca,
                Sede = entity.Sede,
                SemanaInicioMtto = entity.SemanaInicioMtto,
                FrecuenciaMtto = entity.FrecuenciaMtto,
                Semanas = entity.Semanas.ToArray()
            };
        }

        public async Task AddAsync(CronogramaMantenimientoDto cronograma)
        {
            try
            {
                ValidarCronograma(cronograma);
                var entity = new CronogramaMantenimiento
                {
                    Codigo = cronograma.Codigo!,
                    Nombre = cronograma.Nombre!,
                    Marca = cronograma.Marca,
                    Sede = cronograma.Sede,
                    SemanaInicioMtto = cronograma.SemanaInicioMtto,
                    FrecuenciaMtto = cronograma.FrecuenciaMtto,
                    Semanas = cronograma.Semanas.ToArray()
                };
                _dbContext.Cronogramas.Add(entity);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[CronogramaService] Cronograma agregado correctamente: {Codigo}", cronograma?.Codigo ?? "");
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
                var entity = await _dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == cronograma.Codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el cronograma a actualizar.");
                entity.Nombre = cronograma.Nombre!;
                entity.Marca = cronograma.Marca;
                entity.Sede = cronograma.Sede;
                entity.SemanaInicioMtto = cronograma.SemanaInicioMtto;
                entity.FrecuenciaMtto = cronograma.FrecuenciaMtto;
                entity.Semanas = cronograma.Semanas.ToArray();
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("[CronogramaService] Cronograma actualizado correctamente: {Codigo}", cronograma?.Codigo ?? "");
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
                var entity = await _dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el cronograma a eliminar.");
                _dbContext.Cronogramas.Remove(entity);
                await _dbContext.SaveChangesAsync();
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
            if (cronograma.SemanaInicioMtto != null && (cronograma.SemanaInicioMtto < 1 || cronograma.SemanaInicioMtto > 52))
                throw new GestionMantenimientosDomainException("La semana de inicio debe estar entre 1 y 52.");
            if (cronograma.FrecuenciaMtto != null && cronograma.FrecuenciaMtto <= 0)
                throw new GestionMantenimientosDomainException("La frecuencia de mantenimiento debe ser mayor a cero.");
            if (cronograma.Semanas == null || cronograma.Semanas.Length != 52)
                throw new GestionMantenimientosDomainException("El cronograma debe tener 52 semanas definidas.");
            // TODO: Validar duplicados y otras reglas de negocio si aplica
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
                    var headers = new[] { "Codigo", "Nombre", "Marca", "Sede", "SemanaInicioMtto", "FrecuenciaMtto" };
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
                            var dto = new CronogramaMantenimientoDto
                            {
                                Codigo = worksheet.Cell(row, 1).GetString(),
                                Nombre = worksheet.Cell(row, 2).GetString(),
                                Marca = worksheet.Cell(row, 3).GetString(),
                                Sede = worksheet.Cell(row, 4).GetString(),
                                SemanaInicioMtto = worksheet.Cell(row, 5).GetValue<int?>(),
                                FrecuenciaMtto = worksheet.Cell(row, 6).GetValue<int?>(),
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
                var headers = new[] { "Codigo", "Nombre", "Marca", "Sede", "SemanaInicioMtto", "FrecuenciaMtto" };
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
                    worksheet.Cell(row, 5).Value = c.SemanaInicioMtto;
                    worksheet.Cell(row, 6).Value = c.FrecuenciaMtto;
                    for (int s = 0; s < 52; s++)
                    {
                        worksheet.Cell(row, 7 + s).Value = c.Semanas != null && c.Semanas.Length > s && c.Semanas[s] ? "✔" : "";
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
            var cronos = await _dbContext.Cronogramas.ToListAsync();
            return cronos.Select(c => new CronogramaMantenimientoDto
            {
                Codigo = c.Codigo,
                Nombre = c.Nombre,
                Marca = c.Marca,
                Sede = c.Sede,
                SemanaInicioMtto = c.SemanaInicioMtto,
                FrecuenciaMtto = c.FrecuenciaMtto,
                Semanas = c.Semanas.ToArray()
            }).ToList();
        }
    }
}
