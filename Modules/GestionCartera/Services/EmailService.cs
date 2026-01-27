using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionCartera.Exceptions;
using GestLog.Modules.GestionCartera.Models;
using GestLog.Services;
using GestLog.Services.Core.Logging;
using GestLog.Services.Core.Security;

namespace GestLog.Modules.GestionCartera.Services
{
    /// <summary>
    /// Servicio para envío de correos electrónicos con funcionalidad completa
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IGestLogLogger _logger;
        private readonly ICredentialService _credentialService;
        private SmtpConfiguration? _smtpConfiguration;
        private readonly object _configurationLock = new object();

        public EmailService(IGestLogLogger logger, ICredentialService credentialService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _credentialService = credentialService ?? throw new ArgumentNullException(nameof(credentialService));
        }

        public SmtpConfiguration? CurrentConfiguration 
        { 
            get 
            { 
                lock (_configurationLock)
                {
                    return _smtpConfiguration;
                }
            }
        }

        public bool IsConfigured 
        { 
            get 
            { 
                lock (_configurationLock)
                {
                    return _smtpConfiguration != null;
                }
            }
        }

        public async Task ConfigureSmtpAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Configurando servicio SMTP...");

                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                // Validar configuración
                await ValidateSmtpConfigurationAsync(configuration, cancellationToken);

                lock (_configurationLock)
                {
                    _smtpConfiguration = configuration;
                }

