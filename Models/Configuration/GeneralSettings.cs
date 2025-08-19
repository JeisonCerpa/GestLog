using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones generales de la aplicación
/// </summary>
public class GeneralSettings : INotifyPropertyChanged
{
    private readonly string _applicationName = "GestLog";
    private string _version = "1.0.1";
    private string _outputDirectory = "Output";
    private bool _startMaximized = true;

    /// <summary>
    /// Nombre de la aplicación (fijo)
    /// </summary>
    public string ApplicationName => _applicationName;

    /// <summary>
    /// Versión de la aplicación
    /// </summary>
    public string Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
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
