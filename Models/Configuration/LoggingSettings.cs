using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones del sistema de logging (solo lo esencial)
/// </summary>
public class LoggingSettings : INotifyPropertyChanged
{
    private LogLevel _minimumLevel = LogLevel.Information;
    private bool _enableFileLogging = true;
    private bool _enableConsoleLogging = true;
    private string _logDirectory = "Logs";
    private int _maxLogFiles = 30;

    /// <summary>
    /// Nivel mínimo de logging
    /// </summary>
    public LogLevel MinimumLevel
    {
        get => _minimumLevel;
        set => SetProperty(ref _minimumLevel, value);
    }

    /// <summary>
    /// Habilitar logging a archivos
    /// </summary>
    public bool EnableFileLogging
    {
        get => _enableFileLogging;
        set => SetProperty(ref _enableFileLogging, value);
    }

    /// <summary>
    /// Habilitar logging a consola
    /// </summary>
    public bool EnableConsoleLogging
    {
        get => _enableConsoleLogging;
        set => SetProperty(ref _enableConsoleLogging, value);
    }

    /// <summary>
    /// Directorio donde se guardan los logs
    /// </summary>
    public string LogDirectory
    {
        get => _logDirectory;
        set => SetProperty(ref _logDirectory, value);
    }

    /// <summary>
    /// Número máximo de archivos de log a mantener
    /// </summary>
    public int MaxLogFiles
    {
        get => _maxLogFiles;
        set => SetProperty(ref _maxLogFiles, Math.Max(1, value));
    }

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
        OnPropertyChanged(propertyName);
        return true;
    }
}
