using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Interfaces.Import;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Import;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Interfaces.Data;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.Exceptions;
using GestLog.Modules.DatabaseConnection;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages.Mantenimientos;
using System.Globalization;

namespace GestLog.Modules.GestionMantenimientos.Services.Import
{
    public class SeguimientoImportService : ISeguimientoImportService
    {
        private readonly IGestLogLogger _logger;
        private readonly IDbContextFactory<GestLogDbContext> _dbContextFactory;
        private readonly ICronogramaService _cronogramaService;

        public SeguimientoImportService(IGestLogLogger logger, IDbContextFactory<GestLogDbContext> dbContextFactory, ICronogramaService cronogramaService)
        {
            _logger = logger;
            _dbContextFactory = dbContextFactory;
            _cronogramaService = cronogramaService;
        }

        public async Task<SeguimientoImportResult> ImportAsync(string filePath, CancellationToken cancellationToken = default, IProgress<int>? progress = null)
        {
            var result = new SeguimientoImportResult();
            _logger.LogInformation("[SeguimientoImportService] Inicio importación: {FilePath}", filePath);

            if (string.IsNullOrWhiteSpace(filePath))
                throw new GestionMantenimientosDomainException("Ruta de archivo inválida.");

            if (!File.Exists(filePath))
                throw new GestionMantenimientosDomainException($"El archivo Excel seleccionado no existe: {filePath}");

            if (!Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new GestionMantenimientosDomainException("El archivo debe ser un Excel (.xlsx)");

            try
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();

                var columnMap = MapearColumnasExcel(worksheet);
                var codigosValidos = await ObtenerCodigosEquiposValidosAsync();

                var rangeUsed = worksheet.RangeUsed();
                int lastRow = rangeUsed?.LastRow().RowNumber() ?? 1;

                using var dbContext = _dbContextFactory.CreateDbContext();

                for (int row = 2; row <= lastRow; row++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        throw new OperationCanceledException(cancellationToken);

                    try
                    {
                        var codigo = worksheet.Cell(row, columnMap["Codigo"]).GetString()?.Trim() ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(codigo))
                        {
                            // Reportar progreso aunque se salte la fila
                            progress?.Report(CalcProgress(row, lastRow));
                            continue; // fila vacía
                        }

                        if (!codigosValidos.Contains(codigo))
                        {
                            var razon = $"Equipo con código '{codigo}' no existe";
                            result.IgnoredRows.Add((row, razon));
                            result.IgnoredCount++;
                            _logger.LogWarning("[SeguimientoImportService] Fila {Row} - {Reason}", row, razon);
                            progress?.Report(CalcProgress(row, lastRow));
                            continue;
                        }

                        var tipoMtnoStr = worksheet.Cell(row, columnMap["TipoMtno"]).GetString()?.Trim() ?? string.Empty;
                        if (!Enum.TryParse<TipoMantenimiento>(tipoMtnoStr, true, out var tipoMtno) || (tipoMtno != TipoMantenimiento.Preventivo && tipoMtno != TipoMantenimiento.Correctivo))
                        {
                            var razon = $"Tipo de mantenimiento '{tipoMtnoStr}' no es válido";
                            result.IgnoredRows.Add((row, razon));
                            result.IgnoredCount++;
                            _logger.LogWarning("[SeguimientoImportService] Fila {Row} - {Reason}", row, razon);
                            progress?.Report(CalcProgress(row, lastRow));
                            continue;
                        }

                        if (!worksheet.Cell(row, columnMap["FechaRealizacion"]).TryGetValue(out DateTime fechaRealizacion))
                        {
                            var fechaStr = worksheet.Cell(row, columnMap["FechaRealizacion"]).GetString()?.Trim() ?? string.Empty;
                            var razon = "Fecha Realizacion no es una fecha válida";
                            result.IgnoredRows.Add((row, razon));
                            result.IgnoredCount++;
                            _logger.LogWarning("[SeguimientoImportService] Fila {Row} - FechaRealizacion='{FechaStr}' - {Reason}", row, fechaStr, razon);
                            progress?.Report(CalcProgress(row, lastRow));
                            continue;
                        }

                        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                        int semana = cal.GetWeekOfYear(fechaRealizacion, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                        int anio = fechaRealizacion.Year;

                        var dto = new SeguimientoMantenimientoDto
                        {
                            Codigo = codigo,
                            Nombre = worksheet.Cell(row, columnMap["Nombre"]).GetString()?.Trim() ?? string.Empty,
                            TipoMtno = tipoMtno,
                            Descripcion = worksheet.Cell(row, columnMap["Descripcion"]).GetString()?.Trim() ?? string.Empty,
                            Responsable = worksheet.Cell(row, columnMap["Responsable"]).GetString()?.Trim() ?? string.Empty,
                            Costo = worksheet.Cell(row, columnMap["Costo"]).GetValue<decimal?>() ?? 0m,
                            Observaciones = worksheet.Cell(row, columnMap["Observaciones"]).GetString()?.Trim() ?? string.Empty,
                            FechaRegistro = fechaRealizacion,
                            FechaRealizacion = fechaRealizacion,
                            Semana = semana,
                            Anio = anio,
                            Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo
                        };

                        ValidarDto(dto);
                        result.ImportedItems.Add(dto);
                        progress?.Report(CalcProgress(row, lastRow));
                    }
                    catch (GestionMantenimientosDomainException ex)
                    {
                        result.IgnoredRows.Add((row, ex.Message));
                        result.IgnoredCount++;
                        _logger.LogWarning("[SeguimientoImportService] Fila {Row} validacion: {Message}", row, ex.Message);
                        progress?.Report(CalcProgress(row, lastRow));
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("[SeguimientoImportService] Importación cancelada por token");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.IgnoredRows.Add((row, ex.Message));
                        result.IgnoredCount++;
                        _logger.LogWarning(ex, "[SeguimientoImportService] Error inesperado en fila {Row}", row);
                        progress?.Report(CalcProgress(row, lastRow));
                    }
                }

                // Persistir importados en transacción
                if (result.ImportedItems.Any())
                {
                    // Usar la estrategia de ejecución para que la transacción sea reintentable
                    var strategy = _dbContextFactory.CreateDbContext().Database.CreateExecutionStrategy();

                    await strategy.ExecuteAsync(async () =>
                    {
                        using var dbContextTx = _dbContextFactory.CreateDbContext();
                        using var transaction = await dbContextTx.Database.BeginTransactionAsync(cancellationToken);
                        try
                        {
                            int updated = 0;
                            int added = 0;
                            foreach (var seg in result.ImportedItems)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                var existente = await dbContextTx.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == seg.Codigo && s.Semana == seg.Semana && s.Anio == seg.Anio && s.TipoMtno == seg.TipoMtno, cancellationToken);
                                if (existente == null)
                                {
                                    var nuevo = new GestLog.Modules.GestionMantenimientos.Models.Entities.SeguimientoMantenimiento
                                    {
                                        Codigo = seg.Codigo ?? string.Empty,
                                        Nombre = seg.Nombre ?? string.Empty,
                                        TipoMtno = seg.TipoMtno ?? TipoMantenimiento.Preventivo,
                                        Descripcion = seg.Descripcion ?? string.Empty,
                                        Responsable = seg.Responsable ?? string.Empty,
                                        Costo = seg.Costo ?? 0m,
                                        Observaciones = seg.Observaciones ?? string.Empty,
                                        FechaRegistro = seg.FechaRegistro ?? DateTime.Now,
                                        FechaRealizacion = seg.FechaRealizacion,
                                        Semana = seg.Semana,
                                        Anio = seg.Anio,
                                        Estado = seg.Estado,
                                        Frecuencia = seg.Frecuencia
                                    };
                                    dbContextTx.Seguimientos.Add(nuevo);
                                    added++;
                                }
                                else if (existente.TipoMtno == TipoMantenimiento.Preventivo && seg.TipoMtno == TipoMantenimiento.Preventivo)
                                {
                                    existente.Nombre = seg.Nombre ?? existente.Nombre;
                                    existente.Descripcion = seg.Descripcion ?? existente.Descripcion;
                                    existente.Responsable = seg.Responsable ?? existente.Responsable;
                                    existente.Costo = seg.Costo ?? existente.Costo;
                                    existente.Observaciones = seg.Observaciones ?? existente.Observaciones;
                                    existente.FechaRegistro = seg.FechaRegistro ?? existente.FechaRegistro;
                                    existente.FechaRealizacion = seg.FechaRealizacion ?? existente.FechaRealizacion;
                                    existente.Estado = seg.Estado;
                                    dbContextTx.Seguimientos.Update(existente);
                                    updated++;
                                }
                                else
                                {
                                    // Correctivos no se actualizan
                                    _logger.LogWarning("[SeguimientoImportService] Ignorado correctivo existente: {Codigo} Semana {Semana}", seg.Codigo ?? string.Empty, seg.Semana);
                                    result.IgnoredRows.Add((-1, $"Correctivo existente ignorado: {seg.Codigo} Semana {seg.Semana}"));
                                    result.IgnoredCount++;
                                }
                            }

                            await dbContextTx.SaveChangesAsync(cancellationToken);
                            await transaction.CommitAsync(cancellationToken);

                            result.ImportedCount = added;
                            result.UpdatedCount = updated;

                            _logger.LogInformation("[SeguimientoImportService] Persistencia completada: Added={Added} Updated={Updated}", added, updated);
                        }
                        catch (OperationCanceledException)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogInformation("[SeguimientoImportService] Persistencia cancelada por token");
                            throw;
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync(cancellationToken);
                            _logger.LogError(ex, "[SeguimientoImportService] Error al persistir importación");
                            throw new GestionMantenimientosDomainException("Error al persistir importación", ex);
                        }
                    });
                }

