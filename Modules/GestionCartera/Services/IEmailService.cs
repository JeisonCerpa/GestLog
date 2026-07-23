using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GestLog.Modules.GestionCartera.Models;

namespace GestLog.Modules.GestionCartera.Services
{
    /// <summary>
    /// Interfaz para el servicio de envío de correos electrónicos
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Configura el servicio SMTP
        /// </summary>
        /// <param name="configuration">Configuración SMTP</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        Task ConfigureSmtpAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un correo electrónico con un archivo adjunto
        /// </summary>
        /// <param name="emailInfo">Información del correo</param>
        /// <param name="attachmentPath">Ruta del archivo adjunto</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del envío</returns>
        Task<EmailResult> SendEmailWithAttachmentAsync(EmailInfo emailInfo, string attachmentPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un correo electrónico con múltiples archivos adjuntos
        /// </summary>
        /// <param name="emailInfo">Información del correo</param>
        /// <param name="attachmentPaths">Lista de rutas de archivos adjuntos</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del envío</returns>
        Task<EmailResult> SendEmailWithAttachmentsAsync(EmailInfo emailInfo, List<string> attachmentPaths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un correo de prueba
        /// </summary>
        /// <param name="recipient">Destinatario del correo de prueba</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del envío</returns>
        Task<EmailResult> SendTestEmailAsync(string recipient, CancellationToken cancellationToken = default);

        /// <summary>
        /// Envía un correo de prueba con copia oculta
        /// </summary>
        /// <param name="recipient">Destinatario principal</param>
        /// <param name="bccRecipient">Destinatario de copia oculta</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado del envío</returns>
        Task<EmailResult> SendTestEmailWithBccAsync(string recipient, string? bccRecipient = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Valida la configuración SMTP actual
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>True si la configuración es válida</returns>
        Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Prueba la conexión SMTP realmente (handshake + autenticación) SIN enviar correo.
        /// Detecta servidor/puerto/SSL incorrectos y usuario/contraseña inválidos.
        /// </summary>
        /// <param name="configuration">Configuración a probar</param>
        /// <param name="cancellationToken">Token de cancelación</param>
        /// <returns>Resultado con mensaje claro para el usuario</returns>
        Task<EmailResult> TestConnectionAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtiene la plantilla HTML para correos profesionales
        /// </summary>
        /// <param name="textContent">Contenido del texto a incluir</param>
        /// <returns>HTML formateado con firma de SIMICS GROUP</returns>
        string GetEmailHtmlTemplate(string textContent);

        /// <summary>
        /// Información de configuración actual (solo lectura)
        /// </summary>
        SmtpConfiguration? CurrentConfiguration { get; }

        /// <summary>
        /// Indica si el servicio está configurado y listo para usar
        /// </summary>
        bool IsConfigured { get; }
    }
}
