using System.Windows;
using System.Windows.Controls;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Cargos
{
    public partial class CargosView : System.Windows.Controls.UserControl
    {
        public CargosView()
        {
            InitializeComponent();
        }

        private void MenuButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void EliminarCargo_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var cargo = (sender as MenuItem)?.CommandParameter;
            if (cargo != null)
            {
                var menuItem = sender as System.Windows.Controls.MenuItem;
                if (menuItem?.Command != null && menuItem.Command.CanExecute(cargo))
                {
                    menuItem.Command.Execute(cargo);
                }
            }
        }

    }
}


