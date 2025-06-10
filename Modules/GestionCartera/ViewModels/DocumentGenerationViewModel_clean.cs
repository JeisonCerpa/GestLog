using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using Ookii.Dialogs.Wpf;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para la vista de generación de documentos PDF
/// </summary>
public partial class DocumentGenerationViewModel : ObservableObject
{    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IEmailService? _emailService;
    private readonly IExcelEmailService? _excelEmailService;
    private readonly IConfigurationService _configurationService;
    private readonly ICredentialService _credentialService;
    private readonly IGestLogLogger _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private const string DEFAULT_TEMPLATE_FILE = "PlantillaSIMICS.png";

    [ObservableProperty] private string _selectedExcelFilePath = string.Empty;
    [ObservableProperty] private string _outputFolderPath = string.Empty;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private string _templateFilePath = string.Empty;
    
    [ObservableProperty] private string _logText = string.Empty;    [ObservableProperty] private string _statusMessage = "Listo para generar documentos";
    [ObservableProperty] private bool _isProcessing = false;
    [ObservableProperty] private bool _isProcessingCompleted = false;
    [ObservableProperty] private double _progressValue = 0;[ObservableProperty] private int _totalDocuments = 0;
    [ObservableProperty] private int _currentDocument = 0;
    [ObservableProperty] private IReadOnlyList<GeneratedPdfInfo> _generatedDocuments = new List<GeneratedPdfInfo>();
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemplateStatusMessage))]
    private bool _useDefaultTemplate = true;    // Propiedades para funcionalidad de email
    [ObservableProperty] private string _smtpServer = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _smtpUsername = string.Empty;
    [ObservableProperty] private bool _smtpUseSsl = true;
    [ObservableProperty] private bool _isSmtpConfigured = false;
      // Nuevas propiedades para manejo de Excel de correos
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendAutomatically))]
    private string _selectedEmailExcelFilePath = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendAutomatically))]
    private bool _hasEmailExcel = false;
    
    [ObservableProperty] private int _companiesWithEmail = 0;
    [ObservableProperty] private int _companiesWithoutEmail = 0;    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private bool _enableSsl = true;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendAutomatically))]
    private bool _isEmailConfigured = false;
    
    [ObservableProperty] private string _emailSubject = "Estado de Cartera - Documentos";
    [ObservableProperty] private string _emailBody = "Estimado cliente,\n\nAdjunto encontrará los documentos de estado de cartera solicitados.\n\nSaludos cordiales,\nSIMICS GROUP S.A.S.";
    [ObservableProperty] private string _emailRecipients = string.Empty;
    [ObservableProperty] private string _emailCc = string.Empty;
    [ObservableProperty] private string _emailBcc = string.Empty;    [ObservableProperty] private bool _useHtmlEmail = true;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSendAutomatically))]
    private bool _isSendingEmail = false;

    public string TemplateStatusMessage => GetTemplateStatusMessage();

    private string GetTemplateStatusMessage()
    {
        if (!UseDefaultTemplate)
            return "Plantilla desactivada - se usará fondo blanco";
            
        if (string.IsNullOrEmpty(TemplateFilePath))
            return "No se ha encontrado una plantilla";
            
        if (Path.GetFileName(TemplateFilePath) == DEFAULT_TEMPLATE_FILE)
            return $"Usando plantilla predeterminada: {DEFAULT_TEMPLATE_FILE}";
            
        return $"Usando plantilla personalizada: {Path.GetFileName(TemplateFilePath)}";
    }    public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IGestLogLogger logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Obtener servicios del contenedor DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        _excelEmailService = serviceProvider.GetService<IExcelEmailService>();
        
        // Suscribirse a cambios de configuración para sincronización automática
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
        
        // Configurar carpeta de salida por defecto
        InitializeDefaultPaths();
        LoadSmtpConfiguration();
        
        // Cargar documentos previamente generados si existen
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPreviouslyGeneratedDocuments();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar documentos previos en el constructor");
            }
        });
    }    public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IEmailService emailService, IGestLogLogger logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Obtener servicios del contenedor DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        _credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        
        // Suscribirse a cambios de configuración para sincronización automática
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
        
        // Configurar carpeta de salida por defecto
        InitializeDefaultPaths();
        LoadSmtpConfiguration();
        
        // Cargar documentos previamente generados si existen
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPreviouslyGeneratedDocuments();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar documentos previos en el constructor");
            }
        });
    }public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IEmailService emailService, IConfigurationService configurationService, IGestLogLogger logger)
    {
        _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Obtener servicios del contenedor DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        _excelEmailService = serviceProvider.GetService<IExcelEmailService>();
        
        // Suscribirse a cambios de configuración para sincronización automática
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
        
        // Configurar carpeta de salida por defecto
        InitializeDefaultPaths();
        LoadSmtpConfiguration();
        
        // Cargar documentos previamente generados si existen
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadPreviouslyGeneratedDocuments();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar documentos previos en el constructor");
            }
        });
    }

    private void InitializeDefaultPaths()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var archivosFolder = Path.Combine(appDirectory, "Archivos");
            var outputFolder = Path.Combine(archivosFolder, "Clientes cartera pdf");
            
            // Crear carpeta si no existe
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                _logger.LogInformation("📁 Carpeta de salida creada automáticamente");
            }

            OutputFolderPath = outputFolder;
            // Forzar la notificación de cambio de propiedad para re-evaluar los CanExecute
            OnPropertyChanged(nameof(OutputFolderPath));
            
            // Asegurarse de que existe la carpeta Assets en el directorio de salida
            EnsureAssetsDirectoryExists(appDirectory);
            
            // Configurar plantilla por defecto - buscar en varias ubicaciones posibles
            string defaultTemplatePath = Path.Combine(appDirectory, "Assets", DEFAULT_TEMPLATE_FILE);
            
            // Si no se encuentra en la primera ubicación, intentar en la carpeta superior (raíz del proyecto)
            if (!File.Exists(defaultTemplatePath))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
                string sourceTemplatePath = Path.Combine(projectRoot, "Assets", DEFAULT_TEMPLATE_FILE);
                
                // Si existe en la raíz del proyecto, copiarlo al directorio de salida
                if (File.Exists(sourceTemplatePath))
                {
                    _logger.LogInformation("Copiando plantilla desde la raíz del proyecto a la carpeta de salida");
                    var outputAssetsDir = Path.Combine(appDirectory, "Assets");
                    Directory.CreateDirectory(outputAssetsDir);
                    
                    // Asegurarse de que el directorio existe
                    if (!Directory.Exists(outputAssetsDir))
                    {
                        Directory.CreateDirectory(outputAssetsDir);
                    }
                    
                    // Copiar el archivo
                    File.Copy(sourceTemplatePath, defaultTemplatePath, true);
                    _logger.LogInformation($"📋 Plantilla copiada desde la raíz del proyecto");
                }
                else
                {
                    _logger.LogInformation("Buscando plantilla en raíz del proyecto: {TemplatePath}", sourceTemplatePath);
                }
            }
            
            // Verificar de nuevo después de posiblemente copiar el archivo
            if (File.Exists(defaultTemplatePath))
            {
                TemplateFilePath = defaultTemplatePath;
                _logger.LogInformation($"🎨 Plantilla cargada automáticamente: {DEFAULT_TEMPLATE_FILE}");
            }
            else
            {                _logger.LogWarning("No se encontró la plantilla por defecto en {TemplatePath}", defaultTemplatePath);
                _logger.LogWarning("⚠️ No se encontró la plantilla por defecto. Se usará fondo blanco.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al inicializar rutas por defecto");
            _logger.LogError(ex, "❌ Error al configurar rutas: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Asegura que el directorio Assets existe en la carpeta de salida y contiene los archivos necesarios
    /// </summary>
    private void EnsureAssetsDirectoryExists(string appDirectory)
    {
        try
        {
            var outputAssetsDir = Path.Combine(appDirectory, "Assets");
            
            // Crear el directorio si no existe
            if (!Directory.Exists(outputAssetsDir))
            {
                Directory.CreateDirectory(outputAssetsDir);
                _logger.LogInformation("Carpeta Assets creada en el directorio de salida");
                
                // Intentar copiar archivos desde la raíz del proyecto
                string projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
                string sourceAssetsDir = Path.Combine(projectRoot, "Assets");
                
                if (Directory.Exists(sourceAssetsDir))
                {
                    _logger.LogInformation("Copiando archivos de Assets desde la raíz del proyecto");
                    
                    // Copiar PlantillaSIMICS.png y firma.png
                    string[] filesToCopy = { DEFAULT_TEMPLATE_FILE, "firma.png" };
                    
                    foreach (string file in filesToCopy)
                    {
                        string sourcePath = Path.Combine(sourceAssetsDir, file);
                        string destPath = Path.Combine(outputAssetsDir, file);
                        
                        if (File.Exists(sourcePath))
                        {
                            File.Copy(sourcePath, destPath, true);
                            _logger.LogInformation($"📋 Archivo {file} copiado a la carpeta Assets del directorio de salida");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asegurar el directorio Assets");
            _logger.LogError(ex, "❌ Error al configurar carpeta Assets: {Message}", ex.Message);        }
    }    /// <summary>
    /// Carga la configuración SMTP desde el servicio de configuración
    /// </summary>
    private void LoadSmtpConfiguration()
    {
        try
        {
            _logger.LogInformation("🔄 INICIO LoadSmtpConfiguration()");
            
            var smtpConfig = _configurationService.Current.Smtp;
            
            _logger.LogInformation("🔍 DATOS LEÍDOS: Server='{Server}', Username='{Username}', Port={Port}, UseSSL={UseSSL}, IsConfigured={IsConfigured}",
                smtpConfig.Server ?? "[VACÍO]", smtpConfig.Username ?? "[VACÍO]", smtpConfig.Port, smtpConfig.UseSSL, smtpConfig.IsConfigured);
              
            // Cargar datos de configuración SMTP (excepto contraseña)
            SmtpServer = smtpConfig.Server ?? string.Empty;
            SmtpPort = smtpConfig.Port;
            SmtpUsername = smtpConfig.Username ?? string.Empty;
            EnableSsl = smtpConfig.UseSSL;
            IsEmailConfigured = smtpConfig.IsConfigured;
            
            _logger.LogInformation("🔄 VALORES ASIGNADOS: SmtpServer='{Server}', SmtpUsername='{Username}', SmtpPort={Port}, EnableSsl={EnableSsl}, IsEmailConfigured={IsConfigured}",
                SmtpServer, SmtpUsername, SmtpPort, EnableSsl, IsEmailConfigured);
            
            // Notificar cambios de propiedades explícitamente
            OnPropertyChanged(nameof(SmtpServer));
            OnPropertyChanged(nameof(SmtpPort));
            OnPropertyChanged(nameof(SmtpUsername));
            OnPropertyChanged(nameof(EnableSsl));
            OnPropertyChanged(nameof(IsEmailConfigured));
            
            // Cargar contraseña desde Windows Credential Manager
            if (!string.IsNullOrWhiteSpace(smtpConfig.Username))
            {
                var credentialTarget = $"SMTP_{smtpConfig.Server}_{smtpConfig.Username}";
                _logger.LogInformation("🔍 Buscando credenciales con target: '{CredentialTarget}'", credentialTarget);
                
                if (_credentialService.CredentialsExist(credentialTarget))
                {                var (username, password) = _credentialService.GetCredentials(credentialTarget);
                    SmtpPassword = password;
                      // IMPORTANTE: Asignar la contraseña al objeto SmtpSettings también
                    smtpConfig.Password = password;
                    
                    OnPropertyChanged(nameof(SmtpPassword));
                    _logger.LogInformation("🔐 ✅ Contraseña SMTP cargada desde Windows Credential Manager para usuario: {Username}", username);
                    
                    // Revalidar configuración ahora que tenemos la contraseña
                    smtpConfig.ValidateConfiguration();
                    IsEmailConfigured = smtpConfig.IsConfigured;
                    OnPropertyChanged(nameof(IsEmailConfigured));
                    _logger.LogInformation("🔄 Configuración revalidada después de cargar credenciales: IsConfigured = {IsConfigured}", IsEmailConfigured);
                }                else
                {
                    SmtpPassword = string.Empty;
                    OnPropertyChanged(nameof(SmtpPassword));
                    _logger.LogInformation("⚠️ No se encontraron credenciales SMTP en Windows Credential Manager para target: '{CredentialTarget}'", credentialTarget);
                    
                    // En este caso, IsConfigured debe ser false porque no tenemos la contraseña
                    IsEmailConfigured = false;
                    OnPropertyChanged(nameof(IsEmailConfigured));
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ Username está vacío, no se buscarán credenciales");
            }
            
            // Cargar datos adicionales de email
            if (!string.IsNullOrWhiteSpace(smtpConfig.FromEmail))
            {
                // Aquí podrías cargar otros datos como FromEmail si es necesario en el ViewModel
            }
            
            if (smtpConfig.IsConfigured)
            {
                _logger.LogInformation("✅ Configuración SMTP cargada desde configuración persistente");
            }
            else
            {
                _logger.LogInformation("ℹ️ No hay configuración SMTP guardada");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cargar configuración SMTP");
            // Mantener valores por defecto en caso de error
            IsEmailConfigured = false;
            SmtpPassword = string.Empty;
        }
    }    /// <summary>
    /// Guarda la configuración SMTP actual en el servicio de configuración
    /// </summary>
    private async Task SaveSmtpConfigurationAsync()
    {
        try
        {
            _logger.LogInformation("🔄 INICIO SaveSmtpConfigurationAsync()");
            _logger.LogInformation("🔍 VALORES A GUARDAR: SmtpServer='{Server}', SmtpUsername='{Username}', SmtpPort={Port}, EnableSsl={EnableSsl}",
                SmtpServer, SmtpUsername, SmtpPort, EnableSsl);
            
            var smtpConfig = _configurationService.Current.Smtp;
            
            // Actualizar configuración con los valores actuales del ViewModel (excepto contraseña)
            smtpConfig.Server = SmtpServer;
            smtpConfig.Port = SmtpPort;
            smtpConfig.Username = SmtpUsername;
            smtpConfig.FromEmail = SmtpUsername; // Sincronizar fromEmail con username
            // ❌ NO guardar contraseña en JSON: smtpConfig.Password = SmtpPassword;
            smtpConfig.UseSSL = EnableSsl;
            smtpConfig.UseAuthentication = !string.IsNullOrWhiteSpace(SmtpUsername);
            
            _logger.LogInformation("🔄 VALORES ASIGNADOS A CONFIG: Server='{Server}', Username='{Username}', FromEmail='{FromEmail}', Port={Port}, UseSSL={UseSSL}, UseAuthentication={UseAuth}",
                smtpConfig.Server, smtpConfig.Username, smtpConfig.FromEmail, smtpConfig.Port, smtpConfig.UseSSL, smtpConfig.UseAuthentication);
            
            // Guardar contraseña de forma segura en Windows Credential Manager
            if (!string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword))
            {
                var credentialTarget = $"SMTP_{SmtpServer}_{SmtpUsername}";
                _logger.LogInformation("🔐 Guardando credenciales con target: '{CredentialTarget}', Username: '{Username}'", credentialTarget, SmtpUsername);
                
                var credentialsSaved = _credentialService.SaveCredentials(credentialTarget, SmtpUsername, SmtpPassword);
                
                if (credentialsSaved)
                {
                    _logger.LogInformation("🔐 ✅ Contraseña SMTP guardada exitosamente en Windows Credential Manager");
                }                else
                {
                    _logger.LogWarning("🔐 ❌ ERROR: No se pudo guardar la contraseña SMTP en Windows Credential Manager");
                    throw new InvalidOperationException("No se pudo guardar la contraseña de forma segura");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Username o Password están vacíos, no se guardarán credenciales. Username: '{Username}', Password: {HasPassword}",
                    SmtpUsername, !string.IsNullOrWhiteSpace(SmtpPassword) ? "SÍ TIENE" : "VACÍO");
            }            
            // Validar y marcar como configurado
            smtpConfig.ValidateConfiguration();
            _logger.LogInformation("🔄 Configuración validada, IsConfigured: {IsConfigured}", smtpConfig.IsConfigured);
            
            // Guardar configuración (sin contraseña)
            _logger.LogInformation("💾 Guardando configuración en archivo...");
            await _configurationService.SaveAsync();
            
            _logger.LogInformation("✅ FIN SaveSmtpConfigurationAsync() - Configuración SMTP guardada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR en SaveSmtpConfigurationAsync()");
            throw;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectExcelFile))]
    private async Task SelectExcelFile()
    {        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar archivo Excel",
                Filter = "Archivos Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {                SelectedExcelFilePath = dialog.FileName;
                _logger.LogInformation($"📄 Archivo seleccionado: {Path.GetFileName(SelectedExcelFilePath)}");
                
                // Forzar la notificación de cambio de propiedad para re-evaluar los CanExecute
                OnPropertyChanged(nameof(SelectedExcelFilePath));
                GenerateDocumentsCommand.NotifyCanExecuteChanged();
                
                // Validar estructura del archivo
                StatusMessage = "Validando archivo Excel...";
                var isValid = await _pdfGenerator.ValidateExcelStructureAsync(SelectedExcelFilePath);
                
                if (isValid)
                {
                    // Obtener vista previa de empresas
                    var companies = await _pdfGenerator.GetCompaniesPreviewAsync(SelectedExcelFilePath);
                    var companiesList = companies.ToList();
                    
                    _logger.LogInformation($"✅ Archivo válido. Se encontraron {companiesList.Count} empresas");
                    if (companiesList.Count > 0)
                    {
                        _logger.LogInformation($"📊 Empresas encontradas: {string.Join(", ", companiesList.Take(5))}" + 
                               (companiesList.Count > 5 ? "..." : ""));
                    }
                    StatusMessage = $"Archivo válido - {companiesList.Count} empresas encontradas";
                }
                else
                {
                    _logger.LogInformation("❌ El archivo Excel no tiene la estructura esperada");
                    StatusMessage = "Error: Archivo Excel inválido";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel");
            _logger.LogInformation($"❌ Error al seleccionar archivo: {ex.Message}");
            StatusMessage = "Error al seleccionar archivo";
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectOutputFolder))]
    private void SelectOutputFolder()
    {
        try
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Seleccionar carpeta de destino para los PDFs",
                UseDescriptionForTitle = true,
                SelectedPath = OutputFolderPath
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OutputFolderPath = dialog.SelectedPath;
                _logger.LogInformation($"📁 Carpeta de destino: {OutputFolderPath}");
                StatusMessage = "Carpeta de destino actualizada";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar carpeta de destino");
            _logger.LogInformation($"❌ Error al seleccionar carpeta: {ex.Message}");
        }
    }    [RelayCommand(CanExecute = nameof(CanSelectTemplateFile))]
    private void SelectTemplateFile()
    {        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Seleccionar plantilla de fondo personalizada",
                Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                TemplateFilePath = dialog.FileName;
                UseDefaultTemplate = true;
                _logger.LogInformation($"🎨 Plantilla personalizada seleccionada: {Path.GetFileName(TemplateFilePath)}");
                StatusMessage = "Plantilla de fondo configurada";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar plantilla");
            _logger.LogInformation($"❌ Error al seleccionar plantilla: {ex.Message}");
        }
    }    [RelayCommand]
    private void ClearTemplate()
    {
        UseDefaultTemplate = false;
        _logger.LogInformation("🗑️ Uso de plantilla desactivado");
        StatusMessage = "Plantilla desactivada - se usará fondo blanco";
        OnPropertyChanged(nameof(TemplateStatusMessage));
    }

    [RelayCommand(CanExecute = nameof(CanGenerateDocuments))]
    private async Task GenerateDocuments()
    {
        if (IsProcessing) return;

        try
        {            IsProcessing = true;
            IsProcessingCompleted = false;
            _cancellationTokenSource = new CancellationTokenSource();
            GeneratedDocuments = new List<GeneratedPdfInfo>();
            
            _logger.LogInformation("🚀 Iniciando generación de documentos PDF...");
            StatusMessage = "Generando documentos...";
            
            var progress = new Progress<(int current, int total, string status)>(report =>
            {
                CurrentDocument = report.current;
                TotalDocuments = report.total;
                if (report.total > 0)
                {
                    ProgressValue = (double)report.current / report.total * 100;
                }
                StatusMessage = report.status;
                _logger.LogInformation($"📝 {report.status} ({report.current}/{report.total})");            });

            // Determinar si se debe usar la plantilla
            string? templatePath = null;
            if (UseDefaultTemplate && !string.IsNullOrEmpty(TemplateFilePath)) 
            {
                templatePath = TemplateFilePath;
                _logger.LogInformation($"🎨 Usando plantilla: {Path.GetFileName(TemplateFilePath)}");
            }
            else
            {
                _logger.LogInformation("⚪ Generando documentos sin plantilla de fondo");
            }
            
            var result = await _pdfGenerator.GenerateEstadosCuentaAsync(
                SelectedExcelFilePath,
                OutputFolderPath,
                templatePath,
                progress,
                _cancellationTokenSource.Token);

            GeneratedDocuments = result;
              _logger.LogInformation($"🎉 Generación completada exitosamente!");
            _logger.LogInformation($"📊 Documentos generados: {result.Count}");
            
            if (result.Count > 0)
            {
                var totalSize = result.Sum(d => 
                {
                    try
                    {
                        var fileInfo = new FileInfo(d.RutaArchivo);
                        return fileInfo.Exists ? fileInfo.Length : 0;
                    }
                    catch
                    {
                        return 0;
                    }
                });
                _logger.LogInformation($"💾 Tamaño total: {FormatFileSize(totalSize)}");
                _logger.LogInformation($"📁 Ubicación: {OutputFolderPath}");
            }
              StatusMessage = $"Completado - {result.Count} documentos generados";
            ProgressValue = 100;
            IsProcessingCompleted = true;
            
            // Auto-hide completion message after 5 seconds
            _ = Task.Delay(5000).ContinueWith(_ => 
            {
                IsProcessingCompleted = false;
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("⏹️ Generación cancelada por el usuario");
            StatusMessage = "Generación cancelada";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la generación de documentos");
            _logger.LogInformation($"❌ Error durante la generación: {ex.Message}");
            StatusMessage = "Error durante la generación";
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelGeneration))]
    private void CancelGeneration()
    {
        _cancellationTokenSource?.Cancel();
        _logger.LogInformation("⏹️ Solicitando cancelación...");
        StatusMessage = "Cancelando...";
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogText = string.Empty;
        StatusMessage = "Log limpiado";
    }

    [RelayCommand(CanExecute = nameof(CanOpenOutputFolder))]
    private void OpenOutputFolder()
    {
        try
        {
            if (Directory.Exists(OutputFolderPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", OutputFolderPath);
                _logger.LogInformation($"📂 Abriendo carpeta: {OutputFolderPath}");
            }
            else
            {
                _logger.LogInformation("❌ La carpeta de destino no existe");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir carpeta de destino");
            _logger.LogInformation($"❌ Error al abrir carpeta: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDebugInfo()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = Path.GetFullPath(Path.Combine(appDirectory, "..\\..\\..\\"));
            
            // Verificar rutas de búsqueda de la plantilla
            var possiblePaths = new List<string>
            {
                Path.Combine(appDirectory, "Assets", DEFAULT_TEMPLATE_FILE),
                Path.Combine(projectRoot, "Assets", DEFAULT_TEMPLATE_FILE),
                Path.Combine(Environment.CurrentDirectory, "Assets", DEFAULT_TEMPLATE_FILE)
            };
            
            _logger.LogInformation("\n🔍 INFORMACIÓN DE DEPURACIÓN:");
            _logger.LogInformation($"📂 Directorio de la aplicación: {appDirectory}");
            _logger.LogInformation($"📂 Directorio raíz del proyecto: {projectRoot}");
            _logger.LogInformation($"📂 Directorio actual: {Environment.CurrentDirectory}");
            
            _logger.LogInformation("\n🔎 BÚSQUEDA DE PLANTILLA:");
            foreach (var path in possiblePaths)
            {
                bool exists = File.Exists(path);
                _logger.LogInformation($"  - {path}: {(exists ? "✅ ENCONTRADO" : "❌ NO EXISTE")}");
            }
            
            // Verificar carpeta Assets en la salida
            var outputAssetsDir = Path.Combine(appDirectory, "Assets");
            bool assetsExists = Directory.Exists(outputAssetsDir);
            _logger.LogInformation($"\n📁 Carpeta Assets en directorio de salida: {(assetsExists ? "✅ EXISTE" : "❌ NO EXISTE")}");
            
            if (assetsExists)
            {
                var files = Directory.GetFiles(outputAssetsDir);
                _logger.LogInformation($"   Archivos en Assets ({files.Length}):");
                foreach (var file in files)
                {
                    _logger.LogInformation($"   - {Path.GetFileName(file)}");
                }
            }
            
            // Estado actual
            _logger.LogInformation($"\n📄 ESTADO ACTUAL:");
            _logger.LogInformation($"   Plantilla actual: {(string.IsNullOrEmpty(TemplateFilePath) ? "No configurada" : TemplateFilePath)}");
            _logger.LogInformation($"   Usar plantilla predeterminada: {UseDefaultTemplate}");
            _logger.LogInformation($"   {TemplateStatusMessage}");
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al mostrar información de depuración");
            _logger.LogInformation($"❌ Error al generar información de depuración: {ex.Message}");
        }
    }    // Métodos CanExecute
    private bool CanSelectExcelFile() => !IsProcessing;
    private bool CanSelectOutputFolder() => !IsProcessing;
    private bool CanSelectTemplateFile() => !IsProcessing;
    private bool CanGenerateDocuments() => !IsProcessing && !string.IsNullOrEmpty(SelectedExcelFilePath) && Directory.Exists(OutputFolderPath);
    private bool CanCancelGeneration() => IsProcessing;
    private bool CanOpenOutputFolder() => !string.IsNullOrEmpty(OutputFolderPath);

    #region Comandos de Email    /// <summary>
    /// Comando para configurar SMTP
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanConfigureSmtp))]
    private async Task ConfigureSmtpAsync(CancellationToken cancellationToken = default)
    {
        if (_emailService == null)
        {
            _logger.LogInformation("❌ Servicio de email no disponible");
            return;
        }

        try
        {
            _logger.LogInformation("🔧 Configurando servidor SMTP...");

            var smtpConfig = new SmtpConfiguration
            {
                SmtpServer = SmtpServer,
                Port = SmtpPort,
                Username = SmtpUsername,
                Password = SmtpPassword,
                EnableSsl = EnableSsl
            };

            await _emailService.ConfigureSmtpAsync(smtpConfig, cancellationToken);
            
            IsEmailConfigured = await _emailService.ValidateConfigurationAsync(cancellationToken);
            
            if (IsEmailConfigured)
            {
                // Guardar configuración SMTP persistente
                await SaveSmtpConfigurationAsync();
                
                _logger.LogInformation("✅ Configuración SMTP exitosa y guardada");
                StatusMessage = "SMTP configurado correctamente";
            }
            else
            {
                _logger.LogInformation("❌ Error en la configuración SMTP");
                StatusMessage = "Error en configuración SMTP";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al configurar SMTP");
            _logger.LogInformation($"❌ Error configurando SMTP: {ex.Message}");
            IsEmailConfigured = false;
        }
    }

    /// <summary>
    /// Comando para enviar email de prueba
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendTestEmail))]
    private async Task SendTestEmailAsync(CancellationToken cancellationToken = default)
    {
        if (_emailService == null)
        {
            _logger.LogInformation("❌ Servicio de email no disponible");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailRecipients))
        {
            _logger.LogInformation("❌ Debe especificar al menos un destinatario");
            return;
        }

        try
        {
            IsSendingEmail = true;
            _logger.LogInformation("📧 Enviando correo de prueba...");

            var recipients = ParseEmailList(EmailRecipients);
            var result = await _emailService.SendTestEmailAsync(recipients.First(), cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation($"✅ {result.Message}");
                StatusMessage = "Correo de prueba enviado";
            }
            else
            {
                _logger.LogInformation($"❌ {result.Message}");
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                    _logger.LogInformation($"   Detalles: {result.ErrorDetails}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de prueba");
            _logger.LogInformation($"❌ Error enviando correo de prueba: {ex.Message}");
        }
        finally
        {
            IsSendingEmail = false;
        }
    }

    /// <summary>
    /// Comando para enviar documentos por email
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendDocumentsByEmail))]
    private async Task SendDocumentsByEmailAsync(CancellationToken cancellationToken = default)
    {
        if (_emailService == null)
        {
            _logger.LogInformation("❌ Servicio de email no disponible");
            return;
        }

        if (!GeneratedDocuments.Any())
        {
            _logger.LogInformation("❌ No hay documentos generados para enviar");
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailRecipients))
        {
            _logger.LogInformation("❌ Debe especificar al menos un destinatario");
            return;
        }

        try
        {
            IsSendingEmail = true;
            _logger.LogInformation("📧 Enviando documentos por correo...");

            var recipients = ParseEmailList(EmailRecipients);
            var attachmentPaths = GeneratedDocuments.Select(d => d.FilePath).ToList();

            var emailInfo = new EmailInfo
            {
                Recipients = recipients,
                Subject = EmailSubject,
                Body = UseHtmlEmail ? _emailService.GetEmailHtmlTemplate(EmailBody) : EmailBody,
                IsBodyHtml = UseHtmlEmail,
                CcRecipient = string.IsNullOrWhiteSpace(EmailCc) ? null : EmailCc,
                BccRecipient = string.IsNullOrWhiteSpace(EmailBcc) ? null : EmailBcc
            };

            var result = await _emailService.SendEmailWithAttachmentsAsync(emailInfo, attachmentPaths, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation($"✅ {result.Message}");
                _logger.LogInformation($"   📎 {attachmentPaths.Count} archivos adjuntos ({result.TotalAttachmentSizeKb} KB)");
                _logger.LogInformation($"   👥 {result.ProcessedRecipients} destinatarios");
                StatusMessage = "Documents enviados por email";
            }
            else
            {
                _logger.LogInformation($"❌ {result.Message}");
                if (!string.IsNullOrEmpty(result.ErrorDetails))
                    _logger.LogInformation($"   Detalles: {result.ErrorDetails}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar documentos por email");
            _logger.LogInformation($"❌ Error enviando documentos: {ex.Message}");
        }
        finally
        {
            IsSendingEmail = false;
        }
    }

    /// <summary>
    /// Comando para limpiar configuración de email
    /// </summary>
    [RelayCommand]
    private void ClearEmailConfiguration()
    {
        SmtpServer = string.Empty;
        SmtpPort = 587;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
        EnableSsl = true;
        IsEmailConfigured = false;
        _logger.LogInformation("🧹 Configuración de email limpiada");
    }

    #endregion

    #region Métodos CanExecute para Email

    private bool CanConfigureSmtp() => !IsProcessing && !IsSendingEmail && 
        !string.IsNullOrWhiteSpace(SmtpServer) && 
        !string.IsNullOrWhiteSpace(SmtpUsername) && 
        !string.IsNullOrWhiteSpace(SmtpPassword);

    private bool CanSendTestEmail() => !IsProcessing && !IsSendingEmail && 
        IsEmailConfigured && 
        !string.IsNullOrWhiteSpace(EmailRecipients);    private bool CanSendDocumentsByEmail() => !IsProcessing && !IsSendingEmail && 
        IsEmailConfigured && 
        GeneratedDocuments.Any() && 
        !string.IsNullOrWhiteSpace(EmailRecipients) && 
        !string.IsNullOrWhiteSpace(EmailSubject);

    /// <summary>
    /// Comando para seleccionar archivo Excel de correos
    /// </summary>
    [RelayCommand]
    private async Task SelectEmailExcelFileAsync()
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
                
                // Validar el archivo Excel seleccionado
                await ValidateEmailExcelFileAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al seleccionar archivo Excel de correos");
            LogText += $"\n❌ Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Comando para envío automático basado en Excel
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendDocumentsAutomatically))]
    private async Task SendDocumentsAutomaticallyAsync(CancellationToken cancellationToken = default)
    {        if (_emailService == null || _excelEmailService == null)
        {
            var errorMessage = "Servicios de email no disponibles para envío automático";
            _logger.LogInformation($"❌ {errorMessage}");
            return;
        }

        try
        {
            IsSendingEmail = true;
            StatusMessage = "Enviando documentos automáticamente...";
            LogText += "\n🚀 Iniciando envío automático de documentos...\n";

            // Configurar SMTP
            await ConfigureSmtpFromCurrentSettingsAsync();

            // Procesar envíos automáticos
            await ProcessAutomaticEmailSendingAsync(cancellationToken);

            LogText += "\n✅ Envío automático completado";
            StatusMessage = "Envío automático completado";
        }
        catch (OperationCanceledException)
        {
            LogText += "\n⚠️ Envío automático cancelado por el usuario";
            StatusMessage = "Envío cancelado";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el envío automático");
            LogText += $"\n❌ Error durante envío automático: {ex.Message}";
            StatusMessage = "Error en envío automático";
        }
        finally
        {
            IsSendingEmail = false;
        }
    }

    /// <summary>
    /// Indica si se puede realizar envío automático
    /// </summary>
    public bool CanSendAutomatically => CanSendDocumentsAutomatically();

    private bool CanSendDocumentsAutomatically() => !IsProcessing && !IsSendingEmail && 
        IsEmailConfigured && 
        GeneratedDocuments.Any() && 
        HasEmailExcel && 
        !string.IsNullOrWhiteSpace(SelectedEmailExcelFilePath);

    #endregion

    #region Métodos Auxiliares para Email

    /// <summary>
    /// Parsea una lista de emails separados por coma o punto y coma
    /// </summary>
    private List<string> ParseEmailList(string emailList)
    {
        if (string.IsNullOrWhiteSpace(emailList))
            return new List<string>();

        return emailList
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(email => email.Trim())
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .ToList();
    }

    /// <summary>
    /// Formatea el tamaño de archivo en una representación legible
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "0 bytes";

        string[] suffixes = { "bytes", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F2} {suffixes[suffixIndex]}";
    }

    #endregion

    /// <summary>
    /// Maneja los cambios automáticos de configuración para sincronizar el ViewModel
    /// </summary>
    private void OnConfigurationChanged(object? sender, GestLog.Services.Configuration.ConfigurationChangedEventArgs e)
    {
        try
        {
            // Solo recargar si el cambio está relacionado con SMTP
            if (e.SettingPath.StartsWith("smtp", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("🔄 Configuración SMTP cambió externamente - recargando en ViewModel");
                LoadSmtpConfiguration();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al manejar cambio de configuración automático");
        }
    }

    /// <summary>
    /// Recarga manualmente la configuración SMTP desde el servicio de configuración
    /// </summary>
    public void RefreshSmtpConfiguration()
    {
        try
        {
            _logger.LogInformation("🔄 Recargando configuración SMTP manualmente");
            LoadSmtpConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al recargar configuración SMTP manualmente");
        }
    }
    
    /// <summary>
    /// Limpia recursos y desuscribe eventos
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Desuscribirse de eventos para evitar memory leaks
            if (_configurationService != null)
            {
                _configurationService.ConfigurationChanged -= OnConfigurationChanged;
            }
            
            // Cancelar operaciones en curso
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            _logger.LogDebug("🧹 DocumentGenerationViewModel recursos limpiados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al limpiar recursos del ViewModel");
        }
    }    [RelayCommand]
    private void ResetProgress()
    {
        IsProcessing = false;
        IsProcessingCompleted = false;
        ProgressValue = 0;
        CurrentDocument = 0;
        TotalDocuments = 0;
        StatusMessage = "Listo para generar documentos";
        GeneratedDocuments = new List<GeneratedPdfInfo>();
        _logger.LogInformation("🔄 Progreso y estado reiniciados");
    }

    /// <summary>
    /// Comando para cargar documentos previamente generados
    /// </summary>
    [RelayCommand]
    private async Task LoadPreviouslyGeneratedDocuments()
    {
        try
        {
            _logger.LogInformation("🔄 Cargando documentos previamente generados...");
            StatusMessage = "Cargando documentos anteriores...";

            var outputFolder = OutputFolderPath;
            var textFilePath = Path.Combine(outputFolder, "pdfs_generados.txt");

            if (!File.Exists(textFilePath))
            {
                _logger.LogWarning("❌ No se encontró archivo de documentos generados previamente");
                StatusMessage = "No hay documentos previos para cargar";
                return;
            }

            var loadedDocuments = await LoadDocumentsFromTextFileAsync(textFilePath);
              if (loadedDocuments.Any())
            {
                GeneratedDocuments = loadedDocuments;
                _logger.LogInformation("✅ {Count} documentos cargados desde sesión anterior", loadedDocuments.Count);
                StatusMessage = $"Cargados {loadedDocuments.Count} documentos previos";
                
                // Actualizar contadores
                TotalDocuments = loadedDocuments.Count;
                CurrentDocument = loadedDocuments.Count;
                ProgressValue = 100;
                
                // Notificar cambios para actualizar botones
                SendDocumentsAutomaticallyCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(CanSendAutomatically));
            }
            else
            {
                _logger.LogWarning("⚠️ No se pudieron cargar documentos del archivo");
                StatusMessage = "Error al cargar documentos previos";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar documentos previamente generados");
            StatusMessage = "Error al cargar documentos previos";
        }
    }

    /// <summary>
    /// Carga documentos desde el archivo de texto generado
    /// </summary>
    private async Task<List<GeneratedPdfInfo>> LoadDocumentsFromTextFileAsync(string textFilePath)
    {
        var documents = new List<GeneratedPdfInfo>();
        
        try
        {
            _logger.LogInformation("📖 Leyendo archivo de documentos generados: {FilePath}", textFilePath);
            
            var lines = await File.ReadAllLinesAsync(textFilePath);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("=") || line.StartsWith("PDF"))
                    continue;
                
                try
                {
                    // Formato esperado: "NombreArchivo.pdf - Empresa: NOMBRE_EMPRESA - NIT: 123456789"
                    var parts = line.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 3)
                    {
                        var fileName = parts[0].Trim();
                        var empresaPart = parts[1].Replace("Empresa: ", "").Trim();
                        var nitPart = parts[2].Replace("NIT: ", "").Trim();
                        
                        // Construir la ruta completa del archivo
                        var fullPath = Path.Combine(OutputFolderPath, fileName);
                        
                        // Verificar que el archivo existe físicamente
                        if (File.Exists(fullPath))
                        {                            var document = new GeneratedPdfInfo
                            {
                                NombreArchivo = fileName,
                                NombreEmpresa = empresaPart,
                                Nit = nitPart,
                                RutaArchivo = fullPath
                            };
                            
                            documents.Add(document);
                            _logger.LogDebug("📄 Documento cargado: {Archivo} - {Empresa} (NIT: {Nit})", 
                                fileName, empresaPart, nitPart);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Archivo no encontrado: {FilePath}", fullPath);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Formato de línea inválido: {Line}", line);
                    }
                }
                catch (Exception lineEx)
                {
                    _logger.LogWarning(lineEx, "⚠️ Error procesando línea: {Line}", line);
                }
            }
            
            _logger.LogInformation("✅ Documentos cargados desde archivo: {Count} documentos", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error leyendo archivo de documentos generados");
            throw;
        }
          return documents;
    }
}
