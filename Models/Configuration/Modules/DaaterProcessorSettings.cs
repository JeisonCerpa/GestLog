using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using GestLog.Models.Validation.Attributes;

namespace GestLog.Models.Configuration.Modules;

/// <summary>
/// Modos de manejo de registros duplicados en DaaterProcessor
/// </summary>
public enum DuplicateHandlingMode
{
    /// <summary>
    /// Omitir registros duplicados (comportamiento por defecto)
    /// </summary>
    [Description("Omitir duplicados")]
    Skip = 0,
    
    /// <summary>
    /// Reemplazar el registro existente con el nuevo
    /// </summary>
    [Description("Reemplazar duplicados")]
    Replace = 1,
    
    /// <summary>
    /// Lanzar una excepción cuando se encuentre un duplicado
    /// </summary>
    [Description("Error en duplicados")]
    Error = 2,
    
    /// <summary>
    /// Permitir duplicados (no validar)
    /// </summary>
    [Description("Permitir duplicados")]
    Allow = 3
}

/// <summary>
/// Configuraciones específicas del módulo DaaterProcessor
/// </summary>
public class DaaterProcessorSettings : INotifyPropertyChanged
{
    private string _defaultInputPath = "";
    private string _defaultOutputPath = "Output";
    private bool _validateDataOnImport = true;
    private bool _createBackupBeforeProcessing = true;
    private string _backupDirectory = "Backups";
    private bool _enableDataConsolidation = true;
    private bool _enableProviderNormalization = true;
    private bool _enableCountryMapping = true;
    private int _maxRowsPerFile = 1000000; // 1M filas
    private bool _includeHeaderRow = true;
    private string _dateFormat = "yyyy-MM-dd";
    private string _decimalSeparator = ".";    private string _thousandsSeparator = ",";
    private bool _enableProgressReporting = true;
    private bool _enableErrorRecovery = true;
    private bool _enableDuplicateValidation = true;
    private DuplicateHandlingMode _duplicateHandlingMode = DuplicateHandlingMode.Skip;

    /// <summary>
    /// Ruta de entrada por defecto para archivos a procesar
    /// </summary>
    [DirectoryExists(AllowEmpty = true, CreateIfNotExists = false)]
    [Display(Name = "Ruta de Entrada", Description = "Directorio donde se encuentran los archivos Excel a procesar")]
    public string DefaultInputPath
    {
        get => _defaultInputPath;
        set => SetProperty(ref _defaultInputPath, value);
    }

    /// <summary>
    /// Ruta de salida por defecto para archivos procesados
    /// </summary>
    [Required(ErrorMessage = "La ruta de salida es requerida")]
    [DirectoryExists(AllowEmpty = false, CreateIfNotExists = true)]
    [Display(Name = "Ruta de Salida", Description = "Directorio donde se guardarán los archivos procesados")]
    public string DefaultOutputPath
    {
        get => _defaultOutputPath;
        set => SetProperty(ref _defaultOutputPath, value);
    }

    /// <summary>
    /// Validar datos durante la importación
    /// </summary>
    public bool ValidateDataOnImport
    {
        get => _validateDataOnImport;
        set => SetProperty(ref _validateDataOnImport, value);
    }

    /// <summary>
    /// Crear backup antes de procesar archivos
    /// </summary>
    public bool CreateBackupBeforeProcessing
    {
        get => _createBackupBeforeProcessing;
        set => SetProperty(ref _createBackupBeforeProcessing, value);
    }    /// <summary>
    /// Directorio para archivos de backup
    /// </summary>
    [DirectoryExists(AllowEmpty = true, CreateIfNotExists = true)]
    [Display(Name = "Directorio de Backup", Description = "Directorio donde se guardarán los archivos de respaldo")]
    public string BackupDirectory
    {
        get => _backupDirectory;
        set => SetProperty(ref _backupDirectory, value);
    }

    /// <summary>
    /// Habilitar consolidación de datos
    /// </summary>
    public bool EnableDataConsolidation
    {
        get => _enableDataConsolidation;
        set => SetProperty(ref _enableDataConsolidation, value);
    }

