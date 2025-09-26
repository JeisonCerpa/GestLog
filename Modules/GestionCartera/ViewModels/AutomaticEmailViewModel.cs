using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.UI;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para funcionalidades de email automático
/// </summary>
public partial class AutomaticEmailViewModel : ObservableObject
{
    private readonly IEmailService? _emailService;
    private readonly IExcelEmailService? _excelEmailService;
    private readonly IGestLogLogger _logger;    
    [ObservableProperty] private string _selectedEmailExcelFilePath = string.Empty;
    [ObservableProperty] private bool _hasEmailExcel = false;
    [ObservableProperty] private bool _isSendingEmail = false;    
    [ObservableProperty] private int _companiesWithEmail = 0;
    [ObservableProperty] private int _companiesWithoutEmail = 0;
    [ObservableProperty] private string _logText = string.Empty;
    
    // Nuevas propiedades para mejorar la experiencia de usuario
    [ObservableProperty] private string _companiesStatusText = "Sin archivo Excel";
    [ObservableProperty] private bool _hasDocumentsGenerated = false;
    [ObservableProperty] private string _documentStatusWarning = string.Empty;
    
    // Propiedades de progreso para envío de emails
    [ObservableProperty] private double _emailProgressValue = 0.0;
    [ObservableProperty] private string _emailStatusMessage = string.Empty;
    [ObservableProperty] private int _currentEmailDocument = 0;
    [ObservableProperty] private int _totalEmailDocuments = 0;
    
    // Sistema de cancelación para emails
    private CancellationTokenSource? _emailCancellationTokenSource;
    
    // Servicio de progreso suavizado para animaciones fluidas
    private SmoothProgressService _smoothProgress = null!;
    
    // Propiedades adicionales necesarias para el wrapper
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _emailSubject = "Estado de Cartera - Documentos";
    [ObservableProperty] private string _emailBody = "Estimado cliente,\n\nAdjunto encontrará los documentos de estado de cartera solicitados.\n\nSaludos cordiales,\nSIMICS GROUP S.A.S.";
    [ObservableProperty] private string _emailRecipients = string.Empty;
    [ObservableProperty] private string _emailCc = string.Empty;
    [ObservableProperty] private string _emailBcc = string.Empty;
    [ObservableProperty] private bool _useHtmlEmail = true;
    [ObservableProperty] private bool _isEmailConfigured = false;
    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();

    public bool CanSendAutomatically => CanSendDocumentsAutomatically();

    /// <summary>
    /// Determina si se puede cancelar el envío de emails
    /// </summary>
    public bool CanCancelEmailSending 
    {
        get
        {
            var canCancel = IsSendingEmail && _emailCancellationTokenSource != null;
            return canCancel;
        }
    }    
    public AutomaticEmailViewModel(
        IEmailService? emailService,
        IExcelEmailService? excelEmailService,
        IGestLogLogger logger)
    {
        _emailService = emailService;
        _excelEmailService = excelEmailService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Inicializar servicio de progreso suavizado
        _smoothProgress = new SmoothProgressService(value => EmailProgressValue = value);
        
        // ✨ INICIALIZAR estados por defecto
        CompaniesStatusText = "Sin archivo Excel";
        DocumentStatusWarning = string.Empty;
        HasDocumentsGenerated = false;
    }
    
    [RelayCommand]
    public async Task SelectEmailExcelFileAsync()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Archivos Excel (*.xlsx)|*.xlsx|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar Archivo Excel con Correos Electrónicos"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedEmailExcelFilePath = openFileDialog.FileName;
                HasEmailExcel = !string.IsNullOrEmpty(SelectedEmailExcelFilePath);
                
                _logger.LogInformation($"📧 Archivo de correos seleccionado: {Path.GetFileName(SelectedEmailExcelFilePath)}");
                
                ValidateEmailExcelFileAsync();
                
