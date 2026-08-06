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
                "• Gestión de Cartera: el BCC y el CC guardados en la configuración de correo son los que se usan al enviar. Antes, al cambiar el BCC, la pantalla mostraba el nuevo pero los correos seguían saliendo con copia oculta al anterior hasta reiniciar.\n" +
                "• Se corrige el error de acceso desde otro subproceso al guardar la configuración de correo, que la dejaba cargada a medias.\n" +
                "• Nuevo botón \"Borrar configuración\" en la ventana de configuración de correo: elimina servidor, puerto, usuario, BCC, CC y la contraseña guardada en Windows.\n" +
                "• Al cerrar la ventana de configuración, los datos se releen del archivo guardado en vez de copiarse campo por campo.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
