using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;

namespace GestLog.Modules.GestionCartera.ViewModels;

/// <summary>
/// Vista de solo lectura de la configuración SMTP de Cartera.
/// La única autoridad es <see cref="ISmtpPersistenceService"/> (JSON + Credential Manager);
/// aquí solo se copian los valores para la UI y para armar el envío.
/// Quien edite la configuración debe usar SmtpConfigurationWindow.
/// </summary>
public partial class SmtpConfigurationViewModel : ObservableObject
{
    private readonly IGestLogLogger _logger;
    private readonly ISmtpPersistenceService _smtpPersistenceService;

    // Propiedades SMTP
    [ObservableProperty] private string _smtpServer = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _smtpUsername = string.Empty;
    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private bool _enableSsl = true;
    [ObservableProperty] private bool _isEmailConfigured = false;

    // Propiedades BCC y CC
    [ObservableProperty] private string _bccEmail = string.Empty;
    [ObservableProperty] private string _ccEmail = string.Empty;

    public SmtpConfigurationViewModel(
        IGestLogLogger logger,
        ISmtpPersistenceService smtpPersistenceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _smtpPersistenceService = smtpPersistenceService ?? throw new ArgumentNullException(nameof(smtpPersistenceService));

        // Cargar configuración inicial
        LoadSmtpConfiguration();

        _logger.LogDebug("SmtpConfigurationViewModel inicializado - Servidor: {Server}, Configurado: {IsConfigured}",
            SmtpServer ?? "VACIO", IsEmailConfigured);
    }

    /// <summary>
    /// Carga la configuración SMTP desde el almacén único (JSON + Credential Manager).
    /// Síncrono a propósito: son lecturas instantáneas y escribe propiedades observables,
    /// que sólo pueden tocarse desde el hilo de UI.
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
}
