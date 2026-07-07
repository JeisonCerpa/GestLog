using System.Windows;
using GestLog;

namespace GestLog.Modules.Shell.Views
{    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : System.Windows.Controls.UserControl
    {
        private MainWindow? _mainWindow;

        public HomeView()
        {
            InitializeComponent();
            _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        }

        private void btnIrHerramientas_Click(object sender, RoutedEventArgs e)
        {
            var herramientasView = new HerramientasView();
            _mainWindow?.NavigateToView(herramientasView, "Herramientas");
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"GestLog {BuildVersion.VersionLabel}\n\n" +
                "• Corrección para sedes con red inestable: ya no aparecen diálogos de error repetidos por tareas en segundo plano.\n" +
                "• Los errores de red se registran en el log sin interrumpir al usuario; el estado de conexión se sigue viendo en el indicador de la barra superior.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
