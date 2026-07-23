using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionCartera.Exceptions;

namespace GestLog.Modules.GestionCartera.Services
{
    /// <summary>
    /// Servicio optimizado para extraer correos electrónicos desde archivos Excel
    /// Implementa un sistema de índice eficiente para búsquedas O(1)
    /// </summary>
    public class ExcelEmailService : IExcelEmailService
    {
        private readonly IGestLogLogger _logger;
        private readonly Dictionary<string, string> _nitNormalizadoCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Índice para búsquedas eficientes: NIT normalizado -> Lista de emails
        private Dictionary<string, List<string>> _emailIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private string _currentExcelPath = string.Empty;
        private DateTime _lastModified = DateTime.MinValue;

        public ExcelEmailService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }        /// <summary>
        /// Construye un índice en memoria para búsquedas eficientes O(1)
        /// Solo se ejecuta si el archivo cambió desde la última carga
        /// </summary>
        private async Task EnsureEmailIndexLoaded(string excelFilePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(excelFilePath) || !File.Exists(excelFilePath))
            {
                _logger.LogWarning("Archivo Excel no encontrado para construir índice: {ExcelPath}", excelFilePath ?? "null");
                _emailIndex.Clear();
                return;
            }            // Primero validar la estructura del archivo antes de procesarlo
            try
            {
                await ValidateExcelStructureAsync(excelFilePath, cancellationToken);
            }
            catch (EmailExcelValidationException ex)
            {
                _logger.LogError(ex, "❌ Archivo Excel de correos no válido: {Message}", ex.Message);
                _emailIndex.Clear();
                throw;
            }
            catch (EmailExcelStructureException ex)
            {
                _logger.LogError(ex, "❌ Estructura de archivo Excel incorrecta: {Message}. Columnas faltantes: {MissingColumns}", 
                    ex.Message, string.Join(", ", ex.MissingColumns));
                _emailIndex.Clear();
                throw;
            }

            // Verificar si necesitamos recargar el índice
            var fileInfo = new FileInfo(excelFilePath);
            bool needsReload = _currentExcelPath != excelFilePath || 
                              _lastModified != fileInfo.LastWriteTime || 
                              _emailIndex.Count == 0;

            if (!needsReload)
            {
                _logger.LogDebug("Índice de emails ya está actualizado para: {ExcelPath}", excelFilePath);
                return;
            }

            _logger.LogInformation("🔄 Construyendo índice de emails desde: {ExcelPath}", excelFilePath);
            var startTime = DateTime.Now;

            try
            {
                _emailIndex.Clear();
                var tempIndex = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook(excelFilePath);

                    foreach (var worksheet in workbook.Worksheets)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Construcción de índice cancelada por el usuario");
                            return;
                        }

