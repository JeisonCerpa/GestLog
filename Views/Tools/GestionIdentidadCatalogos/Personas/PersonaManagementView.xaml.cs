using System.Windows.Controls;
using GestLog.Modules.Usuarios.ViewModels;
using System.Windows;
using WpfButton = System.Windows.Controls.Button;
using WpfMessageBox = System.Windows.MessageBox;

namespace GestLog.Views.Tools.GestionIdentidadCatalogos.Personas
{
    public partial class PersonaManagementView : System.Windows.Controls.UserControl
    {
        public PersonaManagementView()
        {
            InitializeComponent();
            // DataContext se debe asignar desde el contenedor principal (MainWindow) o XAML si usas DI
        }

        public void DesactivarPersona_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton btn && btn.DataContext is GestLog.Modules.Personas.Models.Persona persona)
            {
                var result = WpfMessageBox.Show($"¿Está seguro que desea desactivar a {persona.NombreCompleto}?\nEsta acción no elimina la persona, solo la desactiva.",
                    "Confirmar desactivación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    if (DataContext is PersonaManagementViewModel vm && vm.ActivarDesactivarPersonaCommand.CanExecute(persona))
                    {
                        vm.ActivarDesactivarPersonaCommand.Execute(persona);
                    }
                }
            }
        }

        public void EliminarPersona_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton btn && btn.DataContext is GestLog.Modules.Personas.Models.Persona persona)
            {
                var result = WpfMessageBox.Show($"¿Está seguro que desea eliminar a {persona.NombreCompleto}?\nEsta acción desactivará la persona, no la eliminará físicamente.",
                    "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    if (DataContext is PersonaManagementViewModel vm && vm.ActivarDesactivarPersonaCommand.CanExecute(persona))
                    {
                        vm.ActivarDesactivarPersonaCommand.Execute(persona);
                    }
                }
            }
        }
    }
}
