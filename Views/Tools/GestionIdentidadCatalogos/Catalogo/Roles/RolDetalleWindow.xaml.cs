using System.Windows;
using System.Collections.ObjectModel;
using GestLog.Modules.Usuarios.Models;
using Modules.Usuarios.ViewModels;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Roles
{
    public partial class RolDetalleWindow : Window
    {
        public RolDetalleWindow(Rol rol, ObservableCollection<RolManagementViewModel.PermisosModuloGroup> permisosPorModulo)
        {
            InitializeComponent();
            DataContext = new RolDetalleViewModel(rol, permisosPorModulo);
        }
    }    public class RolDetalleViewModel
    {
        public string Nombre { get; }
        public string Descripcion { get; }
        public ObservableCollection<ModuloInfo> PermisosPorModulo { get; }        public RolDetalleViewModel(Rol rol, ObservableCollection<RolManagementViewModel.PermisosModuloGroup> permisosPorModulo)
        {
            Nombre = rol.Nombre;
            Descripcion = rol.Descripcion;
            
            // Convertir a estructura más simple para debugging
            PermisosPorModulo = new ObservableCollection<ModuloInfo>();
            foreach (var grupo in permisosPorModulo)
            {
                var modulo = new ModuloInfo
                {
                    Modulo = grupo.Modulo,
                    Permisos = new ObservableCollection<PermisoInfo>()
                };
                
                foreach (var permiso in grupo.Permisos)
                {
                    modulo.Permisos.Add(new PermisoInfo 
                    { 
                        Nombre = permiso.Nombre,
                        Descripcion = permiso.Descripcion 
                    });
                }
                
                PermisosPorModulo.Add(modulo);
            }
        }
    }

    public class ModuloInfo
    {
        public string Modulo { get; set; } = string.Empty;
        public ObservableCollection<PermisoInfo> Permisos { get; set; } = new();
    }

    public class PermisoInfo
    {
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
