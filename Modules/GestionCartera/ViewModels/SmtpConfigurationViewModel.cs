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
    private readonly IGestLogLogger _logger;

    // Propiedades SMTP
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

    // Campo privado para controlar el ciclo de advertencia
    private bool _warnedMissingPassword = false;

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
        
        _logger.LogDebug("SmtpConfigurationViewModel inicializado - Servidor: {Server}, Configurado: {IsConfigured}", 
            SmtpServer ?? "VACIO", IsEmailConfigured);
    }

    [RelayCommand(CanExecute = nameof(CanConfigureSmtp))]
    private async Task ConfigureSmtpAsync(CancellationToken cancellationToken = default)
    {
        if (_emailService == null) return;

        try
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

            _logger.LogInformation($"[TRACE] Configurando SMTP para envío. Usuario: '{SmtpUsername}', Contraseña: '{(string.IsNullOrWhiteSpace(SmtpPassword) ? "VACIA" : "***OCULTA***")}'");
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
    }

    [RelayCommand]
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

    [RelayCommand]
    private void ClearEmailConfiguration()
    {
        ClearConfiguration();
    }

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

    public async Task LoadSmtpConfigurationAsync()
    {
        await Task.Run(() => LoadSmtpConfiguration());
    }

    private bool CanConfigureSmtp() => !IsConfiguring && 
        !string.IsNullOrWhiteSpace(SmtpServer) && 
        !string.IsNullOrWhiteSpace(SmtpUsername) && 
        !string.IsNullOrWhiteSpace(SmtpPassword);

    /// <summary>
    /// Carga configuración SMTP con nueva estrategia: Windows Credential Manager primero, JSON como fallback
    /// </summary>
    public void LoadSmtpConfiguration()
    {
        try
        {
            // PRIMERO: Intentar cargar desde Windows Credential Manager
            bool loadedFromCredentials = TryLoadFromCredentials();
            // Si la contraseña fue cargada correctamente desde credenciales, no mostrar advertencia
            if (!string.IsNullOrWhiteSpace(SmtpPassword))
            {
                _warnedMissingPassword = false;
                return;
            }
            // Cargar el resto de la configuración desde JSON (sin contraseña)
            var smtpConfig = _configurationService.Current.Smtp;
            if (smtpConfig != null)
            {
                SmtpServer = smtpConfig.Server ?? string.Empty;
                SmtpPort = smtpConfig.Port;
                SmtpUsername = smtpConfig.Username ?? string.Empty;
                EnableSsl = smtpConfig.UseSSL;
                BccEmail = smtpConfig.BccEmail ?? string.Empty;
                CcEmail = smtpConfig.CcEmail ?? string.Empty;
                if (!smtpConfig.UseAuthentication)
                {
                    IsEmailConfigured = !string.IsNullOrWhiteSpace(smtpConfig.Server) && smtpConfig.Port > 0 && !string.IsNullOrWhiteSpace(smtpConfig.FromEmail);
                }
                else
                {
                    IsEmailConfigured = !string.IsNullOrWhiteSpace(smtpConfig.Server) && smtpConfig.Port > 0 && !string.IsNullOrWhiteSpace(smtpConfig.Username);
                    // Solo mostrar advertencia si el usuario intenta enviar y la contraseña está vacía
                    if (IsEmailConfigured && string.IsNullOrWhiteSpace(SmtpPassword) && !_warnedMissingPassword)
                    {
                        _warnedMissingPassword = true;
                    }
                }
            }
            else
            {
                _logger.LogWarning("No se encontró configuración SMTP en JSON, usando valores por defecto");
                SetDefaultValues();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la configuración SMTP");
            SetDefaultValues();
        }
    }

    /// <summary>
    /// Intenta cargar configuración desde Windows Credential Manager
    /// </summary>
    private bool TryLoadFromCredentials()
    {
        try
        {
            var knownCredentialTargets = new[]
            {
                $"SMTP_{SmtpServer}_{SmtpUsername}",
                "SMTP_smtppro.zoho.com_prueba@gmail.com",
                "SMTP_smtppro.zoho.com_cartera@simicsgroup.com"
            };
            foreach (var target in knownCredentialTargets)
            {
                if (_credentialService.CredentialsExist(target))
                {
                    var (username, password) = _credentialService.GetCredentials(target);
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        string server = ExtractServerFromTarget("GestLog_SMTP_" + target);
                        SmtpServer = server;
                        SmtpUsername = username;
                        SmtpPassword = password;
                        SmtpPort = 587;
                        EnableSsl = true;
                        IsEmailConfigured = true;
                        var jsonConfig = _configurationService.Current.Smtp;
                        BccEmail = jsonConfig?.BccEmail ?? string.Empty;
                        CcEmail = jsonConfig?.CcEmail ?? string.Empty;
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar cargar desde Windows Credential Manager");
            return false;
        }
    }

    /// <summary>
    /// Extrae el servidor del target de credencial
    /// </summary>
    private string ExtractServerFromTarget(string target)
    {
        try
        {
            // "GestLog_SMTP_SMTP_smtppro.zoho.com_prueba@gmail.com"
            var parts = target.Split('_');
            
            // Buscar la parte que contiene "." pero no "@" (es el servidor)
            foreach (var part in parts)
            {
                if (part.Contains(".") && !part.Contains("@"))
                {
                    return part;
                }
            }
            
            _logger.LogWarning($"No se pudo extraer servidor del target: {target}");
            return "smtppro.zoho.com"; // Fallback al servidor más común
        }
        catch
        {
            return "smtppro.zoho.com"; // Fallback seguro
        }
    }

    private void SetDefaultValues()
    {
        SmtpServer = string.Empty;
        SmtpPort = 587;
        SmtpUsername = string.Empty;
        SmtpPassword = string.Empty;
        EnableSsl = true;
        BccEmail = string.Empty;
        CcEmail = string.Empty;
        IsEmailConfigured = false;
    }

    public void ReloadConfiguration()
    {
        try
        {
            _logger.LogInformation("Recargando configuración SMTP manualmente...");
            LoadSmtpConfiguration();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recargando configuración SMTP manualmente");
        }
    }

    private async Task SaveSmtpConfigurationAsync()
    {
        try
        {
            var smtpConfig = _configurationService.Current.Smtp;
            // Actualizar configuración básica en JSON (sin contraseña)
            smtpConfig.Server = SmtpServer;
            smtpConfig.Port = SmtpPort;
            smtpConfig.Username = SmtpUsername;
            smtpConfig.FromEmail = SmtpUsername;
            smtpConfig.FromName = SmtpUsername;
            smtpConfig.UseSSL = EnableSsl;
            smtpConfig.UseAuthentication = !string.IsNullOrWhiteSpace(SmtpUsername);
            smtpConfig.IsConfigured = true;
            // Guardar contraseña de forma segura en Windows Credential Manager
            if (!string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(SmtpPassword))
            {
                var credentialTarget = $"SMTP_{SmtpServer}_{SmtpUsername}";
                _logger.LogInformation($"[TRACE] Intentando guardar credenciales SMTP en Credential Manager. Target: {credentialTarget}, Usuario: {SmtpUsername}");
                var saved = _credentialService.SaveCredentials(credentialTarget, SmtpUsername, SmtpPassword);
                if (saved)
                {
                    _logger.LogInformation($"[TRACE] Credenciales SMTP guardadas correctamente en Credential Manager. Target: {credentialTarget}");
                }
                else
                {
                    _logger.LogWarning($"[TRACE] Error al guardar credenciales SMTP en Credential Manager. Target: {credentialTarget}");
                }
            }
            else
            {
                _logger.LogWarning($"[TRACE] No se guardaron credenciales SMTP porque el usuario o la contraseña están vacíos. Usuario: '{SmtpUsername}', Contraseña vacía: '{string.IsNullOrWhiteSpace(SmtpPassword)}'");
            }
            await _configurationService.SaveAsync();
            _logger.LogInformation("Configuración SMTP guardada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar configuración SMTP");
            throw;
        }
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        if (e.SettingPath.StartsWith("smtp", StringComparison.OrdinalIgnoreCase))
        {
            LoadSmtpConfiguration();
        }
    }

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
