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
                "• Exportación de Seguimientos legible en pantalla, con columna Sede y anchos ajustados.\n" +
                "• El archivo exportado ahora se puede corregir en Excel y volver a importar directamente.\n" +
                "• Importación: filas \"Realizado en tiempo\" sin Fecha Realización usan su Fecha Registro; barra de progreso visible.\n" +
                "• Al exportar se pregunta si desea abrir el archivo generado.\n" +
                "• Plantilla de importación con el mismo formato del export y columna Sede en la tabla de seguimientos.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
