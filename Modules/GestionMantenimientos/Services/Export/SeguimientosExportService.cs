using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Interfaces.Export;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
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
                        var preventivos = seguimientosList.Count(s => s.TipoMtno == TipoMantenimiento.Preventivo);
                        var correctivos = seguimientosList.Count(s => s.TipoMtno == TipoMantenimiento.Correctivo);

                        var preventivosConEstado = seguimientosList.Where(s => s.TipoMtno == TipoMantenimiento.Preventivo).ToList();
                        var realizadosEnTiempo = preventivosConEstado.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo);
                        var realizadosFueraTiempo = preventivosConEstado.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado);
                        var noRealizados = preventivosConEstado.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado);
                        var pendientes = preventivosConEstado.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente);
                        var totalCosto = seguimientosList.Sum(s => s.Costo ?? 0);
                        var costoPreventivoTotal = seguimientosList.Where(s => s.TipoMtno == TipoMantenimiento.Preventivo).Sum(s => s.Costo ?? 0);
                        var costoCorrectivo = totalCosto - costoPreventivoTotal;

                        var totalMtto = seguimientosList.Count;
                        var totalPreventivos = preventivos;
                        var pctCumplimiento = totalPreventivos > 0 ? (realizadosEnTiempo + realizadosFueraTiempo) / (decimal)totalPreventivos * 100 : 0;
                        var pctCorrectivos = totalMtto > 0 ? correctivos / (decimal)totalMtto * 100 : 0;
                        var pctPreventivos = totalMtto > 0 ? preventivos / (decimal)totalMtto * 100 : 0;

                        var kpiTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                        kpiTitle.Value = "INDICADORES DE DESEMPEÑO - AÑO " + anio;
                        kpiTitle.Style.Font.Bold = true;
                        kpiTitle.Style.Font.FontSize = 14;
                        kpiTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        kpiTitle.Style.Font.FontColor = XLColor.White;
                        kpiTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 11).Merge();
                        wsSeguimientos.Row(currentRowSeg).Height = 22;
                        currentRowSeg++;

                        var kpiLabels = new[] { "Cumplimiento", "Total Mtos", "Correctivos", "Preventivos" };
                        var kpiValues = new object[]
                        {
                            $"{pctCumplimiento:F1}%",
                            totalMtto,
                            $"{correctivos} ({pctCorrectivos:F1}%)",
                            $"{preventivos} ({pctPreventivos:F1}%)"
                        };

                        for (int col = 0; col < kpiLabels.Length; col++)
                        {
                            var labelCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                            labelCell.Value = kpiLabels[col];
                            labelCell.Style.Font.Bold = true;
                            labelCell.Style.Font.FontSize = 10;
                            labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                            var valueCell = wsSeguimientos.Cell(currentRowSeg + 1, col + 1);

                            if (kpiValues[col] is string strVal)
                                valueCell.Value = strVal;
                            else if (kpiValues[col] is int intVal)
                                valueCell.Value = intVal;
                            else if (kpiValues[col] is decimal decVal)
                            {
                                valueCell.Value = decVal;
                                valueCell.Style.NumberFormat.Format = "$#,##0";
                            }
                            else
                                valueCell.Value = kpiValues[col]?.ToString() ?? "-";

                            valueCell.Style.Font.Bold = true;
                            valueCell.Style.Font.FontSize = 12;
                            valueCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                            valueCell.Style.Font.FontColor = XLColor.White;
                            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }

                        currentRowSeg += 2;

                        currentRowSeg += 1;
                        var tipoTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                        tipoTitle.Value = "RESUMEN POR TIPO DE MANTENIMIENTO";
                        tipoTitle.Style.Font.Bold = true;
                        tipoTitle.Style.Font.FontSize = 12;
                        tipoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                        tipoTitle.Style.Font.FontColor = XLColor.White;
                        tipoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 5).Merge();
                        wsSeguimientos.Row(currentRowSeg).Height = 20;
                        currentRowSeg++;

                        var tipoHeaders = new[] { "Tipo", "Cantidad", "%", "Costo Total", "% Costo" };
                        for (int col = 0; col < tipoHeaders.Length; col++)
                        {
                            var headerCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                            headerCell.Value = tipoHeaders[col];
                            headerCell.Style.Font.Bold = true;
                            headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                            headerCell.Style.Font.FontColor = XLColor.White;
                            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                        currentRowSeg++;

                        var tipoData = new (string tipo, int cantidad, decimal costo)[]
                        {
                            ("Preventivo", preventivos, costoPreventivoTotal),
                            ("Correctivo", correctivos, costoCorrectivo),
                            ("TOTAL", totalMtto, totalCosto)
                        };

                        foreach (var data in tipoData)
                        {
                            int col = 1;
                            var tipoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            tipoCell.Value = data.tipo;
                            tipoCell.Style.Font.Bold = data.tipo == "TOTAL";
                            tipoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;

                            var cantCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            cantCell.Value = data.cantidad;
                            cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cantCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;

                            var pctCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            if (data.tipo != "TOTAL" && totalMtto > 0)
                                pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                            pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                            pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            pctCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;

                            var costoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            costoCell.Value = data.costo;
                            costoCell.Style.NumberFormat.Format = "$#,##0";
                            costoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            costoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;

                            var pctCostoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            if (data.tipo != "TOTAL" && totalCosto > 0)
                                pctCostoCell.Value = (data.costo / totalCosto * 100);
                            pctCostoCell.Style.NumberFormat.Format = "0.0\"%\"";
                            pctCostoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            pctCostoCell.Style.Fill.BackgroundColor = data.tipo == "TOTAL" ? XLColor.FromArgb(0xE8E8E8) : XLColor.White;

                            currentRowSeg++;
                        }

                        currentRowSeg += 1;
                        var estadoTitle = wsSeguimientos.Cell(currentRowSeg, 1);
                        estadoTitle.Value = "ANÁLISIS DE CUMPLIMIENTO POR ESTADO";
                        estadoTitle.Style.Font.Bold = true;
                        estadoTitle.Style.Font.FontSize = 12;
                        estadoTitle.Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                        estadoTitle.Style.Font.FontColor = XLColor.White;
                        estadoTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        wsSeguimientos.Range(currentRowSeg, 1, currentRowSeg, 6).Merge();
                        wsSeguimientos.Row(currentRowSeg).Height = 20;
                        currentRowSeg++;

                        var estadoHeaders = new[] { "Estado", "Cantidad", "%", "Color" };
                        for (int col = 0; col < estadoHeaders.Length; col++)
                        {
                            var headerCell = wsSeguimientos.Cell(currentRowSeg, col + 1);
                            headerCell.Value = estadoHeaders[col];
                            headerCell.Style.Font.Bold = true;
                            headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x504F4E);
                            headerCell.Style.Font.FontColor = XLColor.White;
                            headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }
                        currentRowSeg++;

                        var estadoData = new (string estado, int cantidad, decimal costo, string colorHex)[]
                        {
                            ("Realizado en Tiempo", realizadosEnTiempo, preventivosConEstado.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo).Sum(s => s.Costo ?? 0), "388E3C"),
                            ("Realizado Fuera de Tiempo", realizadosFueraTiempo, preventivosConEstado.Where(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || s.Estado == EstadoSeguimientoMantenimiento.Atrasado).Sum(s => s.Costo ?? 0), "FFB300"),
                            ("No Realizado", noRealizados, preventivosConEstado.Where(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado).Sum(s => s.Costo ?? 0), "C80000"),
                            ("Pendiente", pendientes, preventivosConEstado.Where(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente).Sum(s => s.Costo ?? 0), "B3E5FC"),
                            ("Correctivo", correctivos, seguimientosList.Where(s => s.TipoMtno == TipoMantenimiento.Correctivo).Sum(s => s.Costo ?? 0), "7E57C2")
                        };

                        foreach (var data in estadoData)
                        {
                            if (data.cantidad == 0) continue;

                            int col = 1;
                            var estadoCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            estadoCell.Value = data.estado;

                            var cantCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            cantCell.Value = data.cantidad;
                            cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            var pctCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            if (totalMtto > 0)
                                pctCell.Value = (data.cantidad / (decimal)totalMtto * 100);
                            pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                            pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                            var colorCell = wsSeguimientos.Cell(currentRowSeg, col++);
                            colorCell.Value = " ";
                            colorCell.Style.Font.FontSize = 14;
                            var colorValue = XLColor.FromArgb(int.Parse(data.colorHex, System.Globalization.NumberStyles.HexNumber));
                            colorCell.Style.Fill.BackgroundColor = colorValue;
                            colorCell.Style.Font.FontColor = colorValue;
                            colorCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            colorCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                            currentRowSeg++;
                        }
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
                    }
                    catch { }

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
