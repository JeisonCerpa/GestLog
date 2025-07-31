using System.Windows;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.ViewModels;
using System.Linq;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Usuario
{    public partial class UsuarioRegistroWindow : Window
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
