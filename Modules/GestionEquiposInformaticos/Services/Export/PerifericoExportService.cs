using ClosedXML.Excel;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Export;
using GestLog.Modules.GestionEquiposInformaticos.Models.Dtos;
using GestLog.Modules.GestionEquiposInformaticos.Models.Enums;
using GestLog.Services.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Export
{
    /// <summary>
    /// Servicio para exportar periféricos a archivos Excel
    /// Respeta SRP: solo responsable de la exportación a Excel
    /// </summary>
    public class PerifericoExportService : IPerifericoExportService
    {
        private readonly IGestLogLogger _logger;

        /// <summary>
        /// Colores para los estados de periféricos (basados en el diseño de equipos)
        /// </summary>
        private static class EstadoColores
        {
            public static XLColor EnUso => XLColor.FromArgb(43, 142, 63);           // Verde #2B8E3F
            public static XLColor AlmacenadoFuncionando => XLColor.FromArgb(107, 114, 128);  // Gris #6B7280
            public static XLColor DadoDeBaja => XLColor.FromArgb(239, 68, 68);     // Rojo #EF4444
        }

        public PerifericoExportService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Exporta periféricos a un archivo Excel con formato profesional
        /// </summary>
        public async Task ExportarPerifericosAExcelAsync(string rutaArchivo, IEnumerable<PerifericoEquipoInformaticoDto> perifericos, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("[PerifericoExportService] Iniciando exportación de periféricos a {RutaArchivo}", rutaArchivo);

                // Validar entrada
                if (string.IsNullOrWhiteSpace(rutaArchivo))
                    throw new ArgumentException("La ruta del archivo no puede estar vacía", nameof(rutaArchivo));

                if (perifericos == null)
                    throw new ArgumentNullException(nameof(perifericos));

                var perifericosList = perifericos.ToList();

                // Ejecutar exportación en background para no bloquear UI
                await Task.Run(() =>
                {
                    using var workbook = new XLWorkbook();

                    // Crear hoja de periféricos
                    ExportarHojaPerifericos(workbook, perifericosList);

                    // Guardar archivo
                    workbook.SaveAs(rutaArchivo);

                    _logger.LogInformation("[PerifericoExportService] Exportación completada: {RutaArchivo}", rutaArchivo);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("[PerifericoExportService] Exportación cancelada");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PerifericoExportService] Error al exportar periféricos a {RutaArchivo}", rutaArchivo);
                throw;
            }
        }

        /// <summary>
        /// Crea la hoja principal de periféricos con todos los datos
        /// </summary>
        private void ExportarHojaPerifericos(XLWorkbook workbook, List<PerifericoEquipoInformaticoDto> perifericos)
        {
            var worksheet = workbook.Worksheets.Add("Periféricos");

            var headers = new[]
            {
                "Código",
                "Dispositivo",
                "Marca",
                "Modelo",
                "Serial",
                "Costo",
                "Fecha Compra",
                "Equipo Asignado",
                "Usuario Asignado",
                "Sede",
                "Estado",
                "Observaciones",
                "Fecha Creación",
                "Fecha Modificación"
            };

            // Escribir headers
            for (int col = 1; col <= headers.Length; col++)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = headers[col - 1];
                AplicarFormatoHeader(cell);
            }

            // Escribir datos
            int row = 2;
            foreach (var periferico in perifericos)
            {
                worksheet.Cell(row, 1).Value = periferico.Codigo ?? "";
                worksheet.Cell(row, 2).Value = periferico.Dispositivo ?? "";
                worksheet.Cell(row, 3).Value = periferico.Marca ?? "";
                worksheet.Cell(row, 4).Value = periferico.Modelo ?? "";
                worksheet.Cell(row, 5).Value = periferico.Serial ?? "";
                worksheet.Cell(row, 6).Value = periferico.Costo ?? (decimal?)null;
                worksheet.Cell(row, 7).Value = periferico.FechaCompra;
                worksheet.Cell(row, 8).Value = periferico.CodigoEquipoAsignado ?? "";
                worksheet.Cell(row, 9).Value = periferico.UsuarioAsignado ?? "";
                worksheet.Cell(row, 10).Value = periferico.Sede.ToString();
                worksheet.Cell(row, 11).Value = periferico.Estado.ToString();
                
                // Limpiar observaciones: eliminar saltos de línea
                var observacionesLimpia = (periferico.Observaciones ?? "")
                    .Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                worksheet.Cell(row, 12).Value = observacionesLimpia;

                worksheet.Cell(row, 13).Value = periferico.FechaCreacion;
                worksheet.Cell(row, 14).Value = periferico.FechaModificacion;

                // Aplicar color al estado
                AplicarColorEstado(worksheet.Cell(row, 11), periferico.Estado);

                // Formato de números y fechas
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "$#,##0.00";
                worksheet.Cell(row, 6).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                worksheet.Cell(row, 7).Style.DateFormat.Format = "dd/MM/yyyy";
                worksheet.Cell(row, 13).Style.DateFormat.Format = "dd/MM/yyyy hh:mm";
                worksheet.Cell(row, 14).Style.DateFormat.Format = "dd/MM/yyyy hh:mm";

                row++;
            }

            // Ajustar ancho de columnas
            worksheet.Columns().AdjustToContents();
            worksheet.Column(12).Width = 30;  // Observaciones más ancho

            // Congelar fila de headers
            worksheet.SheetView.FreezeRows(1);
        }

        /// <summary>
        /// Aplica formato estándar a headers
        /// </summary>
        private void AplicarFormatoHeader(IXLCell cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(17, 137, 56);  // Verde principal #118938
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Alignment.WrapText = true;
        }

        /// <summary>
        /// Aplica color de fondo a una celda según el estado del periférico
        /// </summary>
        private void AplicarColorEstado(IXLCell cell, EstadoPeriferico estado)
        {
            var color = estado switch
            {
                EstadoPeriferico.EnUso => EstadoColores.EnUso,
                EstadoPeriferico.AlmacenadoFuncionando => EstadoColores.AlmacenadoFuncionando,
                EstadoPeriferico.DadoDeBaja => EstadoColores.DadoDeBaja,
                _ => EstadoColores.EnUso
            };

            cell.Style.Fill.BackgroundColor = color;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
    }
}
