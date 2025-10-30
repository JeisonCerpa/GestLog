using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestLog.Messages;
using GestLog.Modules.Usuarios.Interfaces;
using GestLog.Modules.Usuarios.Models.Authentication;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.Usuarios.ViewModels
{
    /// <summary>
    /// ViewModel para la vista de login
    /// Responsabilidad: Gestionar la UI y lógica de autenticación de usuarios
    /// </summary>
    public partial class LoginViewModel : ObservableValidator
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IGestLogLogger _logger;
        private readonly ICurrentUserService _currentUserService;

        [ObservableProperty]
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        private string _username = string.Empty;

        [ObservableProperty]
        [Required(ErrorMessage = "La contraseña es obligatoria")]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe = false;

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;        [ObservableProperty]
        private string _statusMessage = "Ingrese sus credenciales para acceder";

        [ObservableProperty]
        private bool _isFirstLogin = false;

        public static readonly string UserLoggedInMessageToken = "UserLoggedInMessage";

        /// <summary>
        /// Evento que se dispara cuando se necesita mostrar modal de cambio de contraseña obligatorio
        /// </summary>
        public event Action? ShowPasswordChangeModal;

        /// <summary>
        /// Evento que se dispara cuando se solicita mostrar modal de recuperación de contraseña
        /// </summary>
        public event Action? ShowForgotPasswordModal;

        public LoginViewModel(IAuthenticationService authenticationService, IGestLogLogger logger, ICurrentUserService currentUserService)
        {
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }        [RelayCommand]
        private async Task LoginAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // ⚡ INMEDIATAMENTE mostrar loading para feedback visual instantáneo
                IsLoading = true;
                StatusMessage = "Verificando credenciales...";
                
                ClearError();
                if (!ValidateInput())
                {
                    _logger.LogWarning("Validación falló - no se procederá con el login");
                    return;
                }
                
                _logger.LogInformation("Iniciando proceso de login para usuario: {Username}", Username);
                var loginRequest = new LoginRequest
                {
                    Username = Username.Trim(),
                    Password = Password,
                    RememberMe = RememberMe
                };
                var result = await _authenticationService.LoginAsync(loginRequest, cancellationToken);
                if (result.Success)
                {
                    _logger.LogInformation("Login exitoso para usuario: {Username}", Username);
                    StatusMessage = "¡Bienvenido! Redirigiendo...";                    
                    if (result.CurrentUserInfo != null)
                    {
                        _currentUserService.SetCurrentUser(result.CurrentUserInfo, RememberMe);
                        
                        // ✅ Verificar si es primer login
                        IsFirstLogin = result.CurrentUserInfo.IsFirstLogin;
                        
                        if (IsFirstLogin)
                        {
                            _logger.LogInformation("Primer login detectado para usuario: {Username}. Mostrando modal de cambio de contraseña obligatorio.", Username);
                            // Mostrar modal de cambio de contraseña obligatorio
                            ShowPasswordChangeModal?.Invoke();
                        }
                        else
                        {
                            _logger.LogInformation("Acceso normal para usuario: {Username}", Username);
                            // Enviar mensaje de login exitoso con el usuario autenticado
                            WeakReferenceMessenger.Default.Send(new UserLoggedInMessage(result.CurrentUserInfo));
                        }
                    }
                    LoginSuccessful?.Invoke();
                }
                else
                {
                    _logger.LogWarning("Login fallido para usuario: {Username}. Error: {Error}", Username, result.ErrorMessage);
                    ShowError(result.ErrorMessage);
                    StatusMessage = "Error en el login";
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Login cancelado por el usuario: {Username}", Username);
                StatusMessage = "Login cancelado";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante el login para usuario: {Username}", Username);
                ShowError("Error interno del sistema. Por favor, contacte al administrador.");
                StatusMessage = "Error del sistema";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task CerrarSesionAsync()
        {
            await _authenticationService.LogoutAsync();
            _currentUserService.ClearCurrentUser();
            Username = string.Empty;
            Password = string.Empty;
            RememberMe = false;
            StatusMessage = "Ingrese sus credenciales para acceder";
            ErrorMessage = string.Empty;
            HasError = false;
        }

        [RelayCommand]
        private void ClearFields()
        {
            Username = string.Empty;
            Password = string.Empty;
            RememberMe = false;
            ClearError();
            StatusMessage = "Ingrese sus credenciales para acceder";
            

        }        [RelayCommand]
        private void ForgotPassword()
        {
            _logger.LogInformation("Solicitud de recuperación de contraseña. Mostrando modal de recuperación.");
            ShowForgotPasswordModal?.Invoke();
        }

        /// <summary>
        /// Evento que se dispara cuando el login es exitoso
        /// </summary>
        public event Action? LoginSuccessful;        private bool ValidateInput()
        {

            
            if (string.IsNullOrWhiteSpace(Username))
            {
                _logger.LogWarning("Validación falló: Usuario vacío o nulo");
                ShowError("El nombre de usuario es obligatorio");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                _logger.LogWarning("Validación falló: Contraseña vacía o nula");
                ShowError("La contraseña es obligatoria");
                return false;
            }

            if (Username.Length < 3)
            {
                _logger.LogWarning("Validación falló: Usuario muy corto. Longitud: {Length}", Username.Length);
                ShowError("El nombre de usuario debe tener al menos 3 caracteres");
                return false;
            }

            if (Password.Length < 4)
            {
                _logger.LogWarning("Validación falló: Contraseña muy corta. Longitud: {Length}", Password.Length);
                ShowError("La contraseña debe tener al menos 4 caracteres");
                return false;
            }


            return true;
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;

        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }

        /// <summary>
        /// Método para establecer el foco en el campo de usuario (llamado desde la vista)
        /// </summary>
        public void SetFocusToUsername()
        {
            // Este método puede ser llamado desde la vista para establecer el foco inicial

        }

        /// <summary>
        /// Método para manejar la tecla Enter en los campos de texto
        /// </summary>
        public void HandleEnterKey()
        {
            if (!IsLoading && LoginCommand.CanExecute(null))
            {
                _ = LoginAsync();
            }
        }        /// <summary>
        /// Método para actualizar la contraseña desde el código behind y disparar validaciones
        /// </summary>
        public void UpdatePassword(string password)
        {
            Password = password;
            
            // Limpiar error específicamente si la contraseña ahora es válida
            if (!string.IsNullOrWhiteSpace(password) && password.Length >= 4)
            {
                // Solo limpiar si el error actual es sobre la contraseña
                if (HasError && (ErrorMessage?.Contains("contraseña") == true || ErrorMessage?.Contains("password") == true || 
                               ErrorMessage?.Contains("obligatoria") == true || ErrorMessage?.Contains("requerida") == true))
                {
                    ClearError();
                    StatusMessage = "Ingrese sus credenciales para acceder";
                }
            }
        }/// <summary>
        /// Método para limpiar errores cuando el usuario está escribiendo
        /// </summary>
        public void UpdateUsername(string username)
        {
            Username = username;
            
            // Limpiar error específicamente si el usuario ahora es válido
            if (!string.IsNullOrWhiteSpace(username) && username.Length >= 3)
            {
                // Solo limpiar si el error actual es sobre el usuario
                if (HasError && (ErrorMessage.Contains("usuario") || ErrorMessage.Contains("username") || 
                               ErrorMessage.Contains("obligatorio") || ErrorMessage.Contains("requerido")))
                {
                    ClearError();
                    StatusMessage = "Ingrese sus credenciales para acceder";
                }
            }
  
        }
    }
}