                _logger.LogInformation($"Servicio SMTP configurado correctamente - Servidor: {configuration.SmtpServer}, Puerto: {configuration.Port}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar servicio SMTP");
                throw new SmtpConfigurationException("No se pudo configurar el servicio SMTP", ex);
            }
        }

        public async Task<EmailResult> SendEmailWithAttachmentAsync(EmailInfo emailInfo, string attachmentPath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Enviando correo con adjunto a: {string.Join(", ", emailInfo.Recipients)}");

                EnsureConfigured();
                ValidateEmailInfo(emailInfo);

                if (string.IsNullOrWhiteSpace(attachmentPath))
                    throw new AttachmentException("La ruta del archivo adjunto no puede estar vacía");

                if (!File.Exists(attachmentPath))
                    throw new AttachmentException($"El archivo adjunto no existe: {attachmentPath}", attachmentPath);

                var fileInfo = new FileInfo(attachmentPath);
                var fileSizeKb = fileInfo.Length / 1024;

                _logger.LogInformation($"Archivo adjunto: {Path.GetFileName(attachmentPath)}, tamaño: {fileSizeKb} KB");

                using var message = CreateMailMessage(emailInfo);
                
                // Agregar adjunto
                using var attachment = new Attachment(attachmentPath);
                message.Attachments.Add(attachment);

                using var client = CreateSmtpClient();
                
                _logger.LogInformation("Enviando mensaje...");
                await client.SendMailAsync(message);
                
                var result = EmailResult.Success(
                    "Correo enviado exitosamente", 
                    message.To.Count + message.CC.Count + message.Bcc.Count,
                    fileSizeKb);

                _logger.LogInformation($"Correo enviado con éxito a {result.ProcessedRecipients} destinatarios");
                return result;
            }            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy || ex.StatusCode == SmtpStatusCode.TransactionFailed)
            {
                var errorMessage = "El servidor de correo está ocupado. Intente nuevamente más tarde.";
                _logger.LogError(ex, errorMessage);
                return EmailResult.Error(errorMessage, ex.Message);
            }
            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.InsufficientStorage)
            {
                var errorMessage = "El buzón del destinatario está lleno.";
                _logger.LogError(ex, errorMessage);
                return EmailResult.Error(errorMessage, ex.Message);
            }
            catch (AttachmentException ex)
            {
                _logger.LogError(ex, $"Error con archivo adjunto: {ex.FilePath}");
                return EmailResult.Error(ex.Message, ex.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar correo con adjunto");
                return EmailResult.Error("Error inesperado al enviar correo", ex.Message);
            }
        }

        public async Task<EmailResult> SendEmailWithAttachmentsAsync(EmailInfo emailInfo, List<string> attachmentPaths, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Enviando correo con múltiples adjuntos a: {string.Join(", ", emailInfo.Recipients)}");

                EnsureConfigured();
                ValidateEmailInfo(emailInfo);

                if (attachmentPaths == null || !attachmentPaths.Any())
                    throw new AttachmentException("Debe especificar al menos un archivo adjunto");

                long totalSizeKb = 0;
                var validAttachments = new List<string>();

                // Validar todos los archivos adjuntos
                foreach (var path in attachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        var fileInfo = new FileInfo(path);
                        totalSizeKb += fileInfo.Length / 1024;
                        validAttachments.Add(path);
                    }
                    else
                    {
                        _logger.LogWarning($"Archivo adjunto no encontrado: {path}");
                    }
                }

                if (!validAttachments.Any())
                    throw new AttachmentException("Ninguno de los archivos adjuntos existe");

                _logger.LogInformation($"Archivos adjuntos válidos: {validAttachments.Count}, tamaño total: {totalSizeKb} KB");

                using var message = CreateMailMessage(emailInfo);
                
                // Agregar adjuntos
                foreach (var path in validAttachments)
                {
                    var attachment = new Attachment(path);
                    message.Attachments.Add(attachment);
                }

                using var client = CreateSmtpClient();
                
                _logger.LogInformation("Enviando mensaje con múltiples adjuntos...");
                await client.SendMailAsync(message);
                
                var result = EmailResult.Success(
                    $"Correo enviado exitosamente con {validAttachments.Count} adjuntos", 
                    message.To.Count + message.CC.Count + message.Bcc.Count,
                    totalSizeKb);

                _logger.LogInformation($"Correo enviado con éxito a {result.ProcessedRecipients} destinatarios");
                return result;
            }            catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MailboxBusy || ex.StatusCode == SmtpStatusCode.TransactionFailed)
            {
                var errorMessage = "El servidor de correo está ocupado. Intente nuevamente más tarde.";
                _logger.LogError(ex, errorMessage);
                return EmailResult.Error(errorMessage, ex.Message);
            }
            catch (AttachmentException ex)
            {
                _logger.LogError(ex, "Error con archivos adjuntos");
                return EmailResult.Error(ex.Message, ex.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al enviar correo con múltiples adjuntos");
                return EmailResult.Error("Error inesperado al enviar correo", ex.Message);
            }
        }

        public async Task<EmailResult> SendTestEmailAsync(string recipient, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Enviando correo de prueba a: {recipient}");

                EnsureConfigured();

                if (string.IsNullOrWhiteSpace(recipient))
                    throw new RecipientException("El destinatario no puede estar vacío");

                var emailInfo = new EmailInfo
                {
                    Recipients = new List<string> { recipient },
                    Subject = "Correo de prueba SIMICS - Estado de Cartera",
                    Body = "Este es un correo de prueba enviado desde la aplicación de estado de cartera de SIMICS GROUP S.A.S.",
                    IsBodyHtml = false
                };

                using var message = CreateMailMessage(emailInfo);
                using var client = CreateSmtpClient();
                
                await client.SendMailAsync(message);
                
                var result = EmailResult.Success("Correo de prueba enviado exitosamente", 1);
                _logger.LogInformation("Correo de prueba enviado con éxito");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de prueba");
                return EmailResult.Error("Error al enviar correo de prueba", ex.Message);
            }
        }

        public async Task<EmailResult> SendTestEmailWithBccAsync(string recipient, string? bccRecipient = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var logMessage = $"Enviando correo de prueba a: {recipient}";
                if (!string.IsNullOrWhiteSpace(bccRecipient))
                    logMessage += $" con copia oculta a: {bccRecipient}";

                _logger.LogInformation(logMessage);

                EnsureConfigured();

                if (string.IsNullOrWhiteSpace(recipient))
                    throw new RecipientException("El destinatario no puede estar vacío");

                var emailInfo = new EmailInfo
                {
                    Recipients = new List<string> { recipient },
                    Subject = "Correo de prueba SIMICS - Estado de Cartera",
                    Body = "Este es un correo de prueba enviado desde la aplicación de estado de cartera de SIMICS GROUP S.A.S.",
                    IsBodyHtml = false,
                    BccRecipient = bccRecipient
                };

                using var message = CreateMailMessage(emailInfo);
                using var client = CreateSmtpClient();
                
                await client.SendMailAsync(message);
                
                var recipientCount = 1 + (string.IsNullOrWhiteSpace(bccRecipient) ? 0 : 1);
                var result = EmailResult.Success("Correo de prueba enviado exitosamente", recipientCount);
                _logger.LogInformation("Correo de prueba con BCC enviado con éxito");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de prueba con BCC");
                return EmailResult.Error("Error al enviar correo de prueba", ex.Message);
            }
        }        public async Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureConfigured();
                
                _logger.LogInformation("Validando configuración SMTP...");
                
                // Crear cliente y probar conexión
                using var client = CreateSmtpClient();
                
                // En .NET, no hay un método directo para probar conexión sin enviar correo
                // Pero podemos validar que la configuración sea consistente
                var config = CurrentConfiguration!;
                
                if (string.IsNullOrWhiteSpace(config.SmtpServer))
                    return false;
                    
                if (config.Port <= 0 || config.Port > 65535)
                    return false;
                    
                if (string.IsNullOrWhiteSpace(config.Username))
                    return false;
                    
                if (string.IsNullOrWhiteSpace(config.Password))
                    return false;

                _logger.LogInformation("Configuración SMTP validada correctamente");
                
                // Completar la tarea de forma asíncrona
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar configuración SMTP");
                return false;
            }
        }

        public string GetEmailHtmlTemplate(string textContent)
        {
            // Plantilla HTML idéntica a la implementación original
            string htmlTemplate = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Estado de Cartera</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        {textContent}
        <br/>
        <hr style='border: none; height: 1px; background-color: #ddd; margin: 20px 0;'>
        
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='vertical-align: top; padding-right: 15px;'>
                    <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_Icono2021Firma.png' alt='Logo' style='width: 60px;'>
                </td>
                <td>
                    <h3 style='margin: 0; font-size: 16px;'>JUAN MANUEL CUERVO PINILLA</h3>
                    <p style='margin: 0; font-weight: 500; font-size: 14px;'>Gerente Financiero</p>
                    <p style='margin: 5px 0 0; font-size: 12px;'>
                        <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image002.png' style='width: 12px; vertical-align: middle;'> 
                        <a href='tel:+573163114545' style='color: #333; text-decoration: none;'>+57-3163114545</a>
                    </p>
                    <p style='margin: 3px 0; font-size: 12px;'>
                        <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image003.png' style='width: 12px; vertical-align: middle;'> 
                        <a href='mailto:juan.cuervo@simicsgroup.com' style='color: #333; text-decoration: none;'>juan.cuervo@simicsgroup.com</a>
                    </p>
                    <p style='margin: 3px 0; font-size: 12px;'>
                        <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image004.png' style='width: 12px; vertical-align: middle;'> 
                        CR 53 No. 96-24 Oficina 3D, Barranquilla, Colombia
                    </p>
                </td>
            </tr>
            <tr>
                <td colspan='2' style='text-align: center; padding-top: 20px;'>
                    <img src='http://simicsgroup.com/wp-content/uploads/2023/08/Logo-v6_2021-1Firma.png' style='width: 180px;'><br>
                    <a href='https://www.simicsgroup.com/' style='color: #333; text-decoration: none; font-size: 12px;'>www.simicsgroup.com</a>
                    <div style='margin-top: 10px;'>
                        <a href='https://www.linkedin.com/company/simicsgroupsas' style='margin: 0 5px;'>
                            <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image006.png' alt='LinkedIn' style='width: 24px;'>
                        </a>
                        <a href='https://www.instagram.com/simicsgroupsas/' style='margin: 0 5px;'>
                            <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image007.png' alt='Instagram' style='width: 24px;'>
                        </a>
                        <a href='https://www.facebook.com/SIMICSGroupSAS/' style='margin: 0 5px;'>
                            <img src='http://simicsgroup.com/wp-content/uploads/2023/08/image008.png' alt='Facebook' style='width: 24px;'>
                        </a>
                    </div>
                </td>
            </tr>
        </table>
    </div>
</body>
</html>";

            return htmlTemplate;
        }

        #region Private Methods

        private void EnsureConfigured()
        {
            if (!IsConfigured)
                throw new SmtpConfigurationException("El servicio SMTP no está configurado. Llame ConfigureSmtpAsync primero.");
        }        private async Task ValidateSmtpConfigurationAsync(SmtpConfiguration configuration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.SmtpServer))
                throw new SmtpConfigurationException("El servidor SMTP es requerido");

            if (configuration.Port <= 0 || configuration.Port > 65535)
                throw new SmtpConfigurationException("El puerto debe estar entre 1 y 65535");

            if (string.IsNullOrWhiteSpace(configuration.Username))
                throw new SmtpConfigurationException("El nombre de usuario es requerido");

            if (string.IsNullOrWhiteSpace(configuration.Password))
            {
                _logger.LogWarning("⚠️ Configuración SMTP incompleta: UseAuthentication=true pero Password está vacío para usuario '{Username}'", 
                    configuration.Username);
                throw new SmtpConfigurationException("La contraseña es requerida para la autenticación SMTP");
            }

            // Validar formato de email
            try
            {
                var emailAddress = new System.Net.Mail.MailAddress(configuration.Username);
                if (emailAddress.Address != configuration.Username)
                {
                    throw new SmtpConfigurationException($"El formato del email '{configuration.Username}' no es válido");
                }
            }
            catch (Exception ex) when (!(ex is SmtpConfigurationException))
            {
                throw new SmtpConfigurationException($"El formato del email '{configuration.Username}' no es válido");
            }

            _logger.LogDebug("✅ Validación SMTP completada - Server: {Server}, User: {User}", 
                configuration.SmtpServer, configuration.Username);

            await Task.CompletedTask; // Para mantener la signatura async
        }

        private void ValidateEmailInfo(EmailInfo emailInfo)
        {
            if (emailInfo == null)
                throw new ArgumentNullException(nameof(emailInfo));

            if (emailInfo.Recipients == null || !emailInfo.Recipients.Any())
                throw new RecipientException("Debe especificar al menos un destinatario");

            if (string.IsNullOrWhiteSpace(emailInfo.Subject))
                throw new ArgumentException("El asunto es requerido");

            if (string.IsNullOrWhiteSpace(emailInfo.Body))
                throw new ArgumentException("El cuerpo del mensaje es requerido");

            // Validar direcciones de email
            foreach (var recipient in emailInfo.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                if (!IsValidEmail(recipient))
                    throw new RecipientException($"Dirección de email inválida: {recipient}", recipient);
            }

            if (!string.IsNullOrWhiteSpace(emailInfo.CcRecipient) && !IsValidEmail(emailInfo.CcRecipient))
                throw new RecipientException($"Dirección CC inválida: {emailInfo.CcRecipient}", emailInfo.CcRecipient);

            if (!string.IsNullOrWhiteSpace(emailInfo.BccRecipient) && !IsValidEmail(emailInfo.BccRecipient))
                throw new RecipientException($"Dirección BCC inválida: {emailInfo.BccRecipient}", emailInfo.BccRecipient);
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }        private MailMessage CreateMailMessage(EmailInfo emailInfo)
        {
            var config = CurrentConfiguration!;
            var message = new MailMessage
            {
                From = new MailAddress(config.Username),
                Subject = emailInfo.Subject,
                Body = emailInfo.Body,
                IsBodyHtml = emailInfo.IsBodyHtml
            };

            // Agregar destinatarios
            foreach (var recipient in emailInfo.Recipients.Where(r => !string.IsNullOrWhiteSpace(r)))
            {
                message.To.Add(recipient);
            }

            // Agregar CC desde EmailInfo (opcional, específico por correo)
            if (!string.IsNullOrWhiteSpace(emailInfo.CcRecipient))
                message.CC.Add(emailInfo.CcRecipient);

            // Agregar BCC desde EmailInfo (opcional, específico por correo)
            if (!string.IsNullOrWhiteSpace(emailInfo.BccRecipient))
                message.Bcc.Add(emailInfo.BccRecipient);

            // ✅ NUEVO: Agregar BCC y CC automáticamente desde la configuración SMTP
            if (!string.IsNullOrWhiteSpace(config.BccEmail))
            {
                // Solo agregar si no está ya incluido
                bool alreadyInBcc = message.Bcc.Cast<MailAddress>().Any(addr => 
                    addr.Address.Equals(config.BccEmail, StringComparison.OrdinalIgnoreCase));
                    
                if (!alreadyInBcc)
                {
                    message.Bcc.Add(config.BccEmail);
                    _logger.LogInformation("📧 BCC automático agregado desde configuración SMTP: {BccEmail}", config.BccEmail);
                }
                else
                {
                    _logger.LogDebug("BCC ya está incluido en la lista: {BccEmail}", config.BccEmail);
                }
            }
            else
            {
                _logger.LogDebug("⚠️ BccEmail vacío en configuración SMTP - No se agregará BCC automático");
            }

            if (!string.IsNullOrWhiteSpace(config.CcEmail))
            {
                // Solo agregar si no está ya incluido
                bool alreadyInCc = message.CC.Cast<MailAddress>().Any(addr => 
                    addr.Address.Equals(config.CcEmail, StringComparison.OrdinalIgnoreCase));
                    
                if (!alreadyInCc)
                {
                    message.CC.Add(config.CcEmail);
                    _logger.LogInformation("📧 CC automático agregado desde configuración SMTP: {CcEmail}", config.CcEmail);
                }
                else
                {
                    _logger.LogDebug("CC ya está incluido en la lista: {CcEmail}", config.CcEmail);
                }
            }
            else
            {
                _logger.LogDebug("⚠️ CcEmail vacío en configuración SMTP - No se agregará CC automático");
            }

            // Logging de resumen del mensaje
            _logger.LogInformation("📨 Mensaje de correo creado - De: {From}, Para: {To}, CC: {Cc}, BCC: {Bcc}, Asunto: {Subject}",
                message.From?.Address ?? "DESCONOCIDO",
                string.Join(",", message.To.Select(x => x.Address)),
                message.CC.Count > 0 ? string.Join(",", message.CC.Select(x => x.Address)) : "(ninguno)",
                message.Bcc.Count > 0 ? string.Join(",", message.Bcc.Select(x => x.Address)) : "(ninguno)",
                emailInfo.Subject);

            return message;
        }

                private SmtpClient CreateSmtpClient()
        {
            var config = CurrentConfiguration!;
            // Si la contraseña está vacía, intentar recuperarla del Credential Manager
            if (string.IsNullOrWhiteSpace(config.Password) && !string.IsNullOrWhiteSpace(config.Username))
            {
                var credentialTarget = $"GestionCartera_SMTP_{config.SmtpServer}_{config.Username}";
                var credentials = _credentialService.GetCredentials(credentialTarget);
                if (!string.IsNullOrEmpty(credentials.password))
                {
                    config.Password = credentials.password;
                }
            }            return new SmtpClient(config.SmtpServer)
            {
                Port = config.Port,
                Credentials = new NetworkCredential(config.Username, config.Password),
                EnableSsl = config.EnableSsl,
                Timeout = config.Timeout
            };
        }

        #endregion
    }
}
