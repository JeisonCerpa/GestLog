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

                    currentRowSeg = SeguimientosKpiSection.EscribirTabla(wsSeguimientos, seguimientosList, currentRowSeg, ct);

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
                    wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 12).Merge();
                    wsSeguimientos.Range(1, 1, currentRowSeg, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

                    SeguimientosKpiSection.AjustarAnchosTabla(wsSeguimientos);

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
    }
}
