using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Configuration;

namespace GestLog.Modules.Usuarios.Services
{
    /// <summary>
    /// Servicio de envío de correos para autenticación (cambios de contraseña y recuperación)
    /// Completamente independiente de otros servicios de email
    /// </summary>
    public class AuthenticationEmailService : IAuthenticationEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IGestLogLogger _logger;
        private readonly string? _smtpServer;
        private readonly int _smtpPort;
        private readonly bool _useSSL;
        private readonly string? _senderEmail;
        private readonly string? _senderName;
        private readonly string? _username;
        private readonly string? _password;
        private readonly int _timeout;
        private readonly int _retryCount;
        private readonly int _retryDelayMs;
        private readonly bool _isConfigured;

        public AuthenticationEmailService(IConfiguration configuration, IGestLogLogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Cargar configuración SMTP para autenticación
            var emailConfig = _configuration.GetSection("EmailServices:Authentication");
            var enabled = emailConfig.GetValue("Enabled", false);
            var useEnvironmentVariables = emailConfig.GetValue("UseEnvironmentVariables", true);

            if (!enabled)
            {
                _logger.LogWarning("Servicio de email de autenticación deshabilitado en configuración");
                _isConfigured = false;
                return;
            }

            _smtpServer = emailConfig.GetValue<string>("SmtpServer");
            _smtpPort = emailConfig.GetValue("SmtpPort", 587);
            _useSSL = emailConfig.GetValue("UseSSL", true);
            _senderEmail = emailConfig.GetValue<string>("SenderEmail");
            _senderName = emailConfig.GetValue<string>("SenderName") ?? "GestLog";

            // Manejar Timeout de forma segura (evitar pasar null a TimeSpan.Parse)
            var timeoutStr = emailConfig.GetValue<string>("Timeout");
            if (string.IsNullOrWhiteSpace(timeoutStr))
            {
                timeoutStr = "00:00:30"; // valor por defecto
            }
            if (!TimeSpan.TryParse(timeoutStr, out var timeoutSpan))
            {
                timeoutSpan = TimeSpan.FromSeconds(30);
            }
            _timeout = (int)timeoutSpan.TotalMilliseconds;

            _retryCount = emailConfig.GetValue("RetryCount", 3);
            _retryDelayMs = emailConfig.GetValue("RetryDelayMs", 1000);

            // Cargar credenciales desde variables de entorno si está habilitado
            if (useEnvironmentVariables)
            {
                var envPrefix = emailConfig.GetValue("EnvironmentVariablePrefix", "GESTLOG_AUTH_EMAIL_");
                _username = Environment.GetEnvironmentVariable($"{envPrefix}USERNAME") 
                    ?? emailConfig.GetValue<string>("Username");
                _password = Environment.GetEnvironmentVariable($"{envPrefix}PASSWORD") 
                    ?? emailConfig.GetValue<string>("Password");
            }
            else
            {
                _username = emailConfig.GetValue<string>("Username");
                _password = emailConfig.GetValue<string>("Password");
            }

            // Validar configuración
            _isConfigured = !string.IsNullOrEmpty(_smtpServer) && 
                           !string.IsNullOrEmpty(_senderEmail) && 
                           !string.IsNullOrEmpty(_username) && 
                           !string.IsNullOrEmpty(_password);

            if (_isConfigured)
            {
                // Evitar pasar null en el arreglo params del logger
                _logger.LogInformation("Servicio de email de autenticación configurado correctamente para {SmtpServer}:{SmtpPort}", 
                    _smtpServer ?? "<unset>", _smtpPort);
            }
            else
            {
                _logger.LogWarning("Servicio de email de autenticación NO está completamente configurado. Falta SMTP, email o credenciales.");
            }
        }

