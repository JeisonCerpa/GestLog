using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Services.Configuration;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.Options;

namespace GestLog.Services.Core
{
    /// <summary>
    /// Implementación del servicio de email para reseteo de contraseña
    /// </summary>
    public class PasswordResetEmailService : IPasswordResetEmailService
    {
        private readonly PasswordResetEmailOptions _options;
        private readonly IGestLogLogger _logger;

        public PasswordResetEmailService(
            IOptions<PasswordResetEmailOptions> options,
            IGestLogLogger logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }        public bool ValidateConfiguration()
        {
            if (!_options.Validate())
            {
                _logger.LogWarning("Configuración de PasswordResetEmailService no válida");
                return false;
            }

            _logger.LogInformation("Configuración de PasswordResetEmailService validada correctamente");
            return true;
        }

        public async Task<bool> SendPasswordResetEmailAsync(
            string userEmail,
            string userName,
            string temporaryPassword,
            CancellationToken cancellationToken = default)
        {
            try
            {                if (!_options.Enabled)
                {
                    _logger.LogWarning("Servicio de email de reseteo de contraseña deshabilitado");
                    return false;
                }

                if (!ValidateConfiguration())
                {
                    _logger.LogWarning("Configuración de email de reseteo inválida");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.LogWarning("Email del usuario no válido para reseteo de contraseña");
                    return false;
                }

                var (username, password) = _options.GetCredentials();

                using (var smtpClient = new SmtpClient(_options.SmtpServer, _options.SmtpPort))
                {
                    smtpClient.EnableSsl = _options.UseSSL;
                    smtpClient.Timeout = (int)_options.Timeout.TotalMilliseconds;
                    smtpClient.Credentials = new NetworkCredential(username, password);                    using (var mailMessage = new MailMessage())
                    {
                        // Validar que el email del remitente sea válido
                        if (string.IsNullOrWhiteSpace(_options.SenderEmail))
                        {
                            _logger.LogWarning("Email del remitente no configurado correctamente");
                            return false;
                        }                        mailMessage.From = new MailAddress(_options.SenderEmail, _options.SenderName);
                        
                        // Validar que el email del usuario sea válido
                        if (string.IsNullOrWhiteSpace(userEmail))
                        {
                            _logger.LogWarning("Email del usuario no válido para enviar reseteo de contraseña");
                            return false;
                        }

                        try
                        {
                            mailMessage.To.Add(userEmail);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Formato de email inválido: {Email}", userEmail);
                            return false;
                        }

                        mailMessage.Subject = "GestLog - Reseteo de Contraseña";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Body = BuildEmailBody(userName, temporaryPassword);

                        _logger.LogInformation(
                            "Enviando email de reseteo de contraseña a usuario: {Username} ({Email})",
                            userName, userEmail);

                        await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                        _logger.LogInformation(
                            "Email de reseteo de contraseña enviado exitosamente a: {Email}",
                            userEmail);

                        return true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Envío de email de reseteo cancelado para usuario: {Username}", userName);
                return false;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex,
                    "Error SMTP al enviar email de reseteo de contraseña a {Email}",
                    userEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error inesperado al enviar email de reseteo de contraseña a {Email}",
                    userEmail);
                return false;
            }
        }

        /// <summary>
        /// Envía email específico para creación de nuevo usuario
        /// </summary>
        public async Task<bool> SendNewUserEmailAsync(
            string userEmail,
            string userName,
            string fullName,
            string temporaryPassword,
            string[] assignedRoles,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_options.Enabled)
                {
                    _logger.LogWarning("Servicio de email de nuevo usuario deshabilitado");
                    return false;
                }

                if (!ValidateConfiguration())
                {
                    _logger.LogWarning("Configuración de email de nuevo usuario inválida");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.LogWarning("Email del usuario no válido para nuevo usuario");
                    return false;
                }

                var (username, password) = _options.GetCredentials();

                using (var smtpClient = new SmtpClient(_options.SmtpServer, _options.SmtpPort))
                {
                    smtpClient.EnableSsl = _options.UseSSL;
                    smtpClient.Timeout = (int)_options.Timeout.TotalMilliseconds;
                    smtpClient.Credentials = new NetworkCredential(username, password);

                    using (var mailMessage = new MailMessage())
                    {
                        if (string.IsNullOrWhiteSpace(_options.SenderEmail))
                        {
                            _logger.LogWarning("Email del remitente no configurado correctamente");
                            return false;
                        }

                        mailMessage.From = new MailAddress(_options.SenderEmail, _options.SenderName);

                        try
                        {
                            mailMessage.To.Add(userEmail);
                        }
                        catch (FormatException ex)
                        {
                            _logger.LogWarning(ex, "Formato de email inválido: {Email}", userEmail);
                            return false;
                        }

                        mailMessage.Subject = "GestLog - Bienvenido al Sistema";
                        mailMessage.IsBodyHtml = true;
                        mailMessage.Body = BuildNewUserEmailBody(userName, fullName, temporaryPassword, assignedRoles);

                        _logger.LogInformation(
                            "Enviando email de bienvenida para nuevo usuario: {Username} ({Email})",
                            userName, userEmail);

                        await smtpClient.SendMailAsync(mailMessage, cancellationToken);

                        _logger.LogInformation(
                            "Email de bienvenida enviado exitosamente a: {Email}",
                            userEmail);

                        return true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Envío de email de nuevo usuario cancelado para: {Username}", userName);
                return false;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex,
                    "Error SMTP al enviar email de nuevo usuario a {Email}",
                    userEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error inesperado al enviar email de nuevo usuario a {Email}",
                    userEmail);
                return false;
            }
        }

