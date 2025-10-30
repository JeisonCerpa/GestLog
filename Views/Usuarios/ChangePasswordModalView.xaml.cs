using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    /// <summary>
    /// Vista modal para cambio de contraseña obligatorio en primer login
    /// </summary>
    public partial class ChangePasswordModalView : System.Windows.Controls.UserControl
    {
        public ChangePasswordModalView()
        {
            InitializeComponent();
            
            // Cuando se establezca el DataContext, establecer la referencia de la vista en el ViewModel
            this.DataContextChanged += (s, e) =>
            {
                if (this.DataContext is ChangePasswordViewModel viewModel)
                {
                    viewModel.SetView(this);
                }
            };
        }

        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Permitir cerrar con ESC si el ViewModel lo permite
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Obtiene la contraseña actual del PasswordBox
        /// (Se accede desde el ViewModel mediante Binding si es necesario)
        /// </summary>
        public string GetCurrentPassword()
        {
            return CurrentPasswordBox.Password;
        }

        /// <summary>
        /// Obtiene la nueva contraseña del PasswordBox
        /// </summary>
        public string GetNewPassword()
        {
            return NewPasswordBox.Password;
        }

        /// <summary>
        /// Obtiene la confirmación de contraseña del PasswordBox
        /// </summary>
        public string GetConfirmPassword()
        {
            return ConfirmPasswordBox.Password;
        }

        /// <summary>
        /// Limpia los PasswordBox después de un cambio exitoso
        /// </summary>
        public void ClearPasswords()
        {
            CurrentPasswordBox.Clear();
            NewPasswordBox.Clear();
            ConfirmPasswordBox.Clear();
        }
    }
}
