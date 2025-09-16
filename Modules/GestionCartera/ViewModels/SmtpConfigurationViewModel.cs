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
        _ = LoadSmtpConfiguration();
        
        _logger.LogInformation("SmtpConfigurationViewModel inicializado - Servidor: {Server}, Configurado: {IsConfigured}", 
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
        await LoadSmtpConfiguration();
    }

    private bool CanConfigureSmtp() => !IsConfiguring && 
        !string.IsNullOrWhiteSpace(SmtpServer) && 
        !string.IsNullOrWhiteSpace(SmtpUsername) && 
        !string.IsNullOrWhiteSpace(SmtpPassword);

    /// <summary>
    /// Carga configuración SMTP con nueva estrategia: Windows Credential Manager primero, JSON como fallback
    /// </summary>
    public async Task LoadSmtpConfiguration()
    {
        try
        {
            _logger.LogInformation("Iniciando carga de configuración SMTP");

            // PRIMERO: Intentar cargar desde Windows Credential Manager
            bool loadedFromCredentials = TryLoadFromCredentials();
            
            if (loadedFromCredentials)
            {
                _logger.LogInformation("Configuración SMTP cargada exitosamente desde Windows Credential Manager");
                return;
            }

            // FALLBACK: Cargar desde JSON si no hay credenciales guardadas
            _logger.LogInformation("No se encontraron credenciales guardadas, cargando desde configuración JSON");
            var smtpConfig = _configurationService.Current.Smtp;
            if (smtpConfig != null)
            {
                _logger.LogInformation($"Configuración básica cargada desde JSON: Server={smtpConfig.Server}, Port={smtpConfig.Port}, Username={smtpConfig.Username}");
                
                SmtpServer = smtpConfig.Server ?? string.Empty;
                SmtpPort = smtpConfig.Port;
                SmtpUsername = smtpConfig.Username ?? string.Empty;
                SmtpPassword = string.Empty; // No cargar contraseña desde JSON por seguridad
                EnableSsl = smtpConfig.UseSSL;
                BccEmail = smtpConfig.BccEmail ?? string.Empty;
                CcEmail = smtpConfig.CcEmail ?? string.Empty;
                IsEmailConfigured = false; // Sin credenciales, no está configurado
            }
            else
            {
                _logger.LogWarning("No se encontró configuración SMTP en JSON, usando valores por defecto");
                SetDefaultValues();
            }

            _logger.LogInformation("Carga de configuración SMTP completada");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la configuración SMTP");
            SetDefaultValues();
        }
    }    /// <summary>
    /// Intenta cargar configuración desde Windows Credential Manager
    /// </summary>
    private bool TryLoadFromCredentials()
    {
        try
        {
            _logger.LogInformation("Buscando credenciales SMTP en Windows Credential Manager");

            // Credenciales conocidas - SOLO la parte después de "GestLog_SMTP_"
            var knownCredentialTargets = new[]
            {
                "SMTP_smtppro.zoho.com_prueba@gmail.com",
                "SMTP_smtppro.zoho.com_cartera@simicsgroup.com"
            };

            foreach (var target in knownCredentialTargets)
            {
                _logger.LogInformation($"Verificando credencial con target: {target}");
                
                if (_credentialService.CredentialsExist(target))
                {
                    var (username, password) = _credentialService.GetCredentials(target);
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        // Extraer servidor del target
                        string server = ExtractServerFromTarget("GestLog_SMTP_" + target); // Usar formato completo para extracción
                        
                        SmtpServer = server;
                        SmtpUsername = username;
                        SmtpPassword = password;
                        SmtpPort = 587;
                        EnableSsl = true;
                        IsEmailConfigured = true;
                        
                        // BCC y CC desde JSON si están disponibles
                        var jsonConfig = _configurationService.Current.Smtp;
                        BccEmail = jsonConfig?.BccEmail ?? string.Empty;
                        CcEmail = jsonConfig?.CcEmail ?? string.Empty;
                        
                        _logger.LogInformation($"✅ Configuración SMTP cargada desde credenciales - Servidor: {server}, Usuario: {username}");
                        return true;
                    }
                }
                else
                {
                    _logger.LogInformation($"❌ Credencial no encontrada para target: {target}");
                }
            }

            _logger.LogInformation("No se encontraron credenciales SMTP válidas");
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
            _ = LoadSmtpConfiguration();
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
                // Usar el formato correcto SIN el prefijo "GestLog_SMTP_" (el servicio lo agrega automáticamente)
                var credentialTarget = $"SMTP_{SmtpServer}_{SmtpUsername}";
                _credentialService.SaveCredentials(credentialTarget, SmtpUsername, SmtpPassword);
                
                _logger.LogInformation($"Credenciales SMTP guardadas con target: {credentialTarget}");
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
            _ = LoadSmtpConfiguration();
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
