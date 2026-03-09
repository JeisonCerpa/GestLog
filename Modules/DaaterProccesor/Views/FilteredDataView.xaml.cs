using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using ClosedXML.Excel;
using GestLog.Modules.DaaterProccesor.Services;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.DaaterProccesor.Views
{
    public partial class FilteredDataView : Window
    {
        private DataTable _originalTable = new DataTable();
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly IGestLogLogger _logger;    public FilteredDataView()
    {
        InitializeComponent();
        _logger = LoggingService.GetLogger<FilteredDataView>();
        _logger.LogDebug("🔍 Iniciando FilteredDataView...");
        _ = LoadDataAsync(); // Fire and forget para el constructor
    }private async Task LoadDataAsync()
        {
            using var scope = _logger.BeginScope("LoadDataAsync");
            _logger.LogDebug("🔍 Iniciando carga de datos consolidados...");
            
            try
            {
                // Selección automática del archivo consolidado más reciente en la carpeta Output
                var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                DataTable? dt = null;
                
                _logger.LogDebug("📁 Buscando archivos en directorio: {OutputDir}", outputDir);
                
                // Verificar si la carpeta Output existe antes de buscar archivos
                if (Directory.Exists(outputDir))
                {
                    var files = Directory.GetFiles(outputDir, "*Consolidado*.xlsx");
                    var file = files.OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault();
                    if (file != null)
                    {
                        _logger.LogInformation("📄 Archivo consolidado encontrado: {FileName}", Path.GetFileName(file));
                        dt = await LoadConsolidatedExcelAsync(file);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ No se encontraron archivos consolidados en {OutputDir}", outputDir);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Directorio Output no existe: {OutputDir}", outputDir);
                }
                  if (dt != null)
                {                    // IMPORTANTE: Guardar datos originales SIN FILTRAR para poder aplicar filtros específicos
                    _logger.LogDebug("💾 Guardando datos originales sin filtrar: {RowCount} registros", dt.Rows.Count);
                    _originalTable = dt; // Datos originales completos
                    
                    // Mostrar los datos originales en el DataGrid para visualización
                    _logger.LogDebug("🔧 Mostrando datos originales para visualización: {RowCount} registros...", dt.Rows.Count);
                    UpdateDataGridDisplay(dt);
                    
                    _logger.LogInformation("✅ Datos cargados correctamente: {TotalCount} registros originales disponibles para filtrado", dt.Rows.Count);
                }
                else
                {
                    UpdateDataGridDisplay(new DataTable());
                    
                    _logger.LogWarning("⚠️ No se pudieron cargar datos consolidados");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar datos consolidados");
                
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }        private void UpdateRecordCount(int count)
        {
            // txtRecordCount.Text = $"Registros: {count:N0}";
            _logger.LogDebug("📊 Actualizado conteo de registros: {Count:N0}", count);
        }        /// <summary>
        /// Configura las columnas del DataGrid automáticamente con mejor presentación
        /// </summary>
        private void FilteredDataGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
        {
            // Mejorar los encabezados de columna
            if (e.PropertyName.Contains("_"))
            {
                e.Column.Header = e.PropertyName.Replace("_", " ");
            }

            // Configurar ancho de columnas según el tipo de datos
            if (e.PropertyType == typeof(DateTime) || e.PropertyType == typeof(DateTime?))
            {
                e.Column.Width = new System.Windows.Controls.DataGridLength(120);
            }
            else if (e.PropertyType == typeof(decimal) || e.PropertyType == typeof(double) || 
                     e.PropertyType == typeof(float) || e.PropertyType == typeof(int))
            {
                e.Column.Width = new System.Windows.Controls.DataGridLength(100);
                
                // Formatear números con separadores de miles
                if (e.Column is System.Windows.Controls.DataGridTextColumn textColumn)
                {
                    textColumn.Binding.StringFormat = "{0:N2}";
                }
            }
            else if (e.PropertyName.ToUpper().Contains("DESCRIPCION") || 
                     e.PropertyName.ToUpper().Contains("SIGNIFICADO"))
            {
                e.Column.Width = new System.Windows.Controls.DataGridLength(200);
            }
            else
            {
                e.Column.Width = System.Windows.Controls.DataGridLength.Auto;
            }

            // Limitar número máximo de columnas para mejor rendimiento
            if (FilteredDataGrid.Columns.Count > 20)
            {
                e.Cancel = true;
            }
        }        /// <summary>
        /// Actualiza el DataGrid con los datos filtrados y muestra/oculta el mensaje de "sin datos"
        /// </summary>
        private void UpdateDataGridDisplay(DataTable data)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (data != null && data.Rows.Count > 0)
                    {
                        FilteredDataGrid.ItemsSource = data.DefaultView;
                        FilteredDataGrid.Visibility = Visibility.Visible;
                        txtNoData.Visibility = Visibility.Collapsed;
                        
                        // Actualizar el mensaje de estado
                        txtRecordCount.Text = $"📊 {data.Rows.Count:N0} registros cargados - Datos listos para aplicar filtros especializados";
                    }
                    else
                    {
                        FilteredDataGrid.ItemsSource = null;
                        FilteredDataGrid.Visibility = Visibility.Collapsed;
                        txtNoData.Visibility = Visibility.Visible;
                        
                        txtRecordCount.Text = "Cargue datos consolidados para aplicar filtros especializados";
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error actualizando display del DataGrid");
                Dispatcher.Invoke(() =>
                {
                    txtRecordCount.Text = "Error al mostrar datos";
                });
            }
        }

        private async Task<DataTable?> LoadConsolidatedExcelAsync(string filePath)
        {
            using var scope = _logger.BeginScope("LoadConsolidatedExcelAsync");
            _logger.LogInformation("📂 Iniciando carga de archivo Excel consolidado: {FilePath}", filePath);
            
            return await Task.Run(() =>
            {
                try
                {
                    var dt = new DataTable();
                    var fileInfo = new FileInfo(filePath);
                    _logger.LogDebug("📊 Información del archivo: {FileName} - Tamaño: {FileSize:N0} bytes", 
                        fileInfo.Name, fileInfo.Length);
                    
                    using (var workbook = new XLWorkbook(filePath))
                    {
                        _logger.LogDebug("📋 Archivo Excel abierto correctamente");
                        var worksheet = workbook.Worksheets.Cast<IXLWorksheet>().FirstOrDefault();
                        
                        if (worksheet == null) 
                        {
                            _logger.LogWarning("⚠️ No se encontraron hojas de trabajo en el archivo Excel");
                            return null;
                        }
                        
                        _logger.LogDebug("📄 Procesando hoja de trabajo: {WorksheetName}", worksheet.Name);
                        var usedRows = worksheet.RowsUsed().ToList();
                        _logger.LogDebug("📊 Filas utilizadas encontradas: {RowCount}", usedRows.Count);
                        
                        bool firstRow = true;
                        int processedRows = 0;
                        
                        foreach (var row in usedRows)
                        {
                            if (firstRow)
                            {
                                // Procesar encabezados
                                _logger.LogDebug("📝 Procesando encabezados...");
                                var headers = row.Cells().Select(c => c.GetString()).ToList();
                                foreach (var header in headers)
                                {
                                    dt.Columns.Add(header);
                                }
                                _logger.LogDebug("✅ Encabezados procesados: {ColumnCount} columnas", dt.Columns.Count);
                                firstRow = false;
                            }
                            else
                            {
                                // Procesar datos
                                var values = row.Cells(1, dt.Columns.Count).Select(c => c.GetValue<string>()).ToArray();
                                dt.Rows.Add(values);
                                processedRows++;
                                
                                // Log de progreso cada 1000 filas
                                if (processedRows % 1000 == 0)
                                {
                                    _logger.LogDebug("🔄 Progreso de carga: {ProcessedRows} filas procesadas", processedRows);
                                }
                            }
                        }
                        
                        _logger.LogInformation("✅ Carga completada exitosamente: {TotalRows} filas de datos, {ColumnCount} columnas", 
                            processedRows, dt.Columns.Count);
                    }
                    
                    return dt;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error al cargar archivo Excel consolidado: {FilePath}", filePath);
                    throw;
                }
            });
        }

        /// <summary>
        /// Aplica filtro para ACEROS ESPECIALES y exporta a Excel
        /// </summary>
        private async void ExportAcerosEspeciales_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogUserInteraction("⚡", "ExportAcerosEspeciales", "Usuario solicitó exportación de ACEROS ESPECIALES");
            
            try
            {
                // Verificar que hay datos cargados
                if (_originalTable == null || _originalTable.Rows.Count == 0)
                {
                    _logger.LogWarning("⚠️ No hay datos cargados para filtrar");
                    System.Windows.MessageBox.Show("No hay datos cargados. Por favor, espere a que termine la carga.", "Sin datos", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger.LogDebug("🔍 Iniciando filtrado de ACEROS ESPECIALES...");
                  // Crear servicio de filtrado y aplicar filtro específico
                var filterService = new ConsolidatedFilterService(_logger);
                var acerosEspeciales = filterService.FilterAcerosEspeciales(_originalTable);
                
                if (acerosEspeciales.Rows.Count == 0)
                {
                    _logger.LogWarning("⚠️ No se encontraron registros de ACEROS ESPECIALES");
                    System.Windows.MessageBox.Show("No se encontraron registros con partida arancelaria 7225400000 (ACEROS ESPECIALES).", "Sin resultados", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                _logger.LogInformation("✅ ACEROS ESPECIALES encontrados: {Count} registros", acerosEspeciales.Rows.Count);                // Aplicar filtro de LAMINAS también
                var laminas = filterService.FilterLaminas(_originalTable);
                _logger.LogInformation("✅ LAMINAS encontradas: {Count} registros", laminas.Rows.Count);
                  // Aplicar filtro de ROLLOS también
                var rollos = filterService.FilterRollos(_originalTable);
                _logger.LogInformation("✅ ROLLOS encontrados: {Count} registros", rollos.Rows.Count);
                  // Aplicar filtro de ANGULOS también
                var angulos = filterService.FilterAngulos(_originalTable);
                _logger.LogInformation("✅ ANGULOS encontrados: {Count} registros", angulos.Rows.Count);
                  // Aplicar filtro de CANALES también
                var canales = filterService.FilterCanales(_originalTable);
                _logger.LogInformation("✅ CANALES encontrados: {Count} registros", canales.Rows.Count);
                
                // Aplicar filtro de VIGAS también
                var vigas = filterService.FilterVigas(_originalTable);
                _logger.LogInformation("✅ VIGAS encontradas: {Count} registros", vigas.Rows.Count);                // Confirmar exportación
                var totalRecords = acerosEspeciales.Rows.Count + laminas.Rows.Count + rollos.Rows.Count + angulos.Rows.Count + canales.Rows.Count + vigas.Rows.Count;
                var message = $"Se encontraron:\n" +
                             $"• CONSOLIDADO: {_originalTable.Rows.Count:N0} registros (datos completos sin filtrar)\n" +
                             $"• ACEROS ESPECIALES: {acerosEspeciales.Rows.Count:N0} registros\n" +
                             $"• LAMINAS: {laminas.Rows.Count:N0} registros\n" +
                             $"• ROLLOS: {rollos.Rows.Count:N0} registros\n" +
                             $"• ANGULOS: {angulos.Rows.Count:N0} registros\n" +
                             $"• CANALES: {canales.Rows.Count:N0} registros\n" +
                             $"• VIGAS: {vigas.Rows.Count:N0} registros\n" +
                             $"• TOTAL FILTRADOS: {totalRecords:N0} registros\n\n" +
                             "¿Desea exportar estos datos a Excel (7 hojas)?";                var result = System.Windows.MessageBox.Show(
                    message,
                    "🏗️ Filtros Productos Acero - Generar 7 Hojas Excel",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ExportMultipleSheetToExcelAsync(_originalTable, acerosEspeciales, laminas, rollos, angulos, canales, vigas);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exportando ACEROS ESPECIALES");
                System.Windows.MessageBox.Show($"Error al exportar ACEROS ESPECIALES: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        /// <summary>
        /// Exporta datos de ACEROS ESPECIALES a Excel con formato específico
        /// </summary>
        private async Task ExportAcerosEspecialesToExcelAsync(DataTable acerosEspeciales)
        {
            using var scope = _logger.BeginScope("ExportAcerosEspecialesToExcel");
            _logger.LogInformation("📤 Iniciando exportación de ACEROS ESPECIALES: {RowCount} registros", acerosEspeciales.Rows.Count);
            
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    FileName = $"ACEROS_ESPECIALES_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Guardar ACEROS ESPECIALES"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _logger.LogDebug("📁 Archivo seleccionado: {FileName}", saveFileDialog.FileName);
                    _cancellationTokenSource = new CancellationTokenSource();
                    
                    await Task.Run(() =>
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add("ACEROS ESPECIALES");
                            
                            // ✅ PASO 1: Agregar título y fecha PRIMERO (insertar filas al principio)
                            worksheet.Row(1).InsertRowsAbove(2);
                            
                            var titleCell = worksheet.Cell(1, 1);
                            titleCell.Value = $"ACEROS ESPECIALES - Partida 7225400000";
                            titleCell.Style.Font.Bold = true;
                            titleCell.Style.Font.FontSize = 14;
                            titleCell.Style.Fill.BackgroundColor = XLColor.LightGray;
                            worksheet.Range(1, 1, 1, acerosEspeciales.Columns.Count).Merge();

                            var dateCell = worksheet.Cell(2, 1);
                            dateCell.Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                            dateCell.Style.Font.Italic = true;
                            worksheet.Range(2, 1, 2, acerosEspeciales.Columns.Count).Merge();
                            
                            // ✅ PASO 2: Agregar encabezados (ahora en fila 3)
                            _logger.LogDebug("📝 Agregando encabezados...");
                            for (int col = 0; col < acerosEspeciales.Columns.Count; col++)
                            {
                                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                var cell = worksheet.Cell(3, col + 1);
                                cell.Value = acerosEspeciales.Columns[col].ColumnName;
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = XLColor.DarkGreen;
                                cell.Style.Font.FontColor = XLColor.White;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            }

                            // ✅ PASO 3: Agregar datos (ahora empezando en fila 4)
                            _logger.LogDebug("📋 Agregando {RowCount} filas de datos...", acerosEspeciales.Rows.Count);
                            for (int row = 0; row < acerosEspeciales.Rows.Count; row++)
                            {
                                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                                for (int col = 0; col < acerosEspeciales.Columns.Count; col++)
                                {
                                    worksheet.Cell(row + 4, col + 1).Value = acerosEspeciales.Rows[row][col]?.ToString() ?? "";
                                }
                            }

                            // ✅ PASO 4: Aplicar formato
                            _logger.LogDebug("🎨 Aplicando formato...");
                            worksheet.ColumnsUsed().AdjustToContents();
                              
                            // ✅ PASO 5: Aplicar filtros automáticos (DESPUÉS de insertar filas)
                            var range = worksheet.Range(3, 1, acerosEspeciales.Rows.Count + 3, acerosEspeciales.Columns.Count);
                            range.SetAutoFilter();

                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            _logger.LogDebug("💾 Guardando archivo...");
                            workbook.SaveAs(saveFileDialog.FileName);
                        }
                    }, _cancellationTokenSource.Token);

                    _logger.LogInformation("✅ ACEROS ESPECIALES exportados exitosamente: {FileName}", saveFileDialog.FileName);
                    
                    var openResult = System.Windows.MessageBox.Show(
                        $"ACEROS ESPECIALES exportados exitosamente:\n{saveFileDialog.FileName}\n\n¿Desea abrir el archivo ahora?",
                        "Exportación exitosa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        _logger.LogUserInteraction("🔗", "OpenAcerosEspeciales", "Usuario abrió archivo: {FileName}", saveFileDialog.FileName);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏹️ Exportación de ACEROS ESPECIALES cancelada");
                System.Windows.MessageBox.Show("Exportación cancelada.", "Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exportando ACEROS ESPECIALES");
                throw;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }        /// <summary>
        /// Exporta múltiples hojas (CONSOLIDADO, ACEROS ESPECIALES, LAMINAS, ROLLOS, ANGULOS, CANALES y VIGAS) a un solo archivo Excel
        /// </summary>
        private async Task ExportMultipleSheetToExcelAsync(DataTable consolidado, DataTable acerosEspeciales, DataTable laminas, DataTable rollos, DataTable angulos, DataTable canales, DataTable vigas)
        {
            using var scope = _logger.BeginScope("ExportMultipleSheetToExcel");
            _logger.LogInformation("📤 Iniciando exportación múltiple: CONSOLIDADO ({ConsolidadoCount}), ACEROS ESPECIALES ({AcerosCount}), LAMINAS ({LaminasCount}), ROLLOS ({RollosCount}), ANGULOS ({AngulosCount}), CANALES ({CanalesCount}) y VIGAS ({VigasCount})", 
                consolidado.Rows.Count, acerosEspeciales.Rows.Count, laminas.Rows.Count, rollos.Rows.Count, angulos.Rows.Count, canales.Rows.Count, vigas.Rows.Count);
            
            try
            {                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                    FileName = $"CONSOLIDADO_ACEROS_LAMINAS_ROLLOS_ANGULOS_CANALES_VIGAS_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    Title = "Guardar CONSOLIDADO, ACEROS ESPECIALES, LAMINAS, ROLLOS, ANGULOS, CANALES y VIGAS"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _logger.LogDebug("📁 Archivo seleccionado: {FileName}", saveFileDialog.FileName);
                    _cancellationTokenSource = new CancellationTokenSource();
                    
                    await Task.Run(() =>
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            // HOJA 1: CONSOLIDADO (TODOS LOS DATOS SIN FILTRAR)
                            _logger.LogDebug("📝 Creando hoja CONSOLIDADO...");
                            var worksheetConsolidado = workbook.Worksheets.Add("CONSOLIDADO");
                            CreateWorksheetFromDataTable(worksheetConsolidado, consolidado, "CONSOLIDADO - Todos los datos sin filtrar", XLColor.DarkBlue);
                            
                            // HOJA 2: ACEROS ESPECIALES
                            _logger.LogDebug("📝 Creando hoja ACEROS ESPECIALES...");
                            var worksheetEspeciales = workbook.Worksheets.Add("ACEROS ESPECIALES");
                            CreateWorksheetFromDataTable(worksheetEspeciales, acerosEspeciales, "ACEROS ESPECIALES - Partida 7225400000", XLColor.DarkGreen);
                              // HOJA 3: LAMINAS
                            _logger.LogDebug("📝 Creando hoja LAMINAS...");
                            var worksheetLaminas = workbook.Worksheets.Add("LAMINAS");
                            CreateWorksheetFromDataTable(worksheetLaminas, laminas, "LAMINAS - Productos planos laminados en caliente", XLColor.DarkCyan);                            // HOJA 4: ROLLOS
                            _logger.LogDebug("📝 Creando hoja ROLLOS...");
                            var worksheetRollos = workbook.Worksheets.Add("ROLLOS");
                            CreateWorksheetFromDataTable(worksheetRollos, rollos, "ROLLOS - Productos planos laminados en caliente", XLColor.DarkOrange);                            // HOJA 5: ANGULOS
                            _logger.LogDebug("📝 Creando hoja ANGULOS...");
                            var worksheetAngulos = workbook.Worksheets.Add("ANGULOS");
                            CreateWorksheetFromDataTable(worksheetAngulos, angulos, "ANGULOS - Perfiles en L", XLColor.DarkRed);                            // HOJA 6: CANALES
                            _logger.LogDebug("📝 Creando hoja CANALES...");
                            var worksheetCanales = workbook.Worksheets.Add("CANALES");
                            CreateWorksheetFromDataTable(worksheetCanales, canales, "CANALES - Perfiles en U", XLColor.DarkViolet);

                            // HOJA 7: VIGAS
                            _logger.LogDebug("📝 Creando hoja VIGAS...");
                            var worksheetVigas = workbook.Worksheets.Add("VIGAS");
                            CreateWorksheetFromDataTable(worksheetVigas, vigas, "VIGAS - Perfiles en H e I", XLColor.DarkSlateGray);

                            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                            _logger.LogDebug("💾 Guardando archivo con múltiples hojas...");
                            workbook.SaveAs(saveFileDialog.FileName);
                        }
                    }, _cancellationTokenSource.Token);

                    _logger.LogInformation("✅ Archivo Excel con múltiples hojas exportado exitosamente: {FileName}", saveFileDialog.FileName);                    var openResult = System.Windows.MessageBox.Show(
                        $"Archivo exportado exitosamente:\n{saveFileDialog.FileName}\n\n" +
                        $"Hojas creadas:\n• CONSOLIDADO ({consolidado.Rows.Count:N0} registros)\n• ACEROS ESPECIALES ({acerosEspeciales.Rows.Count:N0} registros)\n• LAMINAS ({laminas.Rows.Count:N0} registros)\n• ROLLOS ({rollos.Rows.Count:N0} registros)\n• ANGULOS ({angulos.Rows.Count:N0} registros)\n• CANALES ({canales.Rows.Count:N0} registros)\n• VIGAS ({vigas.Rows.Count:N0} registros)\n\n" +
                        "¿Desea abrir el archivo ahora?",
                        "Exportación exitosa",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        _logger.LogUserInteraction("🔗", "OpenMultipleSheetFile", "Usuario abrió archivo: {FileName}", saveFileDialog.FileName);
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveFileDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⏹️ Exportación múltiple cancelada");
                System.Windows.MessageBox.Show("Exportación cancelada.", "Cancelado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exportando múltiples hojas");
                throw;
            }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Método auxiliar para crear una hoja de Excel desde un DataTable
        /// </summary>
        private void CreateWorksheetFromDataTable(IXLWorksheet worksheet, DataTable data, string title, XLColor headerColor)
        {
            _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
            
            // Agregar título en la hoja (insertar filas al principio)
            worksheet.Row(1).InsertRowsAbove(2);
            
            var titleCell = worksheet.Cell(1, 1);
            titleCell.Value = title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 14;
            titleCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            worksheet.Range(1, 1, 1, data.Columns.Count).Merge();

            var dateCell = worksheet.Cell(2, 1);
            dateCell.Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            dateCell.Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, data.Columns.Count).Merge();

            // Agregar encabezados con formato
            _logger.LogDebug("📝 Agregando encabezados para {Title}...", title);
            for (int col = 0; col < data.Columns.Count; col++)
            {
                _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
                var cell = worksheet.Cell(3, col + 1);
                cell.Value = data.Columns[col].ColumnName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Agregar datos
            _logger.LogDebug("📋 Agregando {RowCount} filas de datos para {Title}...", data.Rows.Count, title);
            for (int row = 0; row < data.Rows.Count; row++)
            {
                _cancellationTokenSource?.Token.ThrowIfCancellationRequested();
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    worksheet.Cell(row + 4, col + 1).Value = data.Rows[row][col]?.ToString() ?? "";
                }
            }

            // Aplicar formato
            _logger.LogDebug("🎨 Aplicando formato para {Title}...", title);
            worksheet.ColumnsUsed().AdjustToContents();
            
            // Aplicar filtros automáticos
            var range = worksheet.Range(3, 1, data.Rows.Count + 3, data.Columns.Count);
            range.SetAutoFilter();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}

