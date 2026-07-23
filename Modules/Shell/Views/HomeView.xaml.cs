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
                "• Cartera: \"Probar configuración\" ahora se conecta de verdad al servidor y valida la contraseña, sin enviar correos.\n" +
                "• Mensajes claros al enviar: contraseña incorrecta, BCC rechazado o puerto/SSL mal configurado se explican en español.\n" +
                "• Se avisa si el Excel de cartera o de clientes está abierto, y las empresas cuyo PDF no se pudo generar ahora se reportan (antes se omitían en silencio).\n" +
                "• La configuración de correo de Cartera se guarda en un solo lugar: al reabrir la ventana se repueblan todos los campos, incluida la contraseña.\n" +
                "• Gestión de Roles rediseñada (maestro-detalle) y se eliminó la pantalla redundante de Asignación de Permisos.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
