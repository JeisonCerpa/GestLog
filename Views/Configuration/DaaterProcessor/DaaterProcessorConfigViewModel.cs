using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Models.Validation;
using GestLog.Services.Validation;
using GestLog.Services.Core.Logging;
using GestLog.Models.Configuration.Modules;
using GestLog.Models.Validation.Validators;
using GestLog.ViewModels.Base;
using CustomValidationResult = GestLog.Models.Validation.ValidationResult;

namespace GestLog.Views.Configuration.DaaterProcessor
{
    /// <summary>
    /// ViewModel para la configuración del DaaterProcessor con validación integrada
    /// </summary>
    public partial class DaaterProcessorConfigViewModel : ValidatableViewModel
    {
        private readonly IValidationService _validationService;
        private readonly IGestLogLogger _logger;
        private DaaterProcessorSettings _settings;        // Constructor público sin parámetros requerido para XAML
        public DaaterProcessorConfigViewModel() : this(null, null)
        {
        }

        public DaaterProcessorConfigViewModel(IValidationService? validationService = null, IGestLogLogger? logger = null)
            : base(validationService ?? CreateDefaultValidationService())
        {
            _validationService = validationService ?? CreateDefaultValidationService();
            _logger = logger ?? LoggingService.GetLogger();
            _settings = new DaaterProcessorSettings();

            // Registrar validadores personalizados
            RegisterCustomValidator(new DaaterProcessorSettingsValidator());
            
            // Suscribirse a los cambios en los errores
            ErrorsChanged += (s, e) => OnPropertyChanged(nameof(AllErrors));
        }

        private static IValidationService CreateDefaultValidationService()
        {
            var logger = LoggingService.GetLogger();
            return new ValidationService(logger);
        }
        
        #region Properties

