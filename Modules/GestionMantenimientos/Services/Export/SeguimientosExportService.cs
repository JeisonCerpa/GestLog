using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionMantenimientos.Utilities;

namespace GestLog.Modules.GestionMantenimientos.Services.Export
{
    /// <summary>
    /// Servicio para exportar seguimientos de mantenimiento a un archivo Excel.
    /// </summary>
    public class SeguimientosExportService : ISeguimientosExportService
    {
        private readonly IGestLogLogger _logger;

        public SeguimientosExportService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExportAsync(IEnumerable<SeguimientoMantenimientoDto> seguimientos, int anio, string outputPath, CancellationToken ct)
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var seguimientosList = seguimientos?.ToList() ?? new List<SeguimientoMantenimientoDto>();

                    using var workbook = new XLWorkbook();
                    var wsSeguimientos = workbook.Worksheets.Add($"Seguimientos {anio}");

                    wsSeguimientos.ShowGridLines = false;

                    wsSeguimientos.Row(1).Height = 35;
                    wsSeguimientos.Row(2).Height = 35;
                    wsSeguimientos.Range(1, 1, 2, 2).Merge();

                    var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Simics.png");
                    try
                    {
                        if (File.Exists(logoPath))
                        {
                            var picture = wsSeguimientos.AddPicture(logoPath);
                            picture.MoveTo(wsSeguimientos.Cell(1, 1), 10, 10);
                            picture.Scale(0.15);
                        }
                    }
                    catch { }

                    var titleRange = wsSeguimientos.Range(1, 3, 2, 11);
                    titleRange.Merge();
                    var titleCellSeg = titleRange.FirstCell();
                    titleCellSeg.Value = "SEGUIMIENTOS DE MANTENIMIENTOS";
                    titleCellSeg.Style.Font.Bold = true;
                    titleCellSeg.Style.Font.FontSize = 18;
                    titleCellSeg.Style.Font.FontColor = XLColor.Black;
                    titleCellSeg.Style.Fill.BackgroundColor = XLColor.White;
                    titleCellSeg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    titleCellSeg.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    int currentRowSeg = 3;

                    var headersSeg = new[] { "Equipo", "Nombre", "Semana", "Tipo", "Descripción", "Responsable", "Estado", "Fecha Registro", "Fecha Realización", "Costo", "Observaciones" };
                    for (int col = 1; col <= headersSeg.Length; col++)
                    {
                        var headerCell = wsSeguimientos.Cell(currentRowSeg, col);
                        headerCell.Value = headersSeg[col - 1];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }
                    wsSeguimientos.Row(currentRowSeg).Height = 22;
                    currentRowSeg++;