                // Crear cronogramas
                cancellationToken.ThrowIfCancellationRequested();
                await CrearCronogramasDesdeSeguidmientosAsync();

                // Enviar mensaje
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());

                _logger.LogInformation("[SeguimientoImportService] Importación finalizada: Imported={Imported} Updated={Updated} Ignored={Ignored}", result.ImportedCount, result.UpdatedCount, result.IgnoredCount);
                return result;
            }
            catch (GestionMantenimientosDomainException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[SeguimientoImportService] Importación cancelada (catch externo)");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoImportService] Error al importar desde Excel");
                throw new GestionMantenimientosDomainException("Error al importar seguimientos desde Excel", ex);
            }
        }

        private int CalcProgress(int currentRow, int lastRow)
        {
            if (lastRow <= 2) return 100;
            int processed = Math.Max(0, currentRow - 1);
            int total = Math.Max(1, lastRow - 1);
            return (int)Math.Round((processed / (double)total) * 100);
        }

        private void ValidarDto(SeguimientoMantenimientoDto dto)
        {
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, context, results, true);
            if (!isValid)
            {
                var mensaje = string.Join("; ", results.Select(r => r.ErrorMessage));
                throw new GestionMantenimientosDomainException(mensaje);
            }

            if (dto.Costo != null && dto.Costo < 0)
                throw new GestionMantenimientosDomainException("El costo no puede ser negativo.");
        }

        private async Task<HashSet<string>> ObtenerCodigosEquiposValidosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var codigos = await dbContext.Equipos.Where(e => !string.IsNullOrWhiteSpace(e.Codigo)).Select(e => e.Codigo).ToListAsync();
            return new HashSet<string>(codigos, StringComparer.OrdinalIgnoreCase);
        }

        private Dictionary<string,int> MapearColumnasExcel(IXLWorksheet worksheet)
        {
            var columnMap = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            var columnasRequeridas = new[] { "Codigo","Nombre","TipoMtno","Descripcion","Responsable","Costo","Observaciones","FechaRealizacion" };
            Func<string,string> Normalizar = s => string.IsNullOrWhiteSpace(s) ? string.Empty : System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), "[^a-z0-9]", "");

            var rangeUsed = worksheet.RangeUsed();
            if (rangeUsed == null)
                throw new GestionMantenimientosDomainException("El archivo Excel está vacío.");

            for (int col = 1; col <= rangeUsed.ColumnCount(); col++)
            {
                var header = worksheet.Cell(1,col).GetString()?.Trim() ?? string.Empty;
                var headerNorm = Normalizar(header);
                foreach (var req in columnasRequeridas)
                {
                    if (headerNorm == Normalizar(req))
                        columnMap[req] = col;
                }
            }

            foreach (var req in columnasRequeridas)
            {
                if (!columnMap.ContainsKey(req))
                    throw new GestionMantenimientosDomainException($"No se encontró la columna '{req}' en el Excel. Columnas requeridas: {string.Join(",",columnasRequeridas)}");
            }

            _logger.LogInformation("[SeguimientoImportService] ColumnMap: {Map}", string.Join(",", columnMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));
            return columnMap;
        }

        private async Task CrearCronogramasDesdeSeguidmientosAsync()
        {
            // Reusar lógica existente en el servicio de seguimientos si existe
            using var dbContext = _dbContextFactory.CreateDbContext();
            var seguimientos = await dbContext.Seguimientos.Where(s => s.Estado != EstadoSeguimientoMantenimiento.Pendiente).ToListAsync();

            if (!seguimientos.Any()) return;

            var grupos = seguimientos.GroupBy(s => new { s.Codigo, s.Anio }).ToList();
            foreach (var grupo in grupos)
            {
                try
                {
                    var codigo = grupo.Key.Codigo;
                    var anio = grupo.Key.Anio;
                    var equipo = await dbContext.Equipos.FirstOrDefaultAsync(e => e.Codigo == codigo);
                    string nombreEquipo = equipo?.Nombre ?? codigo;
                    var semanasConMantenimiento = grupo.Select(s => s.Semana).Distinct().OrderBy(s => s).ToList();
                    var calendar = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                    int weeksInYear = calendar.GetWeekOfYear(new DateTime(anio, 12, 28), System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                    bool[] semanas = new bool[weeksInYear];
                    foreach (var semanaNum in semanasConMantenimiento)
                    {
                        if (semanaNum >=1 && semanaNum <= weeksInYear)
                            semanas[semanaNum-1] = true;
                    }

                    var cronogramaExistente = await dbContext.Cronogramas.FirstOrDefaultAsync(c => c.Codigo == codigo && c.Anio == anio);
                    if (cronogramaExistente == null)
                    {
                        var nuevo = new GestLog.Modules.GestionMantenimientos.Models.Entities.CronogramaMantenimiento
                        {
                            Codigo = codigo,
                            Nombre = nombreEquipo,
                            Anio = anio,
                            Semanas = semanas
                        };
                        dbContext.Cronogramas.Add(nuevo);
                    }
                    else
                    {
                        for (int i=0;i<semanas.Length && i<cronogramaExistente.Semanas.Length;i++)
                        {
                            if (semanas[i]) cronogramaExistente.Semanas[i] = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SeguimientoImportService] Error creando cronograma para grupo {Codigo} {Anio}", grupo.Key.Codigo, grupo.Key.Anio);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
