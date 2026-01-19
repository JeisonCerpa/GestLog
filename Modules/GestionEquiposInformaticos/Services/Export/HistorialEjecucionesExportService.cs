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
        /// Exporta una colección de ejecuciones de historial a un archivo Excel con 3 hojas:
        /// 1. Historial: Resumen por ejecución con métricas
        /// 2. Checklist: Detalles de cada ítem completado/no completado
        /// 3. Resumen: Hoja explicativa para usuarios no técnicos
        /// </summary>
        public async Task ExportarHistorialAExcelAsync(
            string filePath,
            IEnumerable<EjecucionHistorialItem> items,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[HistorialEjecucionesExportService] Iniciando exportación de historial a {FilePath}", filePath);

                // Validar entrada
                if (string.IsNullOrWhiteSpace(filePath))
                    throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(filePath));

                if (items == null)
                    throw new ArgumentNullException(nameof(items));

                var itemsList = items.ToList();

                // Ejecutar exportación en background para no bloquear UI
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();

                    // Crear las 3 hojas del Excel
                    ExportarHojaHistorial(workbook, itemsList);
                    ExportarHojaChecklist(workbook, itemsList);
                    ExportarHojaResumen(workbook);

                    // Guardar archivo
                    workbook.SaveAs(filePath);

                    _logger.LogInformation("[HistorialEjecucionesExportService] Exportación completada: {FilePath}", filePath);
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
        /// Crea la hoja "Historial" con resumen por ejecución incluyendo métricas clave.
        /// </summary>
        private void ExportarHojaHistorial(XLWorkbook workbook, List<EjecucionHistorialItem> items)
        {
            var ws = workbook.Worksheets.Add("Historial");

            // Encabezados
            var headers = new[] 
            { 
                "Código", "Nombre", "Año", "Semana", "Fecha Objetivo", "Fecha Ejecución", 
                "Estado", "Usuario Asignado", "Total Ítems", "Ítems OK", "% Completado", "Resumen" 
            };

            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            // Datos
            int row = 2;
            foreach (var item in items)
            {
                int totalItems = item.DetalleItems?.Count ?? 0;
                int okCount = item.DetalleItems?.Count(d => d.Completado) ?? 0;
                double pct = totalItems > 0 ? (double)okCount / totalItems : 0.0;

                ws.Cell(row, 1).Value = item.CodigoEquipo;
                ws.Cell(row, 2).Value = item.NombreEquipo;
                ws.Cell(row, 3).Value = item.AnioISO;
                ws.Cell(row, 4).Value = item.SemanaISO;

                ws.Cell(row, 5).Value = item.FechaObjetivo;
                ws.Cell(row, 5).Style.DateFormat.Format = "dd/MM/yyyy";

                if (item.FechaEjecucion.HasValue)
                {
                    ws.Cell(row, 6).Value = item.FechaEjecucion.Value;
                    ws.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                }
                else
                {
                    ws.Cell(row, 6).Value = string.Empty;
                }

                ws.Cell(row, 7).Value = item.EstadoDescripcion;
                ws.Cell(row, 8).Value = item.UsuarioAsignadoEquipo;
                ws.Cell(row, 9).Value = totalItems;
                ws.Cell(row, 10).Value = okCount;

                ws.Cell(row, 11).Value = pct;
                ws.Cell(row, 11).Style.NumberFormat.Format = "0.00%";

                ws.Cell(row, 12).Value = item.Resumen;

                row++;
            }

            // Formatear encabezado y aplicar filtros
            var headerRange = ws.Range(1, 1, 1, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, row - 1, headers.Length).SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        /// <summary>
        /// Crea la hoja "Checklist" con detalles granulares de cada ítem del checklist.
        /// </summary>
        private void ExportarHojaChecklist(XLWorkbook workbook, List<EjecucionHistorialItem> items)
        {
            var wsChk = workbook.Worksheets.Add("Checklist");

            // Encabezados
            var chkHeaders = new[] 
            { 
                "EjecucionId", "PlanId", "CódigoEquipo", "NombreEquipo", "ItemId", 
                "Descripción", "Completado", "Estado Item", "Observación" 
            };

            for (int i = 0; i < chkHeaders.Length; i++)
                wsChk.Cell(1, i + 1).Value = chkHeaders[i];

            // Datos
            int chkRow = 2;
            foreach (var item in items)
            {
                if (item.DetalleItems == null || item.DetalleItems.Count == 0)
                    continue;

                foreach (var detail in item.DetalleItems)
                {
                    wsChk.Cell(chkRow, 1).Value = item.EjecucionId.ToString();
                    wsChk.Cell(chkRow, 2).Value = item.PlanId?.ToString() ?? string.Empty;
                    wsChk.Cell(chkRow, 3).Value = item.CodigoEquipo;
                    wsChk.Cell(chkRow, 4).Value = item.NombreEquipo;
                    wsChk.Cell(chkRow, 5).Value = detail.Id?.ToString() ?? string.Empty;
                    wsChk.Cell(chkRow, 6).Value = detail.Descripcion;
                    wsChk.Cell(chkRow, 7).Value = detail.Completado ? "Sí" : "No";

                    // Estado legible
                    string estadoItem = detail.Completado ? "OK" : (!string.IsNullOrWhiteSpace(detail.Observacion) ? "Observado" : "Pendiente");
                    wsChk.Cell(chkRow, 8).Value = estadoItem;

                    wsChk.Cell(chkRow, 9).Value = detail.Observacion ?? string.Empty;

                    chkRow++;
                }
            }

            // Formatear encabezado y aplicar filtros
            var chkHeaderRange = wsChk.Range(1, 1, 1, chkHeaders.Length);
            chkHeaderRange.Style.Font.Bold = true;
            chkHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");

            wsChk.SheetView.FreezeRows(1);
            wsChk.Range(1, 1, chkRow - 1, chkHeaders.Length).SetAutoFilter();
            wsChk.Columns().AdjustToContents();
        }

        /// <summary>
        /// Crea la hoja "Resumen" con información explicativa para usuarios no técnicos.
        /// </summary>
        private void ExportarHojaResumen(XLWorkbook workbook)
        {
            var wsSummary = workbook.Worksheets.Add("Resumen");

            wsSummary.Cell(1, 1).Value = "Resumen de exportación";
            wsSummary.Cell(1, 1).Style.Font.Bold = true;

            wsSummary.Cell(3, 1).Value = "Fecha de generación:";
            wsSummary.Cell(3, 2).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            wsSummary.Cell(4, 1).Value = "Hojas incluidas:";
            wsSummary.Cell(4, 2).Value = "Historial (resumen por ejecución)";
            wsSummary.Cell(5, 2).Value = "Checklist (cada ítem de checklist por fila)";

            wsSummary.Cell(7, 1).Value = "Qué significa cada columna (breve):";
            wsSummary.Cell(8, 1).Value = "Código:";
            wsSummary.Cell(8, 2).Value = "Código identificador del equipo.";

            wsSummary.Cell(9, 1).Value = "Nombre:";
            wsSummary.Cell(9, 2).Value = "Nombre legible del equipo.";

            wsSummary.Cell(10, 1).Value = "Total Ítems:";
            wsSummary.Cell(10, 2).Value = "Número total de ítems en el checklist para esa ejecución.";

            wsSummary.Cell(11, 1).Value = "Ítems OK:";
            wsSummary.Cell(11, 2).Value = "Número de ítems marcados como completados.";

            wsSummary.Cell(12, 1).Value = "% Completado:";
            wsSummary.Cell(12, 2).Value = "Porcentaje de ítems completados (formato porcentaje).";

            wsSummary.Cell(14, 1).Value = "Checklist - Estado Item:";
            wsSummary.Cell(14, 2).Value = "OK = completado, Observado = tiene observación, Pendiente = no completado y sin observación.";

            wsSummary.Columns().AdjustToContents();
        }
    }
}
