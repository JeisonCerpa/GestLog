using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;
using System.Windows;

namespace GestLog.Views.IdentidadCatalogos.Personas
{
    public partial class PersonaManagementView : System.Windows.Controls.UserControl
    {
        public PersonaManagementView()
        {
            InitializeComponent();
            // DataContext se debe asignar desde el contenedor principal (MainWindow) o XAML si usas DI
        }
    }
}
