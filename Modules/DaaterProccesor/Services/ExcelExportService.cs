using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.DaaterProccesor.Services;

public class ExcelExportService : IExcelExportService
{
    private readonly IGestLogLogger _logger;    public ExcelExportService(IGestLogLogger logger)
    {
        _logger = logger;
    }

    public async Task ExportarConsolidadoAsync(DataTable sortedData, string outputFilePath, CancellationToken cancellationToken = default)
    {
        await _logger.LoggedOperationAsync("Exportación a Excel", async () =>
        {
            _logger.LogDebug("📁 Iniciando exportación de datos consolidados a: {OutputPath}", outputFilePath);
            _logger.LogDebug("📊 Datos a exportar: {RowCount} filas, {ColumnCount} columnas", 
                sortedData.Rows.Count, sortedData.Columns.Count);

            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("📝 Creando workbook de Excel...");
                using var workbook = new XLWorkbook();
                
                // Configurar hoja GenDesc
                _logger.LogDebug("📋 Configurando hoja 'GenDesc'...");
                var genDescWorksheet = workbook.Worksheets.Add("GenDesc");
                // Insertar la tabla y aplicar estilo
                var table = genDescWorksheet.Cell(1, 1).InsertTable(sortedData);
                table.Theme = XLTableTheme.TableStyleLight9; // Azul estilo tabla claro 9

                // Alineación por defecto: centrar (excepto columna de descripción que se alineará a la izquierda)
                genDescWorksheet.Cells().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                genDescWorksheet.Cells().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                // Evitar wrap por defecto para que AdjustToContents expanda columnas al contenido
                genDescWorksheet.Cells().Style.Alignment.WrapText = false;

                // Aplicar formatos de número / fecha antes de ajustar anchos para evitar "#####" en celdas de fecha
                genDescWorksheet.Column(1).Style.NumberFormat.Format = "yyyy-MM-dd";
                genDescWorksheet.Column(3).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(4).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(12).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(19).Style.NumberFormat.Format = "0";
                genDescWorksheet.Column(20).Style.NumberFormat.Format = "0.00";
                genDescWorksheet.Column(21).Style.NumberFormat.Format = "#,##0.00";
                genDescWorksheet.Column(22).Style.NumberFormat.Format = "$#,##0.00";

                // Ajustar ancho de columnas al contenido (después de aplicar formatos)
                genDescWorksheet.Columns().AdjustToContents();

                // Detectar dinámicamente la columna de descripción buscando en el encabezado (evita usar índice fijo 23)
                int lastColumn = genDescWorksheet.LastColumnUsed()?.ColumnNumber() ?? sortedData.Columns.Count;
                int descriptionColIndex = -1;
                string[] descriptionCandidates = new[]
                {
                    "DESCRIPCION",
                    "DESCRIPCIÓN",
                    "DESCRIPCIONMERCANCIA",
                    "DESCRIPCION MERCANCIA",
                    "DESCRIPCION DE LA MERCANCIA",
                    "DESCRIPCION MERCANCIAS",
                    "MERCANCIA",
                    "DESCRIPCION_DE_LA_MERCANCIA",
                    "DESCRIPCION_MERCANCIA",
                    "DESCRIP"
                };

                var headerRow = genDescWorksheet.Row(1);
                for (int c = 1; c <= lastColumn; c++)
                {
                    var headerText = headerRow.Cell(c).GetString().Trim().ToUpperInvariant();
                    // Normalizar acentos básicos para buscar coincidencias
                    var normalized = headerText.Replace("Á", "A").Replace("É", "E").Replace("Í", "I").Replace("Ó", "O").Replace("Ú", "U").Replace("Ñ", "N").Replace("Ü", "U");
                    var headerNoSpaces = normalized.Replace(" ", "");
                    foreach (var cand in descriptionCandidates)
                    {
                        var candNorm = cand.Replace(" ", "").ToUpperInvariant();
                        if (headerNoSpaces.Contains(candNorm))
                        {
                            descriptionColIndex = c;
                            break;
                        }
                    }
                    if (descriptionColIndex != -1) break;
                }                if (descriptionColIndex == -1)
                {
                    // Fallback: usar la columna 23 si existe, o la última columna usada
                    descriptionColIndex = Math.Min(Math.Max(1, 23), Math.Max(1, lastColumn));
                    _logger.LogWarning("No se detectó columna de descripción en encabezados; aplicando fallback a columna {Col}.", descriptionColIndex);
                }

                // Ajustar anchura razonable para la columna detectada
                genDescWorksheet.Column(descriptionColIndex).Width = Math.Max(genDescWorksheet.Column(descriptionColIndex).Width, 40);

                // No forzar alineación a la izquierda: mantener la alineación por defecto (centrada) en GenDesc
                _logger.LogDebug("No se aplicó alineación Left a la columna de descripción; se mantiene la alineación por defecto de la hoja.");

                // Ajustar altura de filas globalmente (sin cambiar alineaciones)
                genDescWorksheet.Rows().AdjustToContents();

                // Agregar filtros automáticos en los encabezados
                _logger.LogDebug("📊 Agregando filtros automáticos en los encabezados...");
                genDescWorksheet.Range(1, 1, sortedData.Rows.Count + 1, lastColumn).SetAutoFilter();

                cancellationToken.ThrowIfCancellationRequested();
                  // Configurar hoja SpecProd_Interes
                _logger.LogDebug("📋 Configurando hoja 'SpecProd_Interes'...");
                var specProdWorksheet = workbook.Worksheets.Add("SpecProd_Interes");
                specProdWorksheet.Cell(1, 1).Value = "NUMERO DECLARACION";
                specProdWorksheet.Cell(1, 2).Value = "ESTANDAR";
                specProdWorksheet.Cell(1, 3).Value = "DIM MAIN";
                specProdWorksheet.Cell(1, 4).Value = "OTRAS DIM";
                specProdWorksheet.Cell(1, 5).Value = "UNIDADES";
                specProdWorksheet.Cell(1, 6).Value = "FORMA";
                specProdWorksheet.Cell(1, 7).Value = "CANTIDAD";
                specProdWorksheet.Cell(1, 8).Value = "PESO T";
                specProdWorksheet.Cell(1, 9).Value = "DETALLES STD";
                specProdWorksheet.Cell(1, 10).Value = "MES";                specProdWorksheet.Row(1).Style.Font.Bold = true;
                specProdWorksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                specProdWorksheet.Columns().AdjustToContents();

                // Agregar filtros automáticos en los encabezados de SpecProd_Interes
                _logger.LogDebug("📊 Agregando filtros automáticos en hoja 'SpecProd_Interes'...");
                // El rango de filtro debe incluir el header y potencialmente filas de datos (aunque esté vacía)
                specProdWorksheet.Range(1, 1, 1000, 10).SetAutoFilter();
                
                cancellationToken.ThrowIfCancellationRequested();
                
                _logger.LogDebug("💾 Guardando archivo: {OutputPath}", outputFilePath);
                workbook.SaveAs(outputFilePath);
                
                _logger.LogDebug("✅ Archivo Excel exportado exitosamente");
            }, cancellationToken);
        });
    }
}
