using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;

namespace GestLog.Views.Authentication
{
    public partial class LoginView : System.Windows.Controls.UserControl
    {
        private LoginViewModel? _viewModel;
        public event EventHandler? LoginSuccessful;
          public LoginView()
        {
            InitializeComponent();
            InitializeViewModel();
            if (_viewModel != null)
            {
                _viewModel.LoginSuccessful += OnLoginSuccessful;
                _viewModel.ShowPasswordChangeModal += OnShowPasswordChangeModal;
                _viewModel.ShowForgotPasswordModal += OnShowForgotPasswordModal;
            }

            // Suscribirse a eventos para detección de Caps Lock
            Loaded += LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            // Obtener referencias a los controles
            var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.PreviewKeyUp += PasswordBox_PreviewKeyUp;
                passwordBox.PreviewKeyDown += PasswordBox_PreviewKeyDown;
            }
        }

        private void InitializeViewModel()
        {
            var serviceProvider = LoggingService.GetServiceProvider();
            _viewModel = serviceProvider.GetService(typeof(LoginViewModel)) as LoginViewModel;
            DataContext = _viewModel;
        }

        private void OnLoginSuccessful()
        {
            LoginSuccessful?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Valida el usuario en tiempo real y muestra indicador visual (✓)
        /// </summary>
        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                var usernameValidation = this.FindName("UsernameValidation") as System.Windows.Controls.TextBlock;
                if (usernameValidation != null)
                {
                    // Mostrar indicador de validación si el username tiene al menos 3 caracteres
                    bool isValid = !string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text.Length >= 3;
                    usernameValidation.Visibility = isValid ? Visibility.Visible : Visibility.Collapsed;
                    
                    // Cambiar texto e icono según validez
                    usernameValidation.Text = isValid ? "✓" : "✗";
                    usernameValidation.Foreground = isValid 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 40, 167, 69))    // Verde: #28A745
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 220, 53, 69));    // Rojo: #DC3545
                }
            }
        }

        /// <summary>
        /// Maneja cambios en el PasswordBox: actualiza placeholder y detecta Caps Lock
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is System.Windows.Controls.PasswordBox pb)
            {
                _viewModel.Password = pb.Password;
                
                // Ocultar/mostrar placeholder
                var placeholder = this.FindName("PasswordPlaceholder") as System.Windows.Controls.TextBlock;
                if (placeholder != null)
                {
                    placeholder.Visibility = string.IsNullOrEmpty(pb.Password) 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }

                // Detectar Caps Lock
                DetectCapsLock();
            }
        }

        /// <summary>
        /// Detecta si Caps Lock está activado cuando el PasswordBox tiene foco
        /// </summary>
        private void DetectCapsLock()
        {
            var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
            var capsLockIndicator = this.FindName("CapsLockIndicator") as System.Windows.Controls.TextBlock;

            if (passwordBox != null && capsLockIndicator != null)
            {
                if (passwordBox.IsFocused && !string.IsNullOrEmpty(passwordBox.Password))
                {
                    // Usar KeyboardDevice.GetKeyStates para detectar Caps Lock
                    bool capsLockOn = (Keyboard.GetKeyStates(Key.CapsLock) & KeyStates.Toggled) == KeyStates.Toggled;
                    capsLockIndicator.Visibility = capsLockOn ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    capsLockIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Detecta Caps Lock cuando se presiona una tecla
        /// </summary>
        private void PasswordBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DetectCapsLock();
        }        /// <summary>
        /// Detecta Caps Lock cuando se libera una tecla
        /// </summary>
        private void PasswordBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DetectCapsLock();
        }

        /// <summary>
        /// Limpia el PasswordBox visualmente cuando el ViewModel limpia la contraseña
        /// </summary>
        private void ClearPasswordBox()
        {
            var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.Clear();
                
                // Mostrar placeholder nuevamente
                var placeholder = this.FindName("PasswordPlaceholder") as System.Windows.Controls.TextBlock;
                if (placeholder != null)
                {
                    placeholder.Visibility = Visibility.Visible;
                }
                
                // Ocultar indicador de Caps Lock
                var capsLockIndicator = this.FindName("CapsLockIndicator") as System.Windows.Controls.TextBlock;
                if (capsLockIndicator != null)
                {
                    capsLockIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Anima el error con un efecto shake
        /// </summary>
        public void TriggerErrorAnimation()
        {
            var errorBorder = this.FindName("ErrorBorder") as System.Windows.Controls.Border;
            if (errorBorder != null)
            {
                var shakeStoryboard = this.FindResource("ShakeAnimation") as Storyboard;
                if (shakeStoryboard != null)
                {
                    // Crear una copia para poder reproducirla múltiples veces
                    shakeStoryboard.Begin(errorBorder);
                }
            }
        }        /// <summary>
        /// Maneja la solicitud de mostrar la modal de cambio de contraseña obligatorio
        /// </summary>
        private void OnShowPasswordChangeModal()
        {
            try
            {
                // Obtener el ChangePasswordViewModel del ServiceProvider
                var serviceProvider = LoggingService.GetServiceProvider();
                
                if (serviceProvider == null)
                {
                    System.Windows.MessageBox.Show("Error: No se pudo obtener el ServiceProvider.", 
                        "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ChangePasswordViewModel? changePasswordVM = null;
                try
                {
                    changePasswordVM = serviceProvider.GetService(typeof(ChangePasswordViewModel)) as ChangePasswordViewModel;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al resolver ChangePasswordViewModel: {ex.Message}\n\n{ex.InnerException?.Message}", 
                        "Error de Resolución", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (changePasswordVM == null)
                {
                    System.Windows.MessageBox.Show("Error: ChangePasswordViewModel es nulo. Verifique que esté registrado en DI.", 
                        "Error de Inyección de Dependencias", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Establecer IsFirstLogin en true
                changePasswordVM.IsFirstLogin = true;

                // Crear la vista modal (ChangePasswordModalView es un Window)
                var changePasswordView = new GestLog.Views.Usuarios.ChangePasswordModalView()
                {
                    DataContext = changePasswordVM
                };

                // Configurar la vista modal para que se muestre como diálogo
                changePasswordView.ConfigurarParaVentanaPadre(Window.GetWindow(this));                // Suscribirse a eventos de éxito y cancelación
                changePasswordVM.PasswordChangeSuccessful += () =>
                {
                    changePasswordView.DialogResult = true;
                    changePasswordView.Close();
                };

                changePasswordVM.PasswordChangeCanceled += () =>
                {                changePasswordView.DialogResult = false;
                    changePasswordView.Close();
                };

                // Mostrar la modal como diálogo
                var result = changePasswordView.ShowDialog();                // DESPUÉS de que la modal cierre completamente, manejar el resultado
                if (result == true)
                {
                    // Cambio exitoso - Mostrar mensaje y cerrar sesión
                    System.Windows.MessageBox.Show("Contraseña actualizada exitosamente.\n\nPor favor, ingrese nuevamente con su nueva contraseña.",
                        "Cambio Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel?.ClearFieldsCommand.Execute(null);
                    ClearPasswordBox();
                }
                else
                {
                    // Cambio cancelado o no completado - Cerrar sesión
                    _viewModel?.ClearFieldsCommand.Execute(null);
                    ClearPasswordBox();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al mostrar modal de cambio de contraseña: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }/// <summary>
        /// Maneja la solicitud de mostrar la modal de recuperación de contraseña
        /// </summary>
        private void OnShowForgotPasswordModal()
        {
            try
            {
                // Obtener el ForgotPasswordViewModel del ServiceProvider
                var serviceProvider = LoggingService.GetServiceProvider();
                
                if (serviceProvider == null)
                {
                    System.Windows.MessageBox.Show("Error: No se pudo obtener el ServiceProvider.", 
                        "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ForgotPasswordViewModel? forgotPasswordVM = null;
                try
                {
                    forgotPasswordVM = serviceProvider.GetService(typeof(ForgotPasswordViewModel)) as ForgotPasswordViewModel;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al resolver ForgotPasswordViewModel: {ex.Message}\n\n{ex.InnerException?.Message}", 
                        "Error de Resolución", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (forgotPasswordVM == null)
                {
                    System.Windows.MessageBox.Show("Error: ForgotPasswordViewModel es nulo. Verifique que esté registrado en DI.", 
                        "Error de Inyección de Dependencias", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Obtener la vista modal
                var forgotPasswordView = new GestLog.Views.Usuarios.ForgotPasswordModalView()
                {
                    DataContext = forgotPasswordVM
                };

                // Configurar para ventana padre
                var parentWindow = Window.GetWindow(this);
                forgotPasswordView.ConfigurarParaVentanaPadre(parentWindow);

                // Suscribirse a eventos de cierre
                forgotPasswordVM.PasswordResetRequested += () =>
                {
                    forgotPasswordView.Close();
                };

                forgotPasswordVM.OperationCanceled += () =>
                {
                    forgotPasswordView.Close();                };

                // Mostrar como modal dialog
                forgotPasswordView.ShowDialog();
                ClearPasswordBox();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al mostrar modal de recuperación de contraseña: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
