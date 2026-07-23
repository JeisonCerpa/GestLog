using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionCartera.Services;

/// <summary>
/// Información sobre un PDF generado
/// </summary>
public class GeneratedPdfInfo
{
    public string NombreEmpresa { get; set; } = string.Empty;
    public string Nit { get; set; } = string.Empty;
    public string RutaArchivo { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public string TipoCartera { get; set; } = string.Empty;
    
    // Propiedades adicionales para el binding del DataGrid
    public string CompanyName => NombreEmpresa;
    public string FilePath => RutaArchivo;
    public DateTime GeneratedDate { get; set; } = DateTime.Now;
    public long FileSize { get; set; }
    public int RecordCount { get; set; }
}

/// <summary>
/// Servicio para generación de documentos PDF de estados de cuenta
/// </summary>
public interface IPdfGeneratorService
{
    /// <summary>
    /// Genera estados de cuenta en PDF a partir de un archivo Excel
    /// </summary>
    /// <param name="excelFilePath">Ruta del archivo Excel con los datos</param>
    /// <param name="outputFolder">Carpeta donde guardar los PDFs</param>
    /// <param name="templatePath">Ruta de la plantilla de fondo (opcional)</param>
    /// <param name="progress">Reporte de progreso</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de PDFs generados</returns>
    Task<IReadOnlyList<GeneratedPdfInfo>> GenerateEstadosCuentaAsync(
        string excelFilePath, 
        string outputFolder,
        string? templatePath = null,
        IProgress<(int current, int total, string status)>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida si un archivo Excel tiene la estructura correcta
    /// </summary>
    Task<bool> ValidateExcelStructureAsync(string excelFilePath);

    /// <summary>
    /// Obtiene una vista previa de las empresas que se encontraron en el Excel
    /// </summary>
    Task<IEnumerable<string>> GetCompaniesPreviewAsync(string excelFilePath);

    /// <summary>
    /// Advertencias de la última generación: empresas cuyo PDF NO se pudo generar y por qué
    /// (p. ej. el archivo PDF estaba abierto en otra aplicación). Vacío si todo salió bien.
    /// </summary>
    IReadOnlyList<string> LastGenerationWarnings { get; }
}
