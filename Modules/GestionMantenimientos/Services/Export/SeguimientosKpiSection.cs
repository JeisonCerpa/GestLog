using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using GestLog.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Utilities;

namespace GestLog.Modules.GestionMantenimientos.Services.Export
{
    /// <summary>
    /// Bloque de KPIs de la hoja de seguimientos (indicadores, resumen por tipo, análisis por estado
    /// y cumplimiento por sede), compartido por SeguimientosExportService y CronogramaExportService.
    /// % Cumplido = (en tiempo + fuera de tiempo) / (preventivos - pendientes);
    /// % Incumplido = (no realizados + atrasados) / (preventivos - pendientes).
    /// Los correctivos no entran al cálculo de cumplimiento (no son programados).
    /// </summary>
    public static class SeguimientosKpiSection
    {
        private static readonly XLColor ColorTituloKpi = XLColor.FromArgb(0x118938);
        private static readonly XLColor ColorTituloSeccion = XLColor.FromArgb(0x2B8E3F);
        private static readonly XLColor ColorEncabezado = XLColor.FromArgb(0x504F4E);
        private static readonly XLColor ColorFilaTotal = XLColor.FromArgb(0xE8E8E8);

        private sealed record Metricas(int Total, int Preventivos, int Correctivos, int EnTiempo, int FueraTiempo, int Atrasados, int NoRealizados, int Pendientes, decimal Costo)
        {
            // Denominador: solo preventivos cuya semana ya venció (excluye pendientes)
            private int Vencidos => Preventivos - Pendientes;
            public decimal PctCumplido => Vencidos > 0 ? (EnTiempo + FueraTiempo) / (decimal)Vencidos * 100 : 0;
            public decimal PctIncumplido => Vencidos > 0 ? (NoRealizados + Atrasados) / (decimal)Vencidos * 100 : 0;
        }

