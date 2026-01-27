using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GestLog.Models.Configuration;

/// <summary>
/// Configuraciones de módulos específicos de la aplicación
/// </summary>
public class ModulesConfiguration : INotifyPropertyChanged
{    private Modules.DaaterProcessorSettings _daaterProcessor = new();
    private Modules.ErrorLogSettings _errorLog = new();
    private Modules.EnvioCatalogoSettings _envioCatalogo = new();
    private Modules.GestionCarteraSettings _gestionCartera = new();

    /// <summary>
    /// Configuraciones del módulo DaaterProcessor
    /// </summary>
    public Modules.DaaterProcessorSettings DaaterProcessor
    {
        get => _daaterProcessor;
        set => SetProperty(ref _daaterProcessor, value);
    }

    /// <summary>
    /// Configuraciones del registro de errores
    /// </summary>
    public Modules.ErrorLogSettings ErrorLog
    {
        get => _errorLog;
        set => SetProperty(ref _errorLog, value);
    }    /// <summary>
    /// Configuraciones del módulo de Envío de Catálogo
    /// </summary>
    public Modules.EnvioCatalogoSettings EnvioCatalogo
    {
        get => _envioCatalogo;
        set => SetProperty(ref _envioCatalogo, value);
    }

    /// <summary>
    /// Configuraciones del módulo de Gestión de Cartera
    /// </summary>
    public Modules.GestionCarteraSettings GestionCartera
    {
        get => _gestionCartera;
        set => SetProperty(ref _gestionCartera, value);
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
