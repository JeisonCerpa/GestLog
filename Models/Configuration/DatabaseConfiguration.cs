namespace GestLog.Models.Configuration;

/// <summary>
/// Configuración de conexión a base de datos
/// </summary>
public class DatabaseConfiguration
{
    /// <summary>
    /// Cadena de conexión principal
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Servidor de base de datos
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// Base de datos
    /// </summary>
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Usuario (opcional si usa autenticación integrada)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Contraseña (opcional si usa autenticación integrada)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Timeout de conexión en segundos
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Timeout de comando en segundos
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Usar autenticación integrada de Windows
    /// </summary>
    public bool UseIntegratedSecurity { get; set; } = false;

    /// <summary>
    /// Habilitar SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Confiar en el certificado del servidor
    /// </summary>
    public bool TrustServerCertificate { get; set; } = false;
}
