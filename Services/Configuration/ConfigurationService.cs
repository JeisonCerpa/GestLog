using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestLog.Models.Configuration;
using GestLog.Services.Core.Logging;

namespace GestLog.Services.Configuration;

/// <summary>
/// Implementación del servicio de configuración unificado
/// Maneja la persistencia, validación y reactividad de las configuraciones
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IGestLogLogger _logger;
    private readonly string _configFilePath;
    private AppConfiguration _current;
    private bool _hasUnsavedChanges;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Configuración actual de la aplicación
    /// </summary>
    public AppConfiguration Current => _current;

    /// <summary>
    /// Indica si hay cambios pendientes de guardar
    /// </summary>
    public bool HasUnsavedChanges => _hasUnsavedChanges;

    /// <summary>
    /// Evento disparado cuando la configuración cambia
    /// </summary>
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Evento disparado antes de validar un valor
    /// </summary>
    public event EventHandler<ConfigurationValidationEventArgs>? ConfigurationValidating;

    /// <summary>
    /// Evento disparado cuando se guarda la configuración
    /// </summary>
    public event EventHandler<ConfigurationSavedEventArgs>? ConfigurationSaved;    public ConfigurationService(IGestLogLogger logger)
    {
        _logger = logger;
        
        // ✅ MEJORA: Usar AppData para configuraciones de usuario (no se pierden al limpiar bin/)
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDirectory = Path.Combine(appDataPath, "GestLog");
        _configFilePath = Path.Combine(configDirectory, "app-config.json");
          _current = new AppConfiguration();
        _hasUnsavedChanges = false;

        // Log de la nueva ubicación para información del desarrollador
        _logger.LogInformation("📁 Configuración se guardará en: {ConfigPath}", _configFilePath);
        _logger.LogInformation("💡 Las configuraciones ahora persisten al limpiar bin/ y obj/");

        // Configurar opciones de serialización JSON
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            IncludeFields = false
        };

        // Suscribirse a cambios en la configuración
        SetupPropertyChangeHandlers(_current);
    }

    /// <summary>
    /// Carga la configuración desde el archivo de configuración
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            _logger.LogDebug("🔄 Cargando configuración desde {FilePath}", _configFilePath);

            // Crear directorio si no existe
            var configDirectory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory!);
                _logger.LogDebug("📁 Directorio de configuración creado: {Directory}", configDirectory ?? "null");
            }

            // Cargar desde archivo si existe
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                _logger.LogInformation($"DEBUG: JSON crudo leído desde disco: {json}");
                GestLog.Models.Configuration.SmtpSettings.SuspendValidation(true);
                var loadedConfig = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
                GestLog.Models.Configuration.SmtpSettings.SuspendValidation(false);
                
                if (loadedConfig != null)
                {
                    _logger.LogInformation($"DEBUG: Configuración deserializada desde JSON: {json}");
                    _logger.LogInformation($"DEBUG: smtp deserializado: {System.Text.Json.JsonSerializer.Serialize(loadedConfig.Smtp)}");
                    _current = loadedConfig;
                    SetupPropertyChangeHandlers(_current);
                    _hasUnsavedChanges = false;
                    _logger.LogInformation("✅ Configuración cargada exitosamente");
                }
                else
                {
                    _logger.LogWarning("⚠️ Archivo de configuración corrupto, usando valores por defecto");
                    await CreateDefaultConfigurationAsync();
                }
            }
            else
            {
                _logger.LogInformation("📋 Archivo de configuración no encontrado, creando valores por defecto");
                await CreateDefaultConfigurationAsync();
            }

            if (_current.Updater == null || !_current.Updater.Enabled || string.IsNullOrWhiteSpace(_current.Updater.UpdateServerPath))
            {
                _logger.LogWarning("⚡ Reparando configuración de actualizaciones por defecto");
                _current.Updater = new UpdaterSettings
                {
                    Enabled = true,
                    UpdateServerPath = @"\\SIMICSGROUPWKS1\Hackerland\Programas\GestLogUpdater"
                };
                _hasUnsavedChanges = true;
                await SaveAsync();
                _logger.LogInformation("✅ Configuración de actualizaciones reparada automáticamente");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al cargar configuración, usando valores por defecto");
            await CreateDefaultConfigurationAsync();
        }
    }

    /// <summary>
    /// Guarda la configuración actual al archivo
    /// </summary>
    public async Task SaveAsync()
    {        try
        {
            _logger.LogDebug("💾 Guardando configuración en {FilePath}", _configFilePath);
            // Actualizar timestamp de modificación
            _current.LastModified = DateTime.Now;
            // Validar configuración antes de guardar
            var validationErrors = await ValidateAsync();
            if (validationErrors.Any())
            {
                var errorMessage = $"Configuración inválida: {string.Join(", ", validationErrors)}";
                _logger.LogInformation("❌ ERRORES DE VALIDACIÓN al guardar configuración: {0}", errorMessage);
                foreach (var error in validationErrors)
                {
                    _logger.LogInformation("  - {0}", error);
                }
                OnConfigurationSaved(new ConfigurationSavedEventArgs(_current, false, errorMessage));
                return;
            }
            // Crear directorio si no existe
            var configDirectory = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory!);
                _logger.LogDebug("📁 Directorio de configuración creado: {Directory}", configDirectory ?? "null");
            }
            // Guardar archivo JSON
            var json = JsonSerializer.Serialize(_current, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json); // Corregido: sin cancellationToken
            _hasUnsavedChanges = false;
            _logger.LogInformation("✅ Configuración guardada exitosamente en {FilePath}", _configFilePath);
            OnConfigurationSaved(new ConfigurationSavedEventArgs(_current, true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al guardar configuración");
            OnConfigurationSaved(new ConfigurationSavedEventArgs(_current, false, ex.Message));
        }
    }

    /// <summary>
    /// Obtiene un valor de configuración específico por ruta
    /// </summary>
    public T? GetValue<T>(string path)
    {
        try
        {
            var value = GetValueByPath(_current, path);
            if (value is T typedValue)
            {
                return typedValue;
            }
            if (value != null && typeof(T).IsAssignableFrom(value.GetType()))
            {
                return (T)value;
            }
            return default(T);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener valor de configuración para path: {Path}", path);
            return default(T);
        }
    }

    /// <summary>
    /// Establece un valor de configuración específico por ruta
    /// </summary>
    public bool SetValue<T>(string path, T value)
    {
        try
        {
            // Validar antes de establecer
            var validationArgs = new ConfigurationValidationEventArgs(path, value);
            OnConfigurationValidating(validationArgs);
              if (!validationArgs.IsValid)
            {
                _logger.LogWarning("⚠️ Valor inválido para {Path}: {Error}", path, validationArgs.ErrorMessage ?? "Error desconocido");
                return false;
            }

            var oldValue = GetValueByPath(_current, path);
            if (SetValueByPath(_current, path, value))
            {
                _hasUnsavedChanges = true;
                OnConfigurationChanged(new ConfigurationChangedEventArgs(path, oldValue, value));
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al establecer valor de configuración para path: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Restaura la configuración a valores por defecto
    /// </summary>
    public async Task ResetToDefaultsAsync(string? section = null)
    {
        try
        {
            _logger.LogInformation("🔄 Restaurando configuración a valores por defecto");
            
            if (string.IsNullOrEmpty(section))
            {
                // Restaurar toda la configuración
                _current = new AppConfiguration();
            }            else
            {
                // Restaurar sección específica
                switch (section?.ToLowerInvariant())
                {
                    case "general":
                        _current.General = new GeneralSettings();
                        break;
                    case "ui":
                        _current.UI = new UISettings();
                        break;
                    case "logging":
                        _current.Logging = new LoggingSettings();
                        break;
                    case "smtp":
                        _current.Smtp = new SmtpSettings();
                        break;
                    case "modules":
                        _current.Modules = new ModulesConfiguration();
                        break;
                    case "updater":
                        _current.Updater = new UpdaterSettings();
                        break;
                    default:
                        _logger.LogWarning("⚠️ Sección desconocida: {Section}", section ?? string.Empty);
                        return;
                }
            }

            SetupPropertyChangeHandlers(_current);
            _hasUnsavedChanges = true;
            await SaveAsync();
            
            _logger.LogInformation("✅ Configuración restaurada a valores por defecto");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al restaurar configuración por defecto");
        }
    }    /// <summary>
    /// Valida la configuración actual
    /// </summary>
    public Task<IEnumerable<string>> ValidateAsync()
    {
        var errors = new List<string>();
        try
        {

            // Validar configuraciones de UI
            // Eliminado: FontSize y WindowOpacity

            // Validar configuraciones de logging
            if (_current.Logging.MaxLogFiles < 1)
                errors.Add("El número máximo de archivos de log debe ser mayor a 0");

            // Validar configuraciones SMTP
            if (_current.Smtp.IsConfigured)
            {
                if (string.IsNullOrWhiteSpace(_current.Smtp.Server))
                    errors.Add("El servidor SMTP es requerido");

                if (_current.Smtp.Port <= 0 || _current.Smtp.Port > 65535)
                    errors.Add("El puerto SMTP debe estar entre 1 y 65535");

                if (string.IsNullOrWhiteSpace(_current.Smtp.FromEmail))
                    errors.Add("El email del remitente es requerido");

                if (_current.Smtp.UseAuthentication && string.IsNullOrWhiteSpace(_current.Smtp.Username))
                    errors.Add("El usuario SMTP es requerido cuando la autenticación está habilitada");

                if (_current.Smtp.Timeout < 1000)
                    errors.Add("El timeout SMTP debe ser mayor a 1000ms");
            }

            _logger.LogDebug("🔍 Validación completada. {ErrorCount} errores encontrados", errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al validar configuración");
        }

        return Task.FromResult<IEnumerable<string>>(errors);
    }

    /// <summary>
    /// Exporta la configuración a un archivo
    /// </summary>
    public async Task ExportAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("📤 Exportando configuración a {FilePath}", filePath);
            
            var json = JsonSerializer.Serialize(_current, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogInformation("✅ Configuración exportada exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al exportar configuración");
            throw;
        }
    }

    /// <summary>
    /// Importa configuración desde un archivo
    /// </summary>
    public async Task ImportAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("📥 Importando configuración desde {FilePath}", filePath);
            
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Archivo no encontrado: {filePath}");

            var json = await File.ReadAllTextAsync(filePath);
            var importedConfig = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
            
            if (importedConfig != null)
            {
                _current = importedConfig;
                SetupPropertyChangeHandlers(_current);
                _hasUnsavedChanges = true;
                await SaveAsync();
                
                _logger.LogInformation("✅ Configuración importada exitosamente");
            }
            else
            {
                throw new InvalidOperationException("El archivo no contiene una configuración válida");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error al importar configuración");
            throw;
        }
    }

    #region Métodos privados

    private async Task CreateDefaultConfigurationAsync()
    {
        _current = new AppConfiguration();
        SetupPropertyChangeHandlers(_current);
        _hasUnsavedChanges = true;
        await SaveAsync();
    }    private void SetupPropertyChangeHandlers(AppConfiguration config)
    {
        config.PropertyChanged += OnConfigurationPropertyChanged;
        config.General.PropertyChanged += OnConfigurationPropertyChanged;
        config.UI.PropertyChanged += OnConfigurationPropertyChanged;
        config.Logging.PropertyChanged += OnConfigurationPropertyChanged;
        config.Smtp.PropertyChanged += OnConfigurationPropertyChanged;
        config.Modules.PropertyChanged += OnConfigurationPropertyChanged;
        config.Modules.DaaterProcessor.PropertyChanged += OnConfigurationPropertyChanged;
        config.Modules.ErrorLog.PropertyChanged += OnConfigurationPropertyChanged;
        config.Updater.PropertyChanged += OnConfigurationPropertyChanged;
    }

    private void OnConfigurationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _hasUnsavedChanges = true;
    }

    private object? GetValueByPath(object obj, string path)
    {
        var parts = path.Split('.');
        object current = obj;

        foreach (var part in parts)
        {
            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return null;

            current = property.GetValue(current)!;
            if (current == null)
                return null;
        }

        return current;
    }

    private bool SetValueByPath(object obj, string path, object? value)
    {
        var parts = path.Split('.');
        object current = obj;

        // Navegar hasta el objeto que contiene la propiedad final
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var property = current.GetType().GetProperty(parts[i], BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            current = property.GetValue(current)!;
            if (current == null)
                return false;
        }

        // Establecer el valor final
        var finalProperty = current.GetType().GetProperty(parts[^1], BindingFlags.Public | BindingFlags.Instance);
        if (finalProperty == null || !finalProperty.CanWrite)
            return false;

        finalProperty.SetValue(current, value);
        return true;
    }

    private void OnConfigurationChanged(ConfigurationChangedEventArgs args)
    {
        ConfigurationChanged?.Invoke(this, args);
    }

    private void OnConfigurationValidating(ConfigurationValidationEventArgs args)
    {
        ConfigurationValidating?.Invoke(this, args);
    }

    private void OnConfigurationSaved(ConfigurationSavedEventArgs args)
    {
        ConfigurationSaved?.Invoke(this, args);
    }

    #endregion
}
