using GestLog.Modules.GestionEquiposInformaticos.Interfaces;
using GestLog.Modules.GestionMantenimientos.Models;

namespace GestLog.Modules.GestionEquiposInformaticos.Services;

/// <summary>
/// Implementación WPF del servicio de diálogo para registrar mantenimiento de equipo.
/// </summary>
public class RegistroMantenimientoEquipoDialogService : IRegistroMantenimientoEquipoDialogService
{
    public bool TryShowRegistroDialog(SeguimientoMantenimientoDto seguimientoBase, out SeguimientoMantenimientoDto? resultado)
    {
        resultado = null;
        var dialog = new GestLog.Views.Tools.GestionEquipos.RegistroMantenimientoEquipoDialog();
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
