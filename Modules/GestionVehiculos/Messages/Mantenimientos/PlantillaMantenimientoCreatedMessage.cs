using GestLog.Modules.GestionVehiculos.Models.DTOs;

namespace GestLog.Modules.GestionVehiculos.Messages.Mantenimientos
{
    /// <summary>
    /// Mensaje publicado cuando se crea una plantilla de mantenimiento
    /// </summary>
    public class PlantillaMantenimientoCreatedMessage
    {
        public PlantillaMantenimientoCreatedMessage(PlantillaMantenimientoDto plantilla)
        {
            Plantilla = plantilla;
        }

        public PlantillaMantenimientoDto Plantilla { get; }
    }
}
