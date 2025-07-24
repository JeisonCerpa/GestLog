using System.Windows;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    public partial class CatalogosManagementView : System.Windows.Controls.UserControl
    {
        public CatalogosManagementView()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as System.Windows.FrameworkElement;
            if (fe?.DataContext is CatalogosManagementViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}