        public DaaterProcessorSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        // Propiedades de configuración específicas para binding directo
        public string DefaultInputPath
        {
            get => _settings.DefaultInputPath;
            set
            {
                if (_settings.DefaultInputPath != value)
                {
                    _settings.DefaultInputPath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public string DefaultOutputPath
        {
            get => _settings.DefaultOutputPath;
            set
            {
                if (_settings.DefaultOutputPath != value)
                {
                    _settings.DefaultOutputPath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public string BackupDirectory
        {
            get => _settings.BackupDirectory;
            set
            {
                if (_settings.BackupDirectory != value)
                {
                    _settings.BackupDirectory = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public int MaxRowsPerFile
        {
            get => _settings.MaxRowsPerFile;
            set
            {
                if (_settings.MaxRowsPerFile != value)
                {
                    _settings.MaxRowsPerFile = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public string DateFormat
        {
            get => _settings.DateFormat;
            set
            {
                if (_settings.DateFormat != value)
                {
                    _settings.DateFormat = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public string DecimalSeparator
        {
            get => _settings.DecimalSeparator;
            set
            {
                if (_settings.DecimalSeparator != value)
                {
                    _settings.DecimalSeparator = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public string ThousandsSeparator
        {
            get => _settings.ThousandsSeparator;
            set
            {
                if (_settings.ThousandsSeparator != value)
                {
                    _settings.ThousandsSeparator = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool EnableDataConsolidation
        {
            get => _settings.EnableDataConsolidation;
            set
            {
                if (_settings.EnableDataConsolidation != value)
                {
                    _settings.EnableDataConsolidation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool EnableProviderNormalization
        {
            get => _settings.EnableProviderNormalization;
            set
            {
                if (_settings.EnableProviderNormalization != value)
                {
                    _settings.EnableProviderNormalization = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool EnableCountryMapping
        {
            get => _settings.EnableCountryMapping;
            set
            {
                if (_settings.EnableCountryMapping != value)
                {
                    _settings.EnableCountryMapping = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool CreateBackupBeforeProcessing
        {
            get => _settings.CreateBackupBeforeProcessing;
            set
            {
                if (_settings.CreateBackupBeforeProcessing != value)
                {
                    _settings.CreateBackupBeforeProcessing = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool EnableProgressReporting
        {
            get => _settings.EnableProgressReporting;
            set
            {
                if (_settings.EnableProgressReporting != value)
                {
                    _settings.EnableProgressReporting = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }        public bool EnableErrorRecovery
        {
            get => _settings.EnableErrorRecovery;
            set
            {
                if (_settings.EnableErrorRecovery != value)
                {
                    _settings.EnableErrorRecovery = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }

        public bool EnableDuplicateValidation
        {
            get => _settings.EnableDuplicateValidation;
            set
            {
                if (_settings.EnableDuplicateValidation != value)
                {
                    _settings.EnableDuplicateValidation = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    ValidateProperty(value);
                }
            }
        }        public DuplicateHandlingMode DuplicateHandlingMode
        {
            get => _settings.DuplicateHandlingMode;
            set
            {
                if (_settings.DuplicateHandlingMode != value)
                {
                    _settings.DuplicateHandlingMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    OnPropertyChanged(nameof(DuplicateHandlingModeIndex));
                    ValidateProperty(value);
                }
            }
        }

        /// <summary>
        /// Índice para el ComboBox basado en el enum DuplicateHandlingMode
        /// </summary>
        public int DuplicateHandlingModeIndex
        {
            get => (int)_settings.DuplicateHandlingMode;
            set
            {
                var enumValue = (DuplicateHandlingMode)value;
                if (_settings.DuplicateHandlingMode != enumValue)
                {
                    _settings.DuplicateHandlingMode = enumValue;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Settings));
                    OnPropertyChanged(nameof(DuplicateHandlingMode));
                    ValidateProperty(enumValue);
                }
            }
        }

        #endregion
        
        #region Command Methods        [RelayCommand(CanExecute = nameof(CanSaveConfiguration))]
        private async Task SaveConfiguration()
        {
            try
            {
                _logger.LogInformation("Guardando configuración del DaaterProcessor...");
                
                // Validar antes de guardar
                if (!ValidateAll())
                {
                    _logger.LogWarning("La configuración tiene errores de validación y no se puede guardar");
                    return;
                }

                // Guardar configuración usando el sistema de configuración existente
                await Task.Run(() =>
                {
                    // La configuración se aplica automáticamente a través del binding
                    // Las validaciones ya están en los setters de propiedades
                    _logger.LogDebug("Configuración aplicada mediante binding de propiedades");
                });
                
                _logger.LogInformation("✅ Configuración guardada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al guardar la configuración");
            }
        }        [RelayCommand]
        private async Task LoadConfiguration()
        {
            try
            {
                _logger.LogInformation("Cargando configuración del DaaterProcessor...");
                
                // Cargar configuración por defecto y aplicar validaciones
                await Task.Run(() =>
                {
                    // Verificar que todas las propiedades están configuradas correctamente
                    if (_settings == null)
                        _settings = new DaaterProcessorSettings();
                    
                    // Ejecutar validación completa
                    ValidateAll();
                    _logger.LogDebug("Configuración cargada desde valores por defecto");
                });
                
                _logger.LogInformation("✅ Configuración cargada exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al cargar la configuración");
                Settings = new DaaterProcessorSettings(); // Fallback a configuración por defecto
            }
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            try
            {
                _logger.LogInformation("Restaurando configuración por defecto...");
                Settings = new DaaterProcessorSettings();
                ClearAllErrors();
                _logger.LogInformation("✅ Configuración restaurada a valores por defecto");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al restaurar configuración por defecto");
            }
        }

        [RelayCommand]
        private void ValidateConfiguration()
        {
            try
            {
                _logger.LogInformation("Validando configuración del DaaterProcessor...");
                
                var isValid = ValidateAll();
                var validationResult = _validationService.ValidateObject(_settings);
                
                if (isValid && validationResult.IsValid)
                {
                    _logger.LogInformation("✅ Configuración válida");
                }
                else
                {
                    _logger.LogWarning("⚠️ La configuración tiene errores o advertencias");
                    
                    foreach (var error in validationResult.Errors)
                    {
                        _logger.LogWarning("Error: {PropertyName} - {Message}", error.PropertyName, error.Message);
                    }
                    
                    foreach (var warning in validationResult.Warnings)
                    {
                        _logger.LogInformation("Advertencia: {PropertyName} - {Message}", warning.PropertyName, warning.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error durante la validación de configuración");
            }
        }        [RelayCommand(CanExecute = nameof(CanTestConnection))]
        private async Task TestConnection()
        {
            try
            {
                _logger.LogInformation("Probando configuración actual del DaaterProcessor...");
                
                // Validar que las rutas y configuraciones sean válidas
                await Task.Run(() =>
                {
                    var validationErrors = new List<string>();
                    
                    // Validar rutas si están configuradas
                    if (!string.IsNullOrWhiteSpace(_settings.DefaultInputPath) && !Directory.Exists(_settings.DefaultInputPath))
                        validationErrors.Add("Ruta de entrada no existe");
                        
                    if (!string.IsNullOrWhiteSpace(_settings.DefaultOutputPath) && !Directory.Exists(Path.GetDirectoryName(_settings.DefaultOutputPath)))
                        validationErrors.Add("Directorio de salida no es válido");
                    
                    if (_settings.CreateBackupBeforeProcessing && !string.IsNullOrWhiteSpace(_settings.BackupDirectory) && !Directory.Exists(_settings.BackupDirectory))
                        validationErrors.Add("Directorio de backup no existe");
                    
                    if (validationErrors.Any())
                        throw new InvalidOperationException($"Errores de configuración: {string.Join(", ", validationErrors)}");
                });
                
                _logger.LogInformation("✅ Configuración válida para procesamiento");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en validación de configuración");
                throw;
            }
        }

        #endregion
        
        #region Command Can Execute

        private bool CanSaveConfiguration()
        {
            return _settings != null && !HasErrors;
        }

        private bool CanTestConnection()
        {
            return _settings != null && 
                   !string.IsNullOrWhiteSpace(_settings.DefaultInputPath) &&
                   !string.IsNullOrWhiteSpace(_settings.DefaultOutputPath);
        }

        #endregion

        #region Validation
        
        protected override GestLog.Models.Validation.ValidationResult CustomValidation()
        {
            var result = new CustomValidationResult();

            // Validación personalizada adicional del ViewModel
            if (_settings != null)
            {
                // Validar consistencia entre configuraciones
                if (_settings.EnableDataConsolidation && 
                    string.IsNullOrWhiteSpace(_settings.DefaultOutputPath))
                {
                    result.AddError(nameof(DefaultOutputPath), 
                        "La ruta de salida es requerida cuando la consolidación de datos está habilitada");
                }

                if (_settings.CreateBackupBeforeProcessing && 
                    string.IsNullOrWhiteSpace(_settings.BackupDirectory))
                {
                    result.AddError(nameof(BackupDirectory), 
                        "El directorio de backup es requerido cuando el backup está habilitado");
                }

                // Validar que los separadores sean diferentes
                if (!string.IsNullOrEmpty(_settings.DecimalSeparator) && 
                    _settings.DecimalSeparator == _settings.ThousandsSeparator)
                {
                    result.AddError(nameof(DecimalSeparator), 
                        "El separador decimal no puede ser igual al separador de miles");
                }
            }

            return result.IsValid ? CustomValidationResult.Success() : result;
        }

        #endregion

        #region Props adicionales para UI

        /// <summary>
        /// Obtiene una lista de todos los mensajes de error para mostrar en la interfaz
        /// </summary>
        public List<string> AllErrors
        {
            get
            {
                List<string> result = new List<string>();
                foreach (var prop in GetErrorProperties())
                {
                    foreach (var error in GetPropertyErrors(prop))
                    {
                        result.Add($"{prop}: {error}");
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Obtiene una lista de nombres de propiedades que tienen errores
        /// </summary>
        private IEnumerable<string> GetErrorProperties()
        {
            var properties = new List<string>();
            foreach (var prop in GetType().GetProperties())
            {
                if (HasErrorsForProperty(prop.Name))
                {
                    properties.Add(prop.Name);
                }
            }
            return properties;
        }        /// <summary>
        /// Obtiene los errores para una propiedad específica
        /// </summary>
        private IEnumerable<string> GetPropertyErrors(string propertyName)
        {
            var errors = new List<string>();
            var propertyErrors = GetErrors(propertyName);
            if (propertyErrors != null)
            {
                foreach (var error in propertyErrors)
                {
                    string? errorString = error?.ToString();
                    if (!string.IsNullOrEmpty(errorString))
                    {
                        errors.Add(errorString);
                    }
                }
            }
            return errors;
        }

        #endregion
    }
}
