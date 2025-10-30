using System;

namespace GestLog.Services.Configuration
{
    /// <summary>
    /// Opciones de configuración para el servicio de email de reseteo de contraseña
    /// </summary>
    public class PasswordResetEmailOptions
    {
        public const string SectionName = "EmailServices:PasswordReset";

        /// <summary>
        /// Indica si el servicio de email de reseteo de contraseña está habilitado
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Servidor SMTP
        /// </summary>
        public string SmtpServer { get; set; } = "smtp.gmail.com";

        /// <summary>
        /// Puerto del servidor SMTP
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Usar SSL para la conexión SMTP
        /// </summary>
        public bool UseSSL { get; set; } = true;

        /// <summary>
        /// Email del remitente
        /// </summary>
        public string SenderEmail { get; set; } = "noreply@gestlog.local";

        /// <summary>
        /// Nombre del remitente
        /// </summary>
        public string SenderName { get; set; } = "GestLog - Sistema de Gestión Logística";

        /// <summary>
        /// Nombre de usuario para autenticarse en el servidor SMTP
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña para autenticarse en el servidor SMTP
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Indica si usar variables de entorno para credenciales
        /// </summary>
        public bool UseEnvironmentVariables { get; set; } = true;

        /// <summary>
        /// Prefijo de las variables de entorno
        /// </summary>
        public string EnvironmentVariablePrefix { get; set; } = "GESTLOG_PASSWORD_RESET_EMAIL_";

        /// <summary>
        /// Timeout para la conexión SMTP
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Número de reintentos en caso de fallo
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// Retraso en milisegundos entre reintentos
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;

        /// <summary>
        /// Valida que la configuración sea válida
        /// </summary>
        /// <returns>true si la configuración es válida, false en caso contrario</returns>
        public bool Validate()
        {
            if (!Enabled)
                return true;

            if (string.IsNullOrWhiteSpace(SmtpServer))
                return false;

            if (SmtpPort <= 0 || SmtpPort > 65535)
                return false;

            if (string.IsNullOrWhiteSpace(SenderEmail))
                return false;

            if (UseEnvironmentVariables)
            {
                // Si usa variables de entorno, validar que existan
                var usernameVar = Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}USERNAME");
                var passwordVar = Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}PASSWORD");
                return !string.IsNullOrWhiteSpace(usernameVar) && !string.IsNullOrWhiteSpace(passwordVar);
            }
            else
            {
                // Si no usa variables de entorno, validar que tenga credenciales
                return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
            }
        }

        /// <summary>
        /// Obtiene las credenciales (desde variables de entorno o propiedades)
        /// </summary>
        public (string username, string password) GetCredentials()
        {
            if (UseEnvironmentVariables)
            {
                var username = Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}USERNAME") ?? Username;
                var password = Environment.GetEnvironmentVariable($"{EnvironmentVariablePrefix}PASSWORD") ?? Password;
                return (username, password);
            }

            return (Username, Password);
        }
    }
}
