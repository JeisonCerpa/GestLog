using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Services.Core.Logging;
using GestLog.Services.Interfaces;

namespace GestLog.Modules.Usuarios.ViewModels
{
    /// <summary>
    /// ViewModel para el modal de recuperación de contraseña
    /// Permite a usuarios olvidar su contraseña solicitar una temporal por email
    /// </summary>
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly IPasswordManagementService _passwordManagementService;
        private readonly IGestLogLogger _logger;

        [ObservableProperty]
        private string _usernameOrEmail = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _showError = false;

        [ObservableProperty]
        private string _successMessage = string.Empty;

        [ObservableProperty]
        private bool _showSuccess = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isNotLoading = true;

        /// <summary>
        /// Evento que se dispara cuando la solicitud es exitosa
        /// </summary>
        public event Action? PasswordResetRequested;

        /// <summary>
        /// Evento que se dispara cuando se cancela la operación
        /// </summary>
        public event Action? OperationCanceled;

        public ForgotPasswordViewModel(
            IPasswordManagementService passwordManagementService,
            IGestLogLogger logger)
        {
            _passwordManagementService = passwordManagementService ?? throw new ArgumentNullException(nameof(passwordManagementService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RelayCommand]
        public async Task SendResetEmailAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IsLoading = true;
                IsNotLoading = false;
                ClearMessages();                // Validar entrada
                if (string.IsNullOrWhiteSpace(UsernameOrEmail))
                {
                    SetError("Por favor ingrese su nombre de usuario o correo");
                    return;
                }

                _logger.LogInformation("Solicitud de recuperación de contraseña para: {UsernameOrEmail}", UsernameOrEmail);

                // Llamar al servicio
                var response = await _passwordManagementService.SendPasswordResetEmailAsync(UsernameOrEmail, cancellationToken);

                if (response.Success)
                {
                    SuccessMessage = response.Message;
                    if (!string.IsNullOrEmpty(response.AdditionalInfo))
                    {
                        SuccessMessage += $"\n\nEmail enviado a: {response.AdditionalInfo}";
                    }
                    ShowSuccess = true;

                    _logger.LogInformation("Email de recuperación enviado exitosamente para: {UsernameOrEmail}", UsernameOrEmail);

                    // Disparar evento de éxito
                    PasswordResetRequested?.Invoke();

                    // Limpiar campo después de envío exitoso
                    await Task.Delay(2000, cancellationToken); // Mostrar mensaje por 2 segundos
                    UsernameOrEmail = string.Empty;
                }
                else
                {
                    SetError(response.Message);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Solicitud de recuperación cancelada");
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante solicitud de recuperación de contraseña para: {UsernameOrEmail}", UsernameOrEmail);
                SetError("Error al procesar la solicitud. Por favor, intenta más tarde.");
            }
            finally
            {
                IsLoading = false;
                IsNotLoading = true;
            }
        }

        [RelayCommand]
        public void Cancel()
        {
            _logger.LogInformation("Modal de recuperación de contraseña cerrado");
            OperationCanceled?.Invoke();
        }        private void SetError(string message)
        {
            ErrorMessage = message;
            ShowError = true;
            ShowSuccess = false;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            ShowError = false;
            SuccessMessage = string.Empty;
            ShowSuccess = false;
        }
    }
}
