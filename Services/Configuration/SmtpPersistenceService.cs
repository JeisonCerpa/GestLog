using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;

namespace GestLog.Services.Configuration;

/// <summary>
/// Implementación de servicio unificado de persistencia SMTP con auditoría exhaustiva
/// 
/// Responsabilidades:
/// 1. Centralizar carga/guardado de configuración SMTP
/// 2. Validar configuración antes de persistencia
/// 3. Crear y mantener pista de auditoría en archivo JSONL
/// 4. Proporcionar logging exhaustivo en cada operación
/// 5. Sincronizar con ConfigurationService
/// 
/// Ubicación de archivos:
/// - Configuración: %APPDATA%\GestLog\app-config.json
/// - Auditoría: %APPDATA%\GestLog\Audits\smtp_audit.jsonl
/// </summary>
public class SmtpPersistenceService : ISmtpPersistenceService
{
    private readonly IGestLogLogger _logger;
    private readonly IConfigurationService _configurationService;
    private readonly string _auditFilePath;
    private readonly object _auditLock = new object();
    private SmtpSettings? _cachedConfiguration;

    public SmtpPersistenceService(IGestLogLogger logger, IConfigurationService configurationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

        // Configurar ruta del archivo de auditoría
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var auditDirectory = Path.Combine(appDataPath, "GestLog", "Audits");
        _auditFilePath = Path.Combine(auditDirectory, "smtp_audit.jsonl");

        _logger.LogInformation("🔧 SmtpPersistenceService inicializado");
        _logger.LogDebug("📁 Ruta de auditoría SMTP: {AuditPath}", _auditFilePath);
    }

    /// <summary>
    /// Carga la configuración SMTP desde el almacenamiento con logging exhaustivo
    /// </summary>
    public async Task<SmtpSettings?> LoadSmtpConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("📖 [SmtpPersistenceService] Iniciando carga de configuración SMTP");

                // Intentar cargar desde cache primero
                if (_cachedConfiguration != null)
                {
                    _logger.LogDebug("✅ [SmtpPersistenceService] Configuración encontrada en cache - Server: {Server}", 
                        _cachedConfiguration.Server);
                    LogAuditTrail("LOAD_FROM_CACHE", 
                        "Configuración cargada desde cache", 
                        null, 
                        _cachedConfiguration);
                    return _cachedConfiguration;
                }

                // Cargar desde ConfigurationService (JSON)
                var smtpConfig = _configurationService.Current?.Modules?.GestionCartera?.Smtp;

                if (smtpConfig == null)
                {
                    _logger.LogWarning("⚠️ [SmtpPersistenceService] No se encontró configuración SMTP en JSON");
                    LogAuditTrail("LOAD_NOT_FOUND", 
                        "No se encontró configuración SMTP en almacenamiento", 
                        null, 
                        null);
                    return null;
                }

                // Validar configuración cargada
                if (!ValidateConfiguration(smtpConfig))
                {
                    _logger.LogWarning("⚠️ [SmtpPersistenceService] Configuración SMTP cargada pero inválida - Server: {Server}", 
                        smtpConfig.Server);
                    LogAuditTrail("LOAD_INVALID", 
                        "Configuración cargada pero falló validación", 
                        null, 
                        smtpConfig);
                    return smtpConfig; // Retornar igualmente
                }

                // Cachear configuración válida
                _cachedConfiguration = smtpConfig;

                _logger.LogInformation("✅ [SmtpPersistenceService] Configuración SMTP cargada exitosamente");
                _logger.LogInformation("   📌 Servidor: {Server}:{Port}", smtpConfig.Server, smtpConfig.Port);
                _logger.LogInformation("   📧 Usuario: {Username}", smtpConfig.Username ?? "(vacío)");
                _logger.LogInformation("   📨 BCC: {BccEmail}", string.IsNullOrWhiteSpace(smtpConfig.BccEmail) ? "(vacío)" : smtpConfig.BccEmail);
                _logger.LogInformation("   📋 CC: {CcEmail}", string.IsNullOrWhiteSpace(smtpConfig.CcEmail) ? "(vacío)" : smtpConfig.CcEmail);
                _logger.LogInformation("   🔐 SSL: {UseSSL}", smtpConfig.UseSSL ? "✓" : "✗");

