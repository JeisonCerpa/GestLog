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
        }        
        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "GestLog v1.0.38\n\n" +
                "Sistema modular de gestión integrada\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Módulos integrados:\n" +
                "• DaaterProccesor - Procesamiento de datos Excel\n" +
                "• Gestión de Cartera - Estados de cuenta PDF\n" +
                "• Envío de Catálogo - Envío masivo de catálogo\n\n" +
                "Cambios principales en v1.0.38:\n" +
                "• Gestión de Mantenimiento: múltiples arreglos y mejoras en la lógica de estados.\n" +
                "• Cálculo de semana: soporte para correcto cálculo de la última semana del año y del próximo año.\n" +
                "• Mejoras visuales: correcciones de estilo y compatibilidad DPI en diálogos y tablas.\n" +
                "• Corrección de bugs: registro de mantenimientos y paginación del DataGrid.\n" +
                "• Optimización: reducción de uso de memoria en cargas de tablas.\n" +
                "• Actualización de dependencias y mejoras de estabilidad.\n\n" +
                "Estado: ✅ Operativo\n" +
                "Actualizaciones: ✅ Sistema Velopack 100% funcional\n\n" +
                "Nota: Reemplace este resumen por el changelog detallado si es necesario.",
                "Información del Sistema",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
