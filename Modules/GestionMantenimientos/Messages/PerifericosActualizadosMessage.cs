using CommunityToolkit.Mvvm.Messaging.Messages;

namespace GestLog.Modules.GestionMantenimientos.Messages
{
    /// <summary>
    /// Mensaje enviado cuando los periféricos de un equipo han sido actualizados en la base de datos.
    /// Lleva opcionalmente el código del equipo afectado para que los receptores puedan filtrar/recargar específicamente.
    /// </summary>
    public class PerifericosActualizadosMessage : ValueChangedMessage<string?>
    {
        public PerifericosActualizadosMessage(string? codigoEquipo = null) : base(codigoEquipo) { }
    }
}
