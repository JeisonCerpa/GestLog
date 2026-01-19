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
            var ws = workbook.Worksheets.Add("Resumen Ejecutivo");

            // Colores corporativos reales
            string colorVerdePrimario = "#118938";      // Verde principal (encabezados)
            string colorVerdeSecundario = "#2B8E3F";    // Verde secundario (acentos)
            string colorGrisOscuro = "#504F4E";         // Gris oscuro (textos)
            string colorGrisClaro = "#9D9D9C";          // Gris claro (borde)
            string colorSurface = "#F8F9FA";            // Gris muy claro (fondos)            // Configurar ancho de columnas
            ws.Column(1).Width = 18;  // Código equipo (parte del logo A1-B1)
            ws.Column(2).Width = 18;  // Nombre Equipo (parte del logo A1-B1)
            ws.Column(3).Width = 25;  // Usuario (ajustable al contenido)
            ws.Column(4).Width = 25;  // Sede (ajustable al contenido)
            ws.Column(5).Width = 20;  // Última Ejecución
            ws.Column(6).Width = 16;  // Cumplimiento

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
            ws.Range(1, 3, 1, 6).Merge();
            ws.Range(1, 3, 1, 6).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(1, 3).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Cell(1, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(1, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);            // Subtítulo
            ws.Cell(2, 3).Value = "Historial de Ejecuciones y Cumplimiento de Mantenimiento";
            ws.Cell(2, 3).Style.Font.Italic = true;
            ws.Cell(2, 3).Style.Font.FontSize = 11;
            ws.Cell(2, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            ws.Cell(2, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(2, 3, 2, 6).Merge();
            ws.Range(2, 3, 2, 6).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(2, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(2, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);            // Metadata
            ws.Cell(3, 3).Value = $"Reporte generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Cell(3, 3).Style.Font.FontSize = 9;
            ws.Cell(3, 3).Style.Font.FontColor = XLColor.FromHtml(colorGrisClaro);
            ws.Cell(3, 3).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(3, 3, 3, 6).Merge();
            ws.Range(3, 3, 3, 6).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(3, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(3, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);// ===== ENCABEZADOS DE TABLA =====
            int headerRow = 4;
            ws.Cell(headerRow, 1).Value = "Código";
            ws.Cell(headerRow, 2).Value = "Nombre Equipo";
            ws.Cell(headerRow, 3).Value = "Usuario Asignado";
            ws.Cell(headerRow, 4).Value = "Sede";
            ws.Cell(headerRow, 5).Value = "Última Ejecución";
            ws.Cell(headerRow, 6).Value = "Cumplimiento";            var headerRange = ws.Range(headerRow, 1, headerRow, 6);
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
            ws.Cell(headerRow, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(headerRow, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

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

                var totalItems = grupoEquipo.SelectMany(x => x.DetalleItems ?? new()).Count();
                var completados = grupoEquipo.SelectMany(x => x.DetalleItems ?? new()).Count(x => x.Completado);
                double cumplimiento = totalItems > 0 ? (double)completados / totalItems * 100 : 0;                // Color de fila alternado para legibilidad
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

                // Cumplimiento (%) - CON COLORES CORPORATIVOS
                ws.Cell(dataRow, 6).Value = cumplimiento / 100;
                ws.Cell(dataRow, 6).Style.NumberFormat.Format = "0.00%";
                ws.Cell(dataRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(dataRow, 6).Style.Font.Bold = true;
                ws.Cell(dataRow, 6).Style.Font.FontSize = 10;                // Color según cumplimiento - Escala con verde corporativo
                if (cumplimiento >= 90)
                {
                    ws.Cell(dataRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdeSecundario);
                    ws.Cell(dataRow, 6).Style.Font.FontColor = XLColor.White;
                }
                else if (cumplimiento >= 70)
                {
                    ws.Cell(dataRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F4C430"); // Dorado/Amarillo
                    ws.Cell(dataRow, 6).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
                }
                else
                {
                    ws.Cell(dataRow, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#E74C3C"); // Rojo suave
                    ws.Cell(dataRow, 6).Style.Font.FontColor = XLColor.White;
                }
                ws.Cell(dataRow, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
                ws.Cell(dataRow, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

                dataRow++;
                esFilaPar = !esFilaPar;
            }            // ===== PIE DE PÁGINA =====
            ws.Cell(dataRow, 1).Value = $"Total de equipos: {equiposAgrupados.Count}";
            ws.Cell(dataRow, 1).Style.Font.Bold = true;
            ws.Cell(dataRow, 1).Style.Font.FontSize = 10;
            ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.White;
            ws.Cell(dataRow, 1).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);            ws.Range(dataRow, 1, dataRow, 6).Merge();
            ws.Range(dataRow, 1, dataRow, 6).Style.Fill.BackgroundColor = XLColor.White;
            ws.Range(dataRow, 1, dataRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(dataRow, 1, dataRow, 6).Style.Font.Bold = true;
            ws.Range(dataRow, 1, dataRow, 6).Style.Font.FontSize = 10;
            ws.Range(dataRow, 1, dataRow, 6).Style.Font.FontColor = XLColor.FromHtml(colorGrisOscuro);
            
            // Bordes en la última fila (pie de página)
            ws.Range(dataRow, 1, dataRow, 6).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.Range(dataRow, 1, dataRow, 6).Style.Border.BottomBorderColor = XLColor.FromHtml(colorGrisOscuro);            ws.Cell(dataRow, 6).Style.Border.RightBorder = XLBorderStyleValues.Thin;
            ws.Cell(dataRow, 6).Style.Border.RightBorderColor = XLColor.FromHtml(colorGrisOscuro);

            // Ajustar columnas C y D al contenido (sin mínimo restrictivo)
            ws.Column(3).AdjustToContents();  // Usuario - ajusta al contenido
            ws.Column(4).AdjustToContents();  // Sede - ajusta al contenido

            // Congelar encabezados
            ws.SheetView.FreezeRows(4);
        }

        /// <summary>
        /// Crea una hoja por equipo con matriz semanal: Filas = Semanas, Columnas = Items del checklist.
        /// </summary>
        private void ExportarHojaEquipo(XLWorkbook workbook, string nombreEquipo, List<EjecucionHistorialItem> ejecucionesEquipo)
        {
            // Validar y sanitizar nombre de hoja (máximo 31 caracteres)
            string nombreHoja = $"Equipo_{nombreEquipo}";
            if (nombreHoja.Length > 31)
                nombreHoja = nombreHoja.Substring(0, 31);

            var ws = workbook.Worksheets.Add(nombreHoja);

            // Encabezado
            ws.Cell(1, 1).Value = $"EQUIPO: {nombreEquipo}";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 14;
            ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E78");
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            ws.Range(1, 1, 1, 12).Merge();

            // Métricas del equipo
            int row = 3;
            var ejecucionesOrdenadas = ejecucionesEquipo.OrderBy(x => x.SemanaISO).ToList();
            var primeraEjecucion = ejecucionesOrdenadas.FirstOrDefault();
            var ultimaEjecucion = ejecucionesOrdenadas
                .Where(x => x.FechaEjecucion.HasValue)
                .OrderByDescending(x => x.FechaEjecucion)
                .FirstOrDefault();

            ws.Cell(row, 1).Value = "Última Ejecución:";
            ws.Cell(row, 2).Value = ultimaEjecucion?.FechaEjecucion?.ToString("dd/MM/yyyy") ?? "Nunca";
            row++;

            var totalItems = ejecucionesEquipo.SelectMany(x => x.DetalleItems ?? new()).Count();
            var completados = ejecucionesEquipo.SelectMany(x => x.DetalleItems ?? new()).Count(x => x.Completado);
            double cumplimientoGlobal = totalItems > 0 ? (double)completados / totalItems * 100 : 0;

            ws.Cell(row, 1).Value = "Cumplimiento Global:";
            ws.Cell(row, 2).Value = cumplimientoGlobal / 100;
            ws.Cell(row, 2).Style.NumberFormat.Format = "0.00%";
            row += 2;

            // Encabezado de tabla matricial
            ws.Cell(row, 1).Value = "Semana ISO";

            // Obtener todos los items únicos del equipo para usarlos como encabezados de columna
            var itemsUnicos = ejecucionesEquipo
                .SelectMany(x => x.DetalleItems ?? new())
                .DistinctBy(x => x.Descripcion)
                .OrderBy(x => x.Descripcion)
                .ToList();

            // Crear encabezados de columnas con items
            int col = 2;
            foreach (var item in itemsUnicos)
            {
                ws.Cell(row, col).Value = item.Descripcion;
                col++;
            }
            ws.Cell(row, col).Value = "% Cumplimiento";

            // Formatear encabezado de tabla
            var headerRange = ws.Range(row, 1, row, col);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#000000");
            headerRange.Style.Font.FontColor = XLColor.White;

            // Datos de matriz semanal
            row++;
            foreach (var ejecucion in ejecucionesOrdenadas)
            {
                ws.Cell(row, 1).Value = $"{ejecucion.AnioISO}-{ejecucion.SemanaISO:D2}";

                col = 2;
                int itemsCompletados = 0;

                foreach (var itemUnico in itemsUnicos)
                {
                    var itemEnEjecucion = ejecucion.DetalleItems?
                        .FirstOrDefault(x => x.Descripcion == itemUnico.Descripcion);

                    if (itemEnEjecucion != null)
                    {
                        // Asignar símbolo según estado
                        string simbolo = itemEnEjecucion.Completado ? "✅"
                            : (!string.IsNullOrWhiteSpace(itemEnEjecucion.Observacion) ? "⚠️" : "❌");
                        ws.Cell(row, col).Value = simbolo;

                        // Colorear celda
                        if (itemEnEjecucion.Completado)
                            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#2B8E3F");
                        else if (!string.IsNullOrWhiteSpace(itemEnEjecucion.Observacion))
                            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9B233");
                        else
                            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#C0392B");

                        ws.Cell(row, col).Style.Font.FontColor = itemEnEjecucion.Completado || !string.IsNullOrWhiteSpace(itemEnEjecucion.Observacion)
                            ? XLColor.Black : XLColor.White;

                        if (itemEnEjecucion.Completado)
                            itemsCompletados++;
                    }
                    else
                    {
                        ws.Cell(row, col).Value = "—";
                        ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#9E9E9E");
                        ws.Cell(row, col).Style.Font.FontColor = XLColor.White;
                    }

                    col++;
                }

                // Calcular % de cumplimiento de la semana
                double cumplimientoSemana = itemsUnicos.Count > 0 ? (double)itemsCompletados / itemsUnicos.Count * 100 : 0;
                ws.Cell(row, col).Value = cumplimientoSemana / 100;
                ws.Cell(row, col).Style.NumberFormat.Format = "0.00%";

                // Color para % de cumplimiento
                if (cumplimientoSemana >= 90)
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#2B8E3F");
                else if (cumplimientoSemana >= 70)
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9B233");
                else
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#C0392B");

                ws.Cell(row, col).Style.Font.FontColor = cumplimientoSemana >= 70 ? XLColor.Black : XLColor.White;
                ws.Cell(row, col).Style.Font.Bold = true;

                row++;
            }

            // Fila de totales por item
            ws.Cell(row, 1).Value = "Total (%)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

            int colTotal = 2;
            foreach (var itemUnico in itemsUnicos)
            {
                var completadosItem = ejecucionesEquipo
                    .SelectMany(x => x.DetalleItems ?? new())
                    .Where(x => x.Descripcion == itemUnico.Descripcion && x.Completado)
                    .Count();

                var totalItem = ejecucionesEquipo
                    .SelectMany(x => x.DetalleItems ?? new())
                    .Where(x => x.Descripcion == itemUnico.Descripcion)
                    .Count();

                double porcentajeItem = totalItem > 0 ? (double)completadosItem / totalItem * 100 : 0;
                ws.Cell(row, colTotal).Value = porcentajeItem / 100;
                ws.Cell(row, colTotal).Style.NumberFormat.Format = "0.00%";
                ws.Cell(row, colTotal).Style.Font.Bold = true;
                ws.Cell(row, colTotal).Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F2F2");

                colTotal++;
            }

            // Ajustar columnas y congelar encabezados
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);
        }

        /// <summary>
        /// Crea la hoja "Ayuda" con leyenda de símbolos y definiciones con diseño corporativo.
        /// </summary>
        private void ExportarHojaAyuda(XLWorkbook workbook)
        {
            var ws = workbook.Worksheets.Add("Ayuda");

            // Colores corporativos
            string colorVerdePrimario = "#118938";
            string colorSurface = "#F8F9FA";

            ws.Column(1).Width = 15;
            ws.Column(2).Width = 50;

            // ===== ENCABEZADO =====
            ws.Cell(1, 1).Value = "GUÍA DE USO";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(1, 1, 1, 2).Merge();

            ws.Cell(2, 1).Value = "Historial de Ejecuciones";
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Font.FontSize = 11;
            ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#666666");
            ws.Range(2, 1, 2, 2).Merge();

            int row = 4;

            // ===== LEYENDA DE SÍMBOLOS =====
            ws.Cell(row, 1).Value = "LEYENDA DE SÍMBOLOS";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 12;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(colorVerdePrimario);
            ws.Range(row, 1, row, 2).Merge();
            ws.Row(row).Height = 20;
            row += 2;

            ws.Cell(row, 1).Value = "✅";
            ws.Cell(row, 2).Value = "Completado - Item fue ejecutado correctamente";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#1B5E20");
            row++;

            ws.Cell(row, 1).Value = "⚠️";
            ws.Cell(row, 2).Value = "Observado - Item completado pero con observaciones";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8E1");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8E1");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#F57F17");
            row++;

            ws.Cell(row, 1).Value = "❌";
            ws.Cell(row, 2).Value = "No Completado - Item no fue ejecutado";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#C62828");
            row++;

            ws.Cell(row, 1).Value = "—";
            ws.Cell(row, 2).Value = "No Disponible - No hay registro para esa semana";
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#616161");
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
            ws.Cell(row, 2).Value = "Vistazo general de todos los equipos con información de contacto, sede y cumplimiento general";
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

            ws.Cell(row, 1).Value = "Cumplimiento (%)";
            ws.Cell(row, 2).Value = "Porcentaje de items completados correctamente (sin observaciones)";
            ws.Cell(row, 1).Style.Font.Bold = true;
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
