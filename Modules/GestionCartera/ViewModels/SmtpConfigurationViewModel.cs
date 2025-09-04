using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;
using GestLog.Services.Configuration;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para gestión de configuración SMTP
/// </summary>
public partial class SmtpConfigurationViewModel : ObservableObject, IDisposable
{
    private readonly IEmailService? _emailService;
    private readonly IConfigurationService _configurationService;
    private readonly ICredentialService _credentialService;
    private readonly IGestLogLogger _logger;    // Propiedades SMTP
    [ObservableProperty] private string _smtpServer = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _smtpUsername = string.Empty;
    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private bool _enableSsl = true;
    [ObservableProperty] private bool _isEmailConfigured = false;
    [ObservableProperty] private bool _isConfiguring = false;
    
    // Propiedades BCC y CC
    [ObservableProperty] private string _bccEmail = string.Empty;
    [ObservableProperty] private string _ccEmail = string.Empty;
    
    // Propiedades adicionales para compatibilidad
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SmtpConfigurationViewModel(
        IEmailService? emailService, 
        IConfigurationService configurationService,
        ICredentialService credentialService,
        IGestLogLogger logger)
    {
        _emailService = emailService;
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Suscribirse a cambios de configuración
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
        
        // Cargar configuración inicial
        LoadSmtpConfiguration();
    }    [RelayCommand(CanExecute = nameof(CanConfigureSmtp))]
    private async Task ConfigureSmtpAsync(CancellationToken cancellationToken = default)
    {
        if (_emailService == null) return;        try
        {
            IsConfiguring = true;

            var smtpConfig = new SmtpConfiguration
            {
                SmtpServer = SmtpServer,
                Port = SmtpPort,
                Username = SmtpUsername,
                Password = SmtpPassword,
                EnableSsl = EnableSsl,
                BccEmail = BccEmail,
                CcEmail = CcEmail
            };

            await _emailService.ConfigureSmtpAsync(smtpConfig, cancellationToken);
            IsEmailConfigured = await _emailService.ValidateConfigurationAsync(cancellationToken);
            
            if (IsEmailConfigured)
            {
                await SaveSmtpConfigurationAsync();
                _logger.LogInformation("Configuración SMTP exitosa y guardada");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al configurar SMTP");
            IsEmailConfigured = false;
        }
        finally
        {
            IsConfiguring = false;
        }
    }    [RelayCommand]
    private void ClearConfiguration()
    {
        SmtpServer = string.Empty;
        SmtpPort = 587;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
        EnableSsl = true;
        IsEmailConfigured = false;
        StatusMessage = "Configuración de email limpiada";
        _logger.LogInformation("Configuración de email limpiada");
    }

    /// <summary>
    /// Comando para limpiar configuración de email (alias para compatibilidad)
    /// </summary>
    [RelayCommand]
    private void ClearEmailConfiguration()
    {
        ClearConfiguration();
    }

    /// <summary>
    /// Comando para probar conexión SMTP
    /// </summary>
    [RelayCommand]
    private async Task TestSmtpConnection()
    {
        if (_emailService == null)
        {
            StatusMessage = "Servicio de email no disponible";
            _logger.LogWarning("Servicio de email no disponible para prueba de conexión");
            return;
        }

        try
        {
            IsConfiguring = true;
            StatusMessage = "Probando conexión SMTP...";
              var smtpConfig = new SmtpConfiguration
            {
                SmtpServer = SmtpServer,
                Port = SmtpPort,
                Username = SmtpUsername,
                Password = SmtpPassword,
                EnableSsl = EnableSsl,
                BccEmail = BccEmail,
                CcEmail = CcEmail
            };

            await _emailService.ConfigureSmtpAsync(smtpConfig);
            var isValid = await _emailService.ValidateConfigurationAsync();
              if (isValid)
            {
                StatusMessage = "✅ Conexión SMTP exitosa";
                _logger.LogInformation("Prueba de conexión SMTP exitosa");
            }
            else
            {
                StatusMessage = "❌ Error en la conexión SMTP";
                _logger.LogWarning("Prueba de conexión SMTP falló");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
            _logger.LogError(ex, "Error durante prueba de conexión SMTP");
        }
        finally
        {
            IsConfiguring = false;
        }
    }

    /// <summary>
    /// Método para cargar configuración SMTP de forma asíncrona
    /// </summary>
    public async Task LoadSmtpConfigurationAsync()
    {
        await Task.Run(() => LoadSmtpConfiguration());
    }

    private bool CanConfigureSmtp() => !IsConfiguring && 
        !string.IsNullOrWhiteSpace(SmtpServer) && 
        !string.IsNullOrWhiteSpace(SmtpUsername) && 
        !string.IsNullOrWhiteSpace(SmtpPassword);    private void LoadSmtpConfiguration()
    {
        try
        {
            var smtpConfig = _configurationService.Current.Smtp;
              SmtpServer = smtpConfig.Server ?? string.Empty;
            SmtpPort = smtpConfig.Port;
            SmtpUsername = smtpConfig.Username ?? string.Empty;
            EnableSsl = smtpConfig.UseSSL;
            IsEmailConfigured = smtpConfig.IsConfigured;
              // Cargar campos BCC y CC desde la configuración
            BccEmail = smtpConfig.BccEmail ?? string.Empty;
            CcEmail = smtpConfig.CcEmail ?? string.Empty;

            // Cargar contraseña desde Windows Credential Manager
            if (!string.IsNullOrWhiteSpace(smtpConfig.Username))
            {
                var credentialTarget = $"SMTP_{smtpConfig.Server}_{smtpConfig.Username}";
                
                if (_credentialService.CredentialsExist(credentialTarget))
                {
                    var (username, password) = _credentialService.GetCredentials(credentialTarget);
                    SmtpPassword = password;
                    
                    // Revalidar configuración con contraseña
                    smtpConfig.Password = password;
                    smtpConfig.ValidateConfiguration();
                    IsEmailConfigured = smtpConfig.IsConfigured;
                }
                else
                {
                    SmtpPassword = string.Empty;
                    IsEmailConfigured = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar configuración SMTP");
            IsEmailConfigured = false;
            SmtpPassword = string.Empty;
        }
    }    private async Task SaveSmtpConfigurationAsync()
    {
        try
        {
            var smtpConfig = _configurationService.Current.Smtp;
            
            // Actualizar configuración (sin contraseña)
            smtpConfig.Server = SmtpServer;
            smtpConfig.Port = SmtpPort;
            smtpConfig.Username = SmtpUsername;
            smtpConfig.FromEmail = SmtpUsername;
            smtpConfig.FromName = SmtpUsername;
            smtpConfig.UseSSL = EnableSsl;
            smtpConfig.UseAuthentication = !string.IsNullOrWhiteSpace(SmtpUsername);
            
            // ✅ CORRECCIÓN: Mantener campos BCC y CC existentes si ya están configurados
            // Solo actualizamos si no están ya configurados para no sobrescribir valores existentes
            // Los campos BCC y CC se configuran desde la ventana de configuración avanzada

            // Guardar contraseña de forma segura
            if (!string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword))
            {
                var credentialTarget = $"SMTP_{SmtpServer}_{SmtpUsername}";
                _credentialService.SaveCredentials(credentialTarget, SmtpUsername, SmtpPassword);
            }

            smtpConfig.ValidateConfiguration();
            await _configurationService.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuración SMTP");
            throw;
        }
    }private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        if (e.SettingPath.StartsWith("smtp", StringComparison.OrdinalIgnoreCase))
        {
            LoadSmtpConfiguration();
        }
    }

    /// <summary>
    /// Limpia recursos y se desuscribe de eventos
    /// </summary>
    public void Cleanup()
    {
        Dispose();
    }

    public void Dispose()
    {
        _configurationService.ConfigurationChanged -= OnConfigurationChanged;
        GC.SuppressFinalize(this);
    }
}
