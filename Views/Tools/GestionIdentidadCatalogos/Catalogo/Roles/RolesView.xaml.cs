using System.Windows.Controls;
using Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Roles
{    public partial class RolesView : System.Windows.Controls.UserControl
    {        public RolesView()
        {
            InitializeComponent();
            
            // El DataContext se asigna desde IdentidadCatalogosHomeViewModel
            // Cargar roles al inicializar la vista
            this.Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("RolesView Loaded - Buscando ViewModel en DataContext");
                if (DataContext is RolManagementViewModel viewModel)
                {
                    System.Diagnostics.Debug.WriteLine("RolesView Loaded - ViewModel encontrado, ejecutando BuscarRolesCommand");
                    if (viewModel.BuscarRolesCommand.CanExecute(null))
                    {
                        viewModel.BuscarRolesCommand.Execute(null);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"RolesView Loaded - DataContext no es RolManagementViewModel: {DataContext?.GetType()}");
                }
            };
        }
    }
}

