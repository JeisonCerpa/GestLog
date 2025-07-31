using System.Windows;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario
{
    public partial class RestablecerContrasenaWindow : Window
    {        public string? NuevaContrasena { get; private set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public RestablecerContrasenaWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Agregar eventos para validación en tiempo real
            passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
            confirmPasswordBox.PasswordChanged += ConfirmPasswordBox_PasswordChanged;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidarFormulario();
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidarFormulario();
        }

        private void ValidarFormulario()
        {
            // Ocultar error si ambos campos están vacíos (estado inicial)
            if (string.IsNullOrEmpty(passwordBox.Password) && string.IsNullOrEmpty(confirmPasswordBox.Password))
            {
                OcultarError();
                return;
            }

            // Validar si hay contenido en confirmPassword pero no coincide
            if (!string.IsNullOrEmpty(confirmPasswordBox.Password) && 
                passwordBox.Password != confirmPasswordBox.Password)
            {
                MostrarError("Las contraseñas no coinciden");
                return;
            }

            // Validar longitud mínima si hay contenido
            if (!string.IsNullOrEmpty(passwordBox.Password) && passwordBox.Password.Length < 6)
            {
                MostrarError("La contraseña debe tener al menos 6 caracteres");
                return;
            }

            // Todo válido o campos vacíos
            OcultarError();
        }

        private void RestablecerButton_Click(object sender, RoutedEventArgs e)
        {
            // Validar que las contraseñas no estén vacías
            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                MostrarError("La contraseña no puede estar vacía");
                passwordBox.Focus();
                return;
            }

            // Validar longitud mínima
            if (passwordBox.Password.Length < 6)
            {
                MostrarError("La contraseña debe tener al menos 6 caracteres");
                passwordBox.Focus();
                return;
            }

            // Validar que las contraseñas coincidan
            if (passwordBox.Password != confirmPasswordBox.Password)
            {
                MostrarError("Las contraseñas no coinciden");
                confirmPasswordBox.Focus();
                return;
            }

            // Todo válido
            NuevaContrasena = passwordBox.Password;
            DialogResult = true;
            Close();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            txtError.Visibility = Visibility.Visible;
        }

        private void OcultarError()
        {
            txtError.Visibility = Visibility.Collapsed;
        }
    }
}
