using GestLog.Modules.GestionVehiculos.Interfaces.Dialog;
using System.Linq;

namespace GestLog.Modules.GestionVehiculos.Services.Dialog
{
    /// <summary>
    /// Implementación WPF simple del servicio de diálogos (sustituye MessageBox en VMs).
    /// </summary>
    public class AppDialogService : IAppDialogService
    {
        public bool Confirm(string message, string title = "Confirmar")
        {
            var owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            var dlg = new GestLog.Modules.GestionVehiculos.Views.Dialog.ConfirmDialog();
            dlg.Owner = owner;
            dlg.Title = title;
            dlg.Message = message;
            var r = dlg.ShowDialog();
            return r == true;
        }

        public bool ConfirmWarning(string message, string title = "Atención")
        {
            var owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            var dlg = new GestLog.Modules.GestionVehiculos.Views.Dialog.ConfirmDialog();
            dlg.Owner = owner;
            dlg.Title = title;
            dlg.Message = message;
            var r = dlg.ShowDialog();
            return r == true;
        }

        public void ShowError(string message, string title = "Error")
        {
            var owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive) ?? System.Windows.Application.Current.MainWindow;
            System.Windows.MessageBox.Show(owner, message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