    /// <summary>
    /// Habilitar normalización de nombres de proveedores
    /// </summary>
    public bool EnableProviderNormalization
    {
        get => _enableProviderNormalization;
        set => SetProperty(ref _enableProviderNormalization, value);
    }

    /// <summary>
    /// Habilitar mapeo de países
    /// </summary>
    public bool EnableCountryMapping
    {
        get => _enableCountryMapping;
        set => SetProperty(ref _enableCountryMapping, value);
    }    /// <summary>
    /// Número máximo de filas por archivo de salida
    /// </summary>
    [NumericRange(1000, 10000000)]
    [Display(Name = "Máximo de Filas por Archivo", Description = "Número máximo de filas que puede contener un archivo de salida")]
    public int MaxRowsPerFile
    {
        get => _maxRowsPerFile;
        set => SetProperty(ref _maxRowsPerFile, Math.Max(1000, value));
    }

    /// <summary>
    /// Incluir fila de encabezados en archivos de salida
    /// </summary>
    public bool IncludeHeaderRow
    {
        get => _includeHeaderRow;
        set => SetProperty(ref _includeHeaderRow, value);
    }

    /// <summary>
    /// Formato de fecha para exportación
    /// </summary>
    [Required(ErrorMessage = "El formato de fecha es requerido")]
    [DateFormat("yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy")]
    [Display(Name = "Formato de Fecha", Description = "Formato utilizado para las fechas en los archivos de salida")]
    public string DateFormat
    {
        get => _dateFormat;
        set => SetProperty(ref _dateFormat, value);
    }

    /// <summary>
    /// Separador decimal
    /// </summary>
    [Required(ErrorMessage = "El separador decimal es requerido")]
    [StringLength(1, ErrorMessage = "El separador decimal debe ser un solo carácter")]
    [GestLog.Models.Validation.Attributes.AllowedValues(".", ",")]
    [Display(Name = "Separador Decimal", Description = "Carácter utilizado como separador decimal")]
    public string DecimalSeparator
    {
        get => _decimalSeparator;
        set => SetProperty(ref _decimalSeparator, value);
    }    /// <summary>
    /// Separador de miles
    /// </summary>
    [StringLength(1, ErrorMessage = "El separador de miles debe ser un solo carácter")]
    [GestLog.Models.Validation.Attributes.AllowedValues(",", ".", " ", "'", "")]
    [Display(Name = "Separador de Miles", Description = "Carácter utilizado como separador de miles (puede estar vacío)")]
    public string ThousandsSeparator
    {
        get => _thousandsSeparator;
        set => SetProperty(ref _thousandsSeparator, value);
    }

    /// <summary>
    /// Habilitar reportes de progreso detallados
    /// </summary>
    public bool EnableProgressReporting
    {
        get => _enableProgressReporting;
        set => SetProperty(ref _enableProgressReporting, value);
    }    /// <summary>
    /// Habilitar recuperación automática de errores
    /// </summary>
    public bool EnableErrorRecovery
    {
        get => _enableErrorRecovery;
        set => SetProperty(ref _enableErrorRecovery, value);
    }

    /// <summary>
    /// Habilitar validación de registros duplicados
    /// </summary>
    [Display(Name = "Validar Duplicados", Description = "Habilitar la detección y manejo de registros duplicados basados en partida arancelaria y número de declaración")]
    public bool EnableDuplicateValidation
    {
        get => _enableDuplicateValidation;
        set => SetProperty(ref _enableDuplicateValidation, value);
    }

    /// <summary>
    /// Modo de manejo de registros duplicados
    /// </summary>
    [Display(Name = "Manejo de Duplicados", Description = "Define cómo se manejan los registros duplicados encontrados durante el procesamiento")]
    public DuplicateHandlingMode DuplicateHandlingMode
    {
        get => _duplicateHandlingMode;
        set => SetProperty(ref _duplicateHandlingMode, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
