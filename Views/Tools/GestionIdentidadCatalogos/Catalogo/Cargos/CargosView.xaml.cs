using System.Windows;
using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Cargos
{
    public partial class CargosView : System.Windows.Controls.UserControl
    {
        public CargosView()
        {
            InitializeComponent();
            this.Loaded += CargosView_Loaded;
        }

        private void CargosView_Loaded(object sender, RoutedEventArgs e)
        {
            // Si el padre tiene el ViewModel, lo reenviamos como DataContext
            if (this.DataContext == null && this.Parent is FrameworkElement parent && parent.DataContext is CatalogosManagementViewModel vm)
            {
                this.DataContext = vm;
            }
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
                var result = System.Windows.MessageBox.Show(
                    "¿Está seguro que desea eliminar este cargo?",
                    "Confirmar eliminación",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    var menuItem = sender as System.Windows.Controls.MenuItem;
                    if (menuItem?.Command != null && menuItem.Command.CanExecute(cargo))
                    {
                        menuItem.Command.Execute(cargo);
                    }
                }
            }
        }

        private void EliminarCargoDirecto_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            var cargo = button?.Tag as GestLog.Modules.Usuarios.Models.Cargo;
            if (cargo != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"¿Está seguro que desea eliminar el cargo '{cargo.Nombre}'?",
                    "Confirmar eliminación",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (result == System.Windows.MessageBoxResult.Yes)
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
}

