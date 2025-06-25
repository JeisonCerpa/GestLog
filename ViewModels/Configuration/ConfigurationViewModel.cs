using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;
using GestLog.Services.Configuration;

namespace GestLog.ViewModels.Configuration;

/// <summary>
/// ViewModel para la gestión de configuraciones de la aplicación
/// Proporciona interfaz reactiva para todas las configuraciones con validación y persistencia
/// </summary>
public partial class ConfigurationViewModel : ObservableObject
{    private readonly IConfigurationService _configurationService;
    private readonly IGestLogLogger _logger;

    private AppConfiguration _configuration = null!;
      /// <summary>
    /// Configuración actual de la aplicación
    /// </summary>
    public AppConfiguration Configuration
    {
        get => _configuration;
        set
        {
            if (SetProperty(ref _configuration, value))
            {
                // Reconfigurar eventos de PropertyChanged para la nueva configuración
                if (value != null)
                {
                    value.PropertyChanged += async (_, _) => await ValidateCurrentConfiguration();
                }
            }
        }
    }

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _validationErrors = new();

    [ObservableProperty]
    private string _selectedSection = "General";

    /// <summary>
    /// Lista de secciones disponibles para configuración
    /// </summary>
    public ObservableCollection<ConfigurationSection> AvailableSections { get; } = new()
    {
        new("General", "🔧", "Configuraciones generales de la aplicación"),        new("UI", "🎨", "Configuraciones de interfaz de usuario"),
        new("Logging", "📝", "Configuraciones del sistema de logging"),
        new("Performance", "⚡", "Configuraciones de rendimiento"),
        new("SMTP", "📧", "Configuraciones del servidor de correo"),
        new("DaaterProcessor", "📊", "Configuraciones del procesador de datos"),
        new("ErrorLog", "⚠️", "Configuraciones del registro de errores")
    };    public ConfigurationViewModel(IConfigurationService configurationService, IGestLogLogger logger)
    {
        _configurationService = configurationService;
        _logger = logger;
        
        // Usar la propiedad en lugar del campo para activar el setter personalizado
        Configuration = _configurationService.Current;

        // Suscribirse a eventos del servicio de configuración
        _configurationService.ConfigurationChanged += OnConfigurationChanged;
        _configurationService.ConfigurationSaved += OnConfigurationSaved;        // Inicializar con la configuración actual
        HasUnsavedChanges = _configurationService.HasUnsavedChanges;        
        
        // Configurar validación automática cuando cambien las propiedades
        // (ya se hace en el setter de Configuration)
    }/// <summary>
    /// Comando para cargar la configuración
    /// </summary>
    [RelayCommand]
    private async Task LoadConfiguration()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Cargando configuración...";
            
            _logger.LogDebug("🔄 Cargando configuración desde ViewModel");
            
            await _configurationService.LoadAsync();
            
            Configuration = _configurationService.Current;
            
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
            
            await ValidateCurrentConfiguration();
            
