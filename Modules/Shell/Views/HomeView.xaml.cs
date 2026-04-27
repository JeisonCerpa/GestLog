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
                "• Apertura más rápida de la ventana principal.\n" +
                "• Tareas no críticas de arranque ejecutadas en segundo plano.\n" +
                "• Inicialización optimizada del login y del estado de base de datos.\n" +
                "• Arreglo al agregar un equipo en mantenimiento.\n" +
                "• Corrección de un error en Cronograma Diario.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
