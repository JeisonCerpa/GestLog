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

        public async Task AddAsync(EquipoDto equipo)
        {
            try
            {
                ValidarEquipo(equipo);
                using var dbContext = _dbContextFactory.CreateDbContext();
                if (await dbContext.Equipos.AnyAsync(e => e.Codigo == equipo.Codigo))
                    throw new GestionMantenimientosDomainException($"Ya existe un equipo con el código '{equipo.Codigo}'.");
                // Forzar la fecha de registro a la fecha actual
                var fechaRegistro = DateTime.Now;
                int semanaInicio = CalcularSemanaISO8601(fechaRegistro);
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
                    FechaCompra = equipo.FechaCompra
                    // SemanaInicioMtto eliminado
                };
                dbContext.Equipos.Add(entity);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[EquipoService] Equipo agregado correctamente: {Codigo}", equipo.Codigo ?? "");

                // Generar cronogramas desde el año de registro hasta el año actual
                if (equipo.FrecuenciaMtto != null)
                {
                    var anioRegistro = fechaRegistro.Year;
                    var anioActual = DateTime.Now.Year;
                    var mesActual = DateTime.Now.Month;
                    int anioLimite = anioActual;
                    if (mesActual >= 10) // Octubre o más, también crear el del siguiente año
                        anioLimite = anioActual + 1;
                    using var dbContext2 = _dbContextFactory.CreateDbContext();                    for (int anio = anioRegistro; anio <= anioLimite; anio++)
                    {
                        // No duplicar cronogramas si ya existen
                        bool existe = await dbContext2.Cronogramas.AnyAsync(c => c.Codigo == equipo.Codigo && c.Anio == anio);
                        if (existe) continue;
                        // Calcular semana de inicio para el primer año
                        int semanaIni = 1;
                        if (anio == anioRegistro && equipo.FrecuenciaMtto != null)
                        {
                            // La semana de inicio debe ser: semana_registro + salto
                            int salto = equipo.FrecuenciaMtto switch
                            {
                                Models.Enums.FrecuenciaMantenimiento.Semanal => 1,
                                Models.Enums.FrecuenciaMantenimiento.Quincenal => 2,
                                Models.Enums.FrecuenciaMantenimiento.Mensual => 4,
                                Models.Enums.FrecuenciaMantenimiento.Bimestral => 8,
                                Models.Enums.FrecuenciaMantenimiento.Trimestral => 13,
                                Models.Enums.FrecuenciaMantenimiento.Semestral => 26,
                                Models.Enums.FrecuenciaMantenimiento.Anual => System.Globalization.ISOWeek.GetWeeksInYear(anio),
                                _ => 1
                            };
                            int proximaSemana = semanaInicio + salto;
                            int yearsWeeks = System.Globalization.ISOWeek.GetWeeksInYear(anio);
                            if (proximaSemana > yearsWeeks)
                            {
                                proximaSemana = ((proximaSemana - 1) % yearsWeeks) + 1;
                            }
                            semanaIni = proximaSemana;                            _logger.LogInformation($"[EquipoService] Cronograma creado - Equipo={equipo.Codigo}, FechaRegistro={fechaRegistro:yyyy-MM-dd}, SemanaRegistro={semanaInicio}, Salto={salto}, SemanaInicio={semanaIni}, TotalSemanas={yearsWeeks}, Año={anio}");
                        }
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
                        dbContext2.Cronogramas.Add(cronograma);
                    }
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
            // Nombre, Marca y Sede ya no son obligatorios por requerimiento
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
        }

        // Calcula la semana ISO 8601 para una fecha dada
        private int CalcularSemanaISO8601(DateTime fecha)
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(fecha, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
