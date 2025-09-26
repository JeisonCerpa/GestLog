using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Modules.GestionCartera.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel principal que orquesta todos los componentes de generación de documentos
/// </summary>
public partial class MainDocumentGenerationViewModel : ObservableObject
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _configurationService;

    // ViewModels especializados
    public PdfGenerationViewModel PdfGeneration { get; }
    public DocumentManagementViewModel DocumentManagement { get; }
    public SmtpConfigurationViewModel SmtpConfiguration { get; }
    public AutomaticEmailViewModel AutomaticEmail { get; }

    // Propiedades de estado general
    [ObservableProperty] private string _logText = string.Empty;
    [ObservableProperty] private string _globalStatusMessage = "Listo para generar documentos";

    public MainDocumentGenerationViewModel(
        IPdfGeneratorService pdfGenerator,
        IEmailService emailService,
        IGestLogLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Obtener servicios del contenedor DI
        var serviceProvider = LoggingService.GetServiceProvider();
        _configurationService = serviceProvider.GetRequiredService<IConfigurationService>();
        var credentialService = serviceProvider.GetRequiredService<ICredentialService>();
        var excelEmailService = serviceProvider.GetService<IExcelEmailService>();        // Inicializar ViewModels especializados
        PdfGeneration = new PdfGenerationViewModel(pdfGenerator, logger);
        DocumentManagement = new DocumentManagementViewModel(logger);
        SmtpConfiguration = new SmtpConfigurationViewModel(emailService, _configurationService, credentialService, logger);
        AutomaticEmail = new AutomaticEmailViewModel(emailService, excelEmailService, logger);

        // Suscribirse a eventos de los ViewModels
        SubscribeToViewModelEvents();
        
        // Suscribirse a cambios de configuración
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
    }

    /// <summary>
    /// Suscribirse a eventos de los ViewModels especializados
    /// </summary>
    private void SubscribeToViewModelEvents()
    {
        // Eventos de generación de PDF
        PdfGeneration.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(PdfGeneration.StatusMessage))
            {
                GlobalStatusMessage = PdfGeneration.StatusMessage;
            }
            else if (e.PropertyName == nameof(PdfGeneration.LogText))
            {
                LogText += PdfGeneration.LogText;
            }
            else if (e.PropertyName == nameof(PdfGeneration.GeneratedDocuments))
            {
                // Sincronizar documentos generados con el gestor de documentos
                DocumentManagement.UpdateGeneratedDocuments(PdfGeneration.GeneratedDocuments);
                
                // Actualizar el ViewModel de email automático
                AutomaticEmail.UpdateGeneratedDocuments(PdfGeneration.GeneratedDocuments);
            }
        };

        // Eventos de gestión de documentos
        DocumentManagement.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(DocumentManagement.StatusMessage))
            {
                GlobalStatusMessage = DocumentManagement.StatusMessage;
            }
            else if (e.PropertyName == nameof(DocumentManagement.GeneratedDocuments))
            {
                // Sincronizar con el ViewModel de email automático
                AutomaticEmail.UpdateGeneratedDocuments(DocumentManagement.GeneratedDocuments);
            }
        };

        // Eventos de configuración SMTP
        SmtpConfiguration.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SmtpConfiguration.StatusMessage))
            {
                GlobalStatusMessage = SmtpConfiguration.StatusMessage;
            }
            else if (e.PropertyName == nameof(SmtpConfiguration.IsEmailConfigured))
            {
                // Sincronizar configuración con el ViewModel de email automático
                AutomaticEmail.UpdateEmailConfiguration(SmtpConfiguration.IsEmailConfigured);
                // Notificar cambio en CanSendDocumentsAutomatically
                SendDocumentsAutomaticallyCommand.NotifyCanExecuteChanged();
            }
        };

        // Eventos de email automático
        AutomaticEmail.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AutomaticEmail.StatusMessage))
            {
                GlobalStatusMessage = AutomaticEmail.StatusMessage;
            }
            else if (e.PropertyName == nameof(AutomaticEmail.LogText))
            {
                LogText += AutomaticEmail.LogText;
            }
            else if (e.PropertyName == nameof(AutomaticEmail.CanSendAutomatically) ||
                     e.PropertyName == nameof(AutomaticEmail.IsSendingEmail) ||
                     e.PropertyName == nameof(AutomaticEmail.HasEmailExcel))
            {
                // Notificar cambio en CanSendDocumentsAutomatically
                SendDocumentsAutomaticallyCommand.NotifyCanExecuteChanged();
            }
        };
    }    /// <summary>
    /// Maneja cambios en la configuración
    /// </summary>
    private async void OnConfigurationChanged(object? sender, EventArgs e)
    {        try
        {
            // Recargar configuración en los ViewModels correspondientes
            await SmtpConfiguration.LoadSmtpConfigurationAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando configuración");
        }
    }

    /// <summary>
    /// Limpia el log de texto
    /// </summary>
    [RelayCommand]    private void ClearLog()
    {
        LogText = string.Empty;
        GlobalStatusMessage = "Log limpiado";
    }/// <summary>
    /// Comando para envío automático de documentos por email
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSendDocumentsAutomatically))]
    private async Task SendDocumentsAutomatically()
    {
        try
        {
            if (!AutomaticEmail.CanSendAutomatically)
            {
                GlobalStatusMessage = "No se puede enviar: verifica configuración SMTP, archivo Excel y documentos generados";
                _logger.LogWarning("❌ No se puede enviar automáticamente - requisitos no cumplidos");
                return;
            }

            _logger.LogInformation("🚀 Iniciando envío automático de documentos por email");
            GlobalStatusMessage = "Enviando documentos automáticamente...";

            // Ejecutar envío automático pasando la configuración SMTP
            var result = await AutomaticEmail.SendDocumentsAutomaticallyWithConfig(SmtpConfiguration);

            if (result)
            {
                GlobalStatusMessage = "✅ Envío automático completado exitosamente";
                _logger.LogInformation("✅ Envío automático completado con éxito");
            }
            else
            {
                GlobalStatusMessage = "❌ Envío automático falló - revisar logs para detalles";
                _logger.LogWarning("❌ Envío automático no completado exitosamente");
            }
        }
        catch (Exception ex)
        {
            GlobalStatusMessage = $"❌ Error en envío automático: {ex.Message}";
            _logger.LogError(ex, "❌ Error durante el envío automático de documentos");
        }
    }

    /// <summary>
    /// Determina si se puede ejecutar el envío automático
    /// </summary>
    private bool CanSendDocumentsAutomatically()
    {
        return AutomaticEmail?.CanSendAutomatically == true;
    }    /// <summary>
    /// Inicializa todos los componentes después de la construcción
    /// </summary>
    public async Task InitializeAsync()
    {        
        try
        {
            // Cargar configuración SMTP
            await SmtpConfiguration.LoadSmtpConfigurationAsync();
            
            // NOTA: Los documentos se cargarán cuando se seleccione el archivo Excel de emails
            // await DocumentManagement.LoadPreviouslyGeneratedDocuments();
            
            // Sincronizar estados iniciales
            AutomaticEmail.UpdateGeneratedDocuments(DocumentManagement.GeneratedDocuments);
            AutomaticEmail.UpdateEmailConfiguration(SmtpConfiguration.IsEmailConfigured);
            
            GlobalStatusMessage = "Componentes inicializados correctamente";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error inicializando MainDocumentGenerationViewModel");
            GlobalStatusMessage = "Error en la inicialización";
        }
    }

    /// <summary>
    /// Limpia recursos y se desuscribe de eventos
    /// </summary>
    public void Cleanup()
    {
        try
        {
            _configurationService.ConfigurationChanged -= OnConfigurationChanged;
              SmtpConfiguration?.Cleanup();
            AutomaticEmail?.Cleanup();
            
            _logger.LogInformation("MainDocumentGenerationViewModel limpiado correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la limpieza");
        }
    }
}
