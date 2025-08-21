using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones del sistema de actualizaciones
/// </summary>
public class UpdaterSettings : INotifyPropertyChanged
{    private bool _enabled = true;
    private string _updateServerPath = "\\\\SIMICSGROUPWKS1\\Hackerland\\Programas\\GestLogUpdater";
    private TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private bool _autoInstall = true;
    private bool _requireRestart = true;
    private bool _backupBeforeUpdate = true;
    private string _updateChannel = "stable";
    private bool _allowBetaUpdates = false;

    /// <summary>
    /// Habilitar sistema de actualizaciones
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    /// <summary>
    /// Ruta del servidor de actualizaciones
    /// </summary>
    public string UpdateServerPath
    {
        get => _updateServerPath;
        set => SetProperty(ref _updateServerPath, value);
    }

    /// <summary>
    /// Intervalo de verificación de actualizaciones
    /// </summary>
    public TimeSpan CheckInterval
    {
        get => _checkInterval;
        set => SetProperty(ref _checkInterval, value);
    }

    /// <summary>
    /// Instalar actualizaciones automáticamente
    /// </summary>
    public bool AutoInstall
    {
        get => _autoInstall;
        set => SetProperty(ref _autoInstall, value);
    }

    /// <summary>
    /// Requiere reinicio después de la actualización
    /// </summary>
    public bool RequireRestart
    {
        get => _requireRestart;
        set => SetProperty(ref _requireRestart, value);
    }

    /// <summary>
    /// Hacer respaldo antes de actualizar
    /// </summary>
    public bool BackupBeforeUpdate
    {
        get => _backupBeforeUpdate;
        set => SetProperty(ref _backupBeforeUpdate, value);
    }

    /// <summary>
    /// Canal de actualizaciones (stable, beta, etc.)
    /// </summary>
    public string UpdateChannel
    {
        get => _updateChannel;
        set => SetProperty(ref _updateChannel, value);
    }

    /// <summary>
    /// Permitir actualizaciones beta
    /// </summary>
    public bool AllowBetaUpdates
    {
        get => _allowBetaUpdates;
        set => SetProperty(ref _allowBetaUpdates, value);
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
