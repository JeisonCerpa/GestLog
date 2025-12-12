using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Dialog;
using GestLog.Modules.GestionMantenimientos.Models.DTOs;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Dialog
{
    /// <summary>
    /// Implementación WPF del servicio de diálogo para registrar mantenimiento de equipo.
    /// </summary>
    public class RegistroMantenimientoEquipoDialogService : IRegistroMantenimientoEquipoDialogService
    {
        public bool TryShowRegistroDialog(SeguimientoMantenimientoDto seguimientoBase, out SeguimientoMantenimientoDto? resultado)
        {
            resultado = null;
            var dialog = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.RegistroMantenimientoEquipoDialog();
            dialog.CargarDesde(seguimientoBase);
            var parentWindow = System.Windows.Application.Current.Windows
                .OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            if (parentWindow != null)
                dialog.Owner = parentWindow;
            var ok = dialog.ShowDialog();
            if (ok == true)
            {
                resultado = dialog.Resultado;
                return resultado != null;
            }
            return false;
        }
    }
}
