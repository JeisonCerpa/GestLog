using System;
using System.ComponentModel.DataAnnotations;

namespace GestLog.Modules.GestionCartera.Models
{
    /// <summary>
    /// Configuración SMTP para el envío de correos electrónicos
    /// </summary>
    public class SmtpConfiguration
    {
        /// <summary>
        /// Nombre descriptivo de la configuración
        /// </summary>
        public string Name { get; set; } = "Configuración SMTP";

        /// <summary>
        /// Servidor SMTP (HOST)
        /// </summary>
        [Required(ErrorMessage = "El servidor SMTP es requerido")]
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// Puerto del servidor SMTP
        /// </summary>
        [Range(1, 65535, ErrorMessage = "El puerto debe estar entre 1 y 65535")]
        public int Port { get; set; } = 587;

        /// <summary>
        /// Dirección de correo electrónico (usuario)
        /// </summary>
        [Required(ErrorMessage = "La dirección de correo es requerida")]
        [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña para autenticación (no se almacena en texto plano)
        /// </summary>
        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Indica si SSL está habilitado
        /// </summary>
        public bool EnableSsl { get; set; } = true;        /// <summary>
        /// Timeout en milisegundos para el envío
        /// </summary>
        [Range(1000, 300000, ErrorMessage = "El timeout debe estar entre 1000 y 300000 ms")]
        public int Timeout { get; set; } = 120000;

        /// <summary>
        /// Dirección de correo para copia oculta (BCC) en todos los envíos
        /// </summary>
        [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida para BCC")]
        public string? BccEmail { get; set; } = string.Empty;

        /// <summary>
        /// Dirección de correo para copia (CC) opcional
        /// </summary>
        [EmailAddress(ErrorMessage = "Ingrese una dirección de correo válida para CC")]
        public string? CcEmail { get; set; } = string.Empty;

        /// <summary>
        /// Indica si las credenciales están guardadas de forma segura
        /// </summary>
        public bool HasSavedCredentials { get; set; } = false;

        /// <summary>
        /// Fecha de última configuración exitosa
        /// </summary>
        public DateTime? LastConfigured { get; set; }

        /// <summary>
        /// Indica si la configuración está validada y lista para usar
        /// </summary>
        public bool IsValid { get; set; } = false;

        /// <summary>
        /// Mensaje de estado de la configuración
        /// </summary>
        public string StatusMessage { get; set; } = "No configurado";

        /// <summary>
        /// Target único para almacenar credenciales
        /// </summary>
        public string CredentialTarget => $"{SmtpServer}_{Port}_{Username}".Replace(".", "_").Replace("@", "_");

        /// <summary>
        /// Configuraciones predefinidas comunes
        /// </summary>
        public static class Presets
        {
            public static SmtpConfiguration Gmail => new()
            {
                Name = "Gmail",
                SmtpServer = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true
            };            public static SmtpConfiguration Zoho => new()
            {
                Name = "Zoho",
                SmtpServer = "smtppro.zoho.com",
                Port = 587,
                EnableSsl = true
            };

            public static SmtpConfiguration Office365 => new()
            {
                Name = "Office 365",
                SmtpServer = "smtp.office365.com",
                Port = 587,
                EnableSsl = true
            };            public static SmtpConfiguration[] All => new[]
            {
                Gmail,
                Zoho,
                Office365
            };
        }        /// <summary>
        /// Crea una copia de la configuración
        /// </summary>
        public SmtpConfiguration Clone()
        {
            return new SmtpConfiguration
            {
                Name = Name,
                SmtpServer = SmtpServer,
                Port = Port,
                Username = Username,
                Password = Password,
                EnableSsl = EnableSsl,
                Timeout = Timeout,
                BccEmail = BccEmail,
                CcEmail = CcEmail,
                HasSavedCredentials = HasSavedCredentials,
                LastConfigured = LastConfigured,
                IsValid = IsValid,
                StatusMessage = StatusMessage
            };
        }

        /// <summary>
        /// Valida la configuración básica (sin credenciales)
        /// </summary>
        public bool IsBasicConfigurationValid()
        {
            return !string.IsNullOrWhiteSpace(SmtpServer) &&
                   Port > 0 && Port <= 65535 &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   IsValidEmail(Username);
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
}
