using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Modules.Usuarios.Models.Authentication;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel wrapper que mantiene compatibilidad con la UI existente
/// Delega funcionalidad al MainDocumentGenerationViewModel refactorizado
/// </summary>
public partial class DocumentGenerationViewModel : ObservableObject
{
    private readonly MainDocumentGenerationViewModel _mainViewModel;
    private readonly IGestLogLogger _logger;
    private readonly CurrentUserInfo _currentUser;

    // Permisos para Gestión de Cartera
    public bool CanAccessGestionCartera => _currentUser.HasPermission("Herramientas.AccederGestionCartera");
    public bool CanGenerateDocuments => _currentUser.HasPermission("GestionCartera.GenerarDocumentos");
    public bool CanSendEmails => _currentUser.HasPermission("GestionCartera.EnviarEmails");
    public bool CanImportExcel => _currentUser.HasPermission("GestionCartera.ImportarExcel");
    // Ahora requiere ambos permisos para configurar SMTP
    public bool CanConfigureSmtp => _currentUser.HasPermission("GestionCartera.ConfigurarSmtp") && CanGenerateDocuments;
    public bool CanOpenOutputFolder => !string.IsNullOrWhiteSpace(OutputFolderPath) && Directory.Exists(OutputFolderPath);
    // Nuevo: proteger envío automático por ambos permisos
    public bool CanSendDocumentsAutomatically => CanSendEmails && CanGenerateDocuments;
    public bool CanSelectExcelFile => CanGenerateDocuments;
    public bool CanTestSmtp => CanConfigureSmtp;

