using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Messages.Mantenimientos
{
    /// <summary>
    /// Mensaje publicado cuando se registra una ejecución de mantenimiento
    /// </summary>
    public class EjecucionMantenimientoRegistradaMessage
    {
        public EjecucionMantenimientoRegistradaMessage(EjecucionMantenimientoDto ejecucion)
        {
            Ejecucion = ejecucion;
        }

        public EjecucionMantenimientoDto Ejecucion { get; }
    }
}
