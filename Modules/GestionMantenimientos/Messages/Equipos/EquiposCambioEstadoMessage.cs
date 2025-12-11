namespace GestLog.Modules.GestionMantenimientos.Messages.Equipos
{
    /// <summary>
    /// Mensaje para notificar que ha cambiado el estado de uno o más equipos.
    /// Este mensaje es más específico que EquiposActualizadosMessage para evitar recargas innecesarias.
    /// </summary>
    public class EquiposCambioEstadoMessage 
    {
        public string? CodigoEquipo { get; }
        public bool RequiereRecargaCompleta { get; }

        public EquiposCambioEstadoMessage(string? codigoEquipo = null, bool requiereRecargaCompleta = false)
        {
            CodigoEquipo = codigoEquipo;
            RequiereRecargaCompleta = requiereRecargaCompleta;
        }
    }
}
