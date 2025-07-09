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
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestLog.Modules.GestionMantenimientos.Services
{
    public class SeguimientoService : ISeguimientoService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        public SeguimientoService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<IEnumerable<SeguimientoMantenimientoDto>> GetAllAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var seguimientos = await dbContext.Seguimientos.ToListAsync();
            return seguimientos.Select(s => new SeguimientoMantenimientoDto
            {
                Codigo = s.Codigo,
                Nombre = s.Nombre,
                Fecha = s.Fecha,
                TipoMtno = s.TipoMtno,
                Descripcion = s.Descripcion,
                Responsable = s.Responsable,
                Costo = s.Costo,
                Observaciones = s.Observaciones,
                FechaRegistro = s.FechaRegistro
            });
        }

        public async Task<SeguimientoMantenimientoDto?> GetByCodigoAsync(string codigo)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == codigo);
            if (entity == null) return null;
            return new SeguimientoMantenimientoDto
            {
                Codigo = entity.Codigo,
                Nombre = entity.Nombre,
                Fecha = entity.Fecha,
                TipoMtno = entity.TipoMtno,
                Descripcion = entity.Descripcion,
                Responsable = entity.Responsable,
                Costo = entity.Costo,
                Observaciones = entity.Observaciones,
                FechaRegistro = entity.FechaRegistro
            };
        }

        public async Task AddAsync(SeguimientoMantenimientoDto seguimiento)
        {
            try
            {
                ValidarSeguimiento(seguimiento);
                using var dbContext = _dbContextFactory.CreateDbContext();
                if (await dbContext.Seguimientos.AnyAsync(s => s.Codigo == seguimiento.Codigo))
                    throw new GestionMantenimientosDomainException($"Ya existe un seguimiento con el código '{seguimiento.Codigo}'.");               
                var entity = new SeguimientoMantenimiento
                {
                    Codigo = seguimiento.Codigo!,
                    Nombre = seguimiento.Nombre!,
                    Fecha = seguimiento.Fecha,
                    TipoMtno = seguimiento.TipoMtno!.Value,
                    Descripcion = seguimiento.Descripcion,
                    Responsable = seguimiento.Responsable,
                    Costo = seguimiento.Costo,
                    Observaciones = seguimiento.Observaciones,
                    FechaRegistro = seguimiento.FechaRegistro
                };
                dbContext.Seguimientos.Add(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimiento agregado correctamente: {Codigo}", seguimiento?.Codigo ?? "");
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on add");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on add");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al agregar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task UpdateAsync(SeguimientoMantenimientoDto seguimiento)
        {
            try
            {
                ValidarSeguimiento(seguimiento);
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == seguimiento.Codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el seguimiento a actualizar.");
                // No permitir cambiar el código
                // entity.Codigo = seguimiento.Codigo; // NO modificar
                entity.Nombre = seguimiento.Nombre!;
                entity.Fecha = seguimiento.Fecha;
                entity.TipoMtno = seguimiento.TipoMtno!.Value;
                entity.Descripcion = seguimiento.Descripcion;
                entity.Responsable = seguimiento.Responsable;
                entity.Costo = seguimiento.Costo;
                entity.Observaciones = seguimiento.Observaciones;
                entity.FechaRegistro = seguimiento.FechaRegistro;
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimiento actualizado correctamente: {Codigo}", seguimiento?.Codigo ?? "");
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on update");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on update");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al actualizar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task DeleteAsync(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    throw new GestionMantenimientosDomainException("El código del seguimiento es obligatorio para eliminar.");
                using var dbContext = _dbContextFactory.CreateDbContext();
                var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == codigo);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el seguimiento a eliminar.");
                dbContext.Seguimientos.Remove(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimiento eliminado correctamente: {Codigo}", codigo ?? "");
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogWarning(ex, "[SeguimientoService] Validation error on delete");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Unexpected error on delete");
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al eliminar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        private void ValidarSeguimiento(SeguimientoMantenimientoDto seguimiento)
        {
            if (seguimiento == null)
                throw new GestionMantenimientosDomainException("El seguimiento no puede ser nulo.");
            if (string.IsNullOrWhiteSpace(seguimiento.Codigo))
                throw new GestionMantenimientosDomainException("El código es obligatorio.");
            if (string.IsNullOrWhiteSpace(seguimiento.Nombre))
                throw new GestionMantenimientosDomainException("El nombre es obligatorio.");
            if (seguimiento.Fecha == null)
                throw new GestionMantenimientosDomainException("La fecha es obligatoria.");
            if (seguimiento.Fecha > DateTime.Now)
                throw new GestionMantenimientosDomainException("La fecha no puede ser futura.");
            if (seguimiento.TipoMtno == null)
                throw new GestionMantenimientosDomainException("El tipo de mantenimiento es obligatorio.");
            if (string.IsNullOrWhiteSpace(seguimiento.Descripcion))
                throw new GestionMantenimientosDomainException("La descripción es obligatoria.");
            if (string.IsNullOrWhiteSpace(seguimiento.Responsable))
                throw new GestionMantenimientosDomainException("El responsable es obligatorio.");
            if (seguimiento.Costo != null && seguimiento.Costo < 0)
                throw new GestionMantenimientosDomainException("El costo no puede ser negativo.");
            // TODO: Validar duplicados y otras reglas de negocio si aplica
        }

        public async Task ImportarDesdeExcelAsync(string filePath)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("[SeguimientoService] Starting import from Excel: {FilePath}", filePath);
                try
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"El archivo no existe: {filePath}");

                    using var workbook = new XLWorkbook(filePath);
                    var worksheet = workbook.Worksheets.First();
                    var headers = new[] { "Codigo", "Nombre", "Fecha", "TipoMtno", "Descripcion", "Responsable", "Costo", "Observaciones", "FechaRegistro" };
                    // Validar encabezados
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var cellValue = worksheet.Cell(1, i + 1).GetString();
                        if (!string.Equals(cellValue, headers[i], StringComparison.OrdinalIgnoreCase))
                            throw new GestionMantenimientosDomainException($"Columna esperada '{headers[i]}' no encontrada en la posición {i + 1}.");
                    }
                    var seguimientos = new List<SeguimientoMantenimientoDto>();
                    int row = 2;
                    while (!worksheet.Cell(row, 1).IsEmpty())
                    {
                        try
                        {
                            var dto = new SeguimientoMantenimientoDto
                            {
                                Codigo = worksheet.Cell(row, 1).GetString(),
                                Nombre = worksheet.Cell(row, 2).GetString(),
                                Fecha = worksheet.Cell(row, 3).GetDateTime(),
                                TipoMtno = Enum.TryParse<TipoMantenimiento>(worksheet.Cell(row, 4).GetString(), out var tipo) ? tipo : (TipoMantenimiento?)null,
                                Descripcion = worksheet.Cell(row, 5).GetString(),
                                Responsable = worksheet.Cell(row, 6).GetString(),
                                Costo = worksheet.Cell(row, 7).GetValue<decimal?>(),
                                Observaciones = worksheet.Cell(row, 8).GetString(),
                                FechaRegistro = worksheet.Cell(row, 9).GetDateTime()
                            };
                            ValidarSeguimiento(dto);
                            seguimientos.Add(dto);
                        }
                        catch (GestionMantenimientosDomainException ex)
                        {
                            _logger.LogWarning(ex, $"[SeguimientoService] Validation error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error de validación en la fila {row}: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"[SeguimientoService] Unexpected error on import at row {row}");
                            throw new GestionMantenimientosDomainException($"Error inesperado en la fila {row}: {ex.Message}", ex);
                        }
                        row++;
                    }
                    // Aquí deberías guardar los seguimientos importados en la base de datos o colección interna
                    _logger.LogInformation("[SeguimientoService] Seguimientos importados: {Count}", seguimientos.Count);
                }
                catch (GestionMantenimientosDomainException ex)
                {
                    _logger.LogWarning(ex, "[SeguimientoService] Validation error on import");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SeguimientoService] Error importing from Excel");
                    throw new GestionMantenimientosDomainException($"Error al importar desde Excel: {ex.Message}", ex);
                }
            });
        }

        public async Task ExportarAExcelAsync(string filePath)
        {
            _logger.LogInformation("[SeguimientoService] Starting export to Excel: {FilePath}", filePath);
            try
            {
                var seguimientos = (await GetAllAsync()).ToList();
                if (!seguimientos.Any())
                {
                    _logger.LogWarning("[SeguimientoService] No data to export.");
                    throw new InvalidOperationException("No hay datos de seguimientos para exportar.");
                }

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Seguimientos");

                // Encabezados
                var headers = new[] { "Codigo", "Nombre", "Fecha", "TipoMtno", "Descripcion", "Responsable", "Costo", "Observaciones", "FechaRegistro" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = headers[i];
                    worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                }

                // Datos
                int row = 2;
                foreach (var s in seguimientos)
                {
                    worksheet.Cell(row, 1).Value = s.Codigo;
                    worksheet.Cell(row, 2).Value = s.Nombre;
                    worksheet.Cell(row, 3).Value = s.Fecha;
                    worksheet.Cell(row, 4).Value = s.TipoMtno?.ToString();
                    worksheet.Cell(row, 5).Value = s.Descripcion;
                    worksheet.Cell(row, 6).Value = s.Responsable;
                    worksheet.Cell(row, 7).Value = s.Costo;
                    worksheet.Cell(row, 8).Value = s.Observaciones;
                    worksheet.Cell(row, 9).Value = s.FechaRegistro;
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
                _logger.LogInformation("[SeguimientoService] Export completed: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error exporting to Excel");
                throw new Exception($"Error al exportar a Excel: {ex.Message}", ex);
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
    }
}
