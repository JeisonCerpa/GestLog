using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using GestLog.Modules.EnvioCatalogo.Models;
using GestLog.Modules.EnvioCatalogo.Exceptions;
using GestLog.Modules.GestionCartera.Models; // Para reutilizar SmtpConfiguration
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.EnvioCatalogo.Services
{
    /// <summary>
    /// Servicio para envío de catálogo por email - Configuración SMTP independiente
    /// </summary>
    public class EnvioCatalogoService : IEnvioCatalogoService
    {
        private readonly IGestLogLogger _logger;
        private SmtpConfiguration? _smtpConfiguration;
        private readonly object _configurationLock = new object();

        // Configuración específica para el módulo de envío de catálogo
        private const string CONFIG_KEY_PREFIX = "EnvioCatalogo_SMTP_";

        public EnvioCatalogoService(IGestLogLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Configura el SMTP específico para este módulo (independiente de Gestión de Cartera)
        /// </summary>
        public async Task ConfigureSmtpAsync(SmtpConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("📧 Configurando SMTP para Envío de Catálogo...");

                if (configuration == null)
                    throw new ArgumentNullException(nameof(configuration));

                await ValidateSmtpConfigurationAsync(configuration, cancellationToken);

                lock (_configurationLock)
                {
                    _smtpConfiguration = configuration;
                }

                _logger.LogInformation("✅ SMTP configurado para Envío de Catálogo - Servidor: {Server}, Puerto: {Port}", 
                    configuration.SmtpServer, configuration.Port);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al configurar SMTP para Envío de Catálogo");
                throw new CatalogoSmtpConfigurationException("No se pudo configurar el servicio SMTP para Envío de Catálogo", ex);
            }
        }        public async Task<List<string>> ReadEmailsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("📊 Leyendo emails desde Excel: {FilePath}", excelFilePath);
                    
                    // Validar archivo
                    if (!File.Exists(excelFilePath))
                        throw new CatalogoFileException("El archivo Excel no existe", excelFilePath);

                    if (!Path.GetExtension(excelFilePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                        throw new CatalogoExcelException("El archivo debe ser un Excel (.xlsx)", excelFilePath);

                    var emails = new List<string>();

                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheets.First();

                    var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    
                    // Leer desde fila 2 (fila 1 son encabezados)
                    // Columna A: NOMBRE, B: NIT, C: CORREO
                    for (int row = 2; row <= rowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var emailValue = worksheet.Cell(row, 3).GetValue<string>(); // Columna C = Correo
                        
                        if (!string.IsNullOrWhiteSpace(emailValue) && IsValidEmail(emailValue.Trim()))
                        {
                            emails.Add(emailValue.Trim());
                        }
                    }

                    _logger.LogInformation("✅ Leídos {Count} emails válidos desde Excel", emails.Count);
                    return emails.Distinct().ToList(); // Eliminar duplicados
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error leyendo emails desde Excel");
                    throw;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Lee información completa del cliente desde Excel (Nombre, NIT, Email)
        /// </summary>
        public async Task<List<CatalogoClientInfo>> ReadClientInfoFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("📊 Leyendo información de clientes desde Excel: {FilePath}", excelFilePath);
                    
                    // Validar archivo
                    if (!File.Exists(excelFilePath))
                        throw new CatalogoFileException("El archivo Excel no existe", excelFilePath);

                    if (!Path.GetExtension(excelFilePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                        throw new CatalogoExcelException("El archivo debe ser un Excel (.xlsx)", excelFilePath);

                    var clients = new List<CatalogoClientInfo>();

                    using var workbook = new XLWorkbook(excelFilePath);
                    var worksheet = workbook.Worksheets.First();

                    var rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;
                    
                    // Leer desde fila 2 (fila 1 son encabezados)
                    // Columna A: NOMBRE, B: NIT, C: CORREO
                    for (int row = 2; row <= rowCount; row++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var nombre = worksheet.Cell(row, 1).GetValue<string>()?.Trim() ?? string.Empty; // Columna A
                        var nit = worksheet.Cell(row, 2).GetValue<string>()?.Trim() ?? string.Empty;    // Columna B
                        var email = worksheet.Cell(row, 3).GetValue<string>()?.Trim() ?? string.Empty;  // Columna C
                        
                        // Solo agregar si tiene email válido
                        if (!string.IsNullOrWhiteSpace(email) && IsValidEmail(email))
                        {
                            clients.Add(new CatalogoClientInfo
                            {
                                Nombre = !string.IsNullOrWhiteSpace(nombre) ? nombre : "Estimados Señores",
                                NIT = nit,
                                Email = email
                            });
                        }
                    }

                    _logger.LogInformation("✅ Leídos {Count} clientes válidos desde Excel", clients.Count);
                    return clients;
                }                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error leyendo información de clientes desde Excel");
                    throw;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Alias de compatibilidad para ReadClientInfoFromExcelAsync
        /// </summary>
        public async Task<IEnumerable<CatalogoClientInfo>> ReadClientsFromExcelAsync(string excelFilePath, CancellationToken cancellationToken = default)
        {
            var clients = await ReadClientInfoFromExcelAsync(excelFilePath, cancellationToken);
            return clients;
        }

        /// <summary>
        /// Envía un catálogo por email a un destinatario específico
        /// </summary>
        public async Task SendCatalogoEmailAsync(CatalogoEmailInfo emailInfo, CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureConfigured();
                ValidateCatalogoEmailInfo(emailInfo);

                if (emailInfo.Recipients == null || !emailInfo.Recipients.Any())
                {
                    throw new ArgumentException("No se han especificado destinatarios para el envío", nameof(emailInfo));
                }

                _logger.LogInformation("📧 Enviando catálogo por email a {Count} destinatarios", emailInfo.Recipients.Count);

                // Si hay múltiples destinatarios, usar el método múltiple
                if (emailInfo.Recipients.Count > 1)
                {
                    await SendCatalogoToMultipleRecipientsAsync(emailInfo, null, cancellationToken);
                }
                else
                {
                    // Un solo destinatario
                    var recipient = emailInfo.Recipients.First();
                    await SendSingleCatalogoEmailAsync(emailInfo, recipient, cancellationToken);
                    _logger.LogInformation("✅ Catálogo enviado exitosamente a: {Recipient}", recipient);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando catálogo por email");
                throw;
            }
        }

        public async Task<CatalogoSendResult> SendCatalogoToMultipleRecipientsAsync(
            CatalogoEmailInfo emailInfo,
            IProgress<CatalogoProgressInfo>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                EnsureConfigured();
                ValidateCatalogoEmailInfo(emailInfo);

                _logger.LogInformation("🚀 Iniciando envío de catálogo a {Count} destinatarios", emailInfo.Recipients.Count);

                var progressInfo = new CatalogoProgressInfo
                {
                    TotalEmails = emailInfo.Recipients.Count,
                    StatusMessage = "Iniciando envío de catálogo..."
                };

                progress?.Report(progressInfo);

                var successfulSends = 0;
                var failedRecipients = new List<string>();

                for (int i = 0; i < emailInfo.Recipients.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var recipient = emailInfo.Recipients[i];
                    
                    progressInfo.ProcessedEmails = i;
                    progressInfo.CurrentRecipient = recipient;
                    progressInfo.StatusMessage = $"Enviando a {recipient}...";
                    progress?.Report(progressInfo);

                    try
                    {
                        await SendSingleCatalogoEmailAsync(emailInfo, recipient, cancellationToken);
                        successfulSends++;
                        progressInfo.SuccessfulSends = successfulSends;

                        _logger.LogInformation("✅ Catálogo enviado exitosamente a: {Recipient}", recipient);

                        // Pequeña pausa entre envíos para evitar spam
                        await Task.Delay(500, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error enviando catálogo a: {Recipient}", recipient);
                        failedRecipients.Add(recipient);
                        progressInfo.FailedSends = failedRecipients.Count;
                    }

                    progressInfo.ProcessedEmails = i + 1;
                    progress?.Report(progressInfo);
                }

                stopwatch.Stop();

                progressInfo.StatusMessage = "Envío completado";
                progress?.Report(progressInfo);

                var result = CatalogoSendResult.Success(emailInfo.Recipients.Count, successfulSends, stopwatch.Elapsed);
                result.FailedRecipients = failedRecipients;

                _logger.LogInformation("🎯 Envío de catálogo completado: {Successful}/{Total} exitosos en {Duration}",
                    successfulSends, emailInfo.Recipients.Count, stopwatch.Elapsed);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "❌ Error durante envío masivo de catálogo");
                return CatalogoSendResult.Error($"Error durante envío: {ex.Message}", emailInfo.Recipients?.Count ?? 0);
            }
        }        public async Task<bool> SendTestEmailAsync(string recipient, CancellationToken cancellationToken = default)
        {
            try
            {
                EnsureConfigured();
                if (!IsValidEmail(recipient))
                    throw new CatalogoEmailSendException($"Email inválido: {recipient}", recipient);

                _logger.LogInformation("🧪 Enviando email de prueba a: {Recipient}", recipient);

                var config = GetCurrentConfiguration();
                using var client = CreateSmtpClient();

                var message = new MailMessage
                {
                    From = new MailAddress(config.Username),
                    Subject = "Prueba de Configuración SMTP - Envío de Catálogo",
                    Body = GetTestEmailHtmlTemplate(),
                    IsBodyHtml = true
                };

                message.To.Add(recipient);                // Embebir logo de la empresa en email de prueba si existe
                var logoPath = GetCompanyLogoPath();
                if (!string.IsNullOrEmpty(logoPath))
                {
                    var logoAttachment = new Attachment(logoPath);
                    logoAttachment.ContentId = "company-logo";
                    if (logoAttachment.ContentDisposition != null)
                    {
                        logoAttachment.ContentDisposition.Inline = true;
                        logoAttachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                    }
                    message.Attachments.Add(logoAttachment);
                    _logger.LogInformation("✅ Logo embebido en email de prueba");
                }

                // Embebir icono para viñetas en email de prueba si existe
                var iconPath = GetBulletIconPath();
                if (!string.IsNullOrEmpty(iconPath))
                {
                    var iconAttachment = new Attachment(iconPath);
                    iconAttachment.ContentId = "bullet-icon";
                    if (iconAttachment.ContentDisposition != null)
                    {
                        iconAttachment.ContentDisposition.Inline = true;
                        iconAttachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                    }
                    message.Attachments.Add(iconAttachment);
                }

                await client.SendMailAsync(message);

                _logger.LogInformation("✅ Email de prueba enviado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error enviando email de prueba");
                return false;
            }
        }

        public bool ValidateCatalogFile(string catalogFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catalogFilePath))
                    return false;

                if (!File.Exists(catalogFilePath))
                    return false;

                var extension = Path.GetExtension(catalogFilePath);
                return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }        public string GetDefaultCatalogPath()
        {
            var dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            return Path.Combine(dataPath, "Catalogo Productos y Servicios Simics Group SAS.pdf");
        }        /// <summary>
        /// Obtiene la ruta del logo de la empresa desde la carpeta Assets
        /// </summary>
        private string? GetCompanyLogoPath()
        {
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            var logoPath = Path.Combine(assetsPath, "Simics.png");
            
            if (!File.Exists(logoPath))
            {
                _logger.LogWarning("⚠️ Logo no encontrado en: {LogoPath}", logoPath);
                return null;
            }
            
            return logoPath;
        }

        /// <summary>
        /// Obtiene la ruta del icono para viñetas desde la carpeta Assets
        /// </summary>
        private string? GetBulletIconPath()
        {
            var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            var iconPath = Path.Combine(assetsPath, "image001.ico");
            
            if (!File.Exists(iconPath))
            {
                _logger.LogWarning("⚠️ Icono de viñeta no encontrado en: {IconPath}", iconPath);
                return null;
            }
            
            return iconPath;
        }

        #region Métodos Privados

        private void EnsureConfigured()
        {            lock (_configurationLock)
            {
                if (_smtpConfiguration == null)
                    throw new CatalogoSmtpConfigurationException("El servicio SMTP no está configurado para Envío de Catálogo. Configure primero el SMTP.");
            }
        }

        private SmtpConfiguration GetCurrentConfiguration()
        {            lock (_configurationLock)
            {
                return _smtpConfiguration ?? throw new CatalogoSmtpConfigurationException("SMTP no configurado");
            }
        }        private async Task ValidateSmtpConfigurationAsync(SmtpConfiguration configuration, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.SmtpServer))
                throw new CatalogoSmtpConfigurationException("El servidor SMTP es requerido");

            if (configuration.Port <= 0 || configuration.Port > 65535)
                throw new CatalogoSmtpConfigurationException("El puerto debe estar entre 1 y 65535");

            if (string.IsNullOrWhiteSpace(configuration.Username))
                throw new CatalogoSmtpConfigurationException("El nombre de usuario es requerido");

            if (string.IsNullOrWhiteSpace(configuration.Password))
                throw new CatalogoSmtpConfigurationException("La contraseña es requerida");

            await Task.CompletedTask;
        }

        private void ValidateCatalogoEmailInfo(CatalogoEmailInfo emailInfo)
        {
            if (emailInfo == null)
                throw new ArgumentNullException(nameof(emailInfo));            if (emailInfo.Recipients == null || !emailInfo.Recipients.Any())
                throw new CatalogoEmailSendException("Debe especificar al menos un destinatario", "");

            foreach (var recipient in emailInfo.Recipients)
            {
                if (!IsValidEmail(recipient))
                    throw new CatalogoEmailSendException($"Dirección de email inválida: {recipient}", recipient);
            }

            if (!ValidateCatalogFile(emailInfo.CatalogFilePath))
                throw new CatalogoFileException("El archivo del catálogo no es válido o no existe", emailInfo.CatalogFilePath);
        }        private async Task SendSingleCatalogoEmailAsync(CatalogoEmailInfo emailInfo, string recipient, CancellationToken cancellationToken)
        {
            var config = GetCurrentConfiguration();
            using var client = CreateSmtpClient();

            // Generar el cuerpo personalizado del email
            var personalizedBody = GeneratePersonalizedEmailBody(emailInfo.CompanyName ?? "Estimados Señores");

            var message = new MailMessage
            {
                From = new MailAddress(config.Username),
                Subject = emailInfo.Subject,
                Body = personalizedBody,
                IsBodyHtml = true
            };

            message.To.Add(recipient);

            // Agregar copia oculta si está configurada
            if (!string.IsNullOrWhiteSpace(emailInfo.BccRecipient))
                message.Bcc.Add(emailInfo.BccRecipient);

            if (!string.IsNullOrWhiteSpace(emailInfo.CcRecipient))
                message.CC.Add(emailInfo.CcRecipient);            // Embebir logo de la empresa si existe
            var logoPath = GetCompanyLogoPath();
            if (!string.IsNullOrEmpty(logoPath))
            {
                var logoAttachment = new Attachment(logoPath);
                logoAttachment.ContentId = "company-logo";
                if (logoAttachment.ContentDisposition != null)
                {
                    logoAttachment.ContentDisposition.Inline = true;
                    logoAttachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                }
                message.Attachments.Add(logoAttachment);
                _logger.LogInformation("✅ Logo embebido en el email con ContentId: company-logo");
            }
            else
            {
                _logger.LogWarning("⚠️ No se pudo embebir el logo - archivo no encontrado");
            }

            // Embebir icono para viñetas si existe
            var iconPath = GetBulletIconPath();
            if (!string.IsNullOrEmpty(iconPath))
            {
                var iconAttachment = new Attachment(iconPath);
                iconAttachment.ContentId = "bullet-icon";
                if (iconAttachment.ContentDisposition != null)
                {
                    iconAttachment.ContentDisposition.Inline = true;
                    iconAttachment.ContentDisposition.DispositionType = System.Net.Mime.DispositionTypeNames.Inline;
                }
                message.Attachments.Add(iconAttachment);
                _logger.LogInformation("✅ Icono de viñeta embebido con ContentId: bullet-icon");
            }

            // Adjuntar catálogo
            var attachment = new Attachment(emailInfo.CatalogFilePath);
            attachment.Name = "Catalogo_Productos_Servicios_SIMICS_GROUP.pdf";
            message.Attachments.Add(attachment);

            await client.SendMailAsync(message);
        }

        private SmtpClient CreateSmtpClient()
        {
            var config = GetCurrentConfiguration();
            return new System.Net.Mail.SmtpClient(config.SmtpServer)
            {
                Port = config.Port,
                Credentials = new System.Net.NetworkCredential(config.Username, config.Password),
                EnableSsl = config.EnableSsl,
                Timeout = config.Timeout
            };
        }

        private int FindEmailColumn(IXLWorksheet worksheet)
        {
            // Buscar en la primera fila columnas que contengan "email", "correo", "mail"
            var firstRow = worksheet.Row(1);
            var lastColumn = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 1;

            for (int col = 1; col <= lastColumn; col++)
            {
                var cellValue = firstRow.Cell(col).GetValue<string>().ToUpperInvariant();
                
                if (cellValue.Contains("EMAIL") || cellValue.Contains("CORREO") || cellValue.Contains("MAIL"))
                {
                    return col;
                }
            }

            return -1; // No encontrado
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
        }        private string GetTestEmailHtmlTemplate()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Prueba SMTP - Envío de Catálogo</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table cellpadding='0' cellspacing='0' border='0' width='100%' style='background-color: #f5f5f5; padding: 20px 0;'>
        <tr>
            <td align='center'>
                <table cellpadding='0' cellspacing='0' border='0' width='100%' style='max-width: 600px; background-color: white; border-radius: 8px;'>                    <!-- Header with Logo -->
                    <tr>                        <td style='background-color: white; color: #2c3e50; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; border-bottom: 4px solid #118938;'>
                            <img src='cid:company-logo' alt='SIMICS GROUP S.A.S.' style='max-width: 200px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto;' />
                            <p style='margin: 10px 0 0 0; font-size: 14px; font-family: Arial, sans-serif; color: #8e8e8e; font-weight: normal;'>
                                Sistema GestLog - Módulo de Envío de Catálogo
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 30px;'>
                            <h2 style='color: #2c3e50; margin: 0 0 20px 0; font-family: Arial, sans-serif;'>PRUEBA DE CONFIGURACIÓN SMTP</h2>
                            
                            <p style='color: #34495e; line-height: 1.6; margin: 0 0 15px 0; font-family: Arial, sans-serif;'>
                                Este es un email de prueba del módulo <strong>Envío de Catálogo</strong> de GestLog.
                            </p>
                            
                            <p style='color: #34495e; line-height: 1.6; margin: 0 0 20px 0; font-family: Arial, sans-serif;'>
                                Si recibe este mensaje, significa que la configuración SMTP está funcionando correctamente.
                            </p>
                            
                            <table cellpadding='0' cellspacing='0' border='0' width='100%' style='background-color: #27ae60; border-radius: 5px; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 15px; color: white; font-family: Arial, sans-serif;'>
                                        <strong>✓ Configuración SMTP validada exitosamente</strong>
                                    </td>
                                </tr>
                            </table>
                            
                            <hr style='border: none; border-top: 1px solid #ecf0f1; margin: 20px 0;'>
                            
                            <p style='color: #7f8c8d; font-size: 12px; text-align: center; margin: 0; font-family: Arial, sans-serif;'>
                                <strong>SIMICS GROUP SAS</strong><br>
                                Sistema GestLog - Módulo de Envío de Catálogo
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }/// <summary>
        /// Genera el cuerpo personalizado del email con la plantilla comercial (versión compatible)
        /// </summary>
        private string GeneratePersonalizedEmailBody(string clientName)
        {
            var template = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Importadores y Comercializadores de Aceros y Servicios - Simics Group SAS</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f8f9fa;'>
    <table cellpadding='0' cellspacing='0' border='0' width='100%' style='background-color: #f8f9fa; padding: 20px 0;'>
        <tr>
            <td align='center'>
                <table cellpadding='0' cellspacing='0' border='0' width='100%' style='max-width: 800px; background-color: white; border-radius: 8px;'>                    <!-- Header -->
                    <tr>                        <td style='background-color: white; color: #2c3e50; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; border-bottom: 4px solid #118938;'>
                            <img src='cid:company-logo' alt='SIMICS GROUP S.A.S.' style='max-width: 280px; height: auto; margin-bottom: 15px; display: block; margin-left: auto; margin-right: auto;' />
                            <p style='margin: 10px 0 0 0; font-size: 16px; font-family: Arial, sans-serif; color: #8e8e8e; font-weight: normal;'>
                                Importadores y Comercializadores de Aceros y Servicios
                            </p>
                        </td>
                    </tr>
                    
                    <!-- Content -->
                    <tr>
                        <td style='padding: 40px;'>
                            <p style='font-size: 16px; color: #2c3e50; margin: 0 0 20px 0; line-height: 1.6; font-family: Arial, sans-serif;'>
                                <strong>Buenos días Señores {CLIENT_NAME}</strong>
                            </p>
                            
                            <p style='font-size: 14px; color: #34495e; margin: 0 0 20px 0; line-height: 1.6; font-family: Arial, sans-serif;'>
                                Nos dirigimos a ustedes para presentarles <strong>SIMICS GROUP S.A.S.</strong>, una empresa dedicada a la importación y comercialización de todo tipo de aceros. Contamos con personal técnico y profesional con más de <strong>40 años de experiencia</strong> en el sector, lo que nos permite ofrecer soluciones que se adaptan a las necesidades específicas de cada cliente.
                            </p>
                            
                            <p style='font-size: 14px; color: #34495e; margin: 0 0 25px 0; line-height: 1.6; font-family: Arial, sans-serif;'>
                                Hemos participado en proyectos representando a las siderúrgicas más importantes de China, Japón, Turquía entre otros países. Nuestra empresa se destaca por la calidad de los materiales y por brindar una atención rápida y oportuna a nuestros clientes, no solo en el suministro de materiales sino también por el acompañamiento técnico que brindamos en cada proyecto.
                            </p>
                              <!-- Productos Section -->
                            <table cellpadding='0' cellspacing='0' border='0' width='100%' style='margin: 25px 0; border: 1px solid #e0e0e0; border-radius: 6px;'>
                                <tr>
                                    <td style='background-color: #f8f9fa; padding: 15px; border-bottom: 1px solid #e0e0e0;'>                                        <h3 style='margin: 0; font-size: 18px; color: #118938; font-weight: bold; font-family: Arial, sans-serif;'>
                                            <img src='cid:bullet-icon' alt='•' style='width: 16px; height: 16px; margin-right: 8px; vertical-align: middle;' />
                                            PRODUCTOS
                                        </h3>
                                    </td>
                                </tr>                                <tr>
                                    <td style='padding: 20px;'>
                                        <table cellpadding='0' cellspacing='0' border='0' width='100%'>                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Láminas:</strong> A-36, A-283, A-131, A-572, A-516 GR 70</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Lámina Antidesgaste</strong> 400-450HB</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Lámina Inoxidable</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Perfilería:</strong> Ángulos, Canales UPN, Vigas H, I, HEA, HEB, IPE, WF</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Duraluminios</strong> en barra o platina</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Redondos:</strong> SAE 4140, 4340, 1045, 1020</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Barras perforadas</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Aceros especiales</strong> (Importamos según la necesidad del cliente)</td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Materiales de ferretería,</strong> soldaduras, repuestos para mantenimientos de plantas industriales</td></tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                              <!-- Servicios Section -->
                            <table cellpadding='0' cellspacing='0' border='0' width='100%' style='margin: 25px 0; border: 1px solid #e0e0e0; border-radius: 6px;'>
                                <tr>
                                    <td style='background-color: #f8f9fa; padding: 15px; border-bottom: 1px solid #e0e0e0;'>                                        <h3 style='margin: 0; font-size: 18px; color: #118938; font-weight: bold; font-family: Arial, sans-serif;'>
                                            <img src='cid:bullet-icon' alt='•' style='width: 16px; height: 16px; margin-right: 8px; vertical-align: middle;' />
                                            SERVICIOS
                                        </h3>
                                    </td>
                                </tr>                                <tr>
                                    <td style='padding: 20px;'>
                                        <table cellpadding='0' cellspacing='0' border='0' width='100%'>                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Oxicorte</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Corte por láser</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Corte por plasma</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Soldadura</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Sandblasting</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Pintura</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Torneado, fresado, taladrado</strong></td></tr>
                                            <tr><td style='font-size: 14px; color: #34495e; line-height: 1.7; font-family: Arial, sans-serif; padding: 3px 0;'>• <strong>Doblez, biselado, rolado</strong></td></tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            
                            <p style='font-size: 14px; color: #34495e; margin: 25px 0; line-height: 1.6; font-family: Arial, sans-serif;'>
                                Adjuntamos nuestro catálogo de productos y servicios, si le interesa conocer más detalles sobre nuestra empresa o programar una reunión, no dude en responder a este correo o llamarnos al <strong>+57 XXXXXXX</strong>. Queremos hacernos visibles para ustedes y que encuentren en nosotros un apoyo para cada una de sus operaciones.
                            </p>
                            
                            <p style='font-size: 14px; color: #2c3e50; margin: 25px 0; line-height: 1.6; font-weight: bold; font-family: Arial, sans-serif;'>
                                Gracias por su atención. Esperamos tener la oportunidad de colaborar con ustedes.
                            </p>                              <!-- Contact Info -->
                            <table cellpadding='0' cellspacing='0' border='0' width='100%' style='margin: 30px 0; background-color: #b8b8b8; border-radius: 6px;'>
                                <tr>
                                    <td style='padding: 25px; color: white;'>                                        <h3 style='margin: 0 0 15px 0; font-size: 18px; color: white; font-family: Arial, sans-serif;'>
                                            DATOS DE CONTACTO
                                        </h3>
                                        
                                        <table cellpadding='0' cellspacing='0' border='0' width='100%'>
                                            <tr>                                                <td style='font-size: 14px; line-height: 1.6; padding: 4px 0; font-family: Arial, sans-serif; color: white;'>
                                                    <strong>Email:</strong> <a href='mailto:contactenos@simicsgroup.com' style='color: white; text-decoration: none;'>contactenos@simicsgroup.com</a>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='font-size: 14px; line-height: 1.6; padding: 4px 0; font-family: Arial, sans-serif; color: white;'>
                                                    <strong>Celular:</strong> 315 224 05 20
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style='font-size: 14px; line-height: 1.6; padding: 4px 0; font-family: Arial, sans-serif; color: white;'>
                                                    <strong>Teléfono fijo:</strong> 605 329 55 05
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #ecf0f1; padding: 20px; text-align: center; border-radius: 0 0 8px 8px;'>
                            <p style='margin: 0; font-size: 12px; color: #7f8c8d; font-family: Arial, sans-serif;'>
                                <strong>SIMICS GROUP S.A.S.</strong> - Más de 40 años de experiencia en el sector<br>
                                Este mensaje fue enviado desde nuestro sistema automatizado de comunicaciones comerciales.
                            </p>
                        </td>
                    </tr>
                    
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            return template.Replace("{CLIENT_NAME}", clientName);
        }

        #endregion
    }
}
