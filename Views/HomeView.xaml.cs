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
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"GestLog {BuildVersion.VersionLabel}\n\n" +
                "• Exportación: formato actualizado (SST-F-83 v4).\n" +
                "• Normalización: campos clave y Responsable en MAYÚSCULAS.\n" +
                "• Límites: Descripción y Observaciones hasta 1000 caracteres.\n" +
                "• Trazabilidad: 'No Realizado' registrados e identificados en rojo.\n" +
                "• Usuarios: contraseña temporal auto-generada.\n" +
                "• UI: desplegables mejorados, diálogo Datos del Equipo rediseñado.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
