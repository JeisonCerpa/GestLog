using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Messages.Mantenimientos
{
    /// <summary>
    /// Mensaje publicado cuando se crea un plan de mantenimiento
    /// </summary>
    public class PlanMantenimientoCreatedMessage
    {
        public PlanMantenimientoCreatedMessage(PlanMantenimientoVehiculoDto plan)
        {
            Plan = plan;
        }

        public PlanMantenimientoVehiculoDto Plan { get; }
    }
}