        /// <summary>
        /// Construye el cuerpo HTML del email de reseteo de contraseña
        /// </summary>
        private string BuildEmailBody(string userName, string temporaryPassword)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine("        .container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine("        .header { background-color: #118938; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }");
            sb.AppendLine("        .content { background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }");
            sb.AppendLine("        .password-box { background-color: #FFF3CD; padding: 15px; border-left: 4px solid #FFC107; margin: 20px 0; }");
            sb.AppendLine("        .password-text { font-size: 18px; font-weight: bold; color: #000; font-family: monospace; }");
            sb.AppendLine("        .warning { background-color: #FFE5E5; padding: 10px; border-left: 4px solid #DC3545; margin: 15px 0; color: #721c24; }");
            sb.AppendLine("        .footer { text-align: center; font-size: 12px; color: #666; margin-top: 20px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class='container'>");
            sb.AppendLine("        <div class='header'>");
            sb.AppendLine("            <h1>🔐 GestLog - Reseteo de Contraseña</h1>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='content'>");
            sb.AppendLine($"            <p>Hola <strong>{userName}</strong>,</p>");
            sb.AppendLine("            <p>Se ha solicitado un reseteo de contraseña para tu cuenta en GestLog.</p>");
            sb.AppendLine("            <p><strong>Tu contraseña temporal es:</strong></p>");
            sb.AppendLine("            <div class='password-box'>");
            sb.AppendLine($"                <div class='password-text'>{temporaryPassword}</div>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <p><strong>Instrucciones:</strong></p>");
            sb.AppendLine("            <ol>");
            sb.AppendLine("                <li>Inicia sesión en GestLog con tu nombre de usuario y la contraseña temporal anterior</li>");
            sb.AppendLine("                <li>Se te pedirá que cambies tu contraseña inmediatamente</li>");
            sb.AppendLine("                <li>Crea una contraseña nueva y segura</li>");
            sb.AppendLine("            </ol>");
            sb.AppendLine("            <div class='warning'>");
            sb.AppendLine("                <strong>⚠️ Importante:</strong>");
            sb.AppendLine("                <ul>");
            sb.AppendLine("                    <li>No compartas esta contraseña temporal con nadie</li>");
            sb.AppendLine("                    <li>Esta contraseña solo funciona para una sesión</li>");
            sb.AppendLine("                    <li>Después de cambiar tu contraseña, deberá seguir usando la nueva</li>");
            sb.AppendLine("                </ul>");
            sb.AppendLine("            </div>");
            sb.AppendLine("            <p>Si no solicitaste este reseteo, ignora este email.</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='footer'>");
            sb.AppendLine("            <p>© 2025 GestLog - Sistema de Gestión Logística</p>");
            sb.AppendLine("            <p>Este es un email automático, por favor no responda</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Construye el cuerpo HTML del email de bienvenida para nuevo usuario
        /// </summary>
        private string BuildNewUserEmailBody(string userName, string fullName, string temporaryPassword, string[] assignedRoles)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='es'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <style>");
            sb.AppendLine("        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine("        .container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine("        .header { background: linear-gradient(135deg, #118938 0%, #37AB4E 100%); color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }");
            sb.AppendLine("        .content { background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-radius: 0 0 5px 5px; }");
            sb.AppendLine("        .credentials-box { background-color: #FFF3CD; padding: 15px; border-left: 4px solid #FFC107; margin: 20px 0; border-radius: 4px; }");
            sb.AppendLine("        .credential-row { margin: 10px 0; }");
            sb.AppendLine("        .credential-label { font-weight: bold; color: #555; font-size: 13px; }");
            sb.AppendLine("        .credential-value { font-size: 16px; font-weight: bold; color: #000; font-family: monospace; background-color: #FFFACD; padding: 8px; border-radius: 3px; margin-top: 4px; }");
            sb.AppendLine("        .roles-box { background-color: #E8F5E9; padding: 15px; border-left: 4px solid #4CAF50; margin: 15px 0; border-radius: 4px; }");
            sb.AppendLine("        .roles-list { list-style-type: none; padding-left: 0; }");
            sb.AppendLine("        .roles-list li { padding: 5px 0; color: #2E7D32; }");
            sb.AppendLine("        .roles-list li:before { content: '✓ '; margin-right: 8px; font-weight: bold; }");
            sb.AppendLine("        .important-box { background-color: #FFE5E5; padding: 15px; border-left: 4px solid #DC3545; margin: 15px 0; border-radius: 4px; }");
            sb.AppendLine("        .important-title { font-weight: bold; color: #721c24; margin-bottom: 10px; }");
            sb.AppendLine("        .important-list { list-style-type: none; padding-left: 0; color: #721c24; font-size: 13px; }");
            sb.AppendLine("        .important-list li { padding: 5px 0; }");
            sb.AppendLine("        .important-list li:before { content: '⚠️ '; margin-right: 8px; }");
            sb.AppendLine("        .instructions { background-color: #E3F2FD; padding: 15px; border-left: 4px solid #3B82F6; margin: 15px 0; border-radius: 4px; }");
            sb.AppendLine("        .instructions-title { font-weight: bold; color: #1565C0; margin-bottom: 10px; }");
            sb.AppendLine("        .instructions ol { margin: 10px 0; padding-left: 20px; }");
            sb.AppendLine("        .instructions li { margin: 8px 0; color: #1565C0; font-size: 13px; }");
            sb.AppendLine("        .footer { text-align: center; font-size: 12px; color: #666; margin-top: 20px; border-top: 1px solid #ddd; padding-top: 10px; }");
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <div class='container'>");
            sb.AppendLine("        <div class='header'>");
            sb.AppendLine("            <h1>🎉 ¡Bienvenido a GestLog!</h1>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='content'>");
            sb.AppendLine($"            <p>Hola <strong>{fullName}</strong>,</p>");
            sb.AppendLine("            <p>Se ha creado una nueva cuenta de usuario en <strong>GestLog - Sistema de Gestión Logística</strong>.</p>");
            sb.AppendLine("            <p><strong>Tus credenciales de acceso:</strong></p>");
            sb.AppendLine("            <div class='credentials-box'>");
            sb.AppendLine("                <div class='credential-row'>");
            sb.AppendLine("                    <div class='credential-label'>👤 Nombre de Usuario:</div>");
            sb.AppendLine($"                   <div class='credential-value'>{userName}</div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("                <div class='credential-row'>");
            sb.AppendLine("                    <div class='credential-label'>🔑 Contraseña Temporal:</div>");
            sb.AppendLine($"                   <div class='credential-value'>{temporaryPassword}</div>");
            sb.AppendLine("                </div>");
            sb.AppendLine("            </div>");

            // Mostrar roles asignados si existen
            if (assignedRoles != null && assignedRoles.Length > 0)
            {
                sb.AppendLine("            <div class='roles-box'>");
                sb.AppendLine("                <strong style='color: #2E7D32;'>✓ Roles Asignados:</strong>");
                sb.AppendLine("                <ul class='roles-list'>");
                foreach (var role in assignedRoles)
                {
                    sb.AppendLine($"                    <li>{role}</li>");
                }
                sb.AppendLine("                </ul>");
                sb.AppendLine("            </div>");
            }

            sb.AppendLine("            <div class='instructions'>");
            sb.AppendLine("                <div class='instructions-title'>📋 Primeros Pasos:</div>");
            sb.AppendLine("                <ol class='instructions'>");
            sb.AppendLine("                    <li>Accede a GestLog con tu nombre de usuario y contraseña temporal</li>");
            sb.AppendLine("                    <li>El sistema te pedirá que cambies tu contraseña inmediatamente</li>");
            sb.AppendLine("                    <li>Crea una contraseña nueva, segura y fácil de recordar</li>");
            sb.AppendLine("                    <li>¡Listo! Ya puedes usar todas las funciones de GestLog</li>");
            sb.AppendLine("                </ol>");
            sb.AppendLine("            </div>");

            sb.AppendLine("            <div class='important-box'>");
            sb.AppendLine("                <div class='important-title'>⚠️ Información Importante:</div>");
            sb.AppendLine("                <ul class='important-list'>");
            sb.AppendLine("                    <li>No compartas tu contraseña con nadie</li>");
            sb.AppendLine("                    <li>Esta contraseña temporal solo funciona para la primera sesión</li>");
            sb.AppendLine("                    <li>Después de cambiarla, deberás usar la nueva contraseña</li>");
            sb.AppendLine("                    <li>La contraseña temporal vence en 24 horas</li>");
            sb.AppendLine("                </ul>");
            sb.AppendLine("            </div>");

            sb.AppendLine("            <p style='color: #666; font-size: 12px;'>Si no esperabas recibir este email, contacta al administrador del sistema.</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("        <div class='footer'>");
            sb.AppendLine("            <p>© 2025 GestLog - Sistema de Gestión Logística</p>");
            sb.AppendLine("            <p>Este es un email automático, por favor no responda</p>");
            sb.AppendLine("        </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
