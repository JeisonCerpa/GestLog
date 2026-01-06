using System.Windows;
using System.Windows.Controls;
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
                "GestLog v1.0.42\n\n" +
                "Sistema modular de gestión integrada\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Módulos integrados:\n" +
                "• DaaterProccesor - Procesamiento de datos Excel\n" +
                "• Gestión de Cartera - Estados de cuenta PDF\n" +
                "• Envío de Catálogo - Envío masivo de catálogo\n" +
                "• Gestión de Equipos Informáticos - Administración de periféricos\n\n" +
                "Cambios principales en v1.0.42:\n" +
                "• Corrección de duplicados: eliminados registros duplicados en Historial de Ejecuciones.\n" +
                "• Mayor estabilidad: serialización de cargas y deduplicación por EjecucionId.\n" +
                "• Refactorización crítica: desacoplamiento de EjecucionSemanal de PlanCronogramaEquipo.\n" +
                "• Historial preservado: eliminación de planes no afecta el registro de ejecuciones.\n" +
                "• Nuevas snapshots: preservación de descripción y responsable de planes históricos.\n" +
                "• Optimización de queries: uso de AsSplitQuery() para evitar productos cartesianos.\n" +
                "• Exportaciones mejoradas: corrección de saltos de línea en CSV.\n" +
                "• Mejoras de rendimiento y estabilidad.\n\n" +
                "Estado: ✅ Operativo\n" +
                "Actualizaciones: ✅ Sistema Velopack 100% funcional\n\n" +
                "Nota: Para obtener el changelog completo, consulte la documentación.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