    // Modificar constructor para DI
    public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IGestLogLogger logger, CurrentUserInfo currentUser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mainViewModel = new MainDocumentGenerationViewModel(pdfGenerator, null!, logger);
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        InitializeAsync();
        SubscribeToViewModelEvents();
    }    public DocumentGenerationViewModel(IPdfGeneratorService pdfGenerator, IEmailService emailService, IGestLogLogger logger, CurrentUserInfo currentUser)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mainViewModel = new MainDocumentGenerationViewModel(pdfGenerator, emailService, logger);
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        InitializeAsync();
        SubscribeToViewModelEvents();
    }    private async void InitializeAsync()
    {
        try
        {
            await _mainViewModel.InitializeAsync();
              // Suscribirse a eventos de cambio de propiedad de los sub-ViewModels
            _mainViewModel.PdfGeneration.PropertyChanged += (s, e) => 
            {
                OnPropertyChanged(e.PropertyName);                // Notificar cambios en comandos cuando cambien propiedades relevantes
                if (e.PropertyName == nameof(_mainViewModel.PdfGeneration.SelectedExcelFilePath) ||
                    e.PropertyName == nameof(_mainViewModel.PdfGeneration.OutputFolderPath) ||
                    e.PropertyName == nameof(_mainViewModel.PdfGeneration.IsProcessing))
                {
                    GenerateDocumentsCommand.NotifyCanExecuteChanged();
                    CancelGenerationCommand.NotifyCanExecuteChanged();
                }
            };
            _mainViewModel.DocumentManagement.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            _mainViewModel.AutomaticEmail.PropertyChanged += (s, e) => 
            {
                OnPropertyChanged(e.PropertyName);
                // Notificar cambios en CanSendAutomatically cuando cambien propiedades relevantes
                if (e.PropertyName == nameof(_mainViewModel.AutomaticEmail.CanSendAutomatically) ||
                    e.PropertyName == nameof(_mainViewModel.AutomaticEmail.IsSendingEmail) ||
                    e.PropertyName == nameof(_mainViewModel.AutomaticEmail.HasEmailExcel))
                {
                    SendDocumentsAutomaticallyCommand.NotifyCanExecuteChanged();
                    // 🔥 Solución: Notificar explícitamente el cambio de CanSendAutomatically en el wrapper
                    OnPropertyChanged(nameof(CanSendAutomatically));
                }
            };
            _mainViewModel.SmtpConfiguration.PropertyChanged += (s, e) => 
            {
                OnPropertyChanged(e.PropertyName);
                // Notificar cambios en CanSendAutomatically cuando cambie la configuración SMTP
                if (e.PropertyName == nameof(_mainViewModel.SmtpConfiguration.IsEmailConfigured))
                {
                    SendDocumentsAutomaticallyCommand.NotifyCanExecuteChanged();
                }
            };
            _mainViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inicializando DocumentGenerationViewModel");
        }
    }

    // Notificar cambios en propiedades de los subviewmodels
    private void SubscribeToViewModelEvents()
    {
        _mainViewModel.PdfGeneration.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(PdfCanGenerateDocuments));
        };
        _mainViewModel.AutomaticEmail.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(AutoCanSendDocuments));
        };
    }

    #region Propiedades que delegan al MainViewModel

    // Propiedades de generación de PDF
    public string SelectedExcelFilePath 
    { 
        get => _mainViewModel.PdfGeneration.SelectedExcelFilePath; 
        set => _mainViewModel.PdfGeneration.SelectedExcelFilePath = value; 
    }

    public string OutputFolderPath 
    { 
        get => _mainViewModel.PdfGeneration.OutputFolderPath; 
        set => _mainViewModel.PdfGeneration.OutputFolderPath = value; 
    }

    public string TemplateFilePath 
    { 
        get => _mainViewModel.PdfGeneration.TemplateFilePath; 
        set => _mainViewModel.PdfGeneration.TemplateFilePath = value; 
    }

    public bool UseDefaultTemplate 
    { 
        get => _mainViewModel.PdfGeneration.UseDefaultTemplate; 
        set => _mainViewModel.PdfGeneration.UseDefaultTemplate = value; 
    }

    public string TemplateStatusMessage => _mainViewModel.PdfGeneration.TemplateStatusMessage;

    // Propiedades de estado
    public string LogText 
    { 
        get => _mainViewModel.LogText; 
        set => _mainViewModel.LogText = value; 
    }

    public string StatusMessage 
    { 
        get => _mainViewModel.GlobalStatusMessage; 
        set => _mainViewModel.GlobalStatusMessage = value; 
    }

    public bool IsProcessing => _mainViewModel.PdfGeneration.IsProcessing;
    public bool IsProcessingCompleted => _mainViewModel.PdfGeneration.IsProcessingCompleted;
    public double ProgressValue => _mainViewModel.PdfGeneration.ProgressValue;
    public int TotalDocuments => _mainViewModel.DocumentManagement.TotalDocuments;
    public int CurrentDocument => _mainViewModel.PdfGeneration.CurrentDocument;
    public IReadOnlyList<GeneratedPdfInfo> GeneratedDocuments => _mainViewModel.DocumentManagement.GeneratedDocuments;

    // Propiedades de configuración SMTP
    public string SmtpServer 
    { 
        get => _mainViewModel.SmtpConfiguration.SmtpServer; 
        set => _mainViewModel.SmtpConfiguration.SmtpServer = value; 
    }

    public int SmtpPort 
    { 
        get => _mainViewModel.SmtpConfiguration.SmtpPort; 
        set => _mainViewModel.SmtpConfiguration.SmtpPort = value; 
    }

    public string SmtpUsername 
    { 
        get => _mainViewModel.SmtpConfiguration.SmtpUsername; 
        set => _mainViewModel.SmtpConfiguration.SmtpUsername = value; 
    }

    public string SmtpPassword 
    { 
        get => _mainViewModel.SmtpConfiguration.SmtpPassword; 
        set => _mainViewModel.SmtpConfiguration.SmtpPassword = value; 
    }

    public bool SmtpUseSsl 
    { 
        get => _mainViewModel.SmtpConfiguration.EnableSsl; 
        set => _mainViewModel.SmtpConfiguration.EnableSsl = value; 
    }

    public bool EnableSsl 
    { 
        get => _mainViewModel.SmtpConfiguration.EnableSsl; 
        set => _mainViewModel.SmtpConfiguration.EnableSsl = value; 
    }    public bool IsSmtpConfigured => _mainViewModel.SmtpConfiguration.IsEmailConfigured;
    public bool IsEmailConfigured 
    { 
        get => _mainViewModel.SmtpConfiguration.IsEmailConfigured; 
        set => _mainViewModel.SmtpConfiguration.IsEmailConfigured = value; 
    }

    // Propiedades de email automático
    public string SelectedEmailExcelFilePath 
    { 
        get => _mainViewModel.AutomaticEmail.SelectedEmailExcelFilePath; 
        set => _mainViewModel.AutomaticEmail.SelectedEmailExcelFilePath = value; 
    }

    public bool HasEmailExcel => _mainViewModel.AutomaticEmail.HasEmailExcel;
    public int CompaniesWithEmail => _mainViewModel.AutomaticEmail.CompaniesWithEmail;
    public int CompaniesWithoutEmail => _mainViewModel.AutomaticEmail.CompaniesWithoutEmail;
    public bool IsSendingEmail => _mainViewModel.AutomaticEmail.IsSendingEmail;    public bool CanSendAutomatically => _mainViewModel.AutomaticEmail.CanSendAutomatically;

    /// <summary>
    /// Determina si se puede cancelar el envío de emails
    /// </summary>
    public bool CanCancelEmailSending => _mainViewModel.AutomaticEmail.CanCancelEmailSending;    // Propiedades del panel de finalización
    public bool ShowCompletionPanel => _mainViewModel.PdfGeneration.ShowCompletionPanel;
    public string CompletionMessage => _mainViewModel.PdfGeneration.CompletionMessage;

    // Propiedades de progreso de email
    public double EmailProgressValue => _mainViewModel.AutomaticEmail.EmailProgressValue;
    public string EmailStatusMessage => _mainViewModel.AutomaticEmail.EmailStatusMessage;
    public int CurrentEmailDocument => _mainViewModel.AutomaticEmail.CurrentEmailDocument;
    public int TotalEmailDocuments => _mainViewModel.AutomaticEmail.TotalEmailDocuments;

    public string EmailSubject 
    { 
        get => _mainViewModel.AutomaticEmail.EmailSubject; 
        set => _mainViewModel.AutomaticEmail.EmailSubject = value; 
    }

    public string EmailBody 
    { 
        get => _mainViewModel.AutomaticEmail.EmailBody; 
        set => _mainViewModel.AutomaticEmail.EmailBody = value; 
    }

    public string EmailRecipients 
    { 
        get => _mainViewModel.AutomaticEmail.EmailRecipients; 
        set => _mainViewModel.AutomaticEmail.EmailRecipients = value; 
    }

    public string EmailCc 
    { 
        get => _mainViewModel.AutomaticEmail.EmailCc; 
        set => _mainViewModel.AutomaticEmail.EmailCc = value; 
    }

    public string EmailBcc 
    { 
        get => _mainViewModel.AutomaticEmail.EmailBcc; 
        set => _mainViewModel.AutomaticEmail.EmailBcc = value; 
    }    public bool UseHtmlEmail 
    { 
        get => _mainViewModel.AutomaticEmail.UseHtmlEmail; 
        set => _mainViewModel.AutomaticEmail.UseHtmlEmail = value; 
    }

    // ✨ Nuevas propiedades agregadas para la UI modernizada del tab "Envío Automático"
    public string CompaniesStatusText => _mainViewModel.AutomaticEmail.CompaniesStatusText;
    public string DocumentStatusWarning => _mainViewModel.AutomaticEmail.DocumentStatusWarning;
    public bool HasDocumentsGenerated => _mainViewModel.AutomaticEmail.HasDocumentsGenerated;

    // Nuevas propiedades para exponer el estado de los sub-ViewModels
    public bool PdfCanGenerateDocuments
    {
        get => _mainViewModel.PdfGeneration.CanGenerateDocuments();
    }
    public bool AutoCanSendDocuments
    {
        get => _mainViewModel.AutomaticEmail.CanSendAutomatically;
    }

    #endregion

    #region Comandos que delegan al MainViewModel

    // Comandos protegidos por permisos
    [RelayCommand(CanExecute = nameof(CanGenerateDocuments))]
    private async Task GenerateDocuments() => await _mainViewModel.PdfGeneration.GenerateDocumentsCommand.ExecuteAsync(null);

    [RelayCommand(CanExecute = nameof(CanConfigureSmtp))]
    private async Task ConfigureSmtp() => await _mainViewModel.SmtpConfiguration.ConfigureSmtpCommand.ExecuteAsync(null);

    // Otros comandos
    [RelayCommand(CanExecute = nameof(CanSelectExcelFile))]
    private void SelectExcelFile() => _mainViewModel.PdfGeneration.SelectExcelFileCommand.Execute(null);
    [RelayCommand]
    private void SelectOutputFolder() => _mainViewModel.PdfGeneration.SelectOutputFolderCommand.Execute(null);
    [RelayCommand]
    private void SelectTemplate() => _mainViewModel.PdfGeneration.SelectTemplateCommand.Execute(null);
    [RelayCommand]
    private void ClearTemplate() => _mainViewModel.PdfGeneration.ClearTemplateCommand.Execute(null);
    [RelayCommand]
    private void ClearLog() => _mainViewModel.ClearLogCommand.Execute(null);
    [RelayCommand(CanExecute = nameof(CanOpenOutputFolder))]
    private void OpenOutputFolder() => _mainViewModel.PdfGeneration.OpenOutputFolderCommand.Execute(null);
    [RelayCommand]
    private void CancelGeneration() => _mainViewModel.PdfGeneration.CancelGenerationCommand.Execute(null);
    [RelayCommand]
    private void ResetProgress() => _mainViewModel.PdfGeneration.ResetProgressDataCommand.Execute(null);
    [RelayCommand]
    private void GoToEmailTab() => _mainViewModel.PdfGeneration.GoToEmailTabCommand.Execute(null);
    [RelayCommand(CanExecute = nameof(CanAccessGestionCartera))]
    private async Task SelectEmailExcelFile() => await _mainViewModel.AutomaticEmail.SelectEmailExcelFileAsync();
    [RelayCommand(CanExecute = nameof(CanSendDocumentsAutomatically))]
    private async Task SendDocumentsAutomatically() => await _mainViewModel.SendDocumentsAutomaticallyCommand.ExecuteAsync(null);
    [RelayCommand(CanExecute = nameof(CanSendEmails))]
    private void CancelEmailSending() => _mainViewModel.AutomaticEmail.CancelEmailSendingCommand.Execute(null);
    [RelayCommand(CanExecute = nameof(CanTestSmtp))]
    private async Task TestSmtpConnection() => await _mainViewModel.SmtpConfiguration.TestSmtpConnectionCommand.ExecuteAsync(null);

    #endregion

    #region Métodos CanExecute

    private bool CanSendAutomaticallyMethod() => _mainViewModel.AutomaticEmail.CanSendAutomatically;

    // Eliminar métodos duplicados y limpiar comandos
    // Validar existencia de ImportExcelCommand en PdfGenerationViewModel
    // Si no existe, eliminar el comando y el método relacionado
    // Si existe, mantenerlo protegido por CanImportExcel

    #endregion

    /// <summary>
    /// Limpia recursos
    /// </summary>
    public void Cleanup()
    {
        _mainViewModel?.Cleanup();
    }
}

    // -------------------------------------------------------------------------
    // DOCUMENTACIÓN DE PATRÓN DE PERMISOS (para Copilot y desarrolladores)
    // -------------------------------------------------------------------------
    // 1. Declara propiedades públicas bool en el ViewModel que consulten CurrentUserInfo.HasPermission("Permiso")
    // 2. Usa esas propiedades en los métodos CanExecute de los comandos RelayCommand
    // 3. Enlaza la visibilidad y habilitación de los controles en la UI a esas propiedades
    // 4. Para agregar un nuevo permiso:
    //    a) Añade la propiedad bool correspondiente
    //    b) Usa en CanExecute del comando
    //    c) Enlaza en la UI
    // 5. Documenta el permiso en el README del módulo
    // -------------------------------------------------------------------------
