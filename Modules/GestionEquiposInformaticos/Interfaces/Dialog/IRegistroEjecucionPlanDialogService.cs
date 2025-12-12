using System;
using System.Threading.Tasks;

namespace GestLog.Modules.GestionEquiposInformaticos.Interfaces.Dialog
{
    /// <summary>
    /// Servicio de presentación para registrar la ejecución semanal (EjecucionSemanal) de un plan.
    /// </summary>
    public interface IRegistroEjecucionPlanDialogService
    {
        /// <summary>
        /// Muestra el diálogo de ejecución del plan y registra la ejecución si se confirma.
        /// </summary>
        /// <param name="planId">Id del plan.</param>
        /// <param name="anioISO">Año ISO.</param>
        /// <param name="semanaISO">Semana ISO.</param>
        /// <param name="usuarioActual">Usuario que ejecuta.</param>
        /// <returns>true si se registró ejecución.</returns>
        Task<bool> TryShowAsync(Guid planId, int anioISO, int semanaISO, string usuarioActual);
    }
}
