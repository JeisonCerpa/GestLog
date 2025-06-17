using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Models.Exceptions;
using GestLog.Services.Interfaces;
using GestLog.Services.Core.Logging;

namespace GestLog.ViewModels;

/// <summary>
/// ViewModel for first run setup dialog
/// Follows SRP: Only responsible for first run setup UI logic
/// </summary>
public partial class FirstRunSetupViewModel : ObservableObject, IDisposable
{
    private readonly IFirstRunSetupService _setupService;
    private readonly IGestLogLogger _logger;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    [ObservableProperty] private string _server = "localhost";
    [ObservableProperty] private string _database = "GestLog";
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private bool _isConfiguring;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private bool _isTestingConnection;

    public event EventHandler<bool>? SetupCompleted;

    public FirstRunSetupViewModel(
        IFirstRunSetupService setupService,
        IGestLogLogger logger)
    {
        _setupService = setupService ?? throw new ArgumentNullException(nameof(setupService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTestingConnection = true;
            HasError = false;
            ErrorMessage = string.Empty;

            _logger.LogInformation("Testing automatic database connection for first run setup");

            var connectionWorks = await _setupService.TestAutomaticConnectionAsync(_cancellationTokenSource.Token);

            if (connectionWorks)
            {
                ErrorMessage = "✅ Conexión automática exitosa";
                HasError = false;
                _logger.LogInformation("Automatic database connection test successful");
            }
            else
            {
                ErrorMessage = "❌ No se pudo conectar automáticamente. Revise la configuración de SQL Server.";
                HasError = true;
                _logger.LogWarning("Automatic database connection test failed");
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Prueba de conexión cancelada";
            HasError = true;
            _logger.LogWarning("Connection test cancelled");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error inesperado probando la conexión automática";
            HasError = true;
            _logger.LogError(ex, "Unexpected error testing automatic connection");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }    [RelayCommand]
    private async Task ConfigureAsync()
    {
        try
        {
            IsConfiguring = true;
            HasError = false;
            ErrorMessage = string.Empty;

            _logger.LogInformation("Starting automatic first run configuration");

            await _setupService.ConfigureAutomaticEnvironmentVariablesAsync(_cancellationTokenSource.Token);

            _logger.LogInformation("Automatic first run configuration completed successfully");
            SetupCompleted?.Invoke(this, true);
        }
        catch (SecurityConfigurationException ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Security configuration error during automatic first run setup");
        }
        catch (ArgumentException ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            _logger.LogError(ex, "Validation error during automatic first run setup");
        }
        catch (OperationCanceledException)
        {
            HasError = true;
            ErrorMessage = "Configuración cancelada";
            _logger.LogWarning("Automatic first run setup cancelled");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = "Error inesperado durante la configuración automática";
            _logger.LogError(ex, "Unexpected error during automatic first run setup");
        }
        finally
        {
            IsConfiguring = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellationTokenSource.Cancel();
        SetupCompleted?.Invoke(this, false);
    }

    [RelayCommand]
    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    private bool ValidateInput()
    {
        if (string.IsNullOrWhiteSpace(Server))
        {
            ErrorMessage = "Debe especificar el servidor de base de datos";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Database))
        {
            ErrorMessage = "Debe especificar el nombre de la base de datos";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Debe especificar el usuario de base de datos";
            HasError = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Debe especificar la contraseña";
            HasError = true;
            return false;
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "La contraseña debe tener al menos 8 caracteres";
            HasError = true;
            return false;
        }

        HasError = false;
        ErrorMessage = string.Empty;
        return true;
    }    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _cancellationTokenSource?.Dispose();
        }
        _disposed = true;
    }
}
