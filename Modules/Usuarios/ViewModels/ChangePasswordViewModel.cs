using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestLog.Modules.Usuarios.Models.DTOs;
using GestLog.Services.Core.Logging;
using GestLog.Services.Interfaces;
using GestLog.Modules.Usuarios.Interfaces;

namespace GestLog.Modules.Usuarios.ViewModels
{
    /// <summary>
    /// ViewModel para el modal de cambio de contraseña obligatorio
    /// Se usa en primer login o cuando se solicita cambio de contraseña
    /// </summary>
    public partial class ChangePasswordViewModel : ObservableObject
    {
        private readonly IPasswordManagementService _passwordManagementService;
        private readonly IGestLogLogger _logger;
        private readonly ICurrentUserService _currentUserService;
        private string? _userId;
        private Views.Usuarios.ChangePasswordModalView? _view;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _showError = false;

        [ObservableProperty]
        private bool _showSuccess = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private bool _isNotLoading = true;

        [ObservableProperty]
        private bool _isFirstLogin = false;

        /// <summary>
        /// Evento que se dispara cuando el cambio de contraseña es exitoso
        /// </summary>
        public event Action? PasswordChangeSuccessful;

        /// <summary>
        /// Evento que se dispara cuando se cancela el cambio de contraseña
        /// </summary>
        public event Action? PasswordChangeCanceled;        public ChangePasswordViewModel(
            IPasswordManagementService passwordManagementService,
            IGestLogLogger logger,
            ICurrentUserService currentUserService)
        {
            _passwordManagementService = passwordManagementService ?? throw new ArgumentNullException(nameof(passwordManagementService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            
            // Obtener el userId del usuario actual en sesión
            _userId = _currentUserService.Current?.UserId.ToString();
        }

        /// <summary>
        /// Establece la referencia a la vista (se llama desde el code-behind)
        /// </summary>
        public void SetView(Views.Usuarios.ChangePasswordModalView view)
        {
            _view = view;
        }[RelayCommand]
        public async Task ChangePasswordAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Verificar que tenemos acceso a la vista
                if (_view == null)
                {
                    SetError("Error: No se pudo acceder a la vista. Por favor, intente de nuevo.");
                    return;
                }

                IsLoading = true;
                IsNotLoading = false;
                
                // Obtener valores de contraseña desde la vista
                var currentPassword = _view.GetCurrentPassword();
                var newPassword = _view.GetNewPassword();
                var confirmPassword = _view.GetConfirmPassword();

                // Realizar el cambio de contraseña
                var result = await PerformPasswordChangeAsync(currentPassword, newPassword, confirmPassword, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Cambio de contraseña exitoso para usuario: {UserId}", _userId ?? "Desconocido");
                    ShowSuccess = true;
                    _view.ClearPasswords();
                    
                    // Esperar un poco para que el usuario vea el mensaje de éxito
                    await Task.Delay(1500, cancellationToken);
                    
                    PasswordChangeSuccessful?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Cambio de contraseña cancelado para usuario: {UserId}", _userId ?? "Desconocido");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante cambio de contraseña para usuario: {UserId}", _userId ?? "Desconocido");
                SetError("Error inesperado al cambiar la contraseña");
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
            _logger.LogInformation("Cambio de contraseña cancelado por usuario: {UserId}", _userId ?? "Desconocido");
            PasswordChangeCanceled?.Invoke();
        }        /// <summary>
        /// Realiza el cambio de contraseña (llamado desde el ChangePasswordCommand con datos del PasswordBox)
        /// </summary>
        public async Task<bool> PerformPasswordChangeAsync(string currentPassword, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                ClearMessages();
                
                // Validar que el userId esté disponible
                if (string.IsNullOrEmpty(_userId))
                {
                    SetError("Error: No se pudo obtener el ID del usuario. Por favor, intente nuevamente.");
                    return false;
                }

                // Validar que las contraseñas coincidan
                if (newPassword != confirmPassword)
                {
                    SetError("Las contraseñas no coinciden");
                    return false;
                }

                // Crear request
                var request = new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword,
                    IsFirstLoginChange = IsFirstLogin
                };

                // Llamar al servicio
                var result = await _passwordManagementService.ChangePasswordAsync(_userId, request, cancellationToken);

                if (!result)
                {
                    SetError("Error al cambiar la contraseña. Verifica que la contraseña actual sea correcta.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante cambio de contraseña");
                SetError("Error inesperado al cambiar la contraseña");
                return false;
            }
        }private void SetError(string message)
        {
            ErrorMessage = message;
            ShowError = true;
            ShowSuccess = false;
        }

        private void ClearMessages()
        {
            ErrorMessage = string.Empty;
            ShowError = false;
            ShowSuccess = false;
        }
    }
}
