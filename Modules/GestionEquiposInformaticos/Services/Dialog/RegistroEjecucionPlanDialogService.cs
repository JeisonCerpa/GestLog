using System;
using System.Linq; // añadido para OfType
using System.Threading.Tasks;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Dialog;
using GestLog.Modules.GestionEquiposInformaticos.Interfaces.Data;
using GestLog.Modules.GestionEquiposInformaticos.ViewModels.Mantenimiento;
using GestLog.Services.Core.Logging;

namespace GestLog.Modules.GestionEquiposInformaticos.Services.Dialog
{
    public class RegistroEjecucionPlanDialogService : IRegistroEjecucionPlanDialogService
    {
        private readonly IPlanCronogramaService _planService;
        private readonly IGestLogLogger _logger;
        public RegistroEjecucionPlanDialogService(IPlanCronogramaService planService, IGestLogLogger logger)
        {
            _planService = planService;
            _logger = logger;
        }
        public async Task<bool> TryShowAsync(Guid planId, int anioISO, int semanaISO, string usuarioActual)
        {
            try
            {                var plan = await _planService.GetByIdAsync(planId).ConfigureAwait(true); // necesitamos continuar en hilo UI para dialog
                if (plan == null) return false;
                var vm = new RegistroEjecucionPlanViewModel(_planService, _logger);
                vm.Load(plan, anioISO, semanaISO, usuarioActual);
                var dlg = new GestLog.Modules.GestionEquiposInformaticos.Views.Mantenimiento.RegistroEjecucionPlanDialog(vm);
                var owner = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w=>w.IsActive) ?? System.Windows.Application.Current.MainWindow;
                if (owner != null)
                {
                    dlg.Owner = owner;
                    dlg.ConfigurarParaVentanaPadre(owner);
                }

                var ok = dlg.ShowDialog();
                return ok == true && dlg.Guardado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RegistroEjecucionPlanDialogService] Error mostrando diálogo ejecución plan");
                return false;
            }
        }
    }
}
