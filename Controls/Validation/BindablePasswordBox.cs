using System.Windows;
using System.Windows.Controls;

namespace GestLog.Controls.Validation
{
    public class BindablePasswordBox : System.Windows.Controls.Control
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.Register(
                nameof(Password),
                typeof(string),
                typeof(BindablePasswordBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordPropertyChanged));

        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        private PasswordBox? _passwordBox;

        public BindablePasswordBox()
        {
            DefaultStyleKey = typeof(BindablePasswordBox);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _passwordBox = GetTemplateChild("PART_PasswordBox") as PasswordBox;
            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                _passwordBox.Password = Password;
            }
        }

        private static void OnPasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindablePasswordBox bindable && bindable._passwordBox != null)
            {
                var newPassword = (string)e.NewValue ?? string.Empty;
                if (bindable._passwordBox.Password != newPassword)
                    bindable._passwordBox.Password = newPassword;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_passwordBox != null && Password != _passwordBox.Password)
                Password = _passwordBox.Password;
        }
    }
}
