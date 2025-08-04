using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.ViewModels;
using Modules.Usuarios.Interfaces;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Roles
{
    public partial class RolesView : System.Windows.Controls.UserControl
    {
        public RolesView()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (DataContext is RolManagementViewModel viewModel)
                {
                    if (viewModel.BuscarRolesCommand.CanExecute(null))
                        viewModel.BuscarRolesCommand.Execute(null);
                }
            };
        }        private async void BtnVerRol_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is Rol rol)
            {
                try
                {
                    // Obtener servicios directamente
                    var serviceProvider = LoggingService.GetServiceProvider();
                    var rolService = serviceProvider.GetService<IRolService>();
                    
                    if (rolService == null)
                    {
                        System.Windows.MessageBox.Show("No se pudo obtener el servicio de roles.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }                    // Cargar permisos del rol directamente
                    var permisos = await rolService.ObtenerPermisosDeRolAsync(rol.IdRol);
                    
                    // Agrupar por módulo
                    var permisosPorModulo = new ObservableCollection<RolManagementViewModel.PermisosModuloGroup>();
                    var grupos = permisos.GroupBy(p => p.Modulo);
                    
                    foreach (var grupo in grupos)
                    {
                        var moduloGroup = new RolManagementViewModel.PermisosModuloGroup
                        {
                            Modulo = grupo.Key,
                            Permisos = new ObservableCollection<Permiso>(grupo)
                        };
                        permisosPorModulo.Add(moduloGroup);
                    }
                    
                    var window = new RolDetalleWindow(rol, permisosPorModulo);
                    window.Owner = System.Windows.Application.Current.MainWindow;
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error al abrir detalles del rol: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}

