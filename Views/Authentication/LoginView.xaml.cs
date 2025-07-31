using System;
using System.Windows.Controls;
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
                _viewModel.LoginSuccessful += OnLoginSuccessful;
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

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_viewModel != null && sender is PasswordBox pb)
            {
                _viewModel.Password = pb.Password;
            }
        }
    }
}