                    int rowCountSeg = 0;
                    foreach (var seg in seguimientosList.OrderBy(s => s.Semana).ThenBy(s => s.Codigo))
                    {
                        ct.ThrowIfCancellationRequested();

                        wsSeguimientos.Cell(currentRowSeg, 1).Value = seg.Codigo;
                        wsSeguimientos.Cell(currentRowSeg, 2).Value = seg.Nombre;
                        wsSeguimientos.Cell(currentRowSeg, 3).Value = seg.Semana;
                        wsSeguimientos.Cell(currentRowSeg, 4).Value = seg.TipoMtno?.ToString() ?? "-";

                        var descCell = wsSeguimientos.Cell(currentRowSeg, 5);
                        descCell.Value = seg.Descripcion;
                        descCell.Style.Alignment.WrapText = true;
                        wsSeguimientos.Cell(currentRowSeg, 6).Value = seg.Responsable;

                        var estadoCell = wsSeguimientos.Cell(currentRowSeg, 7);
                        estadoCell.Value = EstadoToTexto(seg.Estado);
                        if (seg.TipoMtno == TipoMantenimiento.Correctivo)
                        {
                            estadoCell.Style.Fill.BackgroundColor = EstadoSeguimientoUtils.XLColorFromTipo(TipoMantenimiento.Correctivo);
                            estadoCell.Value = "Correctivo";
                        }
                        else
                        {
                            estadoCell.Style.Fill.BackgroundColor = XLColorFromEstado(seg.Estado);
                        }
                        estadoCell.Style.Font.FontColor = XLColor.White;
                        estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        wsSeguimientos.Cell(currentRowSeg, 8).Value = seg.FechaRegistro?.ToString("dd/MM/yyyy HH:mm") ?? "-";
                        wsSeguimientos.Cell(currentRowSeg, 9).Value = seg.FechaRealizacion?.ToString("dd/MM/yyyy HH:mm") ?? "-";

                        var costoCell = wsSeguimientos.Cell(currentRowSeg, 10);
                        costoCell.Value = seg.Costo ?? 0;
                        costoCell.Style.NumberFormat.Format = "$#,##0";

                        var obsCell = wsSeguimientos.Cell(currentRowSeg, 11);
                        obsCell.Value = seg.Observaciones ?? "-";
                        obsCell.Style.Alignment.WrapText = true;
                        obsCell.Style.Alignment.Indent = 2;

                        if (rowCountSeg % 2 == 0)
                        {
                            for (int col = 1; col <= 11; col++)
                            {
                                if (col != 7)
                                    wsSeguimientos.Cell(currentRowSeg, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                            }
                        }

                        wsSeguimientos.Cell(currentRowSeg, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsSeguimientos.Cell(currentRowSeg, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsSeguimientos.Cell(currentRowSeg, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        wsSeguimientos.Row(currentRowSeg).Height = 30;

                        currentRowSeg++;
                        rowCountSeg++;
                    }

                    if (seguimientosList.Count > 0)
                    {
                        int headerRow = currentRowSeg - seguimientosList.Count - 1;
                        wsSeguimientos.Range(headerRow, 1, currentRowSeg - 1, 11).SetAutoFilter();
                    }

                    if (seguimientosList.Count > 0)
                    {
                        currentRowSeg += 2;
                        currentRowSeg = SeguimientosKpiSection.Escribir(wsSeguimientos, seguimientosList, anio, currentRowSeg);
                    }

                    currentRowSeg += 2;
                    var footerCellSeg = wsSeguimientos.Cell(currentRowSeg, 1);
                    footerCellSeg.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss}  Sistema GestLog © SIMICS Group SAS";
                    footerCellSeg.Style.Font.Italic = true;
                    footerCellSeg.Style.Font.FontSize = 9;
                    footerCellSeg.Style.Font.FontColor = XLColor.Gray;
                    wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                    wsSeguimientos.Range(1, 1, currentRowSeg, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

                    try
                    {
                        wsSeguimientos.Columns("A", "K").AdjustToContents();
                        // Descripción (col E) y Observaciones (col K): ancho fijo para que el texto se ajuste (WrapText) en vez de ensanchar la columna
                        wsSeguimientos.Column(5).Width = 45;
                        wsSeguimientos.Column(11).Width = 45;
                    }
                    catch { }

                    // Congelar filas 1-3 (encabezado/título) y columnas A-B (Equipo/Nombre)
                    wsSeguimientos.SheetView.Freeze(3, 2);

                    wsSeguimientos.PageSetup.PageOrientation = XLPageOrientation.Landscape;
                    wsSeguimientos.PageSetup.AdjustTo(100);
                    wsSeguimientos.PageSetup.FitToPages(1, 0);
                    wsSeguimientos.PageSetup.Margins.Top = 0.5;
                    wsSeguimientos.PageSetup.Margins.Bottom = 0.5;
                    wsSeguimientos.PageSetup.Margins.Left = 0.5;
                    wsSeguimientos.PageSetup.Margins.Right = 0.5;

                    workbook.SaveAs(outputPath);

                    _logger.LogInformation("[SeguimientosExportService] Export completado: {Path}", outputPath);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("[SeguimientosExportService] Export cancelado por el usuario");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[SeguimientosExportService] Error durante la generación del Excel");
                    throw;
                }
            }, ct);
        }

        private string EstadoToTexto(EstadoSeguimientoMantenimiento estado)
        {
            return EstadoSeguimientoUtils.EstadoToTexto(estado);
        }

        private XLColor XLColorFromEstado(EstadoSeguimientoMantenimiento estado)
        {
            return EstadoSeguimientoUtils.XLColorFromEstado(estado);
        }
    }
}
