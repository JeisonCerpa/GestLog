using System.Windows;
using Modules.Usuarios.ViewModels;
using GestLog.Modules.Usuarios.Models;
using System.Linq;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario
{
    public partial class UsuarioEdicionWindow : Window
    {
        public UsuarioEdicionWindow()
        {
            InitializeComponent();
        }

        private void CancelarButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }        private void GuardarButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsuarioManagementViewModel viewModel && viewModel.RolesSeleccionados.Count == 0)
            {
                System.Windows.MessageBox.Show("Debe seleccionar al menos un rol para el usuario.", "Validación", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }        private void CheckBox_Checked_Edit(object sender, RoutedEventArgs e)
        {
            // El ViewModel maneja la lógica de agregar roles
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Rol rol)
            {
                if (DataContext is UsuarioManagementViewModel viewModel)
                {
                    if (!viewModel.RolesSeleccionados.Any(r => r.IdRol == rol.IdRol))
                    {
                        viewModel.RolesSeleccionados.Add(rol);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Rol agregado (edición): {rol.Nombre}. Total roles: {viewModel.RolesSeleccionados.Count}");
                    }
                }
            }
        }        private void CheckBox_Unchecked_Edit(object sender, RoutedEventArgs e)
        {
            // El ViewModel maneja la lógica de remover roles
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Rol rol)
            {
                if (DataContext is UsuarioManagementViewModel viewModel)
                {
                    var toRemove = viewModel.RolesSeleccionados.FirstOrDefault(r => r.IdRol == rol.IdRol);
                    if (toRemove != null)
                    {
                        viewModel.RolesSeleccionados.Remove(toRemove);
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] Rol removido (edición): {rol.Nombre}. Total roles: {viewModel.RolesSeleccionados.Count}");
                    }
                }
            }
        }
    }
}
