using System.Windows;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Views.Usuarios
{
    public partial class CargoModalWindow : Window
    {
        public CargoModalWindow(CatalogosManagementViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Owner = System.Windows.Application.Current.MainWindow;
            vm.SolicitarCerrarModal += () => Dispatcher.Invoke(Close);
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
    }
}
