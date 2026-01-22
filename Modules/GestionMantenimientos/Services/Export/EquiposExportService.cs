using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services.Export
{
    /// <summary>
    /// Servicio para exportar equipos a Excel.
    /// Responsable de generar reportes de inventario de equipos con formato profesional.
    /// </summary>
    public class EquiposExportService : IEquiposExportService
    {
        private readonly IGestLogLogger _logger;
        private const string LogoFileName = "Simics.png";

        public EquiposExportService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExportarEquiposAsync(
            IEnumerable<EquipoDto> equipos,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => ExportarEquiposInternal(equipos, filePath, "INVENTARIO DE EQUIPOS", false), cancellationToken);
        }

        public async Task ExportarEquiposFiltradosAsync(
            IEnumerable<EquipoDto> equipos,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => ExportarEquiposInternal(equipos, filePath, "INVENTARIO DE EQUIPOS (FILTRADOS)", true), cancellationToken);
        }

        private void ExportarEquiposInternal(IEnumerable<EquipoDto> equipos, string filePath, string titulo, bool esFiltrado)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add("Equipos");

                // ===== FILAS 1-2: LOGO (izquierda) + TÍTULO (derecha) =====
                ws.Row(1).Height = 35;
                ws.Row(2).Height = 35;
                ws.ShowGridLines = false;

                // Combinar celdas A1:B2 para el logo
                ws.Range(1, 1, 2, 2).Merge();

                // Agregar logo
                var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", LogoFileName);
                try
                {
                    if (File.Exists(logoPath))
                    {
                        var picture = ws.AddPicture(logoPath);
                        picture.MoveTo(ws.Cell(1, 1), 10, 10);
                        picture.Scale(0.15);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo cargar el logo en la exportación");
                }

                // Agregar título en C1:J2
                var titleRange = ws.Range(1, 3, 2, 10);
                titleRange.Merge();
                var titleCell = titleRange.FirstCell();
                titleCell.Value = titulo;
                titleCell.Style.Font.Bold = true;
                titleCell.Style.Font.FontSize = 18;
                titleCell.Style.Font.FontColor = XLColor.Black;
                titleCell.Style.Fill.BackgroundColor = XLColor.White;
                titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // ===== ENCABEZADOS DE TABLA =====
                int currentRow = 3;
                var headers = new[] { "Código", "Nombre", "Marca", "Estado", "Sede", "Frecuencia", "Precio", "Fecha Registro", "Clasificación", "Comprado a" };
                ConfigurarEncabezados(ws, currentRow, headers);
                currentRow++;

                // ===== FILAS DE DATOS =====
                int rowCount = 0;
                var equiposExportar = equipos.OrderBy(e => e.Codigo).ToList();
                foreach (var eq in equiposExportar)
                {
                    AgregarFilaEquipo(ws, currentRow, eq, rowCount);
                    currentRow++;
                    rowCount++;
                }

                // Agregar filtros automáticos
                if (equiposExportar.Count > 0)
                {
                    int headerRow = currentRow - equiposExportar.Count - 1;
                    ws.Range(headerRow, 1, currentRow - 1, 10).SetAutoFilter();
                }

                // ===== PANEL DE KPIs =====
                if (equiposExportar.Count > 0)
                {
                    currentRow += 2;
                    AgregarPanelKPIs(ws, ref currentRow, equiposExportar);
                }

                // ===== AJUSTAR ANCHO DE COLUMNAS =====
                ConfigurarAnchosColumnas(ws);

                // ===== PIE DE PÁGINA =====
                currentRow += 1;
                var footerCell = ws.Cell(currentRow, 1);
                footerCell.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} • {equiposExportar.Count} equipos • Sistema GestLog © SIMICS Group SAS";
                footerCell.Style.Font.Italic = true;
                footerCell.Style.Font.FontSize = 9;
                footerCell.Style.Font.FontColor = XLColor.Gray;
                ws.Range(currentRow, 1, currentRow, 10).Merge();

                // Configurar página para exportación
                ConfigurarPagina(ws);

                workbook.SaveAs(filePath);
                _logger.LogInformation($"Equipos exportados exitosamente: {filePath} ({equiposExportar.Count} equipos)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al exportar equipos a {filePath}");
                throw;
            }
        }

        private void ConfigurarEncabezados(IXLWorksheet ws, int row, string[] headers)
        {
            for (int col = 1; col <= headers.Length; col++)
            {
                var headerCell = ws.Cell(row, col);
                headerCell.Value = headers[col - 1];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(row).Height = 22;
        }

        private void AgregarFilaEquipo(IXLWorksheet ws, int row, EquipoDto eq, int rowCount)
        {
            ws.Cell(row, 1).Value = eq.Codigo ?? "";
            ws.Cell(row, 2).Value = eq.Nombre ?? "";
            ws.Cell(row, 3).Value = eq.Marca ?? "";
            ws.Cell(row, 4).Value = eq.Estado?.ToString() ?? "";
            ws.Cell(row, 5).Value = eq.Sede?.ToString() ?? "";
            ws.Cell(row, 6).Value = eq.FrecuenciaMtto?.ToString() ?? "";

            // Precio formateado
            var precioCell = ws.Cell(row, 7);
            precioCell.Value = eq.Precio ?? 0;
            precioCell.Style.NumberFormat.Format = "$#,##0";

            ws.Cell(row, 8).Value = eq.FechaRegistro?.ToString("dd/MM/yyyy") ?? "";
            ws.Cell(row, 9).Value = eq.Clasificacion ?? "";
            ws.Cell(row, 10).Value = eq.CompradoA ?? "";

            // Filas alternas con color gris claro
            if (rowCount % 2 == 0)
            {
                for (int col = 1; col <= 10; col++)
                {
                    ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                }
            }

            ws.Row(row).Height = 20;
        }

        private void AgregarPanelKPIs(IXLWorksheet ws, ref int currentRow, List<EquipoDto> equipos)
        {
            // Calcular estadísticas
            var totalEquipos = equipos.Count;
            var equiposActivos = equipos.Count(e => EsEstado(e.Estado, "activo") || EsEstado(e.Estado, "enuso"));
            var equiposInactivos = equipos.Count(e => EsEstado(e.Estado, "inactivo"));
            var equiposSinPrecio = equipos.Count(e => (e.Precio ?? 0) == 0);
            var precioTotal = equipos.Sum(e => e.Precio ?? 0);

            // Título KPIs
            var kpiTitle = ws.Cell(currentRow, 1);
            kpiTitle.Value = "INDICADORES DE INVENTARIO";
            kpiTitle.Style.Font.Bold = true;
            kpiTitle.Style.Font.FontSize = 12;
            kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
            kpiTitle.Style.Font.FontColor = XLColor.White;
            kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(currentRow, 1, currentRow, 11).Merge();
            ws.Row(currentRow).Height = 20;
            currentRow++;

            // KPI Row
            var kpiLabels = new[] { "Total Equipos", "Activos", "Inactivos", "Sin Precio", "Valor Total Inventario" };
            var kpiValues = new object[] { totalEquipos, equiposActivos, equiposInactivos, equiposSinPrecio, precioTotal };

            for (int col = 0; col < kpiLabels.Length; col++)
            {
                // Etiqueta
                var labelCell = ws.Cell(currentRow, col + 1);
                labelCell.Value = kpiLabels[col];
                labelCell.Style.Font.Bold = true;
                labelCell.Style.Font.FontSize = 10;
                labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                // Valor
                var valueCell = ws.Cell(currentRow + 1, col + 1);
                if (col == 4) // Valor Total Inventario
                {
                    valueCell.Value = (decimal)kpiValues[col];
                    valueCell.Style.NumberFormat.Format = "$#,##0";
                }
                else
                {
                    valueCell.Value = (int)kpiValues[col];
                }

                valueCell.Style.Font.Bold = true;
                valueCell.Style.Font.FontSize = 12;
                valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                valueCell.Style.Font.FontColor = XLColor.White;
                valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            currentRow += 2;
        }

        private void ConfigurarAnchosColumnas(IXLWorksheet ws)
        {
            // Ajustar automáticamente las columnas al contenido
            ws.Columns(1, 10).AdjustToContents();

            // Establecer anchos mínimos y un ancho máximo razonable para mantener la estética
            double[] minWidths = new double[] { 12, 20, 15, 15, 12, 15, 14, 14, 16, 18 };
            const double maxWidth = 50.0;

            for (int i = 1; i <= 10; i++)
            {
                var col = ws.Column(i);
                if (col.Width < minWidths[i - 1])
                    col.Width = minWidths[i - 1];
                if (col.Width > maxWidth)
                    col.Width = maxWidth;
            }
        }

        private void ConfigurarPagina(IXLWorksheet ws)
        {
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.Scale = 90;
            ws.PageSetup.Margins.Top = 0.5;
            ws.PageSetup.Margins.Bottom = 0.5;
            ws.PageSetup.Margins.Left = 0.5;
            ws.PageSetup.Margins.Right = 0.5;
        }

        private static bool EsEstado(object? estado, string target)
        {
            var s = estado?.ToString() ?? string.Empty;
            s = s.Trim().ToLowerInvariant().Replace(" ", "");
            target = target.Trim().ToLowerInvariant().Replace(" ", "");
            return s.Contains(target);
        }
    }
}
