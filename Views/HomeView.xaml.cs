using System.Windows;
using System.Windows.Controls;
using GestLog;
using GestLog.Views.Tools;

namespace GestLog.Views
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
        }        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"GestLog {BuildVersion.VersionLabel}\n\n" +
                "• Base de datos reorganizada con prefijos de módulos.\n" +
                "• Migraciones aplicadas exitosamente a producción.\n" +
                "• Mejoras de mantenibilidad y escalabilidad.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
