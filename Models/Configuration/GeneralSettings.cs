using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones generales de la aplicación
/// </summary>
public class GeneralSettings : INotifyPropertyChanged
{
    private string _applicationName = "GestLog";
    private string _version = "1.0.0";
    private string _workingDirectory = "";
    private string _outputDirectory = "Output";
    private bool _autoSave = true;
    private int _autoSaveInterval = 5; // minutos
    private string _language = "es-ES";
    private bool _checkForUpdates = true;
    private bool _startMaximized = true;

    /// <summary>
    /// Nombre de la aplicación
    /// </summary>
    public string ApplicationName
    {
        get => _applicationName;
        set => SetProperty(ref _applicationName, value);
    }

    /// <summary>
    /// Versión de la aplicación
    /// </summary>
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    /// <summary>
    /// Directorio de trabajo principal
    /// </summary>
    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => SetProperty(ref _workingDirectory, value);
    }

    /// <summary>
    /// Directorio de salida por defecto
    /// </summary>
    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetProperty(ref _outputDirectory, value);
    }

    /// <summary>
    /// Activar guardado automático de configuraciones
    /// </summary>
    public bool AutoSave
    {
        get => _autoSave;
        set => SetProperty(ref _autoSave, value);
    }

    /// <summary>
    /// Intervalo de guardado automático en minutos
    /// </summary>
    public int AutoSaveInterval
    {
        get => _autoSaveInterval;
        set => SetProperty(ref _autoSaveInterval, value);
    }

    /// <summary>
    /// Idioma de la aplicación
    /// </summary>
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    /// <summary>
    /// Verificar actualizaciones automáticamente
    /// </summary>
    public bool CheckForUpdates
    {
        get => _checkForUpdates;
        set => SetProperty(ref _checkForUpdates, value);
    }    /// <summary>
    /// Iniciar la aplicación maximizada
    /// </summary>
    public bool StartMaximized
    {
        get => _startMaximized;
        set => SetProperty(ref _startMaximized, value);
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