                        _logger.LogDebug("Indexando hoja: {WorksheetName}", worksheet.Name);
                        ProcessWorksheetForIndex(worksheet, tempIndex);
                    }
                }, cancellationToken);

                // Solo actualizar si completamos exitosamente
                if (!cancellationToken.IsCancellationRequested)
                {
                    _emailIndex = tempIndex;
                    _currentExcelPath = excelFilePath;
                    _lastModified = fileInfo.LastWriteTime;

                    var elapsed = DateTime.Now - startTime;
                    _logger.LogInformation("✅ Índice construido exitosamente: {Count} NITs indexados en {ElapsedMs}ms", 
                        _emailIndex.Count, elapsed.TotalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error construyendo índice de emails");
                _emailIndex.Clear();
                throw;
            }
        }        /// <summary>
        /// Procesa una hoja de Excel para construir el índice NIT->Emails
        /// </summary>
        private void ProcessWorksheetForIndex(IXLWorksheet worksheet, Dictionary<string, List<string>> index)
        {
            try
            {
                int tipoDocCol = 2;    // Columna B - Tipo de documento
                int numIdCol = 3;      // Columna C - Número de identificación  
                int digitoVerCol = 4;  // Columna D - Dígito de verificación
                int correoCol = 6;     // Columna F - Email

                var filas = worksheet.RowsUsed().Skip(1); // Omitir encabezados
                var totalRows = filas.Count();
                var processedRows = 0;
                var validNits = 0;
                var validEmails = 0;

                _logger.LogDebug("🔍 Procesando {TotalRows} filas para índice en hoja {WorksheetName}", totalRows, worksheet.Name);

                foreach (var row in filas)
                {
                    try
                    {
                        processedRows++;
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        // Debug: Log primera fila para diagnóstico
                        if (processedRows <= 3)
                        {
                            _logger.LogDebug("📝 Fila {Row}: TIPO_DOC='{TipoDoc}', NUM_ID='{NumId}', EMAIL='{Email}'", 
                                row.RowNumber(), tipoDoc, numId, correo);
                        }

                        // Verificar condiciones de validación
                        var isNitType = tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase);
                        var hasNumId = !string.IsNullOrEmpty(numId);
                        var hasEmail = !string.IsNullOrWhiteSpace(correo);

                        if (!isNitType || !hasNumId || !hasEmail)
                        {
                            if (processedRows <= 5) // Log primeras 5 filas para debug
                            {
                                _logger.LogDebug("❌ Fila {Row} rechazada: isNIT={IsNit}, hasNumId={HasNumId}, hasEmail={HasEmail}", 
                                    row.RowNumber(), isNitType, hasNumId, hasEmail);
                            }
                            continue;
                        }

                        validNits++;                        // Construir NIT completo y normalizarlo
                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);

                        if (string.IsNullOrEmpty(nitNormalizado))
                        {
                            _logger.LogDebug("❌ NIT normalizado vacío para: {NitCompleto}", nitCompleto);
                            continue;
                        }

                        // Procesar múltiples correos separados por coma o punto y coma
                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string emailLimpio = email.Trim();
                            if (IsValidEmail(emailLimpio))
                            {
                                validEmails++;
                                
                                if (!index.ContainsKey(nitNormalizado))
                                {
                                    index[nitNormalizado] = new List<string>();
                                }

                                if (!index[nitNormalizado].Contains(emailLimpio, StringComparer.OrdinalIgnoreCase))
                                {
                                    index[nitNormalizado].Add(emailLimpio);
                                    
                                    if (index.Count <= 3) // Log primeros registros para debug
                                    {
                                        _logger.LogDebug("✅ Indexado: NIT {Nit} → Email {Email}", nitNormalizado, emailLimpio);
                                    }
                                }
                            }
                            else if (processedRows <= 5)
                            {
                                _logger.LogDebug("❌ Email inválido: '{Email}'", emailLimpio);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila {Row} en índice para hoja {WorksheetName}", row.RowNumber(), worksheet.Name);
                    }
                }

                _logger.LogInformation("📊 Índice construido: {IndexCount} NITs, {ValidNits} NITs válidos, {ValidEmails} emails válidos de {ProcessedRows} filas", 
                    index.Count, validNits, validEmails, processedRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando hoja para índice {WorksheetName}", worksheet.Name);
            }
        }

        /// <summary>
        /// Versión optimizada: Búsqueda O(1) usando índice en memoria
        /// </summary>
        public async Task<List<string>> GetEmailsForCompanyAsync(string excelFilePath, string companyName, string nit, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔍 Buscando correos para empresa: {CompanyName}, NIT: {Nit}", companyName, nit);

                // Normalizar NIT para búsqueda
                var nitNormalizado = NormalizeNit(nit ?? string.Empty);
                
                if (string.IsNullOrEmpty(nitNormalizado))
                {
                    _logger.LogWarning("NIT inválido o vacío para empresa: {CompanyName}", companyName);
                    return new List<string>();
                }

                // Asegurar que el índice esté cargado (solo se ejecuta si es necesario)
                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                // Búsqueda O(1) en el índice
                if (_emailIndex.TryGetValue(nitNormalizado, out var emails))
                {
                    _logger.LogInformation("✅ Se encontraron {EmailCount} correos para {CompanyName} (NIT: {Nit})", 
                        emails.Count, companyName, nitNormalizado);
                    return new List<string>(emails); // Retornar copia para evitar modificaciones externas
                }
                else
                {
                    _logger.LogWarning("❌ No se encontraron correos para {CompanyName} con NIT {Nit}", companyName, nitNormalizado);
                    return new List<string>();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Búsqueda de correos cancelada para empresa: {CompanyName}", companyName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando correos para empresa: {CompanyName}, NIT: {Nit}", companyName, nit);
                return new List<string>();
            }
        }

        public async Task<Dictionary<string, List<string>>> GetEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Iniciando extracción de correos desde Excel: {ExcelPath}", excelFilePath);

                // Usar el índice para generar el diccionario por empresa
                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                var resultado = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                // Convertir índice NIT->Email a Empresa->Email si es necesario
                // Para simplificar, retornamos el índice por NIT
                foreach (var kvp in _emailIndex)
                {
                    resultado[kvp.Key] = new List<string>(kvp.Value);
                }

                _logger.LogInformation("Extracción completada. Se encontraron correos para {CompanyCount} NITs", resultado.Count);
                return resultado;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operación de extracción de correos cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al extraer correos desde Excel: {ExcelPath}", excelFilePath);
                throw;
            }
        }

        public async Task<(Dictionary<string, List<string>> empresaCorreos, Dictionary<string, List<string>> nitCorreos)> GetEmailMappingsAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Obteniendo mapeos de correos desde Excel: {ExcelPath}", excelFilePath);

                await EnsureEmailIndexLoaded(excelFilePath, cancellationToken);

                var empresaCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                var nitCorreos = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                // El índice ya contiene NITs -> Emails
                foreach (var kvp in _emailIndex)
                {
                    nitCorreos[kvp.Key] = new List<string>(kvp.Value);
                }

                _logger.LogInformation("Mapeo completado. Empresas: {EmpresaCount}, NITs: {NitCount}", 
                    empresaCorreos.Count, nitCorreos.Count);

                return (empresaCorreos, nitCorreos);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operación de mapeo de correos cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo mapeos de correos desde Excel: {ExcelPath}", excelFilePath);
                return (new Dictionary<string, List<string>>(), new Dictionary<string, List<string>>());
            }
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (email.Contains("@") && email.Contains("."))
            {
                if (email.StartsWith("@") || email.EndsWith("@") ||
                    email.StartsWith(".") || email.EndsWith(".") ||
                    email.Contains("..") || email.Split('@').Length != 2)
                {
                    return false;
                }

                try
                {
                    var addr = new MailAddress(email);
                    return addr.Address == email;
                }
                catch
                {
                    _logger.LogDebug("Correo con formato incorrecto: {Email}", email);
                    return false;
                }
            }

            return false;
        }

        public string NormalizeNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return string.Empty;

            // Usar caché para evitar procesar el mismo NIT múltiples veces
            if (_nitNormalizadoCache.TryGetValue(nit, out var cached))
                return cached;

            // Remover prefijo "NIT" si existe y normalizar espacios
            string cleanNit = nit.Trim();
            if (cleanNit.StartsWith("NIT", StringComparison.OrdinalIgnoreCase))
            {
                cleanNit = cleanNit.Substring(3).Trim();
            }

            // Remover espacios, puntos y caracteres especiales, mantener solo números y guiones
            var normalized = new string(cleanNit.Where(c => char.IsDigit(c) || c == '-').ToArray()).Trim();

            _nitNormalizadoCache[nit] = normalized;
            return normalized;
        }

        public async Task<bool> ValidateExcelStructureAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            var validationResult = await GetValidationInfoAsync(excelFilePath, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                if (validationResult.MissingColumns.Length > 0)
                {
                    throw new EmailExcelStructureException(
                        validationResult.Message, 
                        excelFilePath,
                        validationResult.MissingColumns,
                        validationResult.FoundColumns);
                }
                else
                {
                    throw new EmailExcelValidationException(
                        validationResult.Message, 
                        excelFilePath, 
                        "Archivo Excel válido con columnas: TIPO_DOC, NUM_ID, DIGITO_VER, EMPRESA, EMAIL");
                }
            }

            return true;
        }

        public async Task<ExcelValidationResult> GetValidationInfoAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔍 Validando estructura del archivo Excel: {ExcelPath}", excelFilePath);

                // Validaciones básicas de archivo
                if (string.IsNullOrWhiteSpace(excelFilePath))
                {
                    return ExcelValidationResult.Invalid(
                        "La ruta del archivo no puede estar vacía",
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                        Array.Empty<string>(),
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                }

                if (!File.Exists(excelFilePath))
                {
                    return ExcelValidationResult.Invalid(
                        $"El archivo no existe: {excelFilePath}",
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                        Array.Empty<string>(),
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                }

                // Validar extensión
                var extension = Path.GetExtension(excelFilePath).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls" && extension != ".xlsm")
                {
                    return ExcelValidationResult.Invalid(
                        $"El archivo debe ser un Excel (.xlsx, .xls, .xlsm). Encontrado: {extension}",
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                        Array.Empty<string>(),
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                }

                // ¿El Excel de clientes/correos está abierto en otra aplicación? Responder sí/no antes de leer.
                // missingColumns vacío => ValidateExcelStructureAsync usa el mensaje tal cual (no "columnas faltantes").
                if (FileAccessHelper.IsLocked(excelFilePath))
                {
                    return ExcelValidationResult.Invalid(
                        $"El archivo Excel de clientes está abierto en otra aplicación (Excel). Ciérrelo e intente de nuevo: {Path.GetFileName(excelFilePath)}",
                        new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                        Array.Empty<string>(),
                        Array.Empty<string>());
                }

                return await Task.Run(() =>
                {
                    try
                    {
                        using var workbook = new XLWorkbook(excelFilePath);
                        
                        if (!workbook.Worksheets.Any())
                        {
                            return ExcelValidationResult.Invalid(
                                "El archivo Excel no contiene hojas de trabajo",
                                new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                                Array.Empty<string>(),
                                new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                        }

                        var worksheet = workbook.Worksheets.First();
                        var usedRange = worksheet.RangeUsed();
                        
                        if (usedRange == null || usedRange.RowCount() < 2)
                        {
                            return ExcelValidationResult.Invalid(
                                "El archivo Excel está vacío o no contiene datos (mínimo 2 filas: encabezados + datos)",
                                new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                                Array.Empty<string>(),
                                new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                        }

                        // Validar estructura de columnas esperada
                        return ValidateColumnStructure(worksheet, excelFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al validar archivo Excel: {ExcelPath}", excelFilePath);
                        return ExcelValidationResult.Invalid(
                            $"Error al leer el archivo Excel: {ex.Message}",
                            new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                            Array.Empty<string>(),
                            new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Validación de archivo Excel cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado validando archivo Excel: {ExcelPath}", excelFilePath);
                return ExcelValidationResult.Invalid(
                    $"Error inesperado: {ex.Message}",
                    new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                    Array.Empty<string>(),
                    new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
            }
        }

        /// <summary>
        /// Valida que el archivo Excel tenga la estructura de columnas esperada
        /// </summary>
        private ExcelValidationResult ValidateColumnStructure(IXLWorksheet worksheet, string excelFilePath)
        {
            try
            {
                // Definir la estructura esperada basada en el código existente
                var expectedStructure = new Dictionary<int, string>
                {
                    { 1, "IDENTIFICACION" },  // Columna A - Información general
                    { 2, "TIPO_DOC" },        // Columna B - Tipo de documento  
                    { 3, "NUM_ID" },          // Columna C - Número de identificación
                    { 4, "DIGITO_VER" },      // Columna D - Dígito de verificación
                    { 5, "EMPRESA" },         // Columna E - Nombre de la empresa
                    { 6, "EMAIL" }            // Columna F - Correo electrónico
                };

                // Obtener encabezados de la primera fila
                var firstRow = worksheet.Row(1);
                var foundColumns = new List<string>();
                var columnCount = Math.Min(6, firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0);

                for (int col = 1; col <= columnCount; col++)
                {
                    var cellValue = firstRow.Cell(col).GetValue<string>().Trim();
                    foundColumns.Add(string.IsNullOrEmpty(cellValue) ? $"Columna_{col}" : cellValue);
                }

                // Verificar que tenemos al menos 6 columnas
                if (columnCount < 6)
                {
                    return ExcelValidationResult.Invalid(
                        $"El archivo debe tener al menos 6 columnas. Encontradas: {columnCount}",
                        expectedStructure.Values.ToArray(),
                        foundColumns.ToArray(),
                        expectedStructure.Values.ToArray());
                }

                // Verificar contenido de datos - buscar registros con NIT y email válidos
                var totalRows = worksheet.RangeUsed()?.RowCount() ?? 0;
                var validEmailRows = 0;
                var validNitRows = 0;
                var sampleEmails = new List<string>();

                // Analizar hasta 100 filas de datos para validación
                var maxRowsToCheck = Math.Min(totalRows, 102); // 1 encabezado + 101 datos máximo
                
                for (int row = 2; row <= maxRowsToCheck; row++)
                {
                    try
                    {
                        var tipoDoc = worksheet.Cell(row, 2).GetValue<string>().Trim();
                        var numId = worksheet.Cell(row, 3).GetValue<string>().Trim();
                        var email = worksheet.Cell(row, 6).GetValue<string>().Trim();

                        // Contar NITs válidos
                        if (tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(numId))
                        {
                            validNitRows++;
                        }

                        // Contar emails válidos
                        if (!string.IsNullOrWhiteSpace(email) && IsValidEmail(email))
                        {
                            validEmailRows++;
                            if (sampleEmails.Count < 3)
                            {
                                sampleEmails.Add(email);
                            }
                        }
                    }
                    catch
                    {
                        // Ignorar errores en filas individuales durante validación
                        continue;
                    }
                }

                // Verificar si encontramos datos válidos
                if (validNitRows == 0)
                {
                    return ExcelValidationResult.Invalid(
                        "No se encontraron registros con TIPO_DOC = 'NIT' válidos en el archivo. " +
                        "Verifique que la columna B contenga 'NIT' y la columna C contenga números de identificación.",
                        expectedStructure.Values.ToArray(),
                        foundColumns.ToArray(),
                        Array.Empty<string>());
                }

                if (validEmailRows == 0)
                {
                    return ExcelValidationResult.Invalid(
                        "No se encontraron correos electrónicos válidos en el archivo. " +
                        "Verifique que la columna F contenga direcciones de email válidas.",
                        expectedStructure.Values.ToArray(),
                        foundColumns.ToArray(),
                        Array.Empty<string>());
                }

                // Archivo válido
                _logger.LogInformation("✅ Archivo Excel válido: {ValidNits} NITs, {ValidEmails} emails válidos", 
                    validNitRows, validEmailRows);

                return ExcelValidationResult.Valid(
                    $"Archivo válido: {validNitRows} registros NIT, {validEmailRows} emails válidos",
                    foundColumns.ToArray(),
                    totalRows - 1, // Excluir fila de encabezados
                    validEmailRows,
                    validNitRows,
                    sampleEmails.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validando estructura de columnas");
                return ExcelValidationResult.Invalid(
                    $"Error validando estructura: {ex.Message}",
                    new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" },
                    Array.Empty<string>(),
                    new[] { "TIPO_DOC", "NUM_ID", "DIGITO_VER", "EMPRESA", "EMAIL" });
            }
        }

        #region Legacy Methods (mantener compatibilidad)

        private void ProcessWorksheet(IXLWorksheet worksheet, Dictionary<string, List<string>> resultado)
        {
            // Implementación legacy mantenida para compatibilidad
            try
            {
                int tipoDocCol = 2;
                int numIdCol = 3;
                int digitoVerCol = 4;
                int empresaCol = 5;
                int correoCol = 6;

                var filas = worksheet.RowsUsed().Skip(1);

                foreach (var row in filas)
                {
                    try
                    {
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string empresa = row.Cell(empresaCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        if (!tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrEmpty(numId) || 
                            string.IsNullOrWhiteSpace(correo))
                            continue;

                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);
                        string clave = !string.IsNullOrWhiteSpace(empresa) ? empresa : nitNormalizado;

                        if (string.IsNullOrWhiteSpace(clave))
                            continue;

                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string cleanEmail = email.Trim();
                            if (IsValidEmail(cleanEmail))
                            {
                                if (!resultado.ContainsKey(clave))
                                    resultado[clave] = new List<string>();

                                if (!resultado[clave].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                {
                                    resultado[clave].Add(cleanEmail);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila en hoja {WorksheetName}", worksheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando hoja {WorksheetName}", worksheet.Name);
            }
        }

        private void ProcessWorksheetForMappings(IXLWorksheet worksheet, 
            Dictionary<string, List<string>> empresaCorreos, 
            Dictionary<string, List<string>> nitCorreos)
        {
            // Implementación legacy mantenida para compatibilidad
            try
            {
                int tipoDocCol = 2;
                int numIdCol = 3;
                int digitoVerCol = 4;
                int empresaCol = 5;
                int correoCol = 6;

                var filas = worksheet.RowsUsed().Skip(1);

                foreach (var row in filas)
                {
                    try
                    {
                        string tipoDoc = row.Cell(tipoDocCol).GetString().Trim();
                        string numId = row.Cell(numIdCol).GetString().Trim();
                        string digitoVer = row.Cell(digitoVerCol).GetString().Trim();
                        string empresa = row.Cell(empresaCol).GetString().Trim();
                        string correo = row.Cell(correoCol).GetString().Trim();

                        if (!tipoDoc.Equals("NIT", StringComparison.OrdinalIgnoreCase) || 
                            string.IsNullOrEmpty(numId) || 
                            string.IsNullOrWhiteSpace(correo))
                            continue;

                        string nitCompleto = !string.IsNullOrEmpty(digitoVer) ? $"{numId}-{digitoVer}" : numId;
                        string nitNormalizado = NormalizeNit(nitCompleto);

                        string[] multipleEmails = correo.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string email in multipleEmails)
                        {
                            string cleanEmail = email.Trim();
                            if (IsValidEmail(cleanEmail))
                            {
                                if (!string.IsNullOrWhiteSpace(empresa))
                                {
                                    if (!empresaCorreos.ContainsKey(empresa))
                                        empresaCorreos[empresa] = new List<string>();

                                    if (!empresaCorreos[empresa].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        empresaCorreos[empresa].Add(cleanEmail);
                                }

                                if (!string.IsNullOrWhiteSpace(nitNormalizado))
                                {
                                    if (!nitCorreos.ContainsKey(nitNormalizado))
                                        nitCorreos[nitNormalizado] = new List<string>();

                                    if (!nitCorreos[nitNormalizado].Contains(cleanEmail, StringComparer.OrdinalIgnoreCase))
                                        nitCorreos[nitNormalizado].Add(cleanEmail);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error procesando fila en mapeo para hoja {WorksheetName}", worksheet.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando mapeos en hoja {WorksheetName}", worksheet.Name);
            }
        }

        #endregion
    }
}
