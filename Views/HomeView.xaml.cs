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
                "GestLog v1.0.41\n\n" +
                "Sistema modular de gestión integrada\n" +
                "Desarrollado con .NET 9 y WPF\n\n" +
                "Módulos integrados:\n" +
                "• DaaterProccesor - Procesamiento de datos Excel\n" +
                "• Gestión de Cartera - Estados de cuenta PDF\n" +
                "• Envío de Catálogo - Envío masivo de catálogo\n" +
                "• Gestión de Equipos Informáticos - Administración de periféricos\n\n" +
                "Cambios principales en v1.0.41:\n" +
                "• Historial de Ejecuciones: nuevo servicio de exportación a Excel con diseño profesional.\n" +
                "• Portada personalizada: con estadísticas clave (equipos, mantenimientos, ítems).\n" +
                "• Hojas por equipo: detalles semanales de actividades completadas.\n" +
                "• Combobox dinámico: años se cargan desde BD, no hardcodeados.\n" +
                "• Exportaciones mejoradas: filtros automáticos en todos los encabezados.\n" +
                "• Formateo avanzado: estados y sedes con textos legibles y colores.\n" +
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
