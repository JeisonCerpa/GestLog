using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuración de tema de la interfaz de usuario
/// </summary>
public class UISettings : INotifyPropertyChanged
{
    private string _theme = "Light";

    /// <summary>
    /// Tema de la aplicación (Light, Dark, Auto)
    /// </summary>
    public string Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
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
