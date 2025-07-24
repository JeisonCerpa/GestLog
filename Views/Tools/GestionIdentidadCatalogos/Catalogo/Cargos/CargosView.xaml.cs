using System.Windows;
using System.Windows.Controls;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Cargos
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

        private void EliminarCargoDirecto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var cargo = button?.Tag as GestLog.Modules.Usuarios.Models.Cargo;
            if (cargo != null)
            {
                var vm = this.DataContext as GestLog.Modules.Usuarios.ViewModels.CatalogosManagementViewModel;
                if (vm != null && vm.EliminarCargoCommand.CanExecute(cargo))
                {
                    vm.EliminarCargoCommand.Execute(cargo);
                }
            }
        }
    }
}

