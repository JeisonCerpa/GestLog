using System.Windows.Controls;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario
{
    public partial class UsuarioManagementView : System.Windows.Controls.UserControl
    {
        public UsuarioManagementView()
        {
            InitializeComponent();
            // El DataContext se asigna automáticamente por DI al crear la vista desde el HomeViewModel o MainWindow.
            // No es necesario resolver ni asignar el ViewModel aquí.
        }
    }
}

