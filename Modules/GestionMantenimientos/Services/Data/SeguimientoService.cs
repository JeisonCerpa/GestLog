using System.Collections.Generic;
using System.Threading.Tasks;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Interfaces;
using System;
using GestLog.Services.Core.Logging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Globalization;
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.DatabaseConnection;
using GestLog.Modules.GestionMantenimientos.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Modules.GestionMantenimientos.Messages;

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
        }

        public async Task<IEnumerable<SeguimientoMantenimientoDto>> GetAllAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var seguimientos = await dbContext.Seguimientos.ToListAsync();
            return seguimientos.Select(s => new SeguimientoMantenimientoDto
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
                Estado = s.Estado,
                Frecuencia = s.Frecuencia
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
                ValidarSeguimiento(seguimiento);
                var hoy = DateTime.Now;
                var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                int semanaActual = cal.GetWeekOfYear(hoy, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                int anioActual = hoy.Year;

                if (!(seguimiento.Anio < anioActual || (seguimiento.Anio == anioActual && seguimiento.Semana <= semanaActual)))
                    throw new GestionMantenimientosDomainException("Solo se permite registrar mantenimientos en semanas anteriores o la actual.");

                using var dbContext = _dbContextFactory.CreateDbContext();

                // NUEVA LÓGICA: Buscar por (Codigo, Semana, Anio, TipoMtno)
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
                    _logger.LogInformation("[SeguimientoService] Seguimiento actualizado: {Codigo} Tipo {Tipo} Semana {Semana} Año {Anio}",
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
                    _logger.LogInformation("[SeguimientoService] Seguimiento agregado: {Codigo} Tipo {Tipo} Semana {Semana} Año {Anio}",
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
                throw new GestionMantenimientosDomainException("Ocurrió un error inesperado al agregar el seguimiento. Por favor, contacte al administrador.", ex);
            }
        }

        public async Task UpdateAsync(SeguimientoMantenimientoDto seguimiento)
        {
            try
            {
                ValidarSeguimiento(seguimiento);
                using var dbContext = _dbContextFactory.CreateDbContext();
                // Buscar por clave compuesta: Codigo, Semana, Anio
                var entity = await dbContext.Seguimientos.FirstOrDefaultAsync(s => s.Codigo == seguimiento.Codigo && s.Semana == seguimiento.Semana && s.Anio == seguimiento.Anio);
                if (entity == null)
                    throw new GestionMantenimientosDomainException("No se encontró el seguimiento a actualizar.");
                // No permitir cambiar el código
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
                _logger.LogInformation("[SeguimientoService] Seguimiento actualizado correctamente: {Codigo} Semana {Semana} Año {Anio}", seguimiento.Codigo ?? "", seguimiento.Semana, seguimiento.Anio);
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
                var seguimientos = await dbContext.Seguimientos.Where(s => s.Codigo == codigo).ToListAsync();
                if (seguimientos.Count == 0)
                    throw new GestionMantenimientosDomainException("No se encontraron seguimientos con ese código.");

                dbContext.Seguimientos.RemoveRange(seguimientos);
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("[SeguimientoService] Seguimientos eliminados para código: {Codigo}", codigo);
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

        public async Task ImportarDesdeExcelAsync(string filePath)
        {
            _logger.LogInformation("[SeguimientoService] Iniciando importación de seguimientos antiguos: {FilePath}", filePath);
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"El archivo no existe: {filePath}");

                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();

                // Mapeo flexible de encabezados: esperado -> posición de columna
                var columnMap = MapearColumnasExcel(worksheet);

                // Obtener lista de códigos válidos de equipos
                var codigosValidos = await ObtenerCodigosEquiposValidosAsync();

                var seguimientosImportados = new List<SeguimientoMantenimientoDto>();
                var seguimientosIgnorados = new List<(int fila, string razon)>();

                // Obtener el rango usado del worksheet para saber cuántas filas hay
                var rangeUsed = worksheet.RangeUsed();
                int lastRow = rangeUsed?.LastRow().RowNumber() ?? 1;

                _logger.LogInformation("[SeguimientoService] Excel detectado: Última fila con datos = {LastRow}", lastRow);

                using var dbContext = _dbContextFactory.CreateDbContext();

                // Iterar desde la fila 2 hasta la última fila usada del archivo (NO solo hasta encontrar una vacía)
                for (int row = 2; row <= lastRow; row++)
                {
                    try
                    {
                        string codigo = worksheet.Cell(row, columnMap["Codigo"]).GetString()?.Trim() ?? "";

                        // Saltar filas completamente vacías
                        if (string.IsNullOrWhiteSpace(codigo))
                        {
                            continue;
                        }

                        string nombre = worksheet.Cell(row, columnMap["Nombre"]).GetString()?.Trim() ?? "";
                        string tipoMtnoStr = worksheet.Cell(row, columnMap["TipoMtno"]).GetString()?.Trim() ?? "";

                        if (!codigosValidos.Contains(codigo))
                        {
                            string razon = $"Equipo con código '{codigo}' no existe";
                            seguimientosIgnorados.Add((row, razon));
                            _logger.LogWarning("[SeguimientoService] Fila {Row} - Campos: Codigo='{Codigo}', Nombre='{Nombre}', TipoMtno='{TipoMtno}' | Rechazo: {Razon}",
                                row, codigo, nombre, tipoMtnoStr, razon);
                            continue;
                        }

                        // Validar tipo de mantenimiento (solo Preventivo o Correctivo)
                        if (!Enum.TryParse<TipoMantenimiento>(tipoMtnoStr, ignoreCase: true, out var tipoMtno) ||
                            (tipoMtno != TipoMantenimiento.Preventivo && tipoMtno != TipoMantenimiento.Correctivo))
                        {
                            string razon = $"Tipo de mantenimiento '{tipoMtnoStr}' no es válido (solo Preventivo o Correctivo)";
                            seguimientosIgnorados.Add((row, razon));
                            _logger.LogWarning("[SeguimientoService] Fila {Row} - Campos: Codigo='{Codigo}', Nombre='{Nombre}', TipoMtno='{TipoMtno}' | Rechazo: {Razon}",
                                row, codigo, nombre, tipoMtnoStr, razon);
                            continue;
                        }

                        // Leer FechaRealizacion para calcular Semana y Anio
                        if (!worksheet.Cell(row, columnMap["FechaRealizacion"]).TryGetValue(out DateTime fechaRealizacion))
                        {
                            string razon = "Fecha Realización no es una fecha válida";
                            string fechaStr = worksheet.Cell(row, columnMap["FechaRealizacion"]).GetString()?.Trim() ?? "(vacía)";
                            seguimientosIgnorados.Add((row, razon));
                            _logger.LogWarning("[SeguimientoService] Fila {Row} - Campos: Codigo='{Codigo}', Nombre='{Nombre}', TipoMtno='{TipoMtno}', FechaRealizacion='{FechaRealizacion}' | Rechazo: {Razon}",
                                row, codigo, nombre, tipoMtnoStr, fechaStr, razon);
                            continue;
                        }

                        // Calcular semana y año a partir de FechaRealizacion
                        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                        int semana = cal.GetWeekOfYear(fechaRealizacion, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
                        int anio = fechaRealizacion.Year;

                        var dto = new SeguimientoMantenimientoDto
                        {
                            Codigo = codigo,
                            Nombre = worksheet.Cell(row, columnMap["Nombre"]).GetString()?.Trim() ?? "",
                            TipoMtno = tipoMtno,
                            Descripcion = worksheet.Cell(row, columnMap["Descripcion"]).GetString()?.Trim() ?? "",
                            Responsable = worksheet.Cell(row, columnMap["Responsable"]).GetString()?.Trim() ?? "",
                            Costo = worksheet.Cell(row, columnMap["Costo"]).GetValue<decimal?>() ?? 0m,
                            Observaciones = worksheet.Cell(row, columnMap["Observaciones"]).GetString()?.Trim() ?? "",
                            FechaRegistro = fechaRealizacion, // Usar la misma fecha de realización
                            FechaRealizacion = fechaRealizacion,
                            Semana = semana,
                            Anio = anio,
                            Estado = EstadoSeguimientoMantenimiento.RealizadoEnTiempo // Todos los importados son realizados en tiempo
                        };

                        ValidarSeguimiento(dto);
                        seguimientosImportados.Add(dto);
                    }
                    catch (GestionMantenimientosDomainException ex)
                    {
                        seguimientosIgnorados.Add((row, ex.Message));
                        _logger.LogWarning("[SeguimientoService] Error de validación en importación - Fila {Row}: {Message}", row, ex.Message);
                    }
                    catch (Exception ex)
                    {
                        seguimientosIgnorados.Add((row, ex.Message));
                        _logger.LogWarning(ex, "[SeguimientoService] Error inesperado en importación - Fila {Row}", row);
                    }
                }                // Guardar todos los seguimientos válidos
                if (seguimientosImportados.Any())
                {
                    var actualizados = 0;
                    var preventivosIgnorados = 0;
                    
                    foreach (var seg in seguimientosImportados)
                    {
                        // Verificar duplicados por (Codigo, Semana, Anio, TipoMtno)
                        var existente = await dbContext.Seguimientos
                            .FirstOrDefaultAsync(s => s.Codigo == seg.Codigo &&
                                                   s.Semana == seg.Semana &&
                                                   s.Anio == seg.Anio &&
                                                   s.TipoMtno == seg.TipoMtno);

                        if (existente == null)
                        {
                            // ✅ NUEVO: Agregar siempre
                            var nuevoSeg = new SeguimientoMantenimiento
                            {
                                Codigo = seg.Codigo ?? "",
                                Nombre = seg.Nombre ?? "",
                                TipoMtno = seg.TipoMtno ?? TipoMantenimiento.Preventivo,
                                Descripcion = seg.Descripcion,
                                Responsable = seg.Responsable,
                                Costo = seg.Costo ?? 0m,
                                Observaciones = seg.Observaciones,
                                FechaRegistro = seg.FechaRegistro ?? DateTime.Now,
                                FechaRealizacion = seg.FechaRealizacion,
                                Semana = seg.Semana,
                                Anio = seg.Anio,
                                Estado = seg.Estado,
                                Frecuencia = seg.Frecuencia
                            };
                            dbContext.Seguimientos.Add(nuevoSeg);
                        }
                        else if (existente.TipoMtno == TipoMantenimiento.Preventivo && seg.TipoMtno == TipoMantenimiento.Preventivo)
                        {
                            // ✏️ ACTUALIZAR: Solo si AMBOS son Preventivos
                            existente.Nombre = seg.Nombre ?? existente.Nombre;
                            existente.Descripcion = seg.Descripcion ?? existente.Descripcion;
                            existente.Responsable = seg.Responsable ?? existente.Responsable;
                            existente.Costo = seg.Costo ?? existente.Costo;
                            existente.Observaciones = seg.Observaciones ?? existente.Observaciones;
                            existente.FechaRegistro = seg.FechaRegistro ?? existente.FechaRegistro;
                            existente.FechaRealizacion = seg.FechaRealizacion ?? existente.FechaRealizacion;
                            existente.Estado = seg.Estado;
                            
                            dbContext.Seguimientos.Update(existente);
                            actualizados++;
                              _logger.LogInformation("[SeguimientoService] Actualizado Preventivo: {Codigo} - Semana {Semana}", 
                                seg.Codigo ?? "DESCONOCIDO", seg.Semana);
                        }
                        else if (existente.TipoMtno == TipoMantenimiento.Correctivo)
                        {
                            // 🔒 IGNORAR: Los Correctivos NUNCA se actualizan (son independientes)
                            preventivosIgnorados++;
                            _logger.LogWarning("[SeguimientoService] No se actualiza Correctivo (son independientes): {Codigo} - Semana {Semana}", 
                                seg.Codigo ?? "DESCONOCIDO", seg.Semana);
                        }
                    }

                    await dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("[SeguimientoService] Importación completada: Nuevos={Nuevos}, Actualizados={Actualizados}, Correctivos ignorados={IgnoradosCorrectivos}", 
                        seguimientosImportados.Count - actualizados - preventivosIgnorados, 
                        actualizados, 
                        preventivosIgnorados);
                }

                _logger.LogInformation("[SeguimientoService] Importación completada: {Importados} importados, {Ignorados} ignorados",
                    seguimientosImportados.Count, seguimientosIgnorados.Count);

                // Crear cronogramas automáticamente desde los seguimientos importados
                await CrearCronogramasDesdeSeguidmientosAsync();

                // Notificar actualización
                WeakReferenceMessenger.Default.Send(new SeguimientosActualizadosMessage());

                // Log de ignored items para diagnóstico
                if (seguimientosIgnorados.Any())
                {
                    foreach (var ignorado in seguimientosIgnorados)
                    {
                        _logger.LogWarning("[SeguimientoService] Fila {Fila} ignorada: {Razon}", ignorado.fila, ignorado.razon);
                    }
                }
            }
            catch (GestionMantenimientosDomainException ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error de validación en importación");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error al importar desde Excel");
                throw new GestionMantenimientosDomainException(
                    $"Error al importar seguimientos: {ex.Message}" +
                    $"\n\nVerifica que el archivo Excel tenga:" +
                    $"\n- Primera fila con encabezados: Codigo, Nombre, TipoMtno, Descripcion, Responsable, Costo, Observaciones, FechaRealizacion" +
                    $"\n- Datos válidos a partir de la fila 2" +
                    $"\n- Tipos de mantenimiento: 'Preventivo' o 'Correctivo'" +
                    $"\n- Fechas en formato dd/MM/yyyy", ex);
            }
        }

        /// <summary>
        /// Obtiene la lista de códigos válidos de equipos existentes en la BD
        /// </summary>
        private async Task<HashSet<string>> ObtenerCodigosEquiposValidosAsync()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var codigos = await dbContext.Equipos
                .Where(e => !string.IsNullOrWhiteSpace(e.Codigo))
                .Select(e => e.Codigo)
                .ToListAsync();
            return new HashSet<string>(codigos, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Mapea flexiblemente los encabezados del Excel (ignora espacios, mayúsculas, caracteres especiales)
        /// Retorna un diccionario con el nombre lógico -> número de columna
        /// </summary>
        private Dictionary<string, int> MapearColumnasExcel(IXLWorksheet worksheet)
        {
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Columnas requeridas que buscamos
            var columnasRequeridas = new[]
            {
                "Codigo", "Nombre", "TipoMtno", "Descripcion", "Responsable", "Costo", "Observaciones", "FechaRealizacion"
            };

            // Normalizar función: quita espacios, caracteres especiales y convierte a minúsculas
            Func<string, string> Normalizar = (s) =>
            {
                if (string.IsNullOrWhiteSpace(s)) return "";
                return System.Text.RegularExpressions.Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9]", "");
            };

            var rangeUsed = worksheet.RangeUsed();
            if (rangeUsed == null)
            {
                throw new GestionMantenimientosDomainException(
                    "El archivo Excel está vacío. Por favor, carga un archivo con datos.");
            }

            // Buscar cada columna requerida en la fila de encabezados
            foreach (var colRequerida in columnasRequeridas)
            {
                string colNormalizada = Normalizar(colRequerida);
                bool encontrada = false;

                for (int col = 1; col <= rangeUsed.ColumnCount(); col++)
                {
                    string headerCell = worksheet.Cell(1, col).GetString()?.Trim() ?? "";
                    string headerNormalizado = Normalizar(headerCell);

                    if (headerNormalizado == colNormalizada)
                    {
                        columnMap[colRequerida] = col;
                        encontrada = true;
                        break;
                    }
                }

                if (!encontrada)
                {
                    throw new GestionMantenimientosDomainException(
                        $"No se encontró la columna '{colRequerida}' en el Excel. " +
                        $"Asegúrate de que el archivo tenga las siguientes columnas: " +
                        $"Codigo, Nombre, TipoMtno, Descripcion, Responsable, Costo, Observaciones, FechaRealizacion");
                }
            }

            _logger.LogInformation("[SeguimientoService] Mapeo de columnas: {ColumnMap}",
                string.Join(", ", columnMap.Select(kvp => $"{kvp.Key}={kvp.Value}")));

            return columnMap;
        }

        /// <summary>
        /// Crea cronogramas automáticamente para datos históricos importados
        /// Agrupa seguimientos por (Codigo, Anio) y marca las semanas donde hubo mantenimientos
        /// </summary>
        public async Task CrearCronogramasDesdeSeguidmientosAsync()
        {
            _logger.LogInformation("[SeguimientoService] Iniciando creación automática de cronogramas desde seguimientos importados");
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

                _logger.LogInformation("[SeguimientoService] Se encontraron {GrupoCount} grupos de seguimientos (Codigo, Año)", grupos.Count);

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

                        // Calcular número de semanas en el año
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
                                FrecuenciaMtto = null // Sin frecuencia para datos históricos
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
                _logger.LogInformation("[SeguimientoService] Creación de cronogramas completada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SeguimientoService] Error al crear cronogramas desde seguimientos");
                throw new GestionMantenimientosDomainException("Error al crear cronogramas: " + ex.Message, ex);
            }
        }

        public async Task ExportarAExcelAsync(string filePath)
        {
            _logger.LogInformation("[SeguimientoService] Iniciando exportación a Excel: {FilePath}", filePath);
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

                _logger.LogInformation("[SeguimientoService] Exportación completada: {FilePath}", filePath);
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

        // Utilidad para obtener el primer día de la semana ISO 8601
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
