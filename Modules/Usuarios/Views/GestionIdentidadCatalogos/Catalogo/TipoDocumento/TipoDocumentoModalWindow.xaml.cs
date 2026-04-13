using System.Linq;
using System.Windows;
using GestLog.Modules.Usuarios.ViewModels;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos
{
    public partial class TipoDocumentoModalWindow : Window
    {
        public TipoDocumentoModalWindow(CatalogosManagementViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            vm.SolicitarCerrarModal += () => Dispatcher.Invoke(Close);
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            ConfigurarParaVentanaPadre(System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current?.MainWindow);
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfigurarParaVentanaPadre(Window? parentWindow)
        {
            if (parentWindow == null)
                return;

            Owner = parentWindow;
            ShowInTaskbar = false;
            WindowState = WindowState.Maximized;
        }
    }
}

