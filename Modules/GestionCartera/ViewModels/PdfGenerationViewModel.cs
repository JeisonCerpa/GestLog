using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.UI;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.GestionCartera.ViewModels.Base;
using GestLog.Modules.GestionCartera.Exceptions;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel especializado en la generación de documentos PDF
/// </summary>
public partial class PdfGenerationViewModel : BaseDocumentGenerationViewModel
{
    private readonly IPdfGeneratorService _pdfGenerator;
    private const string DEFAULT_TEMPLATE_FILE = "PlantillaSIMICS.png";

    // Servicio de progreso suavizado para animación fluida
    private SmoothProgressService _smoothProgress = null!; // Será inicializado en el constructor

    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(GenerateDocumentsCommand))]
    private string _selectedExcelFilePath = string.Empty;
    
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(GenerateDocumentsCommand))]
    private string _outputFolderPath = string.Empty;
    
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private string _templateFilePath = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private bool _useDefaultTemplate = true;    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();
    [ObservableProperty] private string _logText = string.Empty;
    
    // Propiedades para el panel de finalización
    [ObservableProperty] private bool _showCompletionPanel = false;
    [ObservableProperty] private string _completionMessage = string.Empty;

    public string TemplateStatusMessage => GetTemplateStatusMessage();public PdfGenerationViewModel(IPdfGeneratorService pdfGenerator, IGestLogLogger logger)
        : base(logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        
        // Inicializar el servicio de progreso suavizado
        _smoothProgress = new SmoothProgressService(value => ProgressValue = value);
        
        InitializeDefaultPaths();
    }

    private string GetTemplateStatusMessage()
    {
        if (!UseDefaultTemplate)
            return "Plantilla desactivada - se usará fondo blanco";
            
        if (string.IsNullOrEmpty(TemplateFilePath))
            return "No se ha encontrado una plantilla";
            
        if (Path.GetFileName(TemplateFilePath) == DEFAULT_TEMPLATE_FILE)
            return $"Usando plantilla predeterminada: {DEFAULT_TEMPLATE_FILE}";
            
        return $"Usando plantilla personalizada: {Path.GetFileName(TemplateFilePath)}";
    }

    private void InitializeDefaultPaths()
    {
        try
        {
            OutputFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "Clientes cartera pdf");
            
            if (!Directory.Exists(OutputFolderPath))
            {
                Directory.CreateDirectory(OutputFolderPath);
                _logger.LogInformation("📁 Carpeta de salida creada: {Path}", OutputFolderPath);
            }

            // Configurar plantilla por defecto
            var defaultTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_TEMPLATE_FILE);
            if (File.Exists(defaultTemplatePath))
            {
                TemplateFilePath = defaultTemplatePath;
                _logger.LogDebug("🖼️ Plantilla predeterminada encontrada: {Path}", defaultTemplatePath);
            }
            else
            {
                _logger.LogWarning("⚠️ Plantilla predeterminada no encontrada en: {Path}", defaultTemplatePath);
            }
            
            // Notificar explícitamente que ha cambiado la posibilidad de ejecutar los comandos
            // después de la inicialización
            NotifyCommandsCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inicializando rutas por defecto");
        }
    }    [RelayCommand]
    private async Task SelectExcelFile()
    {        
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar archivo Excel",
                Filter = "Archivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFile = openFileDialog.FileName;
                
                // Validar el archivo seleccionado
                if (!File.Exists(selectedFile))
                {
                    throw new DocumentValidationException(
                        $"El archivo seleccionado no existe: {selectedFile}",
                        selectedFile,
                        "FILE_NOT_FOUND");
                }
                
                // Validar que es un archivo Excel
                string extension = Path.GetExtension(selectedFile).ToLowerInvariant();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    throw new DocumentFormatException(
                        $"El archivo seleccionado no es un Excel válido: {Path.GetFileName(selectedFile)}",
                        selectedFile,
                        "XLSX_XLS");
                }
                
                // Validar que se puede acceder al archivo (no está bloqueado)
                try 
                {
                    using (var stream = File.Open(selectedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // Si llega aquí, el archivo puede abrirse correctamente
                    }
                }
                catch (IOException ioEx)
                {
                    throw new DocumentValidationException(
                        $"No se puede acceder al archivo. Puede estar abierto en otra aplicación: {Path.GetFileName(selectedFile)}",
                        selectedFile,
                        "FILE_LOCKED",
                        ioEx);
                }
                
                // Si pasó todas las validaciones, asignar el archivo
                SelectedExcelFilePath = selectedFile;
                _logger.LogInformation("📊 Archivo Excel seleccionado: {Path}", SelectedExcelFilePath);
                StatusMessage = $"Archivo Excel seleccionado: {Path.GetFileName(SelectedExcelFilePath)}";
                
                // Asegurar notificación en el hilo de UI
                NotifyCommandsCanExecuteChanged();
                
                // Opcionalmente, validar la estructura del Excel
                try
                {
                    StatusMessage = "Validando estructura del Excel...";                    // Solo validar si el servicio está disponible
                    if (_pdfGenerator != null)
                    {
                        // Usar cancellation token nuevo para permitir cancelar solo esta operación
                        using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(45)); // Aumentar timeout a 45 segundos
                        try
                        {
                            await _pdfGenerator.ValidateExcelStructureAsync(SelectedExcelFilePath);
                            StatusMessage = $"Archivo Excel válido: {Path.GetFileName(SelectedExcelFilePath)}";
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogWarning("⏰ Validación de Excel cancelada por timeout");
                            StatusMessage = "La validación del archivo Excel tardó demasiado y fue cancelada";
                            throw new DocumentValidationException(
                                "La validación del archivo Excel tardó demasiado tiempo. El archivo puede ser muy grande o estar dañado.",
                                SelectedExcelFilePath,
                                "VALIDATION_TIMEOUT");
                        }
                    }
                }
                catch (Exception validateEx)
                {
                    _logger.LogWarning(validateEx, "⚠️ El archivo Excel tiene problemas de estructura");
                    // No interrumpimos el flujo, solo advertimos
                    StatusMessage = $"⚠️ Advertencia: {validateEx.Message}";
                }
            }
        }
        catch (DocumentValidationException ex)
        {
            _logger.LogWarning(ex, "❌ Error de validación al seleccionar Excel: {ErrorCode}", ex.ValidationRule);
            StatusMessage = $"Error al seleccionar archivo: {ex.Message}";
        }
        catch (DocumentFormatException ex)
        {
            _logger.LogWarning(ex, "❌ Error de formato al seleccionar Excel: {Format}", ex.ExpectedFormat);
            StatusMessage = $"Error de formato: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar archivo Excel");
            StatusMessage = $"Error al seleccionar archivo: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SelectOutputFolder()
    {
        try
        {
            var folderDialog = new VistaFolderBrowserDialog
            {
                Description = "Seleccionar carpeta de salida para los documentos PDF",
                UseDescriptionForTitle = true,
                SelectedPath = OutputFolderPath
            };

            if (folderDialog.ShowDialog() == true)
            {
                OutputFolderPath = folderDialog.SelectedPath;
                _logger.LogInformation("📁 Carpeta de salida seleccionada: {Path}", OutputFolderPath);
                StatusMessage = $"Carpeta de salida: {OutputFolderPath}";
                
                // Notificar explícitamente el cambio para el comando
                NotifyCommandsCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar carpeta de salida");
            StatusMessage = "Error al seleccionar carpeta de salida";
        }
    }    [RelayCommand]
    private void SelectTemplate()
    {        
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar plantilla de imagen",
                Filter = "Archivos de imagen (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedTemplate = openFileDialog.FileName;
                
                // Validar que el archivo existe
                if (!File.Exists(selectedTemplate))
                {
                    throw new TemplateException(
                        $"El archivo de plantilla seleccionado no existe: {selectedTemplate}",
                        selectedTemplate);
                }
                
                // Validar que es una imagen
                string extension = Path.GetExtension(selectedTemplate).ToLowerInvariant();
                if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".bmp")
                {
                    throw new TemplateException(
                        $"El archivo seleccionado no es una imagen válida: {Path.GetFileName(selectedTemplate)}",
                        selectedTemplate);
                }
                
                // Validar que se puede acceder al archivo (no está bloqueado)
                try 
                {
                    using var imageStream = File.OpenRead(selectedTemplate);
                    // Si llega aquí, el archivo puede abrirse correctamente
                }
                catch (IOException ioEx)
                {
                    throw new TemplateException(
                        $"No se puede acceder a la plantilla. Puede estar abierta en otra aplicación: {Path.GetFileName(selectedTemplate)}",
                        selectedTemplate,
                        ioEx);
                }
                
                // Si todo está correcto, asignar la plantilla
                TemplateFilePath = selectedTemplate;
                UseDefaultTemplate = true;  // Activar el uso de la plantilla
                _logger.LogInformation("🖼️ Plantilla personalizada seleccionada: {Path}", TemplateFilePath);
                StatusMessage = $"Plantilla seleccionada: {Path.GetFileName(TemplateFilePath)}";
                OnPropertyChanged(nameof(TemplateStatusMessage));
            }
        }        catch (TemplateException ex)
        {
            _logger.LogWarning(ex, "❌ Error de plantilla: {TemplatePath}", ex.TemplatePath ?? "No especificada");
            StatusMessage = $"Error con la plantilla: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al seleccionar plantilla");
            StatusMessage = $"Error al seleccionar plantilla: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearTemplate()
    {
        try
        {
            UseDefaultTemplate = false;
            _logger.LogInformation("🗑️ Uso de plantilla desactivado");
            StatusMessage = "Plantilla desactivada - se usará fondo blanco";
            OnPropertyChanged(nameof(TemplateStatusMessage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al desactivar la plantilla");
            StatusMessage = "Error al desactivar la plantilla";
        }
    }
    
    /// <summary>
    /// Restaura la plantilla predeterminada
    /// </summary>
    [RelayCommand]
    private void RestoreDefaultTemplate()
    {
        try
        {
            var defaultTemplatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", DEFAULT_TEMPLATE_FILE);
            
            if (!File.Exists(defaultTemplatePath))
            {
                throw new TemplateException(
                    $"No se encuentra la plantilla predeterminada: {DEFAULT_TEMPLATE_FILE}",
                    defaultTemplatePath);
            }
            
            TemplateFilePath = defaultTemplatePath;
            UseDefaultTemplate = true;
            _logger.LogInformation("🔄 Plantilla predeterminada restaurada: {Path}", defaultTemplatePath);
            StatusMessage = $"Plantilla predeterminada restaurada: {DEFAULT_TEMPLATE_FILE}";
            OnPropertyChanged(nameof(TemplateStatusMessage));
        }
        catch (TemplateException ex)
        {
            _logger.LogWarning(ex, "❌ Error al restaurar plantilla predeterminada");
            StatusMessage = $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al restaurar plantilla predeterminada");
            StatusMessage = "Error al restaurar plantilla predeterminada";
        }
    }    [RelayCommand(CanExecute = nameof(CanGenerateDocuments))]
    private async Task GenerateDocuments()
    {
        if (IsProcessing) return;

        try
        {
            IsProcessing = true;
            IsProcessingCompleted = false;
            
            // Resetear progreso usando el servicio suavizado
            _smoothProgress.SetValueDirectly(0);
            CurrentDocument = 0;
            TotalDocuments = 0;
            GeneratedDocuments = new List<GeneratedPdfInfo>();
            ShowCompletionPanel = false;

            _cancellationTokenSource = new CancellationTokenSource();
            
            _logger.LogInformation("🚀 Iniciando generación de documentos PDF");

            // Validación previa de archivos y carpetas con excepciones específicas
            ValidateInputs();
            
            _logger.LogInformation("📊 Archivo Excel: {ExcelPath}", SelectedExcelFilePath);
            _logger.LogInformation("📁 Carpeta de salida: {OutputPath}", OutputFolderPath);
            _logger.LogInformation("🖼️ Plantilla: {Template}", UseDefaultTemplate ? TemplateFilePath : "Sin plantilla");

            StatusMessage = "Generando documentos PDF...";
            LogText += $"\n{DateTime.Now:HH:mm:ss} - Iniciando generación de documentos PDF...\n";

            var templateToUse = UseDefaultTemplate ? TemplateFilePath : null;
            var result = await _pdfGenerator.GenerateEstadosCuentaAsync(
                SelectedExcelFilePath,
                OutputFolderPath,
                templateToUse,
                new Progress<(int current, int total, string status)>(OnProgressUpdated),
                _cancellationTokenSource.Token
            );

            if (_cancellationTokenSource.Token.IsCancellationRequested)
            {
                StatusMessage = "Generación cancelada por el usuario";
                _logger.LogWarning("⚠️ Generación de documentos cancelada por el usuario");
            }            else if (result?.Count > 0)
            {
                GeneratedDocuments = result;
                TotalDocuments = GeneratedDocuments.Count;
                IsProcessingCompleted = true;
                StatusMessage = $"✅ Generación completada: {TotalDocuments} documentos generados";
                _logger.LogInformation("✅ Generación completada exitosamente: {Count} documentos", TotalDocuments);

                // Completar el progreso suavemente al 100%
                _smoothProgress.Report(100);
                await Task.Delay(200); // Pequeña pausa para mostrar la finalización

                // Guardar lista de documentos generados
                await SaveGeneratedDocumentsList();
                
                var warnings = _pdfGenerator.LastGenerationWarnings;

                // Mostrar panel de finalización con mensaje personalizado
                CompletionMessage = $"🎉 ¡Generación completada exitosamente!\n\n" +
                                   $"📊 Documentos generados: {TotalDocuments}\n" +
                                   $"📁 Ubicación: {OutputFolderPath}\n\n" +
                                   $"💡 Siguiente paso: Configure el envío automático de correos para entregar " +
                                   $"los documentos directamente a sus clientes.";

                if (warnings.Count > 0)
                {
                    StatusMessage = $"⚠️ Generación con {warnings.Count} empresa(s) sin PDF de {TotalDocuments + warnings.Count}";
                    CompletionMessage += $"\n\n⚠️ {warnings.Count} empresa(s) NO se generaron:\n- " +
                                         string.Join("\n- ", warnings);
                    LogText += $"\n⚠️ {warnings.Count} empresa(s) sin PDF:\n- {string.Join("\n- ", warnings)}\n";
                }

                ShowCompletionPanel = true;
            }
            else
            {
                var warnings = _pdfGenerator.LastGenerationWarnings;
                if (warnings.Count > 0)
                {
                    StatusMessage = "❌ Ningún documento se pudo generar";
                    CompletionMessage = $"⚠️ No se generó ningún documento.\n\n" +
                                        $"Empresas con error:\n- {string.Join("\n- ", warnings)}";
                    LogText += $"\n❌ Ningún PDF generado:\n- {string.Join("\n- ", warnings)}\n";
                    ShowCompletionPanel = true;
                }
                else
                {
                    StatusMessage = "❌ Error en la generación";
                }
                _logger.LogWarning("❌ Error en la generación de documentos");
            }
        }        catch (OperationCanceledException)
        {
            StatusMessage = "Generación cancelada";
            _logger.LogWarning("⚠️ Generación de documentos cancelada");
        }
        catch (DocumentValidationException ex)
        {
            // Error de validación (archivo no encontrado, etc.)
            StatusMessage = $"❌ Error de validación: {ex.Message}";
            LogText += $"\n⚠️ Error de validación: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error de validación durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ No se pudo completar la operación\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Archivo: {ex.FilePath}\n" +
                               $"Regla de validación: {ex.ValidationRule}";
            ShowCompletionPanel = true;
        }
        catch (DocumentFormatException ex)
        {
            // Error de formato del documento
            StatusMessage = $"❌ Error de formato: {ex.Message}";
            LogText += $"\n⚠️ Error de formato en documento: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error de formato durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ Error de formato en el archivo\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Archivo: {ex.FilePath}\n" +
                               $"Formato esperado: {ex.ExpectedFormat}";
            ShowCompletionPanel = true;
        }
        catch (DocumentDataException ex)
        {
            // Error en los datos del documento
            StatusMessage = $"❌ Error en los datos: {ex.Message}";
            LogText += $"\n⚠️ Error en los datos: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error en los datos durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ Error en los datos del archivo\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Origen de datos: {ex.DataSource ?? "No especificado"}";
            ShowCompletionPanel = true;
        }
        catch (PdfGenerationException ex)
        {
            // Error específico en la generación de PDF
            StatusMessage = $"❌ Error al generar PDF: {ex.Message}";
            LogText += $"\n⚠️ Error al generar PDF: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error al generar PDF durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ Error al generar los documentos PDF\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Ubicación: {ex.OutputPath ?? "No especificada"}";
            ShowCompletionPanel = true;
        }
        catch (TemplateException ex)
        {
            // Error con la plantilla
            StatusMessage = $"❌ Error en la plantilla: {ex.Message}";
            LogText += $"\n⚠️ Error en la plantilla: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error en la plantilla durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ Error con la plantilla del documento\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Plantilla: {ex.TemplatePath ?? "No especificada"}";
            ShowCompletionPanel = true;
        }
        catch (GestLogDocumentException ex)
        {
            // Cualquier otra excepción de documento no capturada específicamente
            StatusMessage = $"❌ Error: {ex.Message}";
            LogText += $"\n⚠️ Error en documento: {ex.Message}\n";
            _logger.LogWarning(ex, "❌ Error durante la generación de documentos. Código: {ErrorCode}", ex.ErrorCode);
            
            // Mostrar panel de finalización con mensaje personalizado para este error
            CompletionMessage = $"⚠️ Error durante la generación\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Código de error: {ex.ErrorCode}";
            ShowCompletionPanel = true;
        }
        catch (Exception ex)
        {
            // Cualquier otra excepción inesperada
            StatusMessage = $"❌ Error inesperado: {ex.Message}";
            LogText += $"\n⚠️ Error inesperado: {ex.Message}\n";
            _logger.LogError(ex, "❌ Error inesperado durante la generación de documentos");
            
            // Mostrar panel de finalización con mensaje personalizado para error genérico
            CompletionMessage = $"⚠️ Error inesperado\n\n" +
                               $"Problema: {ex.Message}\n\n" +
                               $"Si el problema persiste, contacte al soporte técnico.";
            ShowCompletionPanel = true;
        }        finally
        {
            // Asegurar que se limpian adecuadamente todos los recursos
            try
            {
                // Marcar como no en proceso
                IsProcessing = false;
                
                // Liberar el token de cancelación
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                    _logger.LogDebug("✅ Token de cancelación liberado correctamente");
                }
                
                // Asegurar que la UI refleja el estado final
                CommandManager.InvalidateRequerySuggested();
                NotifyCommandsCanExecuteChanged();
                
                _logger.LogInformation("✅ Proceso de generación finalizado y recursos liberados");
            }
            catch (Exception ex)
            {
                // Manejo de excepciones durante la limpieza para evitar crasheos
                _logger.LogError(ex, "❌ Error durante la liberación de recursos");
            }
        }
    }    [RelayCommand(CanExecute = nameof(CanOpenOutputFolder))]
    private void OpenOutputFolder()
    {
        try
        {
            // Verificar que la carpeta existe antes de abrir
            if (string.IsNullOrWhiteSpace(OutputFolderPath))
            {
                throw new DocumentValidationException(
                    "No se ha especificado una carpeta de salida",
                    string.Empty,
                    "OUTPUT_FOLDER_EMPTY");
            }
            
            if (!Directory.Exists(OutputFolderPath))
            {
                // Intentar crear la carpeta si no existe
                try
                {
                    Directory.CreateDirectory(OutputFolderPath);
                    _logger.LogInformation("📁 Se creó la carpeta de salida: {Path}", OutputFolderPath);
                }
                catch (Exception ex)
                {
                    throw new DocumentValidationException(
                        $"No se pudo crear la carpeta de salida: {OutputFolderPath}",
                        OutputFolderPath,
                        "FOLDER_CREATE_ERROR",
                        ex);
                }
            }
            
            // Ahora que sabemos que la carpeta existe, abrirla
            System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
            _logger.LogInformation("📂 Carpeta de salida abierta: {Path}", OutputFolderPath);
            StatusMessage = $"Carpeta abierta: {OutputFolderPath}";
        }
        catch (DocumentValidationException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogWarning(ex, "⚠️ Error de validación al abrir carpeta: {ErrorCode}", ex.ValidationRule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al abrir carpeta de salida");
            StatusMessage = $"Error al abrir carpeta: {ex.Message}";
        }
    }
    
    [RelayCommand(CanExecute = nameof(IsProcessing))]
    private void CancelGeneration()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("🚫 Cancelación de generación solicitada por el usuario");
            StatusMessage = "Cancelando generación...";
        }
        catch (Exception ex)        {
            _logger.LogError(ex, "Error al cancelar generación");
            StatusMessage = "Error al cancelar";
        }
    }
    
    [RelayCommand]
    public void ResetProgressData()
    {
        try
        {
            ResetProgress();
            _logger.LogInformation("🔄 Progreso de generación reiniciado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reiniciar progreso");
            StatusMessage = "Error al reiniciar";
        }
    }
    
    [RelayCommand]
    public void GoToEmailTab()
    {
        try
        {
            _logger.LogInformation("🚀 Usuario navegó a la pestaña de envío de correos");
            
            // Buscar el TabControl en la vista y cambiar a la segunda pestaña (Envío Automático)
            // Este método será llamado desde el XAML y necesita interactuar con la vista
            ShowCompletionPanel = false; // Ocultar el panel de finalización
            
            // Crear mensaje para el log del sistema
            LogText += "\n📧 Navegando a la pestaña de Envío Automático...";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al navegar a la pestaña de email");
        }
    }
    
    protected override void ResetProgress()
    {
        base.ResetProgress();
        // Resetear el progreso suavizado también
        _smoothProgress.SetValueDirectly(0);
        // Ocultar panel de finalización
        ShowCompletionPanel = false;
        CompletionMessage = string.Empty;
    }

    public bool CanGenerateDocuments()
    {
        bool isNotProcessing = !IsProcessing;
        bool hasExcelPath = !string.IsNullOrWhiteSpace(SelectedExcelFilePath);
        bool excelExists = FileExistsWithNetworkSupport(SelectedExcelFilePath);
        bool hasOutputPath = !string.IsNullOrWhiteSpace(OutputFolderPath);
        
        return isNotProcessing && hasExcelPath && excelExists && hasOutputPath;
    }

    private bool CanOpenOutputFolder() => 
        !string.IsNullOrWhiteSpace(OutputFolderPath) && 
        Directory.Exists(OutputFolderPath);    private void OnProgressUpdated((int current, int total, string status) progress)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CurrentDocument = progress.current;
            TotalDocuments = progress.total;
            
            // Calcular el progreso y usar el servicio suavizado para animación fluida
            var progressPercentage = progress.total > 0 ? (double)progress.current / progress.total * 100 : 0;
            _smoothProgress.Report(progressPercentage);
            
            StatusMessage = progress.status;
            
            if (!string.IsNullOrEmpty(progress.status))
            {
                LogText += $"{DateTime.Now:HH:mm:ss} - {progress.status}\n";
            }
        });
    }

    private async Task SaveGeneratedDocumentsList()
    {
        try
        {
            var textFilePath = Path.Combine(OutputFolderPath, "pdfs_generados.txt");
            var lines = new List<string>
            {
                $"Fecha de generación: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Total de PDFs generados: {GeneratedDocuments.Count}",
                "-------------------------------------------------------------"
            };

            foreach (var doc in GeneratedDocuments)
            {
                lines.Add($"Empresa: {doc.NombreEmpresa}");
                lines.Add($"NIT: {doc.Nit}");
                lines.Add($"Archivo: {doc.NombreArchivo}");
                lines.Add($"Tipo: {doc.TipoCartera}");
                lines.Add($"Ruta: {doc.RutaArchivo}");
                lines.Add("-------------------------------------------------------------");
            }

            await File.WriteAllLinesAsync(textFilePath, lines);
            _logger.LogInformation("💾 Lista de documentos guardada en: {Path}", textFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error guardando lista de documentos generados");
        }
    }    /// <summary>
    /// Verifica la existencia de un archivo, con manejo específico para rutas de red
    /// </summary>
    private bool FileExistsWithNetworkSupport(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
            
        try
        {
            // Para rutas de red, implementamos un sistema más robusto
            if (filePath.StartsWith(@"\\"))
            {
                // Implementar reintentos para mayor robustez en rutas de red
                int maxRetries = 3;
                int retryDelayMs = 500;
                
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        bool exists = fileInfo.Exists;
                        
                        // Si lo encontramos, devolvemos inmediatamente
                        if (exists) return true;
                        
                        // Si no existe y no es el último intento, esperamos antes de reintentar
                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(retryDelayMs);
                            retryDelayMs *= 2; // Backoff exponencial
                        }
                    }
                    catch (IOException)
                    {
                        if (attempt < maxRetries)
                        {
                            Thread.Sleep(retryDelayMs);
                            retryDelayMs *= 2;
                        }
                    }
                }
                
                return false;
            }
            
            // Ruta local normal
            return File.Exists(filePath);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error verificando existencia del archivo: {FilePath}. Error: {Error}", 
                filePath, ex.Message);
            return false;
        }
    }/// <summary>
    /// Notifica que el estado de los comandos puede haber cambiado, asegurando que la notificación
    /// se ejecuta en el hilo de UI
    /// </summary>
    private void NotifyCommandsCanExecuteChanged()
    {
        // Asegurar que la notificación se ejecuta en el hilo de UI
        if (System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            GenerateDocumentsCommand.NotifyCanExecuteChanged();
        }
        else
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                GenerateDocumentsCommand.NotifyCanExecuteChanged();
            });
        }
    }
    
    /// <summary>
    /// Valida las entradas antes de iniciar la generación de documentos
    /// </summary>
    /// <exception cref="DocumentValidationException">Si hay problemas de validación</exception>
    /// <exception cref="TemplateException">Si hay problemas con la plantilla</exception>
    private void ValidateInputs()
    {
        // Validar archivo Excel
        if (string.IsNullOrWhiteSpace(SelectedExcelFilePath))
        {
            throw new DocumentValidationException(
                "No se ha seleccionado un archivo Excel",
                string.Empty,
                "EXCEL_NOT_SELECTED");
        }
        
        if (!File.Exists(SelectedExcelFilePath))
        {
            throw new DocumentValidationException(
                $"El archivo Excel seleccionado no existe: {Path.GetFileName(SelectedExcelFilePath)}",
                SelectedExcelFilePath,
                "FILE_NOT_FOUND");
        }
        
        // Validar extensión del archivo
        string extension = Path.GetExtension(SelectedExcelFilePath).ToLowerInvariant();
        if (extension != ".xlsx" && extension != ".xls")
        {
            throw new DocumentFormatException(
                $"El archivo seleccionado no tiene formato Excel válido: {Path.GetFileName(SelectedExcelFilePath)}",
                SelectedExcelFilePath,
                "XLSX_XLS");
        }
        
        // Validar carpeta de salida
        if (string.IsNullOrWhiteSpace(OutputFolderPath))
        {
            throw new DocumentValidationException(
                "No se ha seleccionado una carpeta de salida",
                string.Empty,
                "OUTPUT_FOLDER_NOT_SELECTED");
        }
        
        // Validar plantilla si está activada
        if (UseDefaultTemplate && !string.IsNullOrEmpty(TemplateFilePath))
        {
            if (!File.Exists(TemplateFilePath))
            {
                throw new TemplateException(
                    $"No se encuentra el archivo de plantilla: {Path.GetFileName(TemplateFilePath)}",
                    TemplateFilePath);
            }
            
            // Validar que la plantilla es una imagen
            string templateExt = Path.GetExtension(TemplateFilePath).ToLowerInvariant();
            if (templateExt != ".png" && templateExt != ".jpg" && templateExt != ".jpeg" && templateExt != ".bmp")
            {
                throw new TemplateException(
                    $"El archivo de plantilla no es una imagen válida: {Path.GetFileName(TemplateFilePath)}",
                    TemplateFilePath);
            }
        }
    }
    
    partial void OnSelectedExcelFilePathChanged(string value)
    {
        NotifyCommandsCanExecuteChanged();
        OnPropertyChanged(nameof(SelectedExcelFilePath));
    }
}
