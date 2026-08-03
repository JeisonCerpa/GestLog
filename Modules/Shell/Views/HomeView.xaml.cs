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
                "• Al cambiar la fecha de realización de un mantenimiento ya guardado, el registro se mueve a la semana que le corresponde en el cronograma. Antes la fecha se guardaba pero el mantenimiento se quedaba en la semana anterior.\n" +
                "• Se corrige el error \"No se encontró el seguimiento a actualizar\" al guardar desde \"Detalles de registro\" abierto desde el detalle de semana.\n" +
                "• Agregar un seguimiento desde la vista de Seguimientos vuelve a funcionar: antes fallaba siempre.\n" +
                "• Los mensajes de validación se muestran tal como son, en lugar de un \"Error al...\" genérico.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
