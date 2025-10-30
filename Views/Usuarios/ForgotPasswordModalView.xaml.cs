using System.Windows;
using System.Windows.Controls;

namespace GestLog.Views.Usuarios
{
    /// <summary>
    /// Vista modal para recuperación de contraseña olvidada
    /// </summary>
    public partial class ForgotPasswordModalView : System.Windows.Controls.UserControl
    {
        public ForgotPasswordModalView()
        {
            InitializeComponent();
        }

        private void Grid_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Permitir cerrar con ESC
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
            }
        }
    }
}
