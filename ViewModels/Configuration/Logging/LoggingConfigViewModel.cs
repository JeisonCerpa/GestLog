using CommunityToolkit.Mvvm.ComponentModel;

namespace GestLog.ViewModels.Configuration.Logging;

/// <summary>
/// ViewModel para la configuración de logging (sin opciones editables)
/// </summary>
public partial class LoggingConfigViewModel : ObservableObject
{
    // No se exponen propiedades editables, la configuración es automática.
    public LoggingConfigViewModel() { }
}
