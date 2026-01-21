using ClosedXML.Excel;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Export
{
    /// <summary>
    /// Servicio para exportar el historial de ejecuciones de planes de mantenimiento a archivos Excel.
    /// Respeta SRP: responsable únicamente de la exportación a Excel, no de lógica de UI o diálogos.
    /// </summary>
    public class HistorialEjecucionesExportService : IHistorialEjecucionesExportService
    {
        private readonly IGestLogLogger _logger;

        public HistorialEjecucionesExportService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Exporta una colección de ejecuciones de historial a un archivo Excel con estructura matricial.
        /// Filas = Semanas, Columnas = Items del checklist
        /// 1. Resumen Ejecutivo: Vistazo rápido a todos los equipos
        /// 2. Equipo_[Nombre]: Matriz semanal por equipo (una hoja por equipo)
        /// 3. Ayuda: Leyenda de colores y definiciones
        /// </summary>
        public async Task ExportarHistorialAExcelAsync(
            string filePath,
            IEnumerable<EjecucionHistorialItem> items,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[HistorialEjecucionesExportService] Iniciando exportación de historial (matriz consolidada) a {FilePath}", filePath);

                // Validar entrada
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(filePath));

                if (items == null)
                    throw new ArgumentNullException(nameof(items));

                var itemsList = items.ToList();
                if (itemsList.Count == 0)
                    throw new ArgumentException("No hay datos para exportar", nameof(items));

                // Ejecutar exportación en background para no bloquear UI
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();

                    // 1. Hoja de resumen ejecutivo
                    ExportarHojaResumenEjecutivo(workbook, itemsList);

                    // 2. Hojas por equipo (una por cada equipo único)
                    var equiposAgrupados = itemsList
                        .GroupBy(x => x.NombreEquipo)
                        .OrderBy(g => g.Key)
                        .ToList();

                    foreach (var grupoEquipo in equiposAgrupados)
                    {
                        ExportarHojaEquipo(workbook, grupoEquipo.Key, grupoEquipo.ToList());
                    }

                    // 3. Hoja de ayuda
                    ExportarHojaAyuda(workbook);

                    // Guardar archivo
                    workbook.SaveAs(filePath);

                    _logger.LogInformation("[HistorialEjecucionesExportService] Exportación completada: {FilePath} ({EquiposCount} equipos)", filePath, equiposAgrupados.Count);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[HistorialEjecucionesExportService] Exportación cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HistorialEjecucionesExportService] Error al exportar historial a {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// Crea la hoja "Resumen Ejecutivo" con diseño elegante y profesional.
        /// Incluye logo, colores corporativos y estructura visual moderna.
        /// </summary>
        private void ExportarHojaResumenEjecutivo(XLWorkbook workbook, List<EjecucionHistorialItem> items)        {
            var ws = workbook.Worksheets.Add("Resumen Ejecutivo");            // Colores corporativos reales
            string colorVerdePrimario = "#118938";      // Verde principal (encabezados)
            string colorGrisOscuro = "#504F4E";         // Gris oscuro (textos)
            string colorGrisClaro = "#9D9D9C";          // Gris claro (borde)
            string colorSurface = "#F8F9FA";            // Gris muy claro (fondos)// Configurar ancho de columnas
            ws.Column(1).Width = 18;  // Código equipo (parte del logo A1-B1)
            ws.Column(2).Width = 18;  // Nombre Equipo (parte del logo A1-B1)
            ws.Column(3).Width = 25;  // Usuario (ajustable al contenido)
            ws.Column(4).Width = 25;  // Sede (ajustable al contenido)
            ws.Column(5).Width = 20;  // Última Ejecución

            // ===== ENCABEZADO CON LOGO Y TÍTULO =====
            // Insertar logo
            try
            {
                string? assemblyLocation = System.IO.Path.GetDirectoryName(typeof(HistorialEjecucionesExportService).Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var logoPath = System.IO.Path.Combine(assemblyLocation, "..", "..", "..", "Assets", "logo.png");
                    logoPath = System.IO.Path.GetFullPath(logoPath);

                    if (System.IO.File.Exists(logoPath))
                    {
                        var picture = ws.AddPicture(logoPath);
                        picture.MoveTo(ws.Cell(1, 1));
                        // Dimensiones originales: 2209x571 píxeles (relación 3.87:1)
                        // Tamaño moderado para Excel: 60 puntos de altura
                        picture.Height = 60;  // Puntos de altura
                        picture.Width = 232;  // Puntos de ancho (mantiene proporción 3.87:1)

                        // Combinar A1-B1 para el logo
                        ws.Range(1, 1, 1, 2).Merge();
                    }
                }
            }            catch
            {
                // Ignorar si no se puede cargar el logo
            }

            // Pintar fondo blanco en el área del logo (A1-B4)
            for (int row = 1; row <= 4; row++)
            {
                for (int col = 1; col <= 2; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.White;
                }
            }

            // Configurar altura de filas para acomodar el logo
            ws.Row(1).Height = 60;
            ws.Row(2).Height = 20;
            ws.Row(3).Height = 18;            // Título principal (comienza en columna C)
            ws.Cell(1, 3).Value = "RESUMEN DE EQUIPOS";
            ws.Cell(1, 3).Style.Font.Bold = true;
            ws.Cell(1, 3).Style.Font.FontSize = 18;
            ws.Cell(1, 3).Style.Font.FontColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(1, 3, 1, 4).Merge();
            ws.Range(1, 3, 1, 4).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            
            // Código de formulario en E1
            ws.Cell(1, 5).Value = "SST-F-83";
            ws.Cell(1, 5).Style.Font.Bold = true;
            ws.Cell(1, 5).Style.Font.FontSize = 10;
            ws.Cell(1, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(1, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(1, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);            // Subtítulo
            ws.Cell(2, 3).Value = "Historial de Ejecuciones de Mantenimiento";
            ws.Cell(2, 3).Style.Font.Italic = true;
            ws.Cell(2, 3).Style.Font.FontSize = 11;
            ws.Cell(2, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(2, 3, 2, 4).Merge();
            ws.Range(2, 3, 2, 4).Style.Fill.BackgroundColor = XLColor.White;            ws.Cell(2, 5).Value = "Versión 4";
            ws.Cell(2, 5).Style.Font.FontSize = 9;
            ws.Cell(2, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(2, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(2, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            // Metadata
            ws.Cell(3, 3).Value = $"Reporte generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(3, 3).Style.Font.FontSize = 9;
            ws.Cell(3, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisClaro);
            ws.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(3, 3, 3, 4).Merge();
            ws.Range(3, 3, 3, 4).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(3, 5).Value = string.Empty;
            ws.Cell(3, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(3, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(3, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);// ===== ENCABEZADOS DE TABLA =====
            int headerRow = 4;
            ws.Cell(headerRow, 1).Value = "Código";
            ws.Cell(headerRow, 2).Value = "Nombre Equipo";
            ws.Cell(headerRow, 3).Value = "Usuario Asignado";
            ws.Cell(headerRow, 4).Value = "Sede";
            ws.Cell(headerRow, 5).Value = "Última Ejecución";
            var headerRange = ws.Range(headerRow, 1, headerRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.TopBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.LeftBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.RightBorder = XLBorderStyleValues.None;
            ws.Cell(headerRow, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(headerRow, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            ws.Row(headerRow).Height = 24;

            // ===== DATOS DE EQUIPOS =====
            int dataRow = headerRow + 1;
            var equiposAgrupados = items
                .GroupBy(x => x.NombreEquipo)
                .OrderBy(g => g.Key)
                .ToList();

            bool esFilaPar = true;

            foreach (var grupoEquipo in equiposAgrupados)
            {
                var ultimaEjecucion = grupoEquipo
                    .Where(x => x.FechaEjecucion.HasValue)
                    .OrderByDescending(x => x.FechaEjecucion)
                    .FirstOrDefault();

                // Color de fila alternado para legibilidad
                string colorFilaFondo = esFilaPar ? XLColor.White.ToString() : colorSurface;                // Código
                ws.Cell(dataRow, 1).Value = grupoEquipo.First().CodigoEquipo;
                ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.White;
                ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);                // Nombre Equipo
                ws.Cell(dataRow, 2).Value = grupoEquipo.Key;
                ws.Cell(dataRow, 2).Style.Fill.BackgroundColor = XLColor.White;
                ws.Cell(dataRow, 2).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);                // Usuario Asignado
                ws.Cell(dataRow, 3).Value = grupoEquipo.First().UsuarioAsignadoEquipo ?? "";
                ws.Cell(dataRow, 3).Style.Fill.BackgroundColor = XLColor.White;
                ws.Cell(dataRow, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);                // Sede
                ws.Cell(dataRow, 4).Value = grupoEquipo.First().Sede ?? "";
                ws.Cell(dataRow, 4).Style.Fill.BackgroundColor = XLColor.White;
                ws.Cell(dataRow, 4).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);                // Última Ejecución
                if (ultimaEjecucion?.FechaEjecucion.HasValue == true)
                {
                    ws.Cell(dataRow, 5).Value = ultimaEjecucion.FechaEjecucion.Value;
                    ws.Cell(dataRow, 5).Style.DateFormat.Format = "dd/MM/yyyy";
                }
                else
                {
                    ws.Cell(dataRow, 5).Value = "—";
                }
                ws.Cell(dataRow, 5).Style.Fill.BackgroundColor = XLColor.White;
                ws.Cell(dataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(dataRow, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
                // Borde derecho en la columna "Última Ejecución"
                ws.Cell(dataRow, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                ws.Cell(dataRow, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

                dataRow++;
                esFilaPar = !esFilaPar;
            }

            // Asegurar borde derecho en toda la columna 'Última Ejecución' para filas de datos
            int lastDataRow = dataRow - 1;
            if (lastDataRow >= headerRow + 1)
            {
                ws.Range(headerRow + 1, 5, lastDataRow, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                ws.Range(headerRow + 1, 5, lastDataRow, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);
            }

            // ===== PIE DE PÁGINA =====
            ws.Cell(dataRow, 1).Value = $"Total de equipos: {equiposAgrupados.Count}";
            ws.Cell(dataRow, 1).Style.Font.Bold = true;
            ws.Cell(dataRow, 1).Style.Font.FontSize = 10;
            ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);            ws.Range(dataRow, 1, dataRow, 5).Merge();
            ws.Range(dataRow, 1, dataRow, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(dataRow, 1, dataRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(dataRow, 1, dataRow, 5).Style.Font.Bold = true;
            ws.Range(dataRow, 1, dataRow, 5).Style.Font.FontSize = 10;
            ws.Range(dataRow, 1, dataRow, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            
            // Bordes en la última fila (pie de página)
            ws.Range(dataRow, 1, dataRow, 5).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(dataRow, 1, dataRow, 5).Style.Border.BottomBorderColor = XLColor.FromHtml(colorGrisOscuro);            ws.Cell(dataRow, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(dataRow, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            // Ajustar columnas C y D al contenido (sin mínimo restrictivo)
            ws.Column(3).AdjustToContents();  // Usuario - ajusta al contenido
            ws.Column(4).AdjustToContents();  // Sede - ajusta al contenido

            // Congelar encabezados
            ws.SheetView.FreezeRows(4);
        }        /// <summary>
        /// Crea una hoja por equipo con matriz semanal: Filas = Semanas, Columnas = Items del checklist.
        /// Diseño idéntico a la hoja Resumen Ejecutivo: Logo + Título + Subtítulo + Metadata.
        /// </summary>
        private void ExportarHojaEquipo(XLWorkbook workbook, string nombreEquipo, List<EjecucionHistorialItem> ejecucionesEquipo)
        {
            // Validar y sanitizar nombre de hoja (máximo 31 caracteres)
            string nombreHoja = $"Equipo_{nombreEquipo}";
            if (nombreHoja.Length > 31)
                nombreHoja = nombreHoja.Substring(0, 31);            var ws = workbook.Worksheets.Add(nombreHoja);            // Colores corporativos
            string colorVerdePrimario = "#118938";      // Verde principal
            string colorVerdeSecundario = "#5FBB7D";    // Verde suave secundario ([OK] items)
            string colorGrisOscuro = "#504F4E";         // Gris oscuro (textos)
            string colorGrisClaro = "#9D9D9C";          // Gris claro (bordes)

            // Configurar ancho de columnas
            ws.Column(1).Width = 18;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 25;
            ws.Column(4).Width = 25;

            // ===== ENCABEZADO CON LOGO Y TÍTULO =====
            // Insertar logo
            try
            {
                string? assemblyLocation = System.IO.Path.GetDirectoryName(typeof(HistorialEjecucionesExportService).Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var logoPath = System.IO.Path.Combine(assemblyLocation, "..", "..", "..", "Assets", "logo.png");
                    logoPath = System.IO.Path.GetFullPath(logoPath);

                    if (System.IO.File.Exists(logoPath))
                    {
                        var picture = ws.AddPicture(logoPath);
                        picture.MoveTo(ws.Cell(1, 1));
                        picture.Height = 60;
                        picture.Width = 232;
                        ws.Range(1, 1, 1, 2).Merge();
                    }
                }
            }            catch
            {
                // Ignorar si no se puede cargar el logo
            }

            // Pintar fondo blanco en el área del logo (A1-B4)
            for (int logoRow = 1; logoRow <= 4; logoRow++)
            {
                for (int logoCol = 1; logoCol <= 2; logoCol++)
                {
                    ws.Cell(logoRow, logoCol).Style.Fill.BackgroundColor = XLColor.White;
                }
            }

            ws.Row(1).Height = 60;
            ws.Row(2).Height = 20;
            ws.Row(3).Height = 18;

            // Obtener información del equipo
            var ejecucionesOrdenadas = ejecucionesEquipo.OrderBy(x => x.SemanaISO).ToList();
            var ultimaEjecucion = ejecucionesOrdenadas
                .Where(x => x.FechaEjecucion.HasValue)
                .OrderByDescending(x => x.FechaEjecucion)
                .FirstOrDefault();

            // Si no hay ejecuciones para este equipo, mostrar mensaje y salir
            if (ejecucionesOrdenadas.Count == 0)
            {
                ws.Cell(4, 1).Value = "No hay ejecuciones registradas para este equipo.";
                ws.Cell(4, 1).Style.Font.Italic = true;
                ws.Cell(4, 1).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
                ws.Range(4, 1, 4, 5).Merge();
                ws.Range(4, 1, 4, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(4, 1, 4, 5).Style.Fill.BackgroundColor = XLColor.White;
                ws.Columns().AdjustToContents();
                return;
            }            // Título principal (comienza en columna C)
            ws.Cell(1, 3).Value = $"EQUIPO: {nombreEquipo}";
            ws.Cell(1, 3).Style.Font.Bold = true;
            ws.Cell(1, 3).Style.Font.FontSize = 18;
            ws.Cell(1, 3).Style.Font.FontColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Cell(1, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(1, 3, 1, 4).Merge();
            ws.Range(1, 3, 1, 4).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            
            // Código de formulario en E1
            ws.Cell(1, 5).Value = "SST-F-83";
            ws.Cell(1, 5).Style.Font.Bold = true;
            ws.Cell(1, 5).Style.Font.FontSize = 10;
            ws.Cell(1, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(1, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(1, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);            // Subtítulo con información del equipo
            string subtitulo = $"Código: {ejecucionesEquipo.First().CodigoEquipo} | Usuario: {ejecucionesEquipo.First().UsuarioAsignadoEquipo ?? "—"} | Sede: {ejecucionesEquipo.First().Sede ?? "—"}";
            ws.Cell(2, 3).Value = subtitulo;
            ws.Cell(2, 3).Style.Font.Italic = true;
            ws.Cell(2, 3).Style.Font.FontSize = 11;
            ws.Cell(2, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(2, 3, 2, 4).Merge();
            ws.Range(2, 3, 2, 4).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(2, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);
            
            // Versión en E2
            ws.Cell(2, 5).Value = "Versión 4";
            ws.Cell(2, 5).Style.Font.FontSize = 9;
            ws.Cell(2, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(2, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(2, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            // Metadata
            ws.Cell(3, 3).Value = $"Reporte generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(3, 3).Style.Font.FontSize = 9;
            ws.Cell(3, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisClaro);
            ws.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(3, 3, 3, 5).Merge();
            ws.Range(3, 3, 3, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(3, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(3, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);            // ===== ENCABEZADOS DE TABLA MATRICIAL =====
            int headerRow = 4;
            ws.Cell(headerRow, 1).Value = "Semana ISO / Fecha";

            // Obtener todos los items únicos del equipo
            var itemsUnicos = ejecucionesEquipo
                .SelectMany(x => x.DetalleItems ?? new())
                .DistinctBy(x => x.Descripcion)
                .OrderBy(x => x.Descripcion)
                .ToList();

            // Crear encabezados de columnas con items
            int col = 2;
            foreach (var item in itemsUnicos)
            {
                ws.Cell(headerRow, col).Value = item.Descripcion;
                col++;
            }
            int lastCol = col - 1;

            // Formatear encabezado de tabla
            var headerRange = ws.Range(headerRow, 1, headerRow, lastCol);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            headerRange.Style.Border.TopBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.LeftBorder = XLBorderStyleValues.None;
            headerRange.Style.Border.RightBorder = XLBorderStyleValues.None;
            ws.Cell(headerRow, lastCol).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(headerRow, lastCol).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Row(headerRow).Height = 24;

            // Agregar borde superior en el header para el rango solicitado (Limpieza de monitor .. Respaldo de Archivos Digitalos)
            int inicioIndex = itemsUnicos.FindIndex(x => x.Descripcion == "Limpieza de monitor");
            int finIndex = itemsUnicos.FindIndex(x => x.Descripcion == "Respaldo de Archivos Digitales");
            if (inicioIndex >= 0 && finIndex >= 0 && finIndex >= inicioIndex)
            {
                int inicioCol = 2 + inicioIndex;
                int finCol = 2 + finIndex;
                ws.Range(headerRow, inicioCol, headerRow, finCol).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Range(headerRow, inicioCol, headerRow, finCol).Style.Border.TopBorderColor = XLColor.FromHtml(colorGrisOscuro);
            }            // ===== DATOS DE MATRIZ SEMANAL =====
            int dataRow = headerRow + 1;
            string colorNoRealizado = "#FFF3CD";  // Amarillo claro para semanas no realizadas
            
            foreach (var ejecucion in ejecucionesOrdenadas)
            {
                // Detectar si esta semana es "No Realizado" (Estado = 3)
                bool esNoRealizado = ejecucion.Estado == 3;
                string colorFondoFila = esNoRealizado ? colorNoRealizado : XLColor.White.ToString();
                
                ws.Cell(dataRow, 1).Value = $"{ejecucion.AnioISO}-{ejecucion.SemanaISO:D2} / {ejecucion.FechaObjetivo:dd/MM/yyyy}";
                ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorFondoFila);
                ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);

                col = 2;
                int itemsCompletados = 0;
                
                foreach (var itemUnico in itemsUnicos)
                {
                    var itemEnEjecucion = ejecucion.DetalleItems?
                        .FirstOrDefault(x => x.Descripcion == itemUnico.Descripcion);
                    
                    if (itemEnEjecucion != null && itemEnEjecucion.Completado)
                    {
                        // Solo mostrar [OK] para items completados
                        ws.Cell(dataRow, col).Value = "[OK]";
                        ws.Cell(dataRow, col).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdeSecundario);
                        ws.Cell(dataRow, col).Style.Font.FontColor = XLColor.White;
                        ws.Cell(dataRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(dataRow, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        itemsCompletados++;
                    }
                    else
                    {
                        // Para no completados y no disponibles: mostrar solo "-"
                        ws.Cell(dataRow, col).Value = "-";
                        ws.Cell(dataRow, col).Style.Fill.BackgroundColor = XLColor.FromHtml(colorFondoFila);
                        ws.Cell(dataRow, col).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
                        ws.Cell(dataRow, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(dataRow, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    }

                    col++;
                }

                dataRow++;
            }

            // Aplicar bordes específicos en la hoja de equipo
            int lastDataRowEquipo = dataRow - 1;
            if (lastDataRowEquipo >= headerRow + 1)
            {
                // Buscar columna del item "Respaldo de Archivos Digitales"
                int respaldoIndex = itemsUnicos.FindIndex(x => x.Descripcion == "Respaldo de Archivos Digitales");
                if (respaldoIndex >= 0)
                {
                    int respaldoCol = 2 + respaldoIndex;
                    // Borde derecho en la columna de Respaldo para todas las filas de datos
                    ws.Range(headerRow + 1, respaldoCol, lastDataRowEquipo, respaldoCol).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                    ws.Range(headerRow + 1, respaldoCol, lastDataRowEquipo, respaldoCol).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);
                }

                // Borde inferior en la última fila de datos (toda la fila hasta la última columna)
                ws.Range(lastDataRowEquipo, 1, lastDataRowEquipo, lastCol).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                ws.Range(lastDataRowEquipo, 1, lastDataRowEquipo, lastCol).Style.Border.BottomBorderColor = XLColor.FromHtml(colorGrisOscuro);
            }

            // Ajustar columnas
            ws.Columns().AdjustToContents();

            // Congelar encabezados
            ws.SheetView.FreezeRows(4);
        }        /// <summary>
        /// Crea la hoja "Ayuda" con leyenda de símbolos y definiciones con diseño corporativo.
        /// </summary>
        private void ExportarHojaAyuda(XLWorkbook workbook)
        {
            var ws = workbook.Worksheets.Add("Ayuda");

            // Colores corporativos
            string colorVerdePrimario = "#118938";            string colorGrisOscuro = "#504F4E";
            string colorSurface = "#F8F9FA";

            ws.Column(1).Width = 15;
            ws.Column(2).Width = 50;
            ws.Column(5).Width = 15;

            // ===== ENCABEZADO =====
            ws.Cell(1, 1).Value = "GUÍA DE USO";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(1, 1, 1, 2).Merge();
            
            // Código de formulario en E1
            ws.Cell(1, 5).Value = "SST-F-83";
            ws.Cell(1, 5).Style.Font.Bold = true;
            ws.Cell(1, 5).Style.Font.FontSize = 10;
            ws.Cell(1, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(1, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(1, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(1, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            ws.Cell(2, 1).Value = "Historial de Ejecuciones";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 11;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#666666");
            ws.Range(2, 1, 2, 2).Merge();
            
            // Versión en E2
            ws.Cell(2, 5).Value = "Versión 4";
            ws.Cell(2, 5).Style.Font.FontSize = 9;
            ws.Cell(2, 5).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 5).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(2, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, 5).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(2, 5).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, 5).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            int row = 4;            // ===== LEYENDA DE ETIQUETAS Y COLORES =====
            ws.Cell(row, 1).Value = "LEYENDA DE ESTADOS";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(row, 1, row, 2).Merge();
            ws.Row(row).Height = 20;
            row += 2;            ws.Cell(row, 1).Value = "[OK]";
            ws.Cell(row, 2).Value = "Completado - Item fue ejecutado correctamente";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#5FBB7D");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;            ws.Cell(row, 1).Value = "-";
            ws.Cell(row, 2).Value = "No Completado - Item no fue ejecutado o sin registro";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#504F4E");
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            ws.Cell(row, 1).Value = "(Fila Completa Amarilla)";
            ws.Cell(row, 2).Value = "No Realizado - Mantenimiento no ejecutado (generado automáticamente)";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFFBEA");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#856404");
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row += 3;

            // ===== ESTRUCTURA DEL REPORTE =====
            ws.Cell(row, 1).Value = "ESTRUCTURA DEL REPORTE";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(row, 1, row, 2).Merge();
            ws.Row(row).Height = 20;
            row += 2;

            ws.Cell(row, 1).Value = "Resumen Ejecutivo";
            ws.Cell(row, 2).Value = "Vistazo general de todos los equipos con información de contacto y sede";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            row++;

            ws.Cell(row, 1).Value = "Equipo_[Nombre]";
            ws.Cell(row, 2).Value = "Matriz detallada por equipo (Filas=Semanas ISO, Columnas=Items del checklist)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            row++;

            ws.Cell(row, 1).Value = "Ayuda";
            ws.Cell(row, 2).Value = "Esta guía de referencia";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(colorSurface);
            row += 3;

            // ===== DEFINICIONES =====
            ws.Cell(row, 1).Value = "DEFINICIONES";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(row, 1, row, 2).Merge();
            ws.Row(row).Height = 20;
            row += 2;

            // (Se removió la definición de "Cumplimiento (%)" según configuración solicitada)
            
            row++;

            ws.Cell(row, 1).Value = "Semana ISO";
            ws.Cell(row, 2).Value = "Formato YYYY-WW (ej: 2024-03 = Semana 3 de 2024)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Cell(row, 1).Value = "Última Ejecución";
            ws.Cell(row, 2).Value = "Fecha del último mantenimiento registrado para el equipo";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            ws.Columns().AdjustToContents();
        }
    }
}
