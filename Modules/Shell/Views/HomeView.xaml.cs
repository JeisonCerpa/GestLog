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
                "• Gestión de Mantenimientos: si un equipo tenía un correctivo en una semana, el preventivo programado de esa misma semana nunca se creaba y la casilla quedaba como \"No realizado\", sin tipo y sin poder registrarse. Ahora ambos conviven.\n" +
                "• Las casillas que ya habían quedado así se recuperaron: el cronograma muestra de nuevo el total real de mantenimientos programados.\n" +
                "• En el detalle de semana, la etiqueta con el tipo de mantenimiento ya se lee en el tema oscuro; antes quedaba del mismo tono que el fondo.\n" +
                "• La fecha con la que se crea un mantenimiento programado corresponde al lunes correcto de su semana.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
