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
                "• Los mantenimientos correctivos se registran en la fecha de realización que escribe la persona, no en la de hoy. Antes quedaban como \"Pendiente\" y desaparecían de la hoja de vida del equipo.\n" +
                "• La hoja de vida del equipo muestra la fecha de realización, y un correctivo ya no se pierde en el detalle de semana cuando el equipo también tiene un preventivo esa semana.\n" +
                "• \"Detalles de registro\" permite editar fecha de realización, responsable, costo y observaciones; al cambiar la fecha el mantenimiento se mueve a la semana correcta.\n" +
                "• Eliminar un mantenimiento borra solo ese registro, no el historial completo del equipo.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
