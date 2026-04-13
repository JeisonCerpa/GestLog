using System.Windows;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.ViewModels;
using System.Linq;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Usuario
{    public partial class UsuarioRegistroWindow : Window
    {
        public UsuarioRegistroWindow()
        {
            InitializeComponent();
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void PasswordBoxNuevoUsuario_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox pb)
            {
                pb.DataContext = this.DataContext;
            }
        }

        public void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Rol rol && DataContext is UsuarioManagementViewModel vm)
            {
                if (!vm.RolesSeleccionados.Any(r => r.IdRol == rol.IdRol))
                {
                    vm.RolesSeleccionados.Add(rol);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Rol agregado: {rol.Nombre}. Total roles: {vm.RolesSeleccionados.Count}");
                }
            }
        }

        public void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox checkBox && checkBox.DataContext is Rol rol && DataContext is UsuarioManagementViewModel vm)
            {
                var toRemove = vm.RolesSeleccionados.FirstOrDefault(r => r.IdRol == rol.IdRol);
                if (toRemove != null)
                {
                    vm.RolesSeleccionados.Remove(toRemove);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] Rol removido: {rol.Nombre}. Total roles: {vm.RolesSeleccionados.Count}");
                }
            }
        }
    }
}