        private static Metricas Calcular(IEnumerable<SeguimientoMantenimientoDto> segs)
        {
            var lista = segs.ToList();
            var prev = lista.Where(s => s.TipoMtno == TipoMantenimiento.Preventivo).ToList();
            return new Metricas(
                lista.Count,
                prev.Count,
                lista.Count(s => s.TipoMtno == TipoMantenimiento.Correctivo),
                prev.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo),
                prev.Count(s => s.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo),
                prev.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Atrasado),
                prev.Count(s => s.Estado == EstadoSeguimientoMantenimiento.NoRealizado),
                prev.Count(s => s.Estado == EstadoSeguimientoMantenimiento.Pendiente),
                lista.Sum(s => s.Costo ?? 0));
        }

        /// <summary>
        /// Escribe la tabla de datos de seguimientos (encabezados en la fila indicada + filas + autofiltro)
        /// y devuelve la fila siguiente. Las fechas se escriben como fechas reales de Excel y los encabezados
        /// son compatibles con la importación de seguimientos (exportar → corregir → importar).
        /// </summary>
        public static int EscribirTabla(IXLWorksheet ws, List<SeguimientoMantenimientoDto> seguimientos, int headerRow, CancellationToken ct)
        {
            var headers = new[] { "Equipo", "Nombre", "Sede", "Semana", "Tipo", "Descripción", "Responsable", "Estado", "Fecha Registro", "Fecha Realización", "Costo", "Observaciones" };
            for (int col = 1; col <= headers.Length; col++)
            {
                var headerCell = ws.Cell(headerRow, col);
                headerCell.Value = headers[col - 1];
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = ColorTituloKpi;
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(headerRow).Height = 22;

            int row = headerRow + 1;
            int rowCount = 0;
            foreach (var seg in seguimientos.OrderBy(s => s.Semana).ThenBy(s => s.Codigo))
            {
                ct.ThrowIfCancellationRequested();

                ws.Cell(row, 1).Value = seg.Codigo;

                var nombreCell = ws.Cell(row, 2);
                nombreCell.Value = seg.Nombre;
                nombreCell.Style.Alignment.WrapText = true;

                ws.Cell(row, 3).Value = seg.Sede?.ToString() ?? "-";
                ws.Cell(row, 4).Value = seg.Semana;
                ws.Cell(row, 5).Value = seg.TipoMtno?.ToString() ?? "-";

                var descCell = ws.Cell(row, 6);
                descCell.Value = seg.Descripcion;
                descCell.Style.Alignment.WrapText = true;

                ws.Cell(row, 7).Value = seg.Responsable;

                var estadoCell = ws.Cell(row, 8);
                estadoCell.Value = EstadoSeguimientoUtils.EstadoToTexto(seg.Estado);
                if (seg.TipoMtno == TipoMantenimiento.Correctivo)
                {
                    estadoCell.Style.Fill.BackgroundColor = EstadoSeguimientoUtils.XLColorFromTipo(TipoMantenimiento.Correctivo);
                    estadoCell.Value = "Correctivo";
                }
                else
                {
                    estadoCell.Style.Fill.BackgroundColor = EstadoSeguimientoUtils.XLColorFromEstado(seg.Estado);
                }
                estadoCell.Style.Font.FontColor = XLColor.White;
                estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                EscribirFecha(ws.Cell(row, 9), seg.FechaRegistro);
                EscribirFecha(ws.Cell(row, 10), seg.FechaRealizacion);

                var costoCell = ws.Cell(row, 11);
                costoCell.Value = seg.Costo ?? 0;
                costoCell.Style.NumberFormat.Format = "$#,##0";

                var obsCell = ws.Cell(row, 12);
                obsCell.Value = seg.Observaciones ?? "-";
                obsCell.Style.Alignment.WrapText = true;
                obsCell.Style.Alignment.Indent = 2;

                if (rowCount % 2 == 0)
                {
                    for (int col = 1; col <= 12; col++)
                    {
                        if (col != 8)
                            ws.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                    }
                }

                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Row(row).Height = 30;

                row++;
                rowCount++;
            }

            if (seguimientos.Count > 0)
                ws.Range(headerRow, 1, row - 1, 12).SetAutoFilter();

            return row;
        }

        private static void EscribirFecha(IXLCell cell, DateTime? fecha)
        {
            if (fecha.HasValue)
            {
                cell.Value = fecha.Value;
                cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            }
            else
            {
                cell.Value = "-";
            }
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        /// <summary>
        /// Ajusta anchos de columna de la hoja de seguimientos para que quepa en pantalla:
        /// las columnas de texto largo quedan con ancho fijo y el contenido se ajusta (WrapText).
        /// </summary>
        public static void AjustarAnchosTabla(IXLWorksheet ws)
        {
            try
            {
                ws.Columns("A", "L").AdjustToContents();
                ws.Column(2).Width = Math.Min(ws.Column(2).Width, 28);   // Nombre
                ws.Column(6).Width = 40;                                  // Descripción
                ws.Column(12).Width = 40;                                 // Observaciones
            }
            catch { }
        }

        /// <summary>Escribe el bloque completo de KPIs a partir de la fila indicada y devuelve la fila siguiente a la última escrita.</summary>
        public static int Escribir(IXLWorksheet ws, List<SeguimientoMantenimientoDto> seguimientos, int anio, int row)
        {
            var m = Calcular(seguimientos);
            var costoPreventivo = seguimientos.Where(s => s.TipoMtno == TipoMantenimiento.Preventivo).Sum(s => s.Costo ?? 0);
            var costoCorrectivo = m.Costo - costoPreventivo;
            var pctCorrectivos = m.Total > 0 ? m.Correctivos / (decimal)m.Total * 100 : 0;
            var pctPreventivos = m.Total > 0 ? m.Preventivos / (decimal)m.Total * 100 : 0;

            // ===== INDICADORES DE DESEMPEÑO =====
            EscribirTitulo(ws, row, "INDICADORES DE DESEMPEÑO - AÑO " + anio, 11, ColorTituloKpi, 14);
            ws.Row(row).Height = 22;
            row++;

            var kpiLabels = new[] { "Cumplimiento", "Incumplimiento", "Total Mtos", "Correctivos", "Preventivos" };
            var kpiValues = new[]
            {
                $"{m.PctCumplido:F1}%",
                $"{m.PctIncumplido:F1}%",
                m.Total.ToString(),
                $"{m.Correctivos} ({pctCorrectivos:F1}%)",
                $"{m.Preventivos} ({pctPreventivos:F1}%)"
            };

            for (int col = 0; col < kpiLabels.Length; col++)
            {
                var labelCell = ws.Cell(row, col + 1);
                labelCell.Value = kpiLabels[col];
                labelCell.Style.Font.Bold = true;
                labelCell.Style.Font.FontSize = 10;
                labelCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF0F0F0);
                labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                labelCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

                var valueCell = ws.Cell(row + 1, col + 1);
                valueCell.Value = kpiValues[col];
                valueCell.Style.Font.Bold = true;
                valueCell.Style.Font.FontSize = 12;
                valueCell.Style.Fill.BackgroundColor = ColorTituloKpi;
                valueCell.Style.Font.FontColor = XLColor.White;
                valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                valueCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            row += 3;

            // ===== RESUMEN POR TIPO DE MANTENIMIENTO =====
            EscribirTitulo(ws, row, "RESUMEN POR TIPO DE MANTENIMIENTO", 5, ColorTituloSeccion, 12);
            ws.Row(row).Height = 20;
            row++;
            EscribirEncabezados(ws, row, new[] { "Tipo", "Cantidad", "%", "Costo Total", "% Costo" });
            row++;

            var tipoData = new (string tipo, int cantidad, decimal costo)[]
            {
                ("Preventivo", m.Preventivos, costoPreventivo),
                ("Correctivo", m.Correctivos, costoCorrectivo),
                ("TOTAL", m.Total, m.Costo)
            };

            foreach (var (tipo, cantidad, costo) in tipoData)
            {
                bool esTotal = tipo == "TOTAL";

                var tipoCell = ws.Cell(row, 1);
                tipoCell.Value = tipo;
                tipoCell.Style.Font.Bold = esTotal;

                var cantCell = ws.Cell(row, 2);
                cantCell.Value = cantidad;
                cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var pctCell = ws.Cell(row, 3);
                if (!esTotal && m.Total > 0)
                    pctCell.Value = cantidad / (decimal)m.Total * 100;
                pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var costoCell = ws.Cell(row, 4);
                costoCell.Value = costo;
                costoCell.Style.NumberFormat.Format = "$#,##0";
                costoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var pctCostoCell = ws.Cell(row, 5);
                if (!esTotal && m.Costo > 0)
                    pctCostoCell.Value = costo / m.Costo * 100;
                pctCostoCell.Style.NumberFormat.Format = "0.0\"%\"";
                pctCostoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = esTotal ? ColorFilaTotal : XLColor.White;
                row++;
            }
            row++;

            // ===== ANÁLISIS DE CUMPLIMIENTO POR ESTADO =====
            EscribirTitulo(ws, row, "ANÁLISIS DE CUMPLIMIENTO POR ESTADO", 6, ColorTituloSeccion, 12);
            ws.Row(row).Height = 20;
            row++;
            EscribirEncabezados(ws, row, new[] { "Estado", "Cantidad", "%", "Color" });
            row++;

            var estadoData = new (string estado, int cantidad, XLColor color)[]
            {
                ("Realizado en Tiempo", m.EnTiempo, EstadoSeguimientoUtils.XLColorFromEstado(EstadoSeguimientoMantenimiento.RealizadoEnTiempo)),
                ("Realizado Fuera de Tiempo", m.FueraTiempo, EstadoSeguimientoUtils.XLColorFromEstado(EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo)),
                ("Atrasado", m.Atrasados, EstadoSeguimientoUtils.XLColorFromEstado(EstadoSeguimientoMantenimiento.Atrasado)),
                ("No Realizado", m.NoRealizados, EstadoSeguimientoUtils.XLColorFromEstado(EstadoSeguimientoMantenimiento.NoRealizado)),
                ("Pendiente", m.Pendientes, EstadoSeguimientoUtils.XLColorFromEstado(EstadoSeguimientoMantenimiento.Pendiente)),
                ("Correctivo", m.Correctivos, EstadoSeguimientoUtils.XLColorFromTipo(TipoMantenimiento.Correctivo))
            };

            foreach (var (estado, cantidad, color) in estadoData)
            {
                if (cantidad == 0) continue;

                ws.Cell(row, 1).Value = estado;

                var cantCell = ws.Cell(row, 2);
                cantCell.Value = cantidad;
                cantCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var pctCell = ws.Cell(row, 3);
                if (m.Total > 0)
                    pctCell.Value = cantidad / (decimal)m.Total * 100;
                pctCell.Style.NumberFormat.Format = "0.0\"%\"";
                pctCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                var colorCell = ws.Cell(row, 4);
                colorCell.Value = " ";
                colorCell.Style.Font.FontSize = 14;
                colorCell.Style.Fill.BackgroundColor = color;
                colorCell.Style.Font.FontColor = color;
                colorCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                colorCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                row++;
            }
            row++;

            // ===== CUMPLIMIENTO POR SEDE =====
            EscribirTitulo(ws, row, "CUMPLIMIENTO POR SEDE - AÑO " + anio, 11, ColorTituloSeccion, 12);
            ws.Row(row).Height = 20;
            row++;
            EscribirEncabezados(ws, row, new[] { "Sede", "Total", "En tiempo", "Fuera de tiempo", "No realizado", "Atrasado", "Pendiente", "Correctivo", "% Cumplido", "% Incumplido", "Costo Total" });
            row++;

            var filasSede = seguimientos
                .GroupBy(s => s.Sede)
                .OrderBy(g => g.Key.HasValue ? (int)g.Key.Value : int.MaxValue)
                .Select(g => (nombre: g.Key?.ToString() ?? "Sin sede", metricas: Calcular(g)))
                .ToList();
            filasSede.Add(("TOTAL", m));

            foreach (var (nombre, ms) in filasSede)
            {
                bool esTotal = nombre == "TOTAL";

                ws.Cell(row, 1).Value = nombre;

                var valores = new[] { ms.Total, ms.EnTiempo, ms.FueraTiempo, ms.NoRealizados, ms.Atrasados, ms.Pendientes, ms.Correctivos };
                for (int i = 0; i < valores.Length; i++)
                {
                    var cell = ws.Cell(row, i + 2);
                    cell.Value = valores[i];
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                var pctCumplidoCell = ws.Cell(row, 9);
                pctCumplidoCell.Value = ms.PctCumplido;
                var pctIncumplidoCell = ws.Cell(row, 10);
                pctIncumplidoCell.Value = ms.PctIncumplido;
                foreach (var cell in new[] { pctCumplidoCell, pctIncumplidoCell })
                {
                    cell.Style.NumberFormat.Format = "0.0\"%\"";
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                var costoCell = ws.Cell(row, 11);
                costoCell.Value = ms.Costo;
                costoCell.Style.NumberFormat.Format = "$#,##0";
                costoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                if (esTotal)
                {
                    var rangoTotal = ws.Range(row, 1, row, 11);
                    rangoTotal.Style.Fill.BackgroundColor = ColorFilaTotal;
                    rangoTotal.Style.Font.Bold = true;
                }
                row++;
            }

            return row;
        }

        private static void EscribirTitulo(IXLWorksheet ws, int row, string texto, int colSpan, XLColor fondo, int fontSize)
        {
            var cell = ws.Cell(row, 1);
            cell.Value = texto;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = fontSize;
            cell.Style.Fill.BackgroundColor = fondo;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(row, 1, row, colSpan).Merge();
        }

        private static void EscribirEncabezados(IXLWorksheet ws, int row, string[] headers)
        {
            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(row, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ColorEncabezado;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
    }
}
