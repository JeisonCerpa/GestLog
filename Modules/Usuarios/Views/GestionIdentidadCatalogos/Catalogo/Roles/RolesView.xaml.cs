using System;
using System.Windows;
using Modules.Usuarios.ViewModels;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Catalogo.Roles
{
    public partial class RolesView : System.Windows.Controls.UserControl
    {
        public RolesView()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (DataContext is RolManagementViewModel viewModel)
                {
                    if (viewModel.BuscarRolesCommand.CanExecute(null))
                        viewModel.BuscarRolesCommand.Execute(null);
                }
            };
        }

        private void BtnNuevoRol_Click(object sender, RoutedEventArgs e)
        {
            AbrirFormularioRol(rol: null);
        }

        private void BtnEditarRol_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is RolManagementViewModel viewModel && viewModel.RolSeleccionado != null)
                AbrirFormularioRol(viewModel.RolSeleccionado);
        }

        private void AbrirFormularioRol(GestLog.Modules.Usuarios.Models.Rol? rol)
        {
            try
            {
                var window = new RolFormWindow(rol);
                if (window.ShowDialog() == true && DataContext is RolManagementViewModel viewModel)
                {
                    if (viewModel.BuscarRolesCommand.CanExecute(null))
                        viewModel.BuscarRolesCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al abrir el formulario de rol: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