                // Analizar matching con documentos generados
                await AnalyzeEmailMatchingAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel de correos");
            LogText += $"\n❌ Error: {ex.Message}";
        }
    }    
    
    /// <summary>
    /// Cancela el envío de emails en curso
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCancelEmailSending))]
    public void CancelEmailSending()
    {
        // Cancelar la operación
        if (_emailCancellationTokenSource != null)
        {
            _emailCancellationTokenSource.Cancel();
        }
        
        // Actualizar la UI
        EmailStatusMessage = "Cancelando envío de emails...";
        
        // Notificar cambio en la capacidad de cancelar
        OnPropertyChanged(nameof(CanCancelEmailSending));
    }
    
    /// <summary>
    /// Analiza el matching entre documentos generados y correos del Excel
    /// </summary>
    private async Task AnalyzeEmailMatchingAsync()
    {        
        if (_excelEmailService == null || string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath) || !HasEmailExcel)
        {
            CompaniesWithEmail = 0;
            CompaniesWithoutEmail = 0;
            CompaniesStatusText = "Sin archivo Excel";
            DocumentStatusWarning = string.Empty;
            HasDocumentsGenerated = false;
            
            // ✨ CORRECCIÓN: Notificar cambios cuando se resetean valores
            OnPropertyChanged(nameof(CompaniesWithEmail));
            OnPropertyChanged(nameof(CompaniesStatusText));
            OnPropertyChanged(nameof(DocumentStatusWarning));
            OnPropertyChanged(nameof(HasDocumentsGenerated));
            return;
        }

        try
        {
            LogText += $"\n🎯 Analizando archivo de correos...";
            
            // Obtener información básica del archivo de correos
            var validationInfo = await _excelEmailService.GetValidationInfoAsync(SelectedEmailExcelFilePath);
            
            if (!validationInfo.IsValid)
            {
                LogText += "\n❌ No se puede analizar: archivo de correos no válido";
                CompaniesWithEmail = 0;
                CompaniesWithoutEmail = 0;
                CompaniesStatusText = "Archivo inválido";
                DocumentStatusWarning = string.Empty;
                HasDocumentsGenerated = false;
                
                // ✨ CORRECCIÓN: Notificar cambios cuando hay archivo inválido
                OnPropertyChanged(nameof(CompaniesWithEmail));
                OnPropertyChanged(nameof(CompaniesStatusText));
                OnPropertyChanged(nameof(DocumentStatusWarning));
                OnPropertyChanged(nameof(HasDocumentsGenerated));
                return;
            }

            // Obtener el conteo real de empresas con correos a través del diccionario completo
            var emailMappings = await _excelEmailService.GetEmailsFromExcelAsync(SelectedEmailExcelFilePath);
            var realCompaniesWithEmail = emailMappings.Count;
            
            LogText += $"\n📋 Archivo de correos: {realCompaniesWithEmail} empresas con correos válidos";
            
            // Mostrar información básica del archivo Excel independientemente de los documentos generados
            CompaniesWithEmail = realCompaniesWithEmail; // Conteo real basado en el índice completo
            
            // ✨ CORRECCIÓN: Notificar cambio de CompaniesWithEmail
            OnPropertyChanged(nameof(CompaniesWithEmail));
            
            // Si no hay documentos generados, cargarlos para análisis completo
            if (GeneratedDocuments.Count == 0)
            {
                LogText += "\n📄 Cargando documentos generados para análisis de efectividad...";
                var loadedDocuments = await LoadGeneratedDocuments();
                UpdateGeneratedDocuments(loadedDocuments);
            }

            if (GeneratedDocuments.Count == 0)
            {
                LogText += "\n💡 Información mostrada basada en el archivo Excel";
                LogText += "\n💡 Genere documentos primero para ver análisis de efectividad completo";
                
                // ✨ MEJORA: Indicar claramente que es información del Excel, no análisis de documentos
                CompaniesStatusText = $"📊 En archivo Excel: {realCompaniesWithEmail}";
                DocumentStatusWarning = "⚠️ Genere documentos primero para análisis completo";
                HasDocumentsGenerated = false;
                CompaniesWithoutEmail = 0; // No sabemos cuántos están sin correo hasta que generemos documentos
                
                // ✨ IMPORTANTE: Notificar cambios de propiedades
                OnPropertyChanged(nameof(CompaniesStatusText));
                OnPropertyChanged(nameof(DocumentStatusWarning));
                OnPropertyChanged(nameof(HasDocumentsGenerated));
                
                return;
            }

            LogText += $"\n🎯 Analizando efectividad para {GeneratedDocuments.Count} documentos generados...";
            
            // ✨ MEJORA: Indicar que ahora sí tenemos análisis de documentos real
            CompaniesStatusText = $"📄 Con email: {CompaniesWithEmail} de {GeneratedDocuments.Count}";
            DocumentStatusWarning = string.Empty;
            HasDocumentsGenerated = true;
            
            // ✨ IMPORTANTE: Notificar cambios de propiedades
            OnPropertyChanged(nameof(CompaniesStatusText));
            OnPropertyChanged(nameof(DocumentStatusWarning));
            OnPropertyChanged(nameof(HasDocumentsGenerated));
            
            // ANÁLISIS PRINCIPAL: ¿Cuántos documentos generados tienen email disponible?
            await AnalyzeDocumentEmailMatchingAsync();
            
        }        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analizando matching de correos");
            LogText += $"\n❌ Error en análisis: {ex.Message}";
            
            // ✨ MEJORA: Estado de error claro
            CompaniesWithEmail = 0; // ✨ CORRECCIÓN: Resetear también la propiedad original
            CompaniesStatusText = "❌ Error en análisis";
            DocumentStatusWarning = "Error procesando información";
            HasDocumentsGenerated = false;
            
            // ✨ IMPORTANTE: Notificar cambios de propiedades
            OnPropertyChanged(nameof(CompaniesWithEmail)); // ¡Notificar el reset!
            OnPropertyChanged(nameof(CompaniesStatusText));
            OnPropertyChanged(nameof(DocumentStatusWarning));
            OnPropertyChanged(nameof(HasDocumentsGenerated));
        }
    }
    
    /// <summary>
    /// Analiza el matching específico entre documentos generados y correos disponibles
    /// ENFOQUE CORRECTO: Partir de los documentos y verificar cuáles tienen email
    /// </summary>
    private async Task AnalyzeDocumentEmailMatchingAsync()
    {
        try
        {
            var documentsWithEmail = 0;
            var documentsWithoutEmail = 0;
            var totalEmailsFound = 0;
            var companiesWithMultipleEmails = 0;
            
            LogText += $"\n🔍 Verificando emails para cada documento generado:";

            foreach (var document in GeneratedDocuments)
            {
                try
                {
                    var emails = await _excelEmailService!.GetEmailsForCompanyAsync(
                        SelectedEmailExcelFilePath, 
                        document.CompanyName ?? "N/A", 
                        document.Nit ?? "");

                    if (emails.Count > 0)
                    {
                        documentsWithEmail++;
                        totalEmailsFound += emails.Count;
                        
                        if (emails.Count > 1)
                        {
                            companiesWithMultipleEmails++;
                        }
                        
                        // Log detalles para las primeras empresas
                        if (documentsWithEmail <= 5)
                        {
                            LogText += $"\n   ✅ {document.CompanyName} → {emails.Count} email(s)";
                        }
                    }
                    else
                    {
                        documentsWithoutEmail++;
                        
                        // Log primeras empresas sin email
                        if (documentsWithoutEmail <= 5)
                        {
                            LogText += $"\n   ❌ {document.CompanyName} (NIT: {document.Nit}) → Sin email";
                        }
                    }
                }
                catch
                {
                    // Error en documento individual - contar como sin email
                    documentsWithoutEmail++;
                }
            }

            var totalDocuments = GeneratedDocuments.Count;
            var effectivenessPercentage = totalDocuments > 0 
                ? (documentsWithEmail * 100.0 / totalDocuments) 
                : 0;
            
            // Actualizar propiedades para la UI
            CompaniesWithEmail = documentsWithEmail;
            CompaniesWithoutEmail = documentsWithoutEmail;
            
            // ✨ MEJORA: Actualizar texto de estado para mostrar efectividad real
            CompaniesStatusText = $"📄 Con email: {documentsWithEmail} de {totalDocuments}";
            DocumentStatusWarning = effectivenessPercentage < 50 ? "⚠️ Baja efectividad de envío" : string.Empty;
            HasDocumentsGenerated = true;
            
            // ✨ IMPORTANTE: Notificar cambios de propiedades
            OnPropertyChanged(nameof(CompaniesStatusText));
            OnPropertyChanged(nameof(DocumentStatusWarning));
            OnPropertyChanged(nameof(HasDocumentsGenerated));

            // Resumen enfocado en documentos generados
            LogText += $"\n";
            LogText += $"\n📊 RESUMEN DE EFECTIVIDAD:";
            LogText += $"\n   📄 Total documentos generados: {totalDocuments}";
            LogText += $"\n   ✅ Documentos con destinatario: {documentsWithEmail}";
            LogText += $"\n   ❌ Documentos sin destinatario: {documentsWithoutEmail}";
            LogText += $"\n   🎯 Efectividad de envío: {effectivenessPercentage:F1}%";
            LogText += $"\n   📧 Total emails encontrados: {totalEmailsFound}";
            
            if (companiesWithMultipleEmails > 0)
            {
                LogText += $"\n   📮 Empresas con múltiples emails: {companiesWithMultipleEmails}";
            }

            // Recomendaciones basadas en efectividad
            if (effectivenessPercentage < 30)
            {
                LogText += $"\n⚠️ BAJA EFECTIVIDAD: Verifique que los NITs coincidan entre archivos";
            }
            else if (effectivenessPercentage < 70)
            {
                LogText += $"\n💡 EFECTIVIDAD MEDIA: Considere actualizar emails faltantes";
            }
            else
            {
                LogText += $"\n🎉 BUENA EFECTIVIDAD: La mayoría de documentos tienen destinatario";
            }

            if (GeneratedDocuments.Count > 10 && (documentsWithoutEmail <= 5 || documentsWithEmail <= 5))
            {
                LogText += $"\n   ℹ️ (Mostrando primeros 5 ejemplos de cada categoría)";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en análisis de matching específico");
            LogText += $"\n❌ Error en análisis detallado: {ex.Message}";
        }
    }

    /// <summary>
    /// Carga los documentos generados desde el archivo pdfs_generados.txt
    /// </summary>
    private async Task<List<GeneratedPdfInfo>> LoadGeneratedDocuments()
    {
        var documents = new List<GeneratedPdfInfo>();
        
        try
        {
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Archivos", "Clientes cartera pdf");
            var textFilePath = Path.Combine(outputPath, "pdfs_generados.txt");
            
            if (!File.Exists(textFilePath))
            {
                _logger.LogWarning("No se encontró archivo pdfs_generados.txt en: {Path}", textFilePath);
                return documents;
            }

            _logger.LogInformation("📖 Leyendo archivo de documentos generados: {FilePath}", textFilePath);
            
            var lines = await File.ReadAllLinesAsync(textFilePath) ?? Array.Empty<string>();
            
            string? empresa = null;
            string? nit = null;
            string? archivo = null;
            string? tipo = null;
            string? ruta = null;
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                // Líneas de separación o información general
                if (line.StartsWith("=") || line.StartsWith("PDF") || line.StartsWith("Total") || 
                    line.StartsWith("Fecha de generación") || line.StartsWith("--------") || 
                    line.Trim() == "-------------------------------------------------------------")
                    continue;
                
                if (line.StartsWith("Empresa: "))
                {
                    empresa = line.Replace("Empresa: ", "").Trim();
                }
                else if (line.StartsWith("NIT: "))
                {
                    nit = line.Replace("NIT: ", "").Trim();
                }
                else if (line.StartsWith("Archivo: "))
                {
                    archivo = line.Replace("Archivo: ", "").Trim();
                }
                else if (line.StartsWith("Tipo: "))
                {
                    tipo = line.Replace("Tipo: ", "").Trim();
                }
                else if (line.StartsWith("Ruta: "))
                {
                    ruta = line.Replace("Ruta: ", "").Trim();
                    
                    // Si tenemos ruta, es el final de un bloque de documento
                    if (!string.IsNullOrEmpty(empresa) && !string.IsNullOrEmpty(nit) && 
                        !string.IsNullOrEmpty(archivo) && !string.IsNullOrEmpty(ruta))
                    {
                        // Verificar que el archivo existe físicamente
                        if (File.Exists(ruta))
                        {
                            var document = new GeneratedPdfInfo
                            {
                                NombreArchivo = archivo,
                                NombreEmpresa = empresa,
                                Nit = nit,
                                RutaArchivo = ruta
                            };
                            
                            documents.Add(document);
                        }
                    }
                    
                    // Resetear variables para el siguiente documento
                    empresa = null;
                    nit = null;
                    archivo = null;
                    tipo = null;
                    ruta = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error leyendo archivo de documentos generados");
        }
        
        return documents;
    }

    public async Task<bool> SendDocumentsAutomaticallyAsync(
        IReadOnlyList<GeneratedPdfInfo> documents, 
        SmtpConfigurationViewModel smtpConfig,
        CancellationToken cancellationToken = default)
    {        
        if (_emailService == null || _excelEmailService == null)
        {
            _logger.LogWarning("Servicios de email no disponibles para envío automático");
            return false;
        }

        if (!documents.Any())
        {
            _logger.LogWarning("No hay documentos para enviar");
            return false;
        }

        if (string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath))
        {
            _logger.LogWarning("No hay archivo Excel seleccionado para mapear emails");
            return false;
        }
        
        try
        {
            IsSendingEmail = true;
            
            // Inicializar progreso suave
            _smoothProgress.SetValueDirectly(0);
            CurrentEmailDocument = 0;
            TotalEmailDocuments = documents.Count;
            EmailStatusMessage = "Iniciando envío de emails...";
            
            // Crear token de cancelación para emails
            _emailCancellationTokenSource = new CancellationTokenSource();
            
            // Notificar cambios en comandos
            OnPropertyChanged(nameof(CanCancelEmailSending));
            CancelEmailSendingCommand.NotifyCanExecuteChanged();
            
            LogText += "\n🚀 Iniciando envío automático de documentos...\n";

            // Configurar SMTP
            await ConfigureSmtpFromConfigAsync(smtpConfig);

            // Procesar envíos
            var result = await ProcessAutomaticEmailSendingAsync(documents, _emailCancellationTokenSource.Token);

            LogText += result ? "\n✅ Envío automático completado" : "\n❌ Envío automático falló";
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("📧 Envío de emails cancelado por el usuario");
            LogText += "\n⏹️ Envío de emails cancelado por el usuario";
            EmailStatusMessage = "Envío cancelado por el usuario";
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el envío automático");
            LogText += $"\n❌ Error durante envío automático: {ex.Message}";
            return false;
        }        
        finally
        {
            IsSendingEmail = false;
            
            // Limpiar token de cancelación
            _emailCancellationTokenSource?.Dispose();
            _emailCancellationTokenSource = null;
            
            // Actualizar comandos
            OnPropertyChanged(nameof(CanCancelEmailSending));
            CancelEmailSendingCommand.NotifyCanExecuteChanged();
            
            // Completar progreso si quedó incompleto
            if (EmailProgressValue > 0 && EmailProgressValue < 100)
            {
                _smoothProgress.Report(100);
                await Task.Delay(200); // Pausa visual
                EmailStatusMessage = "Proceso finalizado";
            }
        }
    }
    
    private async void ValidateEmailExcelFileAsync()
    {
        try
        {
            if (_excelEmailService == null)
            {
                _logger.LogWarning("⚠️ Servicio ExcelEmailService no disponible");
                LogText += "\n⚠️ Servicio de correos no disponible";
                HasEmailExcel = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath) || !File.Exists(SelectedEmailExcelFilePath))
            {
                _logger.LogWarning("❌ Archivo Excel de correos no existe o no está seleccionado");
                LogText += "\n❌ Error: Debe seleccionar un archivo Excel válido";
                HasEmailExcel = false;
                SelectedEmailExcelFilePath = string.Empty;
                return;
            }

            // Validar estructura del archivo Excel de correos
            LogText += $"\n🔍 Validando estructura del archivo: {Path.GetFileName(SelectedEmailExcelFilePath)}";
            
            var validationResult = await _excelEmailService.GetValidationInfoAsync(SelectedEmailExcelFilePath);
            
            if (validationResult.IsValid)
            {
                _logger.LogInformation("✅ Archivo Excel de correos válido: {ValidNits} NITs, {ValidEmails} emails", 
                    validationResult.ValidNitRows, validationResult.ValidEmailRows);
                
                LogText += $"\n✅ Archivo válido:";
                LogText += $"\n   📊 {validationResult.TotalRows} filas de datos";
                LogText += $"\n   🏢 {validationResult.ValidNitRows} registros NIT válidos";
                LogText += $"\n   📧 {validationResult.ValidEmailRows} emails válidos";
                
                HasEmailExcel = true;
                
                // ✨ MEJORA: Establecer estado inicial correcto al validar archivo Excel
                CompaniesWithEmail = validationResult.ValidNitRows; // Esto es lo que faltaba!
                
                if (GeneratedDocuments.Count == 0)
                {
                    CompaniesStatusText = $"📊 En archivo Excel: {validationResult.ValidNitRows}";
                    DocumentStatusWarning = "⚠️ Genere documentos primero para análisis completo";
                    HasDocumentsGenerated = false;
                    
                    // Notificar cambios de propiedades
                    OnPropertyChanged(nameof(CompaniesWithEmail));
                    OnPropertyChanged(nameof(CompaniesStatusText));
                    OnPropertyChanged(nameof(DocumentStatusWarning));
                    OnPropertyChanged(nameof(HasDocumentsGenerated));
                }                
                else
                {
                    CompaniesStatusText = "📄 Analizando documentos generados...";
                    DocumentStatusWarning = string.Empty;
                    HasDocumentsGenerated = true;
                    
                    // Notificar cambios de propiedades
                    OnPropertyChanged(nameof(CompaniesWithEmail));
                    OnPropertyChanged(nameof(CompaniesStatusText));
                    OnPropertyChanged(nameof(DocumentStatusWarning));
                    OnPropertyChanged(nameof(HasDocumentsGenerated));
                }
            }            
            else
            {
                _logger.LogWarning("❌ Archivo Excel de correos no válido: {Message}", validationResult.Message);
                LogText += $"\n❌ Archivo no válido: {validationResult.Message}";
                
                if (validationResult.MissingColumns.Length > 0)
                {
                    LogText += $"\n   🔍 Columnas faltantes: {string.Join(", ", validationResult.MissingColumns)}";
                }
                
                if (validationResult.FoundColumns.Length > 0)
                {
                    LogText += $"\n   📋 Columnas encontradas: {string.Join(", ", validationResult.FoundColumns)}";
                }
                
                LogText += "\n   ℹ️ Formato esperado: TIPO_DOC, NUM_ID, DIGITO_VER, EMPRESA, EMAIL";
                
                HasEmailExcel = false;
                SelectedEmailExcelFilePath = string.Empty;
                
                // ✨ RESETEAR estados cuando archivo no es válido
                CompaniesWithEmail = 0; // ¡Resetear también la propiedad original!
                CompaniesStatusText = "Archivo inválido";
                DocumentStatusWarning = "Seleccione un archivo Excel válido";
                HasDocumentsGenerated = false;
                
                // ✨ IMPORTANTE: Notificar cambios de propiedades
                OnPropertyChanged(nameof(CompaniesWithEmail)); // ¡Notificar el reset!
                OnPropertyChanged(nameof(CompaniesStatusText));
                OnPropertyChanged(nameof(DocumentStatusWarning));
                OnPropertyChanged(nameof(HasDocumentsGenerated));
            }
        }        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar archivo Excel de correos");
            LogText += $"\n❌ Error inesperado: {ex.Message}";
            LogText += "\n   💡 Verifique que el archivo no esté abierto en otra aplicación";
            HasEmailExcel = false;
            SelectedEmailExcelFilePath = string.Empty;
            
            // ✨ RESETEAR estados en caso de error
            CompaniesWithEmail = 0; // ¡Resetear también la propiedad original!
            CompaniesStatusText = "❌ Error validando archivo";
            DocumentStatusWarning = "Error procesando archivo Excel";
            HasDocumentsGenerated = false;
            
            // ✨ IMPORTANTE: Notificar cambios de propiedades
            OnPropertyChanged(nameof(CompaniesWithEmail)); // ¡Notificar el reset!
            OnPropertyChanged(nameof(CompaniesStatusText));
            OnPropertyChanged(nameof(DocumentStatusWarning));
            OnPropertyChanged(nameof(HasDocumentsGenerated));
        }
    }
    
    private async Task ConfigureSmtpFromConfigAsync(SmtpConfigurationViewModel config)
    {
        if (_emailService == null) return;

        var smtpConfig = new SmtpConfiguration
        {
            SmtpServer = config.SmtpServer,
            Port = config.SmtpPort,
            Username = config.SmtpUsername,
            Password = config.SmtpPassword,
            EnableSsl = config.EnableSsl,
            BccEmail = config.BccEmail,     // ✅ CORRECCIÓN: Agregar BCC
            CcEmail = config.CcEmail        // ✅ CORRECCIÓN: Agregar CC
        };

        await _emailService.ConfigureSmtpAsync(smtpConfig);
    }
    
    private async Task<bool> ProcessAutomaticEmailSendingAsync(
        IReadOnlyList<GeneratedPdfInfo> documents, 
        CancellationToken cancellationToken)
    {
        if (_emailService == null || _excelEmailService == null) return false;

        var emailsSent = 0;
        var emailsFailed = 0;
        var orphansSent = 0;
        var totalEmails = documents.Count;
        var processedDocuments = 0;
        
        // Actualizar totales iniciales
        TotalEmailDocuments = totalEmails;
        CurrentEmailDocument = 0;
        _smoothProgress.SetValueDirectly(0);
        EmailStatusMessage = "Iniciando procesamiento de documentos...";

        // Obtener la configuración BCC para documentos huérfanos
        var smtpConfig = _emailService.CurrentConfiguration;
        var bccEmail = smtpConfig?.BccEmail;

        // Lista para almacenar documentos huérfanos para envío consolidado
        var orphanDocuments = new List<GeneratedPdfInfo>();

        // Procesar documentos con destinatarios específicos
        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Actualizar progreso con animación suave
                processedDocuments++;
                CurrentEmailDocument = processedDocuments;
                var progressPercentage = (double)processedDocuments / totalEmails * 100;
                _smoothProgress.Report(progressPercentage);
                EmailStatusMessage = $"Procesando {document.NombreEmpresa} ({processedDocuments}/{totalEmails})";
                
                LogText += $"\n  📄 Procesando: {document.NombreArchivo} ({document.NombreEmpresa})";

                var emails = await _excelEmailService.GetEmailsForCompanyAsync(
                    SelectedEmailExcelFilePath, 
                    document.NombreEmpresa, 
                    document.Nit, 
                    cancellationToken);

                if (!emails.Any())
                {
                    // Documento huérfano - agregar a la lista para envío consolidado
                    orphanDocuments.Add(document);
                    LogText += $" 📋 Sin correo → consolidar en BCC";
                    continue;
                }

                // Actualizar estado
                EmailStatusMessage = $"Enviando email a {document.NombreEmpresa}...";

                // Enviar documento con destinatario específico
                var emailInfo = new EmailInfo
                {
                    Recipients = emails.ToList(),
                    Subject = "Estado Cartera - SIMICS GROUP S.A.S",
                    Body = GetCompleteEmailBodyWithSignature(),
                    IsBodyHtml = true
                };

                var result = await _emailService.SendEmailWithAttachmentAsync(emailInfo, document.RutaArchivo, cancellationToken);
                
                if (result.IsSuccess)
                {
                    emailsSent++;
                    LogText += $" ✅ Enviado a {emails.Count} destinatario(s)";
                }
                else
                {
                    emailsFailed++;
                    LogText += $" ❌ Error: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                emailsFailed++;
                LogText += $" ❌ Error: {ex.Message}";
            }
        }
        
        // Enviar documentos huérfanos consolidados al BCC
        if (orphanDocuments.Any() && !string.IsNullOrWhiteSpace(bccEmail))
        {
            try
            {
                EmailStatusMessage = $"Enviando {orphanDocuments.Count} documentos sin destinatario...";
                LogText += $"\n📤 Enviando {orphanDocuments.Count} documento(s) huérfano(s) consolidados al BCC...";

                // Crear lista de rutas de archivos
                var orphanAttachments = orphanDocuments.Select(d => d.RutaArchivo).ToList();

                // Generar cuerpo del email consolidado
                var consolidatedBody = GetConsolidatedOrphanEmailBody(orphanDocuments);

                var orphanEmailInfo = new EmailInfo
                {
                    Recipients = new List<string> { bccEmail },
                    Subject = $"Estado de Cartera - Documentos Sin Destinatario ({orphanDocuments.Count} empresas)",
                    Body = consolidatedBody,
                    IsBodyHtml = true
                };

                var orphanResult = await _emailService.SendEmailWithAttachmentsAsync(orphanEmailInfo, orphanAttachments, cancellationToken);
                
                if (orphanResult.IsSuccess)
                {
                    orphansSent = orphanDocuments.Count;
                    LogText += $" ✅ {orphanDocuments.Count} documento(s) enviado(s) consolidados al BCC";
                }
                else
                {
                    emailsFailed += orphanDocuments.Count;
                    LogText += $" ❌ Error enviando consolidado al BCC: {orphanResult.Message}";
                }
            }
            catch (Exception ex)
            {
                emailsFailed += orphanDocuments.Count;
                LogText += $" ❌ Error enviando consolidado al BCC: {ex.Message}";
            }
        }
        else if (orphanDocuments.Any())
        {
            LogText += $"\n⚠️ {orphanDocuments.Count} documento(s) sin correo y sin BCC configurado";
            emailsFailed += orphanDocuments.Count;
        }
        
        // Completar progreso with animación suave
        _smoothProgress.Report(100);
        await Task.Delay(200); // Pausa visual para mostrar completado
        EmailStatusMessage = $"Completado: {emailsSent + orphansSent} emails enviados";

        LogText += $"\n📊 Resumen final: {emailsSent}/{totalEmails} emails enviados exitosamente";
        if (orphansSent > 0)
        {
            LogText += $", {orphansSent} documento(s) huérfano(s) enviado(s) consolidados al BCC";
        }
        if (emailsFailed > 0)
        {
            LogText += $", {emailsFailed} fallos";
        }

        return emailsSent > 0 || orphansSent > 0;
    }
    
    /// <summary>
    /// Determina si se puede enviar automáticamente
    /// </summary>
    private bool CanSendDocumentsAutomatically()
    {
        var canSend = !IsSendingEmail && 
                     IsEmailConfigured && 
                     HasEmailExcel && 
                     GeneratedDocuments.Count > 0;

        // ✨ MEJORA: Actualizar mensajes de estado según las condiciones
        if (!canSend && HasEmailExcel && !HasDocumentsGenerated)
        {
            DocumentStatusWarning = "⚠️ Genere documentos primero para habilitar envío";
        }
        else if (!canSend && !IsEmailConfigured && HasDocumentsGenerated)
        {
            DocumentStatusWarning = "⚠️ Configure SMTP para habilitar envío";
        }
        
        return canSend;
    }

    /// <summary>
    /// Actualiza la configuración de email
    /// </summary>
    public void UpdateEmailConfiguration(bool isConfigured)
    {
        IsEmailConfigured = isConfigured;
        OnPropertyChanged(nameof(CanSendAutomatically));
    }
    
    /// <summary>
    /// Actualiza la lista de documentos generados
    /// </summary>
    public void UpdateGeneratedDocuments(IReadOnlyList<GeneratedPdfInfo> documents)
    {
        GeneratedDocuments = documents;
        HasDocumentsGenerated = documents.Count > 0;
        
        // ✨ MEJORA: Actualizar estado cuando cambian los documentos
        if (HasDocumentsGenerated && HasEmailExcel)
        {
            // Disparar re-análisis automático cuando tenemos tanto documentos como archivo Excel
            _ = Task.Run(async () => await AnalyzeEmailMatchingAsync());
        }
        else if (!HasDocumentsGenerated && HasEmailExcel)
        {
            // Solo tenemos archivo Excel, mostrar información básica
            CompaniesStatusText = $"📊 En archivo Excel: {CompaniesWithEmail}";
            DocumentStatusWarning = "⚠️ Genere documentos primero para análisis completo";
        }
        
        OnPropertyChanged(nameof(CanSendAutomatically));
    }
    
    /// <summary>
    /// Limpia recursos
    /// </summary>
    public void Cleanup()
    {
        // Detener cualquier animación de progreso en curso
        _smoothProgress?.Stop();
        
        // Cancelar cualquier operación de email en curso
        _emailCancellationTokenSource?.Cancel();
        _emailCancellationTokenSource?.Dispose();
        _emailCancellationTokenSource = null;
    }

    /// <summary>
    /// Genera el cuerpo completo del email con toda la firma HTML (idéntico al proyecto de implementación)
    /// </summary>
    private string GetCompleteEmailBodyWithSignature()
    {
        return @"<div style='font-family: Arial, sans-serif; line-height: 1.6; text-align: justify;'>
<p>Para SIMICS GROUP S.A.S. es muy importante contar con clientes como usted e informar constantemente la situación de cartera que tenemos a la fecha.</p>

<p>Adjuntamos estado de cuenta, en caso de tener alguna factura vencida agradecemos su colaboración con la programación de pagos.</p>

<p>Si tiene alguna observación agradecemos informarla por este medio para su revisión.</p>

<p>En caso de no ser la persona encargada agradecemos enviar este mensaje al responsable o compartirnos su correo electrónico para enviarle este comunicado.</p>

<p>Muchas gracias por su ayuda.</p>

<p>Cordialmente,</p>

<table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
  <tbody>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr>
              <td width='80' style='vertical-align:middle'>
                <span style='margin-right:20px;display:block'>
                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_Icono2021Firma.png' role='presentation' width='80' style='max-width:80px'>
                </span>
              </td>
              <td style='vertical-align:middle'>
                <h3 style='margin:0;font-size:14px;color:#000'>
                  <span>JUAN MANUEL</span> <span>CUERVO PINILLA</span>
                </h3>
                <p style='margin:0;font-weight:500;color:#000;font-size:12px;line-height:15px'>
                  <span>Gerente Financiero</span>
                </p>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                  <tbody>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image002.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='tel:+34654623277' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+34-654623277</span>
                        </a> |
                        <a href='tel:+573163114545' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+57-3163114545</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image003.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='mailto:juan.cuervo@simicsgroup.com' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>juan.cuervo@simicsgroup.com</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image004.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>CR 53 No. 96-24 Oficina 3D</span>
                        </span>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'></td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>Barranquilla, Colombia</span>
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr height='60' style='vertical-align:middle'>
              <th style='width:100%'>
                <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_2021-1Firma.png' width='200' style='max-width:200px;display:inline-block'>
              </th>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='width:100%'>
                <a href='https://www.simicsgroup.com/' style='text-decoration:none;color:#000;font-size:11px;text-align:center'>
                  <span>www.simicsgroup.com</span>
                </a>
              </td>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='text-align:center;vertical-align:top'>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;display:inline-block'>
                  <tbody>
                    <tr style='text-align:right'>
                      <td>
                        <a href='https://www.linkedin.com/company/simicsgroupsas' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image006.png' alt='linkedin' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.instagram.com/simicsgroupsas/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image007.png' alt='instagram' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.facebook.com/SIMICSGroupSAS/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image008.png' alt='facebook' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
  </tbody>
</table>
</div>";
    }
    
    /// <summary>
    /// Genera el cuerpo de email consolidado para múltiples documentos sin destinatario
    /// </summary>
    private string GetConsolidatedOrphanEmailBody(List<GeneratedPdfInfo> orphanDocuments)
    {        
        // ✅ CORRECCIÓN MEJORADA: Usar lista HTML <ul><li> para garantizar formato vertical
        var companiesListItems = string.Join("", orphanDocuments.Select((doc, index) => 
            $"<li><strong>{doc.NombreEmpresa}</strong> (NIT: {doc.Nit})</li>"));

        return @"<div style='font-family: Arial, sans-serif; line-height: 1.6; text-align: justify;'>
<p><strong>DOCUMENTOS SIN CORREO ELECTRÓNICO DESTINATARIO</strong></p>

<p>Se adjuntan los documentos de estado de cartera para las siguientes empresas que no tienen correo electrónico registrado:</p>

<ol style='padding-left: 20px; margin: 10px 0;'>" + companiesListItems + @"</ol>

<p><em>Total de documentos adjuntos: " + orphanDocuments.Count + @"</em></p>

<p>Por favor, procedan con el envío manual o actualización de los datos de contacto correspondientes.</p>

<p>Cordialmente,</p>

<table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
  <tbody>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr>
              <td width='80' style='vertical-align:middle'>
                <span style='margin-right:20px;display:block'>
                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_Icono2021Firma.png' role='presentation' width='80' style='max-width:80px'>
                </span>
              </td>
              <td style='vertical-align:middle'>
                <h3 style='margin:0;font-size:14px;color:#000'>
                  <span>JUAN MANUEL</span> <span>CUERVO PINILLA</span>
                </h3>
                <p style='margin:0;font-weight:500;color:#000;font-size:12px;line-height:15px'>
                  <span>Gerente Financiero</span>
                </p>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                  <tbody>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image002.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='tel:+34654623277' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+34-654623277</span>
                        </a> |
                        <a href='tel:+573163114545' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>+57-3163114545</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image003.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <a href='mailto:juan.cuervo@simicsgroup.com' style='text-decoration:none;color:#000;font-size:11px'>
                          <span>juan.cuervo@simicsgroup.com</span>
                        </a>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'>
                        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial'>
                          <tbody>
                            <tr>
                              <td style='vertical-align:bottom'>
                                <span style='display:block'>
                                  <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image004.png' width='11' style='display:block'>
                                </span>
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>CR 53 No. 96-24 Oficina 3D</span>
                        </span>
                      </td>
                    </tr>
                    <tr height='15' style='vertical-align:middle'>
                      <td width='30' style='vertical-align:middle'></td>
                      <td style='padding:0;color:#000'>
                        <span style='font-size:11px;color:#000'>
                          <span>Barranquilla, Colombia</span>
                        </span>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
    <tr>
      <td>
        <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;width:385px'>
          <tbody>
            <tr height='60' style='vertical-align:middle'>
              <th style='width:100%'>
                <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_2021-1Firma.png' width='200' style='max-width:200px;display:inline-block'>
              </th>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='width:100%'>
                <a href='https://www.simicsgroup.com/' style='text-decoration:none;color:#000;font-size:11px;text-align:center'>
                  <span>www.simicsgroup.com</span>
                </a>
              </td>
            </tr>
            <tr height='25' style='text-align:center'>
              <td style='text-align:center;vertical-align:top'>
                <table cellpadding='0' cellspacing='0' style='vertical-align:-webkit-baseline-middle;font-size:small;font-family:Arial;display:inline-block'>
                  <tbody>
                    <tr style='text-align:right'>
                      <td>
                        <a href='https://www.linkedin.com/company/simicsgroupsas' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image006.png' alt='linkedin' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.instagram.com/simicsgroupsas/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image007.png' alt='instagram' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                      <td>
                        <a href='https://www.facebook.com/SIMICSGroupSAS/' style='display:inline-block;padding:0'>
                          <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image008.png' alt='facebook' height='24' style='max-width:135px;display:block'>
                        </a>
                      </td>
                      <td width='5'>
                        <div></div>
                      </td>
                    </tr>
                  </tbody>
                </table>
              </td>
            </tr>
          </tbody>
        </table>
      </td>
    </tr>
  </tbody>
</table>
</div>";
    }
    
    // Este comando será llamado desde el MainViewModel con el parámetro correcto
    public async Task<bool> SendDocumentsAutomaticallyWithConfig(SmtpConfigurationViewModel smtpConfig)
    {
        return await SendDocumentsAutomaticallyAsync(GeneratedDocuments, smtpConfig, CancellationToken.None);
    }
}
