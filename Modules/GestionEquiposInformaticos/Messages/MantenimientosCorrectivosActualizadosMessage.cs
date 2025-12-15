using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GestLog.Modules.GestionEquiposInformaticos.Messages
{
    /// <summary>
    /// Mensaje enviado cuando los mantenimientos correctivos han sido actualizados en la base de datos.
    /// Lleva opcionalmente el ID de la entidad (Equipo o Periférico) afectada para que los receptores puedan filtrar/recargar específicamente.
    /// </summary>
    public class MantenimientosCorrectivosActualizadosMessage : ValueChangedMessage<int?>
    {
        /// <summary>
        /// Crea un nuevo mensaje de mantenimientos correctivos actualizados
        /// </summary>
        /// <param name="entidadId">ID de la entidad (Equipo o Periférico) afectada, null para actualización general</param>
        public MantenimientosCorrectivosActualizadosMessage(int? entidadId = null) : base(entidadId) { }
    }
}