                LogAuditTrail("LOAD_SUCCESS", 
                    "Configuración SMTP cargada exitosamente desde JSON", 
                    null, 
                    smtpConfig);

                return smtpConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SmtpPersistenceService] Error cargando configuración SMTP");
                LogAuditTrail("LOAD_ERROR", 
                    $"Error: {ex.Message}", 
                    null, 
                    null);
                throw;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Guarda la configuración SMTP con auditoría exhaustiva
    /// </summary>
    public async Task<bool> SaveSmtpConfigurationAsync(SmtpSettings configuration, string operationSource = "Unknown", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("💾 [SmtpPersistenceService] Iniciando guardado de configuración SMTP desde {Source}", operationSource);

                // Validar antes de guardar
                if (!ValidateConfiguration(configuration))
                {
                    _logger.LogError(new InvalidOperationException(), "❌ [SmtpPersistenceService] Validación fallida - no se puede guardar configuración inválida");
                    LogAuditTrail("SAVE_VALIDATION_FAILED", 
                        "Validación fallida antes de guardar", 
                        _cachedConfiguration, 
                        configuration);
                    return false;
                }                // Obtener configuración anterior para auditoría
                var oldConfig = _cachedConfiguration ?? _configurationService.Current?.Modules?.GestionCartera?.Smtp;

                // Actualizar en ConfigurationService
                var smtpConfig = _configurationService.Current?.Modules?.GestionCartera?.Smtp;
                if (smtpConfig != null)
                {
                    smtpConfig.Server = configuration.Server;
                    smtpConfig.Port = configuration.Port;
                    smtpConfig.Username = configuration.Username;
                    smtpConfig.UseSSL = configuration.UseSSL;
                    smtpConfig.BccEmail = configuration.BccEmail ?? string.Empty;
                    smtpConfig.CcEmail = configuration.CcEmail ?? string.Empty;
                    smtpConfig.Timeout = configuration.Timeout;
                    smtpConfig.IsConfigured = true;
                }

                // Guardar en JSON a través de ConfigurationService
                _configurationService.SaveAsync().GetAwaiter().GetResult();

                // Actualizar cache
                _cachedConfiguration = configuration;

                _logger.LogInformation("✅ [SmtpPersistenceService] Configuración SMTP guardada exitosamente");
                _logger.LogInformation("   📌 Servidor: {Server}:{Port}", configuration.Server, configuration.Port);
                _logger.LogInformation("   📧 Usuario: {Username}", configuration.Username ?? "(vacío)");
                _logger.LogInformation("   📨 BCC: {BccEmail}", string.IsNullOrWhiteSpace(configuration.BccEmail) ? "(vacío)" : configuration.BccEmail);
                _logger.LogInformation("   📋 CC: {CcEmail}", string.IsNullOrWhiteSpace(configuration.CcEmail) ? "(vacío)" : configuration.CcEmail);
                _logger.LogInformation("   🔐 SSL: {UseSSL}", configuration.UseSSL ? "✓" : "✗");

                // Registrar cambios específicos en auditoría
                LogAuditTrail("SAVE_SUCCESS", 
                    "Configuración SMTP guardada exitosamente", 
                    oldConfig, 
                    configuration);

                // Detectar cambios específicos
                if (oldConfig != null)
                {
                    if (oldConfig.BccEmail != configuration.BccEmail)
                    {
                        _logger.LogInformation("📝 [Auditoría] BCC cambió: {OldBcc} → {NewBcc}", 
                            string.IsNullOrWhiteSpace(oldConfig.BccEmail) ? "(vacío)" : oldConfig.BccEmail,
                            string.IsNullOrWhiteSpace(configuration.BccEmail) ? "(vacío)" : configuration.BccEmail);
                        LogAuditTrail("FIELD_CHANGED", 
                            "BCC actualizado", 
                            new { Field = "BccEmail", OldValue = oldConfig.BccEmail }, 
                            new { Field = "BccEmail", NewValue = configuration.BccEmail });
                    }

                    if (oldConfig.CcEmail != configuration.CcEmail)
                    {
                        _logger.LogInformation("📝 [Auditoría] CC cambió: {OldCc} → {NewCc}", 
                            string.IsNullOrWhiteSpace(oldConfig.CcEmail) ? "(vacío)" : oldConfig.CcEmail,
                            string.IsNullOrWhiteSpace(configuration.CcEmail) ? "(vacío)" : configuration.CcEmail);
                        LogAuditTrail("FIELD_CHANGED", 
                            "CC actualizado", 
                            new { Field = "CcEmail", OldValue = oldConfig.CcEmail }, 
                            new { Field = "CcEmail", NewValue = configuration.CcEmail });
                    }

                    if (oldConfig.Server != configuration.Server)
                    {
                        _logger.LogInformation("📝 [Auditoría] Servidor SMTP cambió: {OldServer} → {NewServer}", 
                            oldConfig.Server, configuration.Server);
                        LogAuditTrail("FIELD_CHANGED", 
                            "Servidor SMTP actualizado", 
                            new { Field = "Server", OldValue = oldConfig.Server }, 
                            new { Field = "Server", NewValue = configuration.Server });
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SmtpPersistenceService] Error guardando configuración SMTP");
                LogAuditTrail("SAVE_ERROR", 
                    $"Error: {ex.Message}", 
                    _cachedConfiguration, 
                    configuration);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Obtiene la configuración SMTP actual (desde cache si disponible)
    /// </summary>
    public async Task<SmtpSettings?> GetCurrentConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogDebug("📖 [SmtpPersistenceService] Obteniendo configuración SMTP actual");

                if (_cachedConfiguration != null)
                {
                    _logger.LogDebug("✅ Configuración obtenida desde cache");
                    return _cachedConfiguration;
                }

                var config = _configurationService.Current?.Modules?.GestionCartera?.Smtp;
                if (config != null && ValidateConfiguration(config))
                {
                    _cachedConfiguration = config;
                    _logger.LogDebug("✅ Configuración obtenida desde JSON y cacheada");
                }

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error obteniendo configuración SMTP actual");
                return null;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Valida que la configuración SMTP sea válida
    /// </summary>
    public bool ValidateConfiguration(SmtpSettings? configuration)
    {
        if (configuration == null)
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: configuración es null");
            return false;
        }

        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(configuration.Server))
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: servidor SMTP vacío");
            return false;
        }

        if (configuration.Port <= 0 || configuration.Port > 65535)
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: puerto inválido {Port}", configuration.Port);
            return false;
        }

        if (string.IsNullOrWhiteSpace(configuration.Username))
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: usuario vacío");
            return false;
        }

        // Validar emails BCC y CC (permitir listas separadas por ';' o ',')
        var invalidBcc = GetInvalidEmails(configuration.BccEmail).ToList();
        if (invalidBcc.Any())
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: BCC email(s) inválido(s) - {InvalidBcc}",
                string.Join(", ", invalidBcc));
            return false;
        }

        var invalidCc = GetInvalidEmails(configuration.CcEmail).ToList();
        if (invalidCc.Any())
        {
            _logger.LogWarning("⚠️ [SmtpPersistenceService] Validación: CC email(s) inválido(s) - {InvalidCc}",
                string.Join(", ", invalidCc));
            return false;
        }

        _logger.LogDebug("✅ [SmtpPersistenceService] Validación exitosa");
        return true;
    }

    /// <summary>
    /// Obtiene el historial de auditoría
    /// </summary>
    public async Task<string[]> GetAuditTrailAsync(int maxEntries = 0)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(_auditFilePath))
                {
                    _logger.LogWarning("⚠️ [SmtpPersistenceService] Archivo de auditoría no existe: {Path}", _auditFilePath);
                    return Array.Empty<string>();
                }

                lock (_auditLock)
                {
                    var lines = File.ReadAllLines(_auditFilePath);
                    
                    if (maxEntries > 0 && lines.Length > maxEntries)
                    {
                        lines = lines.Skip(lines.Length - maxEntries).ToArray();
                    }

                    _logger.LogInformation("📊 [SmtpPersistenceService] Auditoría: {Count} entradas", lines.Length);
                    return lines;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error leyendo archivo de auditoría");
                return Array.Empty<string>();
            }
        });
    }

    /// <summary>
    /// Limpia la configuración SMTP con auditoría
    /// </summary>
    public async Task<bool> ClearConfigurationAsync(string operationSource = "Unknown", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                _logger.LogInformation("🗑️ [SmtpPersistenceService] Limpiando configuración SMTP desde {Source}", operationSource);

                var oldConfig = _cachedConfiguration ?? _configurationService.Current?.Modules?.GestionCartera?.Smtp;

                // Limpiar configuración
                var smtpConfig = _configurationService.Current?.Modules?.GestionCartera?.Smtp;
                if (smtpConfig != null)
                {
                    smtpConfig.Server = string.Empty;
                    smtpConfig.Port = 587;
                    smtpConfig.Username = string.Empty;
                    smtpConfig.BccEmail = string.Empty;
                    smtpConfig.CcEmail = string.Empty;
                    smtpConfig.IsConfigured = false;
                }

                // Guardar cambios
                _configurationService.SaveAsync().GetAwaiter().GetResult();
                _cachedConfiguration = null;

                _logger.LogInformation("✅ [SmtpPersistenceService] Configuración SMTP limpiada");

                LogAuditTrail("CLEAR_SUCCESS", 
                    "Configuración SMTP limpiada completamente", 
                    oldConfig, 
                    null);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [SmtpPersistenceService] Error limpiando configuración SMTP");
                LogAuditTrail("CLEAR_ERROR", 
                    $"Error: {ex.Message}", 
                    _cachedConfiguration, 
                    null);
                return false;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Registra una entrada en el archivo de auditoría JSONL
    /// </summary>
    private void LogAuditTrail(string operation, string message, object? oldValue, object? newValue)
    {
        try
        {
            lock (_auditLock)
            {
                // Crear directorio de auditoría si no existe
                var auditDirectory = Path.GetDirectoryName(_auditFilePath);
                if (!Directory.Exists(auditDirectory))
                {
                    Directory.CreateDirectory(auditDirectory!);
                }

                // Crear entrada de auditoría
                var auditEntry = new
                {
                    timestamp = DateTime.UtcNow.ToString("o"),
                    operation = operation,
                    message = message,
                    oldValue = oldValue,
                    newValue = newValue,
                    user = Environment.UserName,
                    machineName = Environment.MachineName,
                    processId = Environment.ProcessId
                };

                var jsonLine = JsonSerializer.Serialize(auditEntry, new JsonSerializerOptions { WriteIndented = false });
                
                // Apender línea al archivo JSONL
                File.AppendAllText(_auditFilePath, jsonLine + Environment.NewLine);

                _logger.LogDebug("📝 [Auditoría] {Operation}: {Message}", operation, message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error escribiendo en archivo de auditoría");
        }
    }

    /// <summary>
    /// Valida si una dirección de email es válida
    /// </summary>
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

    private static IEnumerable<string> ParseEmailList(string? rawEmails)
    {
        if (string.IsNullOrWhiteSpace(rawEmails))
            return Enumerable.Empty<string>();

        return rawEmails
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(email => email.Trim())
            .Where(email => !string.IsNullOrWhiteSpace(email));
    }

    private static IEnumerable<string> GetInvalidEmails(string? rawEmails)
    {
        return ParseEmailList(rawEmails)
            .Where(email => !IsValidEmail(email));
    }
}
