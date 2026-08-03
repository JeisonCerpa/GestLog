using System.Windows;
using System.Windows.Input;
using GestLog.Modules.Usuarios.Models;

namespace GestLog.Modules.Usuarios.Views.GestionIdentidadCatalogos.Auditoria
{
    /// <summary>
    /// Detalle completo de un evento de auditoría. Solo lectura: la auditoría no se edita.
    /// </summary>
    public partial class AuditoriaDetalleWindow : Window
    {
        private readonly GestLog.Modules.Usuarios.Models.Auditoria _evento;

        public AuditoriaDetalleWindow(GestLog.Modules.Usuarios.Models.Auditoria evento)
        {
            InitializeComponent();

            _evento = evento ?? throw new System.ArgumentNullException(nameof(evento));
            DataContext = _evento;

            KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        }

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

        private void Panel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void BtnCerrar_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnCopiar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(_evento.Detalle ?? string.Empty);
            }
            catch (System.Exception)
            {
                // El portapapeles puede estar tomado por otra aplicación; no vale interrumpir por esto
            }
        }
    }
}
