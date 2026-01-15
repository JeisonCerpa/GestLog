using ClosedXML.Excel;
using GestLog.Modules.GestionMantenimientos.Models;
using GestLog.Modules.GestionMantenimientos.Models.Enums;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;
using GestLog.Modules.GestionMantenimientos.Models.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionMantenimientos.Services.Export
{
    /// <summary>
    /// Servicio para exportar la "Hoja de Vida" completa de un equipo a Excel.
    /// Incluye información general del equipo e historial de mantenimientos realizados.
    /// </summary>
    public class HojaVidaExportService
    {
        private readonly string _logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo.png");

        /// <summary>
        /// Exporta la hoja de vida del equipo a un archivo Excel con formato profesional.
        /// </summary>
        public Task ExportarHojaVidaAsync(
            EquipoDto equipo,
            List<SeguimientoMantenimientoDto> mantenimientos,
            string filePath)
        {
            return Task.Run(() =>
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Hoja de Vida");

                    // Ocultar líneas de cuadrícula para una apariencia más limpia
                    worksheet.ShowGridLines = false;

                    // Configurar ancho de columnas para mejor visualización en PDF
                    worksheet.Column("A").Width = 20;
                    worksheet.Column("B").Width = 35;
                    worksheet.Column("C").Width = 25;
                    worksheet.Column("D").Width = 30;
                    worksheet.Column("E").Width = 20;
                    worksheet.Column("F").Width = 25;

                    int currentRow = 1;

                    // ===== ENCABEZADO CON LOGO Y TÍTULO =====
                    // Logo en la esquina superior derecha
                    try
                    {
                        if (File.Exists(_logoPath))
                        {
                            var picture = worksheet.AddPicture(_logoPath);
                            // Logo original: 2124x486, escalar a ~8% para tamaño apropiado
                            picture.Scale(0.08); // Aproximadamente 170x39 píxeles
                            // Posicionar en las columnas E-F, filas 1-3
                            picture.MoveTo(worksheet.Cell(currentRow, 5), 5, 5); // Offset para centrar
                        }
                    }
                    catch
                    {
                        // Si hay error al cargar el logo, continuar sin él
                    }

                    // Título principal centrado
                    var titleCell = worksheet.Cell(currentRow, 1);
                    titleCell.Value = "HOJA DE VIDA - EQUIPO";
                    titleCell.Style.Font.Bold = true;
                    titleCell.Style.Font.FontSize = 18;
                    titleCell.Style.Font.FontColor = XLColor.White;
                    titleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                    titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                    worksheet.Row(currentRow).Height = 30;

                    currentRow += 2;

                    // ===== SECCIÓN: INFORMACIÓN GENERAL =====
                    worksheet.Cell(currentRow, 1).Value = "INFORMACIÓN GENERAL DEL EQUIPO";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 14;
                    worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                    worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
                    worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                    worksheet.Row(currentRow).Height = 25;
                    currentRow++;

                    // Datos generales
                    var equipoData = new (string label, string value)[]
                    {
                        ("Código", equipo.Codigo ?? "N/A"),
                        ("Nombre", equipo.Nombre ?? "N/A"),
                        ("Marca", equipo.Marca ?? "N/A"),
                        ("Clasificación", equipo.Clasificacion ?? "N/A"),
                        ("Comprado a", equipo.CompradoA ?? "N/A"),
                        ("Estado", equipo.Estado?.ToString() ?? "N/A"),
                        ("Sede", equipo.Sede?.ToString() ?? "N/A"),
                        ("Frecuencia de Mtto.", equipo.FrecuenciaMtto?.ToString() ?? "N/A"),
                        ("Precio", $"${(equipo.Precio ?? 0):N0}"),
                        ("Fecha Compra", equipo.FechaCompra?.ToString("dd/MM/yyyy") ?? "N/A"),
                        ("Fecha Baja", equipo.FechaBaja?.ToString("dd/MM/yyyy") ?? "N/A"),
                    };

                    // Organizar en 3 pares por fila para ocupar columnas A-F simétricamente
                    int itemsPerRow = 3;
                    for (int i = 0; i < equipoData.Length; i++)
                    {
                        int rowOffset = i / itemsPerRow;
                        int colOffset = (i % itemsPerRow) * 2; // Columnas 1,3,5 para etiquetas

                        worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Value = equipoData[i].label;
                        worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Style.Font.Bold = true;
                        worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Style.Fill.BackgroundColor = XLColor.FromArgb(0xEDF2F7);

                        worksheet.Cell(currentRow + rowOffset, 2 + colOffset).Value = equipoData[i].value;
                    }

                    // Avanzar filas según la cantidad de filas usadas
                    currentRow += (equipoData.Length + itemsPerRow - 1) / itemsPerRow;

                    // Observaciones
                    if (!string.IsNullOrWhiteSpace(equipo.Observaciones))
                    {
                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = "Observaciones";
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0xEDF2F7);
                        worksheet.Cell(currentRow, 2).Value = equipo.Observaciones;
                        worksheet.Range(currentRow, 2, currentRow, 6).Merge();
                        worksheet.Row(currentRow).Height = 20;
                        currentRow++;
                    }

                    currentRow += 3;

                    // ===== SECCIÓN: HISTORIAL DE MANTENIMIENTOS =====
                    worksheet.Cell(currentRow, 1).Value = $"HISTORIAL DE MANTENIMIENTOS ({mantenimientos.Count} registros)";
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
                    worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                    worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
                    worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                    currentRow++;

                    // Encabezados de tabla
                    var headers = new[] { "Fecha", "Tipo", "Descripción", "Responsable", "Costo", "Estado" };
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        var headerCell = worksheet.Cell(currentRow, col);
                        headerCell.Value = headers[col - 1];
                        headerCell.Style.Font.Bold = true;
                        headerCell.Style.Font.FontColor = XLColor.White;
                        headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x118938);
                        headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        headerCell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                    }
                    currentRow++;

                    // Filas de datos (ordenadas de más recientes a más antiguos)
                    var mantenimientosOrdenados = mantenimientos
                        .OrderByDescending(m => m.FechaRegistro)
                        .ToList();

                    foreach (var mtto in mantenimientosOrdenados)
                    {
                        worksheet.Cell(currentRow, 1).Value = mtto.FechaRegistro?.ToString("dd/MM/yyyy");
                        worksheet.Cell(currentRow, 2).Value = mtto.TipoMtno?.ToString() ?? "";
                        worksheet.Cell(currentRow, 3).Value = mtto.Descripcion;
                        worksheet.Cell(currentRow, 4).Value = mtto.Responsable;
                        worksheet.Cell(currentRow, 5).Value = mtto.Costo ?? 0;
                        worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "$#,##0";
                        worksheet.Cell(currentRow, 6).Value = ObtenerEstadoColor(mtto.Estado);

                        // Aplicar colores según estado
                        var estadoCell = worksheet.Cell(currentRow, 6);
                        switch (mtto.Estado)
                        {
                            case EstadoSeguimientoMantenimiento.RealizadoEnTiempo:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0x27AE60); // Verde
                                estadoCell.Style.Font.FontColor = XLColor.White;
                                break;
                            case EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xF9B233); // Naranja
                                estadoCell.Style.Font.FontColor = XLColor.White;
                                break;
                            case EstadoSeguimientoMantenimiento.NoRealizado:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.FromArgb(0xC0392B); // Rojo
                                estadoCell.Style.Font.FontColor = XLColor.White;
                                break;
                            case EstadoSeguimientoMantenimiento.Pendiente:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#BDBDBD"); // Gris
                                estadoCell.Style.Font.FontColor = XLColor.Black;
                                break;
                            case EstadoSeguimientoMantenimiento.Atrasado:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFB300"); // Ámbar
                                estadoCell.Style.Font.FontColor = XLColor.Black;
                                break;
                            default:
                                estadoCell.Style.Fill.BackgroundColor = XLColor.Gray; // Gris para estados no definidos
                                estadoCell.Style.Font.FontColor = XLColor.White;
                                break;
                        }

                        estadoCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Filas alternas con color (excluyendo columna de estado)
                        if (currentRow % 2 == 0)
                        {
                            for (int col = 1; col <= 5; col++)
                            {
                                worksheet.Cell(currentRow, col).Style.Fill.BackgroundColor = XLColor.FromArgb(0xFAFBFC);
                            }
                        }

                        currentRow++;
                    }

                    // Centrar todas las celdas de datos en la tabla de historial
                    if (mantenimientos.Count > 0)
                    {
                        worksheet.Range(currentRow - mantenimientos.Count, 1, currentRow - 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    // Agregar filtros automáticos a la tabla de historial
                    if (mantenimientos.Count > 0)
                    {
                        int headerRow = currentRow - mantenimientos.Count - 1;
                        worksheet.Range(headerRow, 1, currentRow - 1, 6).SetAutoFilter();
                        // Remover borde específico de la tabla
                    }

                    // ===== RESUMEN DE ESTADÍSTICAS =====
                    if (mantenimientos.Count > 0)
                    {
                        currentRow += 2;

                        worksheet.Cell(currentRow, 1).Value = "RESUMEN DE ESTADÍSTICAS";
                        worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, 1).Style.Font.FontSize = 12;
                        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(0x2B8E3F);
                        worksheet.Cell(currentRow, 1).Style.Font.FontColor = XLColor.White;
                        worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Range(currentRow, 1, currentRow, 6).Merge();
                        worksheet.Row(currentRow).Height = 20;
                        currentRow += 2;

                        var estadisticas = new (string label, int count, string color)[]
                        {
                            ("Realizados en Tiempo", mantenimientos.Count(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoEnTiempo), "27AE60"),
                            ("Realizados Fuera de Tiempo", mantenimientos.Count(m => m.Estado == EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo || m.Estado == EstadoSeguimientoMantenimiento.Atrasado), "F9B233"),
                            ("No Realizados", mantenimientos.Count(m => m.Estado == EstadoSeguimientoMantenimiento.NoRealizado), "C0392B"),
                            ("Total Mantenimientos", mantenimientos.Count, "118938"),
                        };

                        // Mostrar estadísticas en formato 2x2
                        for (int i = 0; i < estadisticas.Length; i++)
                        {
                            int colOffset = (i % 2) * 3; // 0 para primera columna, 3 para segunda
                            int rowOffset = i / 2; // 0 para primera fila, 1 para segunda

                            worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Value = estadisticas[i].label;
                            worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Style.Font.Bold = true;
                            worksheet.Cell(currentRow + rowOffset, 1 + colOffset).Style.Fill.BackgroundColor = XLColor.FromArgb(0xF8F9FA);

                            var valueCell = worksheet.Cell(currentRow + rowOffset, 2 + colOffset);
                            valueCell.Value = estadisticas[i].count;
                            valueCell.Style.Font.Bold = true;
                            valueCell.Style.Font.FontSize = 11;
                            valueCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + estadisticas[i].color);
                            valueCell.Style.Font.FontColor = XLColor.White;
                            valueCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        }

                        currentRow += 2; // Avanzar después de las 2 filas de estadísticas

                        // Remover borde específico de estadísticas
                    }

                    // ===== PIE DE PÁGINA =====
                    currentRow += 2;
                    var footerCell = worksheet.Cell(currentRow, 1);
                    footerCell.Value = $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm:ss} • Sistema GestLog © SIMICS Group SAS";
                    footerCell.Style.Font.Italic = true;
                    footerCell.Style.Font.FontSize = 9;
                    footerCell.Style.Font.FontColor = XLColor.Gray;
                    worksheet.Range(currentRow, 1, currentRow, 6).Merge();

                    // Agregar borde exterior grueso a toda la hoja de vida (A1 hasta F{currentRow})
                    worksheet.Range(1, 1, currentRow, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thick;

                    // Configurar página para mejor exportación a PDF
                    worksheet.PageSetup.PageOrientation = XLPageOrientation.Portrait;
                    worksheet.PageSetup.AdjustTo(100); // Ajustar al 100% para PDF
                    worksheet.PageSetup.FitToPages(1, 0); // Ajustar a 1 página de ancho
                    worksheet.PageSetup.Margins.Top = 0.5;
                    worksheet.PageSetup.Margins.Bottom = 0.5;
                    worksheet.PageSetup.Margins.Left = 0.5;
                    worksheet.PageSetup.Margins.Right = 0.5;

                    // Guardar archivo
                    workbook.SaveAs(filePath);
                }
            });
        }

        /// <summary>
        /// Obtiene la descripción del estado del mantenimiento.
        /// </summary>
        private string ObtenerEstadoColor(EstadoSeguimientoMantenimiento estado)
        {
            return estado switch
            {
                EstadoSeguimientoMantenimiento.RealizadoEnTiempo => "✔ En Tiempo",
                EstadoSeguimientoMantenimiento.RealizadoFueraDeTiempo => "⚠ Fuera de Tiempo",
                EstadoSeguimientoMantenimiento.NoRealizado => "✗ No Realizado",
                EstadoSeguimientoMantenimiento.Pendiente => "⏳ Pendiente",
                EstadoSeguimientoMantenimiento.Atrasado => "⚠ Atrasado",
                _ => estado.ToString()
            };
        }
    }
}


