using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;
using GestLog.Services.Core.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Catalogo.Permisos
{
    /// <summary>
    /// Vista para gestionar la asignación de permisos a roles de forma visual
    /// </summary>
    public partial class GestionPermisosRolView : System.Windows.Controls.UserControl
    {
        public GestionPermisosRolView()
        {
            InitializeComponent();
            
            try
            {
                var serviceProvider = LoggingService.GetServiceProvider();
                var viewModel = serviceProvider.GetRequiredService<GestionPermisosRolViewModel>();
                DataContext = viewModel;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inicializando GestionPermisosRolView: {ex.Message}");
                // Fallback: crear una instancia básica para evitar errores de diseño
                DataContext = new GestionPermisosRolViewModel(null!, null!, null!);
            }
        }
    }
}
