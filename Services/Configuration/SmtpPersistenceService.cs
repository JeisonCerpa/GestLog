using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;

namespace GestLog.Services.Configuration;

/// <summary>
/// Almacén único de la configuración de envío de correo de Gestión de Cartera.
///
/// Fuentes de verdad:
/// - No secretos (servidor, puerto, SSL, usuario, BCC, CC): %APPDATA%\GestLog\app-config.json
///   → modules.gestionCartera.smtp (vía IConfigurationService).
/// - Contraseña: Windows Credential Manager, bajo la llave unificada
///   "GestionCartera_SMTP_{servidor}_{usuario}" (el CredentialService antepone "GestLog_SMTP_").
///
/// Sin caché (las lecturas son instantáneas) ni auditoría.
/// </summary>
public class SmtpPersistenceService : ISmtpPersistenceService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _configurationService;
    private readonly ICredentialService _credentialService;

    public SmtpPersistenceService(
        IGestLogLogger logger,
        IConfigurationService configurationService,
        ICredentialService credentialService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
    }

    /// <summary>Llave unificada de credencial para Cartera.</summary>
    private static string CredentialTarget(string server, string username)
        => $"GestionCartera_SMTP_{server}_{username}";

    public Task<SmtpSettings?> LoadSmtpConfigurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var smtp = _configurationService.Current?.Modules?.GestionCartera?.Smtp;
            if (smtp == null)
            {
                _logger.LogWarning("⚠️ [SmtpPersistenceService] No hay configuración SMTP de Cartera en JSON");
                return Task.FromResult<SmtpSettings?>(null);
            }

            // La contraseña nunca está en JSON ([JsonIgnore]); se rellena desde Credential Manager.
            smtp.Password = LoadPassword(smtp.Server, smtp.Username);

            _logger.LogInformation("📖 [SmtpPersistenceService] Config SMTP cargada - {Server}:{Port}, usuario {User}, contraseña {HasPwd}",
                smtp.Server, smtp.Port, string.IsNullOrWhiteSpace(smtp.Username) ? "(vacío)" : smtp.Username,
                string.IsNullOrWhiteSpace(smtp.Password) ? "ausente" : "presente");

            return Task.FromResult<SmtpSettings?>(smtp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SmtpPersistenceService] Error cargando configuración SMTP");
            return Task.FromResult<SmtpSettings?>(null);
        }
    }

    public async Task<bool> SaveSmtpConfigurationAsync(SmtpSettings configuration, string operationSource = "Unknown", CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateConfiguration(configuration))
            {
                _logger.LogWarning("⚠️ [SmtpPersistenceService] Configuración inválida, no se guarda (fuente: {Source})", operationSource);
                return false;
            }

            var smtp = _configurationService.Current?.Modules?.GestionCartera?.Smtp;
            if (smtp != null)
            {
                smtp.Server = configuration.Server;
                smtp.Port = configuration.Port;
                smtp.Username = configuration.Username;
                smtp.FromEmail = configuration.Username;
                smtp.FromName = configuration.Username;
                smtp.UseSSL = configuration.UseSSL;
                smtp.UseAuthentication = !string.IsNullOrWhiteSpace(configuration.Username);
                smtp.BccEmail = configuration.BccEmail ?? string.Empty;
                smtp.CcEmail = configuration.CcEmail ?? string.Empty;
                smtp.Timeout = configuration.Timeout;
                smtp.IsConfigured = true;
                // Password tiene [JsonIgnore]: se mantiene en memoria para lecturas posteriores, no se serializa.
                if (!string.IsNullOrWhiteSpace(configuration.Password))
                    smtp.Password = configuration.Password;
            }

            // await real (no bloquear el hilo de UI): SaveAsync hace I/O de archivo asíncrono.
            await _configurationService.SaveAsync().ConfigureAwait(false);

            // Contraseña → Credential Manager (llave unificada). Solo si viene; si no, se conserva la existente.
            if (!string.IsNullOrWhiteSpace(configuration.Username) && !string.IsNullOrWhiteSpace(configuration.Password))
            {
                var target = CredentialTarget(configuration.Server, configuration.Username);
                _credentialService.DeleteCredentials(target);
                var saved = _credentialService.SaveCredentials(target, configuration.Username, configuration.Password);
                if (!saved)
                    _logger.LogWarning("⚠️ [SmtpPersistenceService] No se pudo guardar la contraseña en Credential Manager");
            }

            _logger.LogInformation("💾 [SmtpPersistenceService] Config SMTP guardada (fuente: {Source}) - {Server}:{Port}",
                operationSource, configuration.Server, configuration.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [SmtpPersistenceService] Error guardando configuración SMTP");
            return false;
        }
    }

    public bool ValidateConfiguration(SmtpSettings? configuration)
    {
        if (configuration == null)
            return false;

        if (string.IsNullOrWhiteSpace(configuration.Server))
            return false;

        if (configuration.Port <= 0 || configuration.Port > 65535)
            return false;

        if (string.IsNullOrWhiteSpace(configuration.Username))
            return false;

        if (GetInvalidEmails(configuration.BccEmail).Any())
            return false;

        if (GetInvalidEmails(configuration.CcEmail).Any())
            return false;

        return true;
    }

    /// <summary>
    /// Lee la contraseña desde Credential Manager. Si no está bajo la llave unificada,
    /// intenta migrar desde llaves antiguas (una sola vez) y la deja bajo la nueva.
    /// </summary>
    private string LoadPassword(string server, string username)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username))
            return string.Empty;

        var target = CredentialTarget(server, username);
        if (_credentialService.CredentialsExist(target))
            return _credentialService.GetCredentials(target).password;

        // Migración desde formatos antiguos.
        var legacyTargets = new[]
        {
            $"SMTP_{server}_{username}",
            "SMTP_smtppro.zoho.com_cartera@simicsgroup.com"
        };

        foreach (var legacy in legacyTargets)
        {
            if (!_credentialService.CredentialsExist(legacy))
                continue;

            var (legacyUser, legacyPassword) = _credentialService.GetCredentials(legacy);
            if (string.IsNullOrEmpty(legacyPassword))
                continue;

            _credentialService.SaveCredentials(target, string.IsNullOrEmpty(legacyUser) ? username : legacyUser, legacyPassword);
            _credentialService.DeleteCredentials(legacy);
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Credencial SMTP migrada: {Old} → {New}", legacy, target);
            return legacyPassword;
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetInvalidEmails(string? rawEmails)
    {
        if (string.IsNullOrWhiteSpace(rawEmails))
            return Enumerable.Empty<string>();

        return rawEmails
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e) && !IsValidEmail(e));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
