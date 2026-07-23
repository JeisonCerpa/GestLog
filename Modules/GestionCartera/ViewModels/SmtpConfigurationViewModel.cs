using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;
using GestLog.Modules.GestionCartera.Services;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// ViewModel para gestión de configuración SMTP.
/// Toda la persistencia (JSON + Credential Manager) se delega al almacén único
/// <see cref="ISmtpPersistenceService"/>; este ViewModel solo expone los datos a la UI.
/// </summary>
public partial class SmtpConfigurationViewModel : ObservableObject, IDisposable
{
    private readonly IEmailService? _emailService;
    private readonly IConfigurationService _configurationService;
    private readonly IGestLogLogger _logger;
    private readonly ISmtpPersistenceService _smtpPersistenceService;

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
        IGestLogLogger logger,
        ISmtpPersistenceService smtpPersistenceService)
    {
        _emailService = emailService;
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _smtpPersistenceService = smtpPersistenceService ?? throw new ArgumentNullException(nameof(smtpPersistenceService));

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

            // Prueba real: handshake + autenticación sin enviar correo.
            var result = await _emailService.TestConnectionAsync(smtpConfig);

            if (result.IsSuccess)
            {
                StatusMessage = "✅ " + result.Message;
                _logger.LogInformation("Prueba de conexión SMTP exitosa");
            }
            else
            {
                StatusMessage = "❌ " + result.Message;
                _logger.LogWarning("Prueba de conexión SMTP falló: {Message}", result.Message);
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
    /// Carga la configuración SMTP desde el almacén único (JSON + Credential Manager).
    /// </summary>
    public void LoadSmtpConfiguration()
    {
        try
        {
            var smtp = _smtpPersistenceService.LoadSmtpConfigurationAsync().GetAwaiter().GetResult();
            if (smtp == null)
            {
                _logger.LogWarning("⚠️ No hay configuración SMTP de Cartera, usando valores por defecto");
                SetDefaultValues();
                return;
            }

            SmtpServer = smtp.Server ?? string.Empty;
            SmtpPort = smtp.Port;
            SmtpUsername = smtp.Username ?? string.Empty;
            SmtpPassword = smtp.Password ?? string.Empty;
            EnableSsl = smtp.UseSSL;
            BccEmail = smtp.BccEmail ?? string.Empty;
            CcEmail = smtp.CcEmail ?? string.Empty;
            IsEmailConfigured = !string.IsNullOrWhiteSpace(SmtpServer) && SmtpPort > 0 && !string.IsNullOrWhiteSpace(SmtpUsername);

            if (string.IsNullOrWhiteSpace(SmtpPassword))
                _logger.LogWarning("⚠️ Contraseña SMTP no encontrada en Credential Manager");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cargar la configuración SMTP");
            SetDefaultValues();
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

    /// <summary>
    /// Recarga la contraseña desde el almacén si está vacía (útil justo antes de enviar).
    /// </summary>
    public void EnsurePasswordLoaded()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SmtpPassword) && !string.IsNullOrWhiteSpace(SmtpServer) && !string.IsNullOrWhiteSpace(SmtpUsername))
            {
                var smtp = _smtpPersistenceService.LoadSmtpConfigurationAsync().GetAwaiter().GetResult();
                if (smtp != null && !string.IsNullOrWhiteSpace(smtp.Password))
                {
                    SmtpPassword = smtp.Password;
                    _logger.LogInformation("✅ Contraseña SMTP recargada desde el almacén");
                }
                else
                {
                    _logger.LogWarning("⚠️ No se pudo recargar la contraseña SMTP desde el almacén");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recargando contraseña SMTP");
        }
    }

    private async Task SaveSmtpConfigurationAsync()
    {
        var settings = new SmtpSettings
        {
            Server = SmtpServer,
            Port = SmtpPort,
            Username = SmtpUsername,
            FromEmail = SmtpUsername,
            FromName = SmtpUsername,
            UseSSL = EnableSsl,
            UseAuthentication = !string.IsNullOrWhiteSpace(SmtpUsername),
            BccEmail = BccEmail ?? string.Empty,
            CcEmail = CcEmail ?? string.Empty,
            Password = SmtpPassword,
            Timeout = 120000,
            IsConfigured = true
        };

        var saved = await _smtpPersistenceService.SaveSmtpConfigurationAsync(settings, "SmtpConfigurationViewModel.ConfigureSmtp");
        if (!saved)
            _logger.LogWarning("No se pudo guardar la configuración SMTP");
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
