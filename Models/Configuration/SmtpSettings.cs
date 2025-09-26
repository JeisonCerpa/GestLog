using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuración del servidor SMTP para envío de emails
/// </summary>
public class SmtpSettings : INotifyPropertyChanged
{
    private string _server = string.Empty;
    private int _port = 587;
    private bool _useSSL = true;    
    private bool _useAuthentication = true;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _fromEmail = string.Empty;
    private string _fromName = string.Empty;    
    private int _timeout = 30000;
    private bool _isConfigured = false;
    private string _bccEmail = string.Empty;
    private string _ccEmail = string.Empty;
    private static bool _suspendValidation = false;

    /// <summary>
    /// Servidor SMTP (ejemplo: smtp.gmail.com)
    /// </summary>
    public string Server
    {
        get => _server;
        set => SetProperty(ref _server, value);
    }

    /// <summary>
    /// Puerto del servidor SMTP (generalmente 587 para TLS, 465 para SSL, 25 para sin cifrado)
    /// </summary>
    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    /// <summary>
    /// Usar conexión SSL/TLS
    /// </summary>
    public bool UseSSL
    {
        get => _useSSL;
        set => SetProperty(ref _useSSL, value);
    }

    /// <summary>
    /// Requiere autenticación con usuario y contraseña
    /// </summary>
    public bool UseAuthentication
    {
        get => _useAuthentication;
        set => SetProperty(ref _useAuthentication, value);
    }    /// <summary>
    /// Nombre de usuario para autenticación SMTP
    /// </summary>
    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }    /// <summary>
    /// Contraseña para autenticación SMTP (NO se almacena en configuración JSON)
    /// La contraseña se guarda de forma segura usando Windows Credential Manager
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    /// <summary>
    /// Email del remitente
    /// </summary>
    public string FromEmail
    {
        get => _fromEmail;
        set => SetProperty(ref _fromEmail, value);
    }

    /// <summary>
    /// Nombre del remitente
    /// </summary>
    public string FromName
    {
        get => _fromName;
        set => SetProperty(ref _fromName, value);
    }

    /// <summary>
    /// Timeout en milisegundos para conexiones SMTP
    /// </summary>
    public int Timeout
    {
        get => _timeout;
        set => SetProperty(ref _timeout, value);
    }    /// <summary>
    /// Indica si la configuración SMTP está completa y válida
    /// </summary>
    public bool IsConfigured
    {
        get => _isConfigured;
        set => SetProperty(ref _isConfigured, value);
    }

    /// <summary>
    /// Email para copia oculta (BCC) en todos los envíos
    /// </summary>
    public string BccEmail
    {
        get => _bccEmail;
        set => SetProperty(ref _bccEmail, value);
    }

    /// <summary>
    /// Email para copia (CC) opcional
    /// </summary>
    public string CcEmail
    {
        get => _ccEmail;
        set => SetProperty(ref _ccEmail, value);
    }/// <summary>
    /// Valida si la configuración SMTP tiene todos los datos necesarios
    /// </summary>
    public void ValidateConfiguration()
    {
        IsConfigured = !string.IsNullOrWhiteSpace(Server) &&
                      Port > 0 &&
                      !string.IsNullOrWhiteSpace(FromEmail) &&
                      (!UseAuthentication || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
    }    /// <summary>
    /// Crea una copia de la configuración SMTP actual
    /// </summary>
    public SmtpSettings Clone()
    {        return new SmtpSettings
        {
            Server = this.Server,
            Port = this.Port,
            UseSSL = this.UseSSL,
            UseAuthentication = this.UseAuthentication,
            Username = this.Username,
            Password = this.Password,
            FromEmail = this.FromEmail,
            FromName = this.FromName,
            Timeout = this.Timeout,
            IsConfigured = this.IsConfigured,
            BccEmail = this.BccEmail,
            CcEmail = this.CcEmail
        };
    }

    public static void SuspendValidation(bool suspend)
    {
        _suspendValidation = suspend;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        
        // Revalidar configuración cuando cambie cualquier propiedad
        if (!_suspendValidation && propertyName != nameof(IsConfigured))
        {
            ValidateConfiguration();
        }
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
