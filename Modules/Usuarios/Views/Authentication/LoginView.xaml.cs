using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.Usuarios.Views.Authentication
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

            Loaded += LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
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

        private void UsernameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                var usernameValidation = this.FindName("UsernameValidation") as System.Windows.Controls.TextBlock;
                if (usernameValidation != null)
                {
                    bool isValid = !string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text.Length >= 3;
                    usernameValidation.Visibility = isValid ? Visibility.Visible : Visibility.Collapsed;

                    usernameValidation.Text = isValid ? "✓" : "✗";
                    usernameValidation.Foreground = isValid
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 40, 167, 69))
                        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 220, 53, 69));
                }
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is System.Windows.Controls.PasswordBox pb)
            {
                _viewModel.Password = pb.Password;

                var placeholder = this.FindName("PasswordPlaceholder") as System.Windows.Controls.TextBlock;
                if (placeholder != null)
                {
                    placeholder.Visibility = string.IsNullOrEmpty(pb.Password)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                DetectCapsLock();
            }
        }

        private void DetectCapsLock()
        {
            var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
            var capsLockIndicator = this.FindName("CapsLockIndicator") as System.Windows.Controls.TextBlock;

            if (passwordBox != null && capsLockIndicator != null)
            {
                if (passwordBox.IsFocused && !string.IsNullOrEmpty(passwordBox.Password))
                {
                    bool capsLockOn = (Keyboard.GetKeyStates(Key.CapsLock) & KeyStates.Toggled) == KeyStates.Toggled;
                    capsLockIndicator.Visibility = capsLockOn ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    capsLockIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void PasswordBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DetectCapsLock();
        }

        private void PasswordBox_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            DetectCapsLock();
        }

        private void ClearPasswordBox()
        {
            var passwordBox = this.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.Clear();

                var placeholder = this.FindName("PasswordPlaceholder") as System.Windows.Controls.TextBlock;
                if (placeholder != null)
                {
                    placeholder.Visibility = Visibility.Visible;
                }

                var capsLockIndicator = this.FindName("CapsLockIndicator") as System.Windows.Controls.TextBlock;
                if (capsLockIndicator != null)
                {
                    capsLockIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }

        public void TriggerErrorAnimation()
        {
            var errorBorder = this.FindName("ErrorBorder") as System.Windows.Controls.Border;
            if (errorBorder != null)
            {
                var shakeStoryboard = this.FindResource("ShakeAnimation") as Storyboard;
                if (shakeStoryboard != null)
                {
                    shakeStoryboard.Begin(errorBorder);
                }
            }
        }

        private void OnShowPasswordChangeModal()
        {
            try
            {
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

                changePasswordVM.IsFirstLogin = true;

                var changePasswordView = new GestLog.Modules.Usuarios.Views.Usuarios.ChangePasswordModalView()
                {
                    DataContext = changePasswordVM
                };

                changePasswordView.ConfigurarParaVentanaPadre(Window.GetWindow(this));
                changePasswordVM.PasswordChangeSuccessful += () =>
                {
                    changePasswordView.DialogResult = true;
                    changePasswordView.Close();
                };

                changePasswordVM.PasswordChangeCanceled += () =>
                {
                    changePasswordView.DialogResult = false;
                    changePasswordView.Close();
                };

                var result = changePasswordView.ShowDialog();
                if (result == true)
                {
                    System.Windows.MessageBox.Show("Contraseña actualizada exitosamente.\n\nPor favor, ingrese nuevamente con su nueva contraseña.",
                        "Cambio Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                    _viewModel?.ClearFieldsCommand.Execute(null);
                    ClearPasswordBox();
                }
                else
                {
                    _viewModel?.ClearFieldsCommand.Execute(null);
                    ClearPasswordBox();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al mostrar modal de cambio de contraseña: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnShowForgotPasswordModal()
        {
            try
            {
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

                var forgotPasswordView = new GestLog.Modules.Usuarios.Views.Usuarios.ForgotPasswordModalView()
                {
                    DataContext = forgotPasswordVM
                };

                var parentWindow = Window.GetWindow(this);
                forgotPasswordView.ConfigurarParaVentanaPadre(parentWindow);

                forgotPasswordVM.PasswordResetRequested += () =>
                {
                    forgotPasswordView.Close();
                };

                forgotPasswordVM.OperationCanceled += () =>
                {
                    forgotPasswordView.Close();
                };

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
