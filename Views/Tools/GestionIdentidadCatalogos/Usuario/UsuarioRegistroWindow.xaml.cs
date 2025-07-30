using System.Windows;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario
{
    public partial class UsuarioRegistroWindow : Window
    {
        public UsuarioRegistroWindow()
        {
            InitializeComponent();
        }

        private void PasswordBoxNuevoUsuario_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox pb)
            {
                pb.DataContext = this.DataContext;
            }
        }
    }
}
