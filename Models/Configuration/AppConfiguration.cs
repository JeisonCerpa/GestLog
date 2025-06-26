using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuración raíz de la aplicación GestLog
/// Centraliza todas las configuraciones de la aplicación con estructura jerárquica
/// </summary>
public class AppConfiguration : INotifyPropertyChanged
{    private GeneralSettings _general = new();
    private UISettings _ui = new();
    private LoggingSettings _logging = new();
    private SmtpSettings _smtp = new();
    private ModulesConfiguration _modules = new();

    /// <summary>
    /// Configuraciones generales de la aplicación
    /// </summary>
    public GeneralSettings General
    {
        get => _general;
        set => SetProperty(ref _general, value);
    }

    /// <summary>
    /// Configuraciones de la interfaz de usuario
    /// </summary>
    public UISettings UI
    {
        get => _ui;
        set => SetProperty(ref _ui, value);
    }

    /// <summary>
    /// Configuraciones del sistema de logging
    /// </summary>
    public LoggingSettings Logging
    {
        get => _logging;
        set => SetProperty(ref _logging, value);
    }    /// <summary>
    /// Configuraciones del servidor SMTP para envío de emails
    /// </summary>
    public SmtpSettings Smtp
    {
        get => _smtp;
        set => SetProperty(ref _smtp, value);
    }

    /// <summary>
    /// Configuraciones de módulos específicos
    /// </summary>
    public ModulesConfiguration Modules
    {
        get => _modules;
        set => SetProperty(ref _modules, value);
    }

    /// <summary>
    /// Versión de la configuración para migración
    /// </summary>
    public string ConfigVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Timestamp de la última modificación
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        LastModified = DateTime.Now;
        OnPropertyChanged(propertyName);
        return true;
    }
}
