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
                "• Gestión de Mantenimientos: nuevas rutinas por horómetro para equipos que se atienden por horas de uso, como los compresores de tornillo AXP. Se indica cada cuántas horas toca servicio y el sistema calcula qué rutina del manual corresponde (INICIAL, A, B, C o D).\n" +
                "• Al registrar el mantenimiento aparece el campo \"Horómetro\" con la última lectura tomada; al escribir la actual, el sistema dice qué rutina toca y llena la descripción con sus tareas y repuestos.\n" +
                "• Si se guarda una lectura que ya cumple para una rutina sin registrarla, el programa avisa antes de continuar.\n" +
                "• El detalle del equipo muestra la última lectura y el próximo servicio con las horas que faltan; la lista de equipos resalta en rojo los que están vencidos.\n" +
                "• Las exportaciones y la hoja de vida incluyen el horómetro y la rutina dentro del texto del registro.\n" +
                "• Nuevo botón para abrir el documento del equipo (manual o ficha técnica) desde su detalle.\n" +
                "• Los equipos que no se atienden por horas quedan igual que antes.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