        public async Task<bool> SendTemporaryPasswordAsync(string userEmail, string userName, string temporaryPassword, CancellationToken cancellationToken = default)
        {
            // Validación de entrada para evitar pasar null al envío
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning("Intento de envío de contraseña temporal fallido: userEmail no especificado");
                return false;
            }

            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de envío de contraseña temporal pero el servicio no está configurado");
                return false;
            }

            try
            {
                _logger.LogInformation("Enviando contraseña temporal a {UserEmail}", userEmail);

                var subject = "GestLog - Contraseña Temporal";
                var body = GenerateTemporaryPasswordEmailBody(userName, temporaryPassword);

                return await SendEmailWithRetryAsync(userEmail, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar contraseña temporal a {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordChangeConfirmationAsync(string userEmail, string userName, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            // Validación de entrada para evitar pasar null al envío
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning("Intento de envío de confirmación de cambio fallido: userEmail no especificado");
                return false;
            }

            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de envío de confirmación pero el servicio no está configurado");
                return false;
            }

            try
            {
                _logger.LogInformation("Enviando confirmación de cambio de contraseña a {UserEmail}", userEmail);

                var subject = "GestLog - Contraseña Actualizada Correctamente";
                var body = GeneratePasswordChangeConfirmationEmailBody(userName, timestamp);

                return await SendEmailWithRetryAsync(userEmail, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar confirmación de cambio de contraseña a {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordRecoveryAlertAsync(string userEmail, string userName, DateTime timestamp, CancellationToken cancellationToken = default)
        {
            // Validación de entrada para evitar pasar null al envío
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _logger.LogWarning("Intento de envío de alerta de recuperación fallido: userEmail no especificado");
                return false;
            }

            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de envío de alerta pero el servicio no está configurado");
                return false;
            }

            try
            {
                _logger.LogInformation("Enviando alerta de recuperación de contraseña a {UserEmail}", userEmail);

                var subject = "GestLog - Solicitud de Recuperación de Contraseña";
                var body = GeneratePasswordRecoveryAlertEmailBody(userName, timestamp);

                return await SendEmailWithRetryAsync(userEmail, subject, body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar alerta de recuperación a {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConfigured)
                return false;

            try
            {
                // Validar sender antes de crear MailMessage
                if (string.IsNullOrWhiteSpace(_senderEmail))
                {
                    _logger.LogWarning("No se puede comprobar servicio SMTP: SenderEmail no está configurado");
                    return false;
                }

                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = _useSSL;
                    client.Timeout = _timeout;
                    client.Credentials = new NetworkCredential(_username, _password);

                    // Intento de conexión sin enviar email
                    var testMessage = new MailMessage();
                    try
                    {
                        testMessage.From = new MailAddress(_senderEmail);
                        testMessage.To.Add(_senderEmail);
                        testMessage.Subject = "Test";
                        testMessage.Body = "Test";

                        await client.SendMailAsync(testMessage, cancellationToken);
                    }
                    finally
                    {
                        testMessage.Dispose();
                    }
                }

                _logger.LogInformation("Servicio de email de autenticación disponible");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Servicio de email de autenticación NO disponible");
                return false;
            }
        }

        #region Private Methods

        private async Task<bool> SendEmailWithRetryAsync(string recipientEmail, string subject, string body, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogWarning("Intento de enviar email fallido: recipientEmail no especificado");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_senderEmail))
            {
                _logger.LogWarning("Intento de enviar email fallido: sender email no configurado");
                return false;
            }

            int attempt = 0;
            while (attempt < _retryCount)
            {
                try
                {
                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.EnableSsl = _useSSL;
                        client.Timeout = _timeout;
                        client.Credentials = new NetworkCredential(_username, _password);

                        var mailMessage = new MailMessage();
                        try
                        {
                            mailMessage.From = new MailAddress(_senderEmail, _senderName ?? string.Empty);
                            mailMessage.Subject = subject;
                            mailMessage.Body = body;
                            mailMessage.IsBodyHtml = true;

                            // Añadir destinatario de forma segura
                            mailMessage.To.Add(new MailAddress(recipientEmail));

                            await client.SendMailAsync(mailMessage, cancellationToken);
                            _logger.LogInformation("Email enviado exitosamente a {RecipientEmail}", recipientEmail);
                            return true;
                        }
                        finally
                        {
                            mailMessage.Dispose();
                        }
                    }
                }
                catch (Exception ex) when (attempt < _retryCount - 1)
                {
                    attempt++;
                    _logger.LogWarning(ex, "Error al enviar email a {RecipientEmail}. Intento {Attempt}/{RetryCount}. Reintentando en {DelayMs}ms", 
                        recipientEmail, attempt, _retryCount, _retryDelayMs);
                    await Task.Delay(_retryDelayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error final al enviar email a {RecipientEmail} después de {RetryCount} intentos", 
                        recipientEmail, _retryCount);
                    return false;
                }
            }

            return false;
        }

        private string GenerateTemporaryPasswordEmailBody(string userName, string temporaryPassword)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ color: #118938; border-bottom: 3px solid #118938; padding-bottom: 10px; margin-bottom: 20px; }}
        .content {{ color: #333; line-height: 1.6; }}
        .password-box {{ background-color: #f9f9f9; border: 2px solid #118938; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .password {{ font-size: 18px; font-weight: bold; color: #118938; font-family: monospace; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 15px 0; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>GestLog - Contraseña Temporal</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{userName}</strong>,</p>
            <p>Se ha solicitado el restablecimiento de tu contraseña en GestLog. A continuación se muestra tu contraseña temporal:</p>
            <div class='password-box'>
                <p>Tu contraseña temporal es:</p>
                <p class='password'>{temporaryPassword}</p>
            </div>
            <div class='warning'>
                <strong>⚠️ Importante:</strong>
                <ul>
                    <li>Esta contraseña es temporal y solo es válida para esta sesión</li>
                    <li>Al iniciar sesión, DEBERÁS cambiar tu contraseña por una permanente</li>
                    <li>Por seguridad, esta contraseña expirará en 1 hora</li>
                </ul>
            </div>
            <p>Si no solicitaste este cambio, por favor contacta al administrador del sistema inmediatamente.</p>
        </div>
        <div class='footer'>
            <p>© 2025 GestLog - Sistema de Gestión Logística</p>
            <p>Este es un correo automatizado. Por favor no respondas a este mensaje.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordChangeConfirmationEmailBody(string userName, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ color: #118938; border-bottom: 3px solid #118938; padding-bottom: 10px; margin-bottom: 20px; }}
        .success {{ background-color: #d4edda; border-left: 4px solid #28a745; padding: 10px; margin: 15px 0; }}
        .content {{ color: #333; line-height: 1.6; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✓ Contraseña Actualizada Correctamente</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{userName}</strong>,</p>
            <div class='success'>
                <strong>✓ Tu contraseña ha sido cambiada exitosamente</strong>
            </div>
            <p><strong>Fecha y hora del cambio:</strong> {timestamp:dd/MM/yyyy HH:mm:ss}</p>
            <p>Ahora puedes acceder a GestLog con tu nueva contraseña.</p>
            <p>Si este cambio no fue realizado por ti, por favor contacta al administrador del sistema inmediatamente.</p>
        </div>
        <div class='footer'>
            <p>© 2025 GestLog - Sistema de Gestión Logística</p>
            <p>Este es un correo automatizado. Por favor no respondas a este mensaje.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GeneratePasswordRecoveryAlertEmailBody(string userName, DateTime timestamp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f5f5f5; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ color: #118938; border-bottom: 3px solid #118938; padding-bottom: 10px; margin-bottom: 20px; }}
        .info {{ background-color: #e7f3ff; border-left: 4px solid #2196F3; padding: 10px; margin: 15px 0; }}
        .content {{ color: #333; line-height: 1.6; }}
        .footer {{ color: #666; font-size: 12px; margin-top: 20px; padding-top: 10px; border-top: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Solicitud de Recuperación de Contraseña</h1>
        </div>
        <div class='content'>
            <p>Hola <strong>{userName}</strong>,</p>
            <p>Se ha iniciado un proceso de recuperación de contraseña en tu cuenta de GestLog.</p>
            <div class='info'>
                <p><strong>📅 Fecha y hora de la solicitud:</strong> {timestamp:dd/MM/yyyy HH:mm:ss}</p>
            </div>
            <p>Se te ha enviado una contraseña temporal para que puedas acceder y cambiar tu contraseña.</p>
            <p>Si no solicitaste este cambio o tienes dudas, por favor contacta al administrador del sistema.</p>
        </div>
        <div class='footer'>
            <p>© 2025 GestLog - Sistema de Gestión Logística</p>
            <p>Este es un correo automatizado. Por favor no respondas a este mensaje.</p>
        </div>
    </div>
</body>
</html>";
        }

        #endregion
    }

    // Usar esta interfaz en la implementación del servicio
    public interface IAuthenticationEmailService
    {
        Task<bool> SendTemporaryPasswordAsync(string userEmail, string userName, string temporaryPassword, CancellationToken cancellationToken = default);
        Task<bool> SendPasswordChangeConfirmationAsync(string userEmail, string userName, DateTime timestamp, CancellationToken cancellationToken = default);
        Task<bool> SendPasswordRecoveryAlertAsync(string userEmail, string userName, DateTime timestamp, CancellationToken cancellationToken = default);
        Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken = default);
    }
}
