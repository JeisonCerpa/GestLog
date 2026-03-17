using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Services.Core.Logging;
using GestLog.Modules.DaaterProccesor.Exceptions;
using GestLog.Models.Configuration.Modules;

namespace GestLog.Modules.DaaterProccesor.Services;

public class DataConsolidationService : IDataConsolidationService
{
    private readonly IGestLogLogger _logger;

    /// <summary>
    /// Estructura para rastrear registros duplicados
    /// </summary>
    private class DuplicateInfo
    {
        public string FileName { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public long PartidaArancelaria { get; set; }
        public long NumeroDeclaracion { get; set; }
    }

    public DataConsolidationService(IGestLogLogger logger)
    {
        _logger = logger;
    }    public DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        System.IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {
        // Usar configuraciones por defecto para mantener compatibilidad hacia atrás
        var defaultSettings = new DaaterProcessorSettings();
        return ConsolidarDatos(folderPath, paises, partidas, proveedores, defaultSettings, progress, cancellationToken);
    }

    public DataTable ConsolidarDatos(
        string folderPath,
        Dictionary<string, string> paises,
        Dictionary<long, string[]> partidas,
        Dictionary<string, string> proveedores,
        DaaterProcessorSettings settings,
        System.IProgress<double> progress,
        CancellationToken cancellationToken = default)
    {        return _logger.LoggedOperation("Consolidación de datos Excel", () =>
        {
            _logger.LogDebug("📊 Iniciando consolidación de datos desde: {FolderPath}", folderPath);

            // Estructuras para validación de duplicados
            var seenRecords = new HashSet<(long partidaArancelaria, long numeroDeclaracion)>();
            var duplicatesList = new List<DuplicateInfo>();
            var duplicatesCount = 0;// Verificar que la carpeta existe
            if (!Directory.Exists(folderPath))
            {
                var ex = new FileValidationException("La carpeta seleccionada no existe", folderPath, "FOLDER_EXISTS");
                _logger.LogError(ex, "❌ Carpeta no encontrada: {FolderPath}", folderPath);
                throw ex;
            }

            // Obtener archivos Excel en la carpeta
            var excelFiles = Directory.GetFiles(folderPath, "*.xlsx");
            
            // Verificar que hay archivos para procesar
            if (excelFiles.Length == 0)
            {
                var ex = new FileValidationException("No se encontraron archivos Excel en la carpeta seleccionada", 
                    folderPath, "NO_EXCEL_FILES");
                _logger.LogError(ex, "❌ No se encontraron archivos Excel en: {FolderPath}", folderPath);
                throw ex;
            }
            
            _logger.LogDebug("📄 Archivos Excel encontrados: {FileCount}", excelFiles.Length);
            
            var requiredColumns = new List<string>
            {
                "FECHA DECLARACIÓN", "NÚMERO DECLARACIÓN", "IMPORTADOR", "IMPORTADOR",
                "EXPORTADOR (PROVEEDOR)", "DIRECCIÓN EXPORTADOR (PROVEEDOR)", "DATO DE CONTACTO EXPORTADOR",
                "PAÍS EXPORTADOR", "PAÍS DE ORIGEN", "PARTIDA ARANCELARIA", "PARTIDA ARANCELARIA",
                "NÚMERO DE BULTOS", "PESO NETO", "VALOR FOB (USD)", "DESCRIPCIÓN MERCANCÍA"
            };
            
            var culture = new CultureInfo("es-ES");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        var consolidatedData = new DataTable();
        consolidatedData.Columns.Add("Fecha", typeof(DateTime));
        consolidatedData.Columns.Add("Mes", typeof(string));
        consolidatedData.Columns.Add("Número Declaración", typeof(long));
        consolidatedData.Columns.Add("NIT IMPORTADOR", typeof(long));
        consolidatedData.Columns.Add("NOMBRE IMPORTADOR", typeof(string));
        consolidatedData.Columns.Add("NOMBRE PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("DIRECCIÓN PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("DATO DE CONTACTO PROVEEDOR", typeof(string));
        consolidatedData.Columns.Add("PAIS EXPORTADOR", typeof(string));
        consolidatedData.Columns.Add("PAIS DE ORIGEN", typeof(string));
        consolidatedData.Columns.Add("NOMBRE PAIS DE ORIGEN", typeof(string));
        consolidatedData.Columns.Add("PARTIDA ARANCELARIA", typeof(long));
        consolidatedData.Columns.Add("NÚMERO DE BULTOS", typeof(string));
        consolidatedData.Columns.Add("DESCRIPCION GENERAL PARTIDA ARANCELARIA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO PARTIDA ARANCELARIA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-PARTIDA", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-PARTIDA NIVEL 1", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-SUB-PARTIDA NIVEL 2", typeof(string));
        consolidatedData.Columns.Add("SIGNIFICADO SUB-SUB-PARTIDA NIVEL 3", typeof(string));
        consolidatedData.Columns.Add("PESO NETO", typeof(double));
        consolidatedData.Columns.Add("PESO TON", typeof(double));
        consolidatedData.Columns.Add("VALOR FOB(USD)", typeof(double));
        consolidatedData.Columns.Add("FOB POR TON", typeof(double));
        consolidatedData.Columns.Add("DESCRIPCION MERCANCIA", typeof(string));        int fileCount = excelFiles.Length;
        int fileIndex = 0;
        var totalRowsProcessed = 0;

        foreach (var file in excelFiles)
        {
            // Verificar cancelación antes de procesar cada archivo
            cancellationToken.ThrowIfCancellationRequested();
            
            var fileName = Path.GetFileName(file);
            _logger.LogDebug("📂 Procesando archivo {FileIndex}/{FileCount}: {FileName}", 
                fileIndex + 1, fileCount, fileName);
              try
            {
                // Verificar que la extensión sea .xlsx
                if (!Path.GetExtension(file).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("⚠️ Archivo con formato incorrecto: {FileName}", fileName);
                    throw new ExcelFormatException(
                        $"El archivo '{fileName}' no tiene formato Excel (.xlsx)",
                        file,
                        ".xlsx");
                }
                
                // Verificar que el archivo exista y no esté dañado
                if (!File.Exists(file))
                {
                    _logger.LogWarning("⚠️ Archivo no encontrado: {FileName}", fileName);
                    throw new FileValidationException(
                        $"El archivo '{fileName}' no existe",
                        file,
                        "FILE_EXISTS");
                }
                
                using var workbook = new XLWorkbook(file);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("⚠️ Archivo sin worksheets válidos: {FileName}", fileName);
                    throw new ExcelFormatException(
                        $"El archivo '{fileName}' no contiene hojas de trabajo válidas",
                        file,
                        "VALID_WORKSHEET");
                }
                var headerRow = worksheet.Row(1);

                // Construir mapa de posiciones de cabeceras: Nombre normalizado -> lista de índices de columna (1-based)
                var headerPositions = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

                foreach (var cell in headerRow.CellsUsed())
                {
                    var raw = cell.GetString() ?? string.Empty;
                    var name = raw.Trim();
                    if (string.IsNullOrEmpty(name))
                        continue;

                    var norm = CanonicalizeHeader(name);
                    if (!headerPositions.ContainsKey(norm))
                        headerPositions[norm] = new List<int>();
                    headerPositions[norm].Add(cell.Address.ColumnNumber);
                }

                // Contar cuántas ocurrencias se esperan por cada cabecera (según requiredColumns)
                var expectedCounts = requiredColumns
                    .GroupBy(c => CanonicalizeHeader(c))
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                // Validar que existan las columnas requeridas con la cantidad de ocurrencias esperada
                var missingColumns = expectedCounts
                    .Where(kv => !headerPositions.ContainsKey(kv.Key) || headerPositions[kv.Key].Count < kv.Value)
                    .Select(kv => kv.Key)
                    .ToList();

                if (missingColumns.Any())
                {
                    var columnasFaltantes = string.Join(", ", missingColumns);
                    _logger.LogWarning("⚠️ Archivo con columnas faltantes: {FileName} - Columnas: {MissingColumns}", fileName, columnasFaltantes);
                    throw new ExcelFormatException(
                        $"El archivo '{fileName}' no tiene el formato esperado. Faltan columnas: {columnasFaltantes}",
                        file,
                        "REQUIRED_COLUMNS");
                }

                // Detectar cabeceras repetidas inesperadas (más ocurrencias de las requeridas). Solo warn, no error en modo permisivo.
                var unexpectedDuplicates = headerPositions
                    .Where(kv => expectedCounts.TryGetValue(kv.Key, out var expected) ? kv.Value.Count > expected : kv.Value.Count > 1)
                    .Select(kv => kv.Key)
                    .ToList();

                if (unexpectedDuplicates.Any())
                {
                    var amb = string.Join(", ", unexpectedDuplicates);
                    _logger.LogWarning("⚠️ Cabeceras repetidas inesperadas en archivo: {FileName} - Cabeceras: {Ambiguous}", fileName, amb);
                    // No lanzamos excepción: modo permisivo por posición
                }

                // Helpers para trabajar con N-ésima aparición de una cabecera
                int GetColumnIndexForOccurrence(string name, int occurrence = 1)
                {
                    var key = CanonicalizeHeader(name);
                    if (!headerPositions.TryGetValue(key, out var positions))
                        return -1;

                    if (occurrence <= 0)
                        occurrence = 1;

                    if (occurrence <= positions.Count)
                        return positions[occurrence - 1];

                    return -1;
                }

                string GetCellString(IXLRow r, string expectedColumnName, int occurrence = 1)
                {
                    var colIndex = GetColumnIndexForOccurrence(expectedColumnName, occurrence);
                    return colIndex > 0 ? r.Cell(colIndex).GetString() : string.Empty;
                }

                var rows = worksheet.RowsUsed().Skip(1).ToList();
                int rowCount = rows.Count;
                int rowIndex = 0;
                
                _logger.LogDebug("📊 Procesando {RowCount} filas del archivo: {FileName}", rowCount, fileName);
                
                foreach (var row in rows)
                {                    // Verificar cancelación cada 100 filas para no impactar mucho el rendimiento
                    if (rowIndex % 100 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (rowIndex > 0)
                        {
                            _logger.LogDebug("⚙️ Progreso archivo {FileName}: {ProcessedRows}/{TotalRows} filas", 
                                fileName, rowIndex, rowCount);
                        }
                    }
                    
                    var fecha = GetCellString(row, "FECHA DECLARACIÓN");
                    var declaracion = GetCellString(row, "NÚMERO DECLARACIÓN");
                    var nitImportador = GetCellString(row, "IMPORTADOR", 1);
                    var nombreImportador = GetCellString(row, "IMPORTADOR", 2);
                    var nombreProveedor = GetCellString(row, "EXPORTADOR (PROVEEDOR)");
                    var direccionProveedor = GetCellString(row, "DIRECCIÓN EXPORTADOR (PROVEEDOR)");
                    var datoContactoProveedor = GetCellString(row, "DATO DE CONTACTO EXPORTADOR");
                    var paisExportador = GetCellString(row, "PAÍS EXPORTADOR");
                    var paisDeOrigen = GetCellString(row, "PAÍS DE ORIGEN");
                    var partidaArancelaria = GetCellString(row, "PARTIDA ARANCELARIA", 1);
                    var numeroDeBultos = GetCellString(row, "NÚMERO DE BULTOS");
                    var pesoNeto = GetCellString(row, "PESO NETO");
                    var valorFobUsd = GetCellString(row, "VALOR FOB (USD)");
                    var descripcionMercancia = GetCellString(row, "DESCRIPCIÓN MERCANCÍA");
                    // --- Normalización de nombre de proveedor ---
                    // Obtener lista de nombres oficiales únicos
                    var nombresOficiales = proveedores.Values.Distinct().ToList();
                    if (!string.IsNullOrWhiteSpace(nombreProveedor))
                    {
                        nombreProveedor = ResourceLoaderService.NormalizarNombreProveedor(nombreProveedor, nombresOficiales, 80);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(direccionProveedor) && proveedores.TryGetValue(direccionProveedor, out var nombreEncontrado))
                            nombreProveedor = nombreEncontrado;
                        else if (!string.IsNullOrWhiteSpace(datoContactoProveedor) && proveedores.TryGetValue(datoContactoProveedor, out nombreEncontrado))
                            nombreProveedor = nombreEncontrado;
                        // Si se encontró, también normalizar
                        if (!string.IsNullOrWhiteSpace(nombreProveedor))
                            nombreProveedor = ResourceLoaderService.NormalizarNombreProveedor(nombreProveedor, nombresOficiales, 80);

                        // Escribir nombre normalizado de proveedor de vuelta en la columna correcta (si existe)
                        var exportadorCol = GetColumnIndexForOccurrence("EXPORTADOR (PROVEEDOR)", 1);
                        if (exportadorCol > 0)
                            row.Cell(exportadorCol).Value = nombreProveedor;
                    }
                    DateTime? fechaValida = null;
                    string mes = string.Empty;
                    if (!string.IsNullOrEmpty(fecha))
                    {
                        if (DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ||
                            DateTime.TryParse(fecha, out parsedDate))
                        {
                            fechaValida = parsedDate;
                            mes = parsedDate.ToString("MMMM", culture).ToUpper();
                        }
                        else
                        {
                            mes = "MES INVÁLIDO";
                        }
                    }
                    else
                    {
                        mes = "MES INVÁLIDO";
                    }
                    if (!string.IsNullOrWhiteSpace(declaracion))
                    {
                        var comaIndex = declaracion.IndexOf(',');
                        if (comaIndex != -1)
                            declaracion = declaracion.Substring(0, comaIndex);
                    }
                    long numeroDeclaracion = 0;
                    if (!string.IsNullOrWhiteSpace(declaracion))
                    {
                        declaracion = declaracion.Replace(",", "");
                        if (long.TryParse(declaracion, out var parsedDeclaracion))
                            numeroDeclaracion = parsedDeclaracion;
                    }
                    if (!string.IsNullOrWhiteSpace(nitImportador))
                    {
                        var comaIndex = nitImportador.IndexOf(',');
                        if (comaIndex != -1)
                            nitImportador = nitImportador.Substring(0, comaIndex);
                    }
                    long numeroNitImportador = 0;
                    if (!string.IsNullOrWhiteSpace(nitImportador))
                    {
                        nitImportador = nitImportador.Replace(",", "");
                        if (long.TryParse(nitImportador, out var parsedNitImportador))
                            numeroNitImportador = parsedNitImportador;
                    }
                    if (!string.IsNullOrWhiteSpace(paisExportador) && (int.TryParse(paisExportador, out _) || paisExportador.Any(char.IsDigit)))
                        paisExportador = "CO";
                    if (!string.IsNullOrWhiteSpace(paisDeOrigen) && (int.TryParse(paisDeOrigen, out _) || paisDeOrigen.Any(char.IsDigit)))
                        paisDeOrigen = "CO";
                    string nombrePaisDeOrigen = paises.ContainsKey(paisDeOrigen) ? paises[paisDeOrigen] : "Desconocido";
                    long numeroPartidaArancelaria = 0;
                    if (!string.IsNullOrWhiteSpace(partidaArancelaria))
                    {
                        partidaArancelaria = partidaArancelaria.Replace(",", "");
                        if (long.TryParse(partidaArancelaria, out var parsedPartida))
                            numeroPartidaArancelaria = parsedPartida;
                    }
                    var descripcionGeneral = "Desconocido";
                    var significadoPartida = "Desconocido";
                    var significadoSubPartida = "Desconocido";
                    var significadoSubPartidaNivel1 = "Desconocido";
                    var significadoSubSubPartidaNivel2 = "Desconocido";
                    var significadoSubSubPartidaNivel3 = "Desconocido";
                    if (partidas.TryGetValue(numeroPartidaArancelaria, out var partidaData))
                    {
                        descripcionGeneral = partidaData[0];
                        significadoPartida = partidaData[1];
                        significadoSubPartida = partidaData[2];
                        significadoSubPartidaNivel1 = partidaData[3];
                        significadoSubSubPartidaNivel2 = partidaData[4];
                        significadoSubSubPartidaNivel3 = partidaData[5];
                    }
                    double pesoNetoValue = 0;
                    if (!string.IsNullOrWhiteSpace(pesoNeto))
                    {
                        pesoNeto = pesoNeto.Replace(",", "");
                        if (double.TryParse(pesoNeto, out var parsedPesoNeto))
                            pesoNetoValue = parsedPesoNeto;
                    }
                    double pesoTonValue = pesoNetoValue / 1000;
                    double valorFobUsdValue = 0;
                    if (!string.IsNullOrWhiteSpace(valorFobUsd))
                    {
                        valorFobUsd = valorFobUsd.Replace(",", "");
                        if (double.TryParse(valorFobUsd, out var parsedValorFobUsd))
                            valorFobUsdValue = parsedValorFobUsd;                    }
                    double fobPorTonValue = pesoTonValue > 0 ? valorFobUsdValue / pesoTonValue : 0;
                      // Validación de duplicados basada en partida arancelaria y número de declaración
                    var recordKey = (numeroPartidaArancelaria, numeroDeclaracion);
                    
                    if (settings.EnableDuplicateValidation && seenRecords.Contains(recordKey))
                    {
                        duplicatesCount++;
                        var duplicateInfo = new DuplicateInfo
                        {
                            FileName = fileName,
                            RowNumber = rowIndex + 2, // +2 porque rowIndex empieza en 0 y hay header
                            PartidaArancelaria = numeroPartidaArancelaria,
                            NumeroDeclaracion = numeroDeclaracion
                        };
                        duplicatesList.Add(duplicateInfo);
                        
                        _logger.LogWarning("⚠️ Duplicado detectado en {FileName}, fila {RowNumber}: " +
                            "Partida {PartidaArancelaria} + Declaración {NumeroDeclaracion}", 
                            fileName, duplicateInfo.RowNumber, numeroPartidaArancelaria, numeroDeclaracion);
                        
                        // Manejar duplicado según configuración
                        switch (settings.DuplicateHandlingMode)
                        {
                            case DuplicateHandlingMode.Skip:
                                _logger.LogDebug("🔄 Omitiendo registro duplicado según configuración");
                                continue; // Omitir el registro duplicado
                                
                            case DuplicateHandlingMode.Replace:
                                _logger.LogDebug("🔄 Reemplazando registro duplicado según configuración");
                                // Buscar y eliminar el registro existente
                                var existingRows = consolidatedData.AsEnumerable()
                                    .Where(r => r.Field<long>("PARTIDA ARANCELARIA") == numeroPartidaArancelaria && 
                                               r.Field<long>("Número Declaración") == numeroDeclaracion)
                                    .ToList();
                                foreach (var existingRow in existingRows)
                                {
                                    consolidatedData.Rows.Remove(existingRow);
                                }
                                // Continuar para agregar el nuevo registro
                                break;                            case DuplicateHandlingMode.Error:
                                _logger.LogWarning("❌ Error por duplicado según configuración en {FileName}, fila {RowNumber}",
                                    fileName, duplicateInfo.RowNumber);
                                throw new ExcelDataException(
                                    $"Registro duplicado encontrado en '{fileName}', fila {duplicateInfo.RowNumber}: " +
                                    $"Partida {numeroPartidaArancelaria} + Declaración {numeroDeclaracion}",
                                    file);
                                    
                            case DuplicateHandlingMode.Allow:
                                _logger.LogDebug("✅ Permitiendo registro duplicado según configuración");
                                // No hacer nada, permitir el duplicado
                                break;
                        }
                    }
                    
                    // Marcar este registro como visto (solo si la validación está habilitada)
                    if (settings.EnableDuplicateValidation)
                    {
                        seenRecords.Add(recordKey);
                    }
                    
                    consolidatedData.Rows.Add(
                        fechaValida, mes, numeroDeclaracion, numeroNitImportador, nombreImportador, nombreProveedor,
                        direccionProveedor, datoContactoProveedor, paisExportador, paisDeOrigen, nombrePaisDeOrigen,
                        numeroPartidaArancelaria, numeroDeBultos, descripcionGeneral, significadoPartida, significadoSubPartida,
                        significadoSubPartidaNivel1, significadoSubSubPartidaNivel2, significadoSubSubPartidaNivel3,
                        pesoNetoValue, pesoTonValue, valorFobUsdValue, fobPorTonValue, descripcionMercancia);
                    rowIndex++;                    // Reporta progreso granular por fila
                    double progressValue = ((double)fileIndex + (rowCount > 0 ? (double)rowIndex / rowCount : 0)) * 100.0 / fileCount;
                    progress?.Report(Math.Min(Math.Max(progressValue, 0), 100)); // Asegurar valor entre 0-100
                }
                
                totalRowsProcessed += rowCount;
                _logger.LogDebug("✅ Archivo completado: {FileName} - {RowCount} filas procesadas", fileName, rowCount);
            }            catch (ExcelFormatException ex)
            {
                // Re-lanzar excepciones específicas de Excel para que las maneje el ViewModel
                _logger.LogError(ex, "❌ Error de formato en archivo: {FileName}", fileName);
                throw;
            }
            catch (FileValidationException ex)
            {
                // Re-lanzar excepciones específicas de validación para que las maneje el ViewModel
                _logger.LogError(ex, "❌ Error de validación en archivo: {FileName}", fileName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error procesando archivo: {FileName}", fileName);
                // Envolver excepciones genéricas en nuestras propias excepciones para mejor manejo
                throw new ExcelDataException($"Error al procesar el archivo '{fileName}': {ex.Message}", file, ex);
            }
            fileIndex++;        }
        
        _logger.LogDebug("🔄 Ordenando datos consolidados...");
        
        // Ordenar los datos por el número del mes y luego por "PARTIDA ARANCELARIA"
        var sortedData = consolidatedData.AsEnumerable()
            .OrderBy(row =>
            {
                var mes = row.Field<string>("Mes");
                return !string.IsNullOrEmpty(mes)
                    ? DateTime.ParseExact(mes, "MMMM", new CultureInfo("es-ES")).Month
                    : 0;
            })            .ThenBy(row => row.Field<long>("PARTIDA ARANCELARIA"))
            .CopyToDataTable();
        
        // Logging de resumen de duplicados
        if (duplicatesCount > 0)
        {
            _logger.LogWarning("⚠️ Se encontraron {DuplicateCount} registros duplicados que fueron omitidos", duplicatesCount);
            
            // Log detallado de los primeros 5 duplicados para debugging
            var topDuplicates = duplicatesList.Take(5);
            foreach (var duplicate in topDuplicates)
            {
                _logger.LogDebug("Duplicado: {FileName}:{RowNumber} - Partida {PartidaArancelaria}, Declaración {NumeroDeclaracion}",
                    duplicate.FileName, duplicate.RowNumber, duplicate.PartidaArancelaria, duplicate.NumeroDeclaracion);
            }
            
            if (duplicatesCount > 5)
            {
                _logger.LogDebug("... y {RemainingCount} duplicados adicionales", duplicatesCount - 5);
            }
        }
        else
        {
            _logger.LogDebug("✅ No se encontraron registros duplicados");
        }
            
        _logger.LogDebug("✅ Consolidación completada: {TotalFiles} archivos, {TotalRows} filas procesadas, {ConsolidatedRows} filas únicas consolidadas, {DuplicatesOmitted} duplicados omitidos", 
            fileCount, totalRowsProcessed, sortedData.Rows.Count, duplicatesCount);
            
        return sortedData;
        });
    }

    private static string CanonicalizeHeader(string header)
    {
        var normalized = RemoveDiacritics(header)
            .Trim()
            .ToUpperInvariant();

        normalized = string.Join(" ", normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

        return normalized switch
        {
            "NUMERO FORMULARIO" => "NUMERO DECLARACION",
            "DIRECCION EXPORTADOR" => "DIRECCION EXPORTADOR (PROVEEDOR)",
            "FECHA" => "FECHA DECLARACION",
            "PAIS ORIGEN" => "PAIS DE ORIGEN",
            _ => normalized
        };
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var formD = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (var c in formD)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
