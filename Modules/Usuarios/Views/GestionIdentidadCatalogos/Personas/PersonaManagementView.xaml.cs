using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GestLog.Modules.Usuarios.ViewModels;
using System.Windows;
using WpfMessageBox = System.Windows.MessageBox;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Personas
{
    public partial class PersonaManagementView : System.Windows.Controls.UserControl
    {
        public PersonaManagementView()
        {
            InitializeComponent();
            // DataContext se debe asignar desde el contenedor principal (MainWindow) o XAML si usas DI
        }

        public void OverflowActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Popup popup)
            {
                popup.IsOpen = true;
            }
        }

        public void OverflowEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GestLog.Modules.Personas.Models.Persona persona)
            {
                if (DataContext is PersonaManagementViewModel vm && vm.EditarPersonaCommand.CanExecute(persona))
                {
                    vm.EditarPersonaCommand.Execute(persona);
                }
            }

            CerrarOverflow(sender);
        }

        public void OverflowCrearUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GestLog.Modules.Personas.Models.Persona persona)
            {
                if (DataContext is PersonaManagementViewModel vm && vm.CrearUsuarioParaPersonaCommand.CanExecute(persona))
                {
                    vm.CrearUsuarioParaPersonaCommand.Execute(persona);
                }
            }

            CerrarOverflow(sender);
        }

        public void OverflowDesactivar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GestLog.Modules.Personas.Models.Persona persona)
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

            CerrarOverflow(sender);
        }

        private static void CerrarOverflow(object sender)
        {
            if (sender is FrameworkElement element && element.Tag is Popup popup)
            {
                popup.IsOpen = false;
            }
        }

    }
}

