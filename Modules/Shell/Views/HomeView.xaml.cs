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
                "• Auditoría automática en todos los módulos: cada creación, modificación y eliminación queda registrada con quién la hizo, cuándo y qué campos cambiaron.\n" +
                "• Nueva pantalla \"Auditoría\" en Identidad y Catálogos: filtros por tipo de registro, usuario y fechas, con ventana de detalle para ver y copiar el evento completo.\n" +
                "• Los eventos identifican el equipo por su código y nombre, y usan los nombres de campo del negocio (\"Sistema operativo\", \"Comprado a\").\n" +
                "• Solo se registran los campos que cambiaron realmente; las contraseñas nunca se escriben en el historial.\n" +
                "• Los cronogramas generados automáticamente no ensucian el historial y las importaciones masivas se resumen en una sola entrada.\n" +
                "• Cronograma: registrar mantenimientos ya no se limita a la semana actual y la anterior; se puede registrar cualquier semana pasada y queda como \"Realizado en tiempo\".\n" +
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