            StatusMessage = "Configuración cargada exitosamente";
            _logger.LogInformation("✅ Configuración cargada en ViewModel");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al cargar configuración: {ex.Message}";
            _logger.LogError(ex, "❌ Error al cargar configuración en ViewModel");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Comando para guardar la configuración
    /// </summary>
    [RelayCommand]
    private async Task SaveConfiguration()
    {
        try
        {
            IsSaving = true;
            StatusMessage = "Guardando configuración...";
            
            _logger.LogDebug("💾 Guardando configuración desde ViewModel");
            
            await _configurationService.SaveAsync();
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
            
            StatusMessage = "Configuración guardada exitosamente";
            _logger.LogInformation("✅ Configuración guardada desde ViewModel");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar configuración: {ex.Message}";
            _logger.LogError(ex, "❌ Error al guardar configuración desde ViewModel");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Comando para restaurar configuración por defecto
    /// </summary>
    [RelayCommand]
    private async Task ResetToDefaults()
    {
        try
        {
            StatusMessage = "Restaurando valores por defecto...";
            
            _logger.LogInformation("🔄 Restaurando configuración a valores por defecto");
            
            await _configurationService.ResetToDefaultsAsync();
            Configuration = _configurationService.Current;
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
            
            await ValidateCurrentConfiguration();
            
            StatusMessage = "Configuración restaurada a valores por defecto";
            _logger.LogInformation("✅ Configuración restaurada a valores por defecto");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al restaurar configuración: {ex.Message}";
            _logger.LogError(ex, "❌ Error al restaurar configuración por defecto");
        }
    }

    /// <summary>
    /// Comando para restaurar una sección específica
    /// </summary>
    [RelayCommand]
    private async Task ResetSection()
    {
        try
        {
            StatusMessage = $"Restaurando sección {SelectedSection}...";
            
            _logger.LogInformation("🔄 Restaurando sección {Section}", SelectedSection);
            
            await _configurationService.ResetToDefaultsAsync(SelectedSection.ToLower());
            Configuration = _configurationService.Current;
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
            
            await ValidateCurrentConfiguration();
            
            StatusMessage = $"Sección {SelectedSection} restaurada";
            _logger.LogInformation("✅ Sección {Section} restaurada", SelectedSection);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al restaurar sección: {ex.Message}";
            _logger.LogError(ex, "❌ Error al restaurar sección {Section}", SelectedSection);
        }
    }

    /// <summary>
    /// Comando para exportar configuración
    /// </summary>
    [RelayCommand]
    private async Task ExportConfiguration(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            StatusMessage = "Ruta de archivo requerida para exportación";
            return;
        }

        try
        {
            StatusMessage = "Exportando configuración...";
            
            _logger.LogInformation("📤 Exportando configuración a {FilePath}", filePath);
            
            await _configurationService.ExportAsync(filePath);
            
            StatusMessage = "Configuración exportada exitosamente";
            _logger.LogInformation("✅ Configuración exportada exitosamente");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al exportar configuración: {ex.Message}";
            _logger.LogError(ex, "❌ Error al exportar configuración");
        }
    }

    /// <summary>
    /// Comando para importar configuración
    /// </summary>
    [RelayCommand]
    private async Task ImportConfiguration(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            StatusMessage = "Ruta de archivo requerida para importación";
            return;
        }

        try
        {
            StatusMessage = "Importando configuración...";
            
            _logger.LogInformation("📥 Importando configuración desde {FilePath}", filePath);
            
            await _configurationService.ImportAsync(filePath);
            Configuration = _configurationService.Current;
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
            
            await ValidateCurrentConfiguration();
            
            StatusMessage = "Configuración importada exitosamente";
            _logger.LogInformation("✅ Configuración importada exitosamente");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al importar configuración: {ex.Message}";
            _logger.LogError(ex, "❌ Error al importar configuración");
        }
    }

    /// <summary>
    /// Comando para cambiar la sección activa
    /// </summary>
    [RelayCommand]
    private void ChangeSection(string sectionName)
    {
        if (!string.IsNullOrEmpty(sectionName))
        {
            SelectedSection = sectionName;
            _logger.LogDebug("🔄 Sección cambiada a: {Section}", sectionName);
        }
    }

    /// <summary>
    /// Valida la configuración actual
    /// </summary>
    private async Task ValidateCurrentConfiguration()
    {
        try
        {
            var errors = await _configurationService.ValidateAsync();
            ValidationErrors.Clear();
            foreach (var error in errors)
            {
                ValidationErrors.Add(error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error durante la validación");
            ValidationErrors.Clear();
            ValidationErrors.Add($"Error de validación: {ex.Message}");
        }
    }

    /// <summary>
    /// Maneja cambios en la configuración
    /// </summary>
    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        HasUnsavedChanges = _configurationService.HasUnsavedChanges;
        _logger.LogDebug("🔄 Configuración cambiada: {Path} = {NewValue}", e.SettingPath, e.NewValue ?? "null");
    }

    /// <summary>
    /// Maneja eventos de guardado de configuración
    /// </summary>
    private void OnConfigurationSaved(object? sender, ConfigurationSavedEventArgs e)
    {
        HasUnsavedChanges = _configurationService.HasUnsavedChanges;
        
        if (e.Success)
        {
            StatusMessage = "Configuración guardada exitosamente";
        }
        else
        {
            StatusMessage = $"Error al guardar: {e.ErrorMessage}";
        }
    }

    /// <summary>
    /// Obtiene un valor de configuración específico
    /// </summary>
    public T? GetConfigValue<T>(string path)
    {
        return _configurationService.GetValue<T>(path);
    }

    /// <summary>
    /// Establece un valor de configuración específico
    /// </summary>
    public bool SetConfigValue<T>(string path, T value)
    {
        var result = _configurationService.SetValue(path, value);
        if (result)
        {
            HasUnsavedChanges = _configurationService.HasUnsavedChanges;
        }
        return result;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        // Auto-validar cuando cambien propiedades relevantes
        if (e.PropertyName == nameof(Configuration))
        {
            _ = ValidateCurrentConfiguration();
        }
    }
}

/// <summary>
/// Representa una sección de configuración
/// </summary>
public record ConfigurationSection(string Name, string Icon, string Description);
